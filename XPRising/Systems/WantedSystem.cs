using System.Collections.Concurrent;
using ProjectM;
using ProjectM.Network;
using System.Text;
using BepInEx.Logging;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XPRising.Models;
using XPRising.Transport;
using XPRising.Utils;
using XPShared;
using Faction = XPRising.Utils.Prefabs.Faction;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Systems
{
    public static class WantedSystem {
        private static EntityManager entityManager = Plugin.Server.EntityManager;

        public static int heat_cooldown = 10;
        public static int ambush_interval = 60;
        public static int ambush_chance = 50;
        public static float ambush_despawn_timer = 300;
        public static int vBloodMultiplier = 20;
        public static float RequiredDistanceFromVBlood = 100;

        private static System.Random rand = new();
        public static int HeatPercentageLostOnDeath = 100;

        private static readonly ConcurrentQueue<(DateTime, Entity)> SpawnedQueue = new();
        private static readonly ConcurrentDictionary<Entity, int> AmbushingEntities = new();
        private static readonly FrameTimer SpawnEntitiesCleaner = new FrameTimer().Initialise(
            CleanAmbushingEntities,
            TimeSpan.FromSeconds(10),
            false);

        public static void AddAmbushingEntity(Entity entity, DateTime time)
        {
            SpawnedQueue.Enqueue((time, entity));
            // We don't care about what the value is, we just want to have a dictionary for the lookup speed.
            AmbushingEntities.TryAdd(entity, 0);
            
            // Start the timer if it is not running
            if (!SpawnEntitiesCleaner.Enabled) SpawnEntitiesCleaner.Start();
        }

        private static void CleanAmbushingEntities()
        {
            var spawnCutOffTime = DateTime.Now - TimeSpan.FromSeconds(ambush_despawn_timer * 1.1);
            while (!SpawnedQueue.IsEmpty && SpawnedQueue.TryPeek(out var spawned) && spawned.Item1 < spawnCutOffTime)
            {
                // If this entity was spawned more than the ambush_despawn_timer ago, then it should have been destroyed by the game.
                // Remove it from the known queue and ambushing entities so we don't take up too much memory
                if (SpawnedQueue.TryDequeue(out spawned))
                {
                    Plugin.Log(LogSystem.Wanted, LogLevel.Warning, () => $"Removed entity from ambushing list");
                    AmbushingEntities.TryRemove(spawned.Item2, out _);
                }
            }

            // Stop the timer if the queue is empty
            if (SpawnedQueue.IsEmpty) SpawnEntitiesCleaner.Stop();
        }

        public static void PlayerKillEntity(List<Alliance.ClosePlayer> closeAllies, Entity victimEntity, bool isVBlood)
        {
            var unit = Helper.ConvertGuidToUnit(Helper.GetPrefabGUID(victimEntity));
            Faction faction = Faction.Unknown;
            if (AmbushingEntities.TryRemove(victimEntity, out _))
            {
                faction = Faction.VampireHunters;
                Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"Unit found in ambush map");
            }
            else
            {
                if (!entityManager.TryGetComponentData<FactionReference>(victimEntity, out var victimFactionReference))
                {
                    Plugin.Log(LogSystem.Faction, LogLevel.Warning, () => $"Player killed: Entity: {unit}, but it has no faction");
                    return;
                }
            
                var victimFaction = victimFactionReference.FactionGuid._Value;
                faction = Helper.ConvertGuidToFaction(victimFaction);
            }

            FactionHeat.GetActiveFactionHeatValue(faction, unit, isVBlood, out var heatValue, out var activeFaction);
            Plugin.Log(
                LogSystem.Faction,
                LogLevel.Warning,
                () => $"Player killed: [{Helper.ConvertGuidToUnit(Helper.GetPrefabGUID(victimEntity))}, {Enum.GetName(faction)}]",
                faction == Faction.Unknown);

            if (activeFaction == Faction.Unknown || heatValue == 0) return;

            foreach (var ally in closeAllies) {
                HandlePlayerKill(ally.userEntity, activeFaction, heatValue);
            }
        }

        private static void HandlePlayerKill(Entity userEntity, Faction victimFaction, int heatValue) {
            HeatManager(userEntity, out var heatData, out var steamID);

            // If the faction is vampire hunters, reduce the heat level of all other active factions
            if (victimFaction == Faction.VampireHunters) {
                foreach (var (faction, heat) in heatData.heat) {
                    UpdatePlayerHeat(userEntity, faction, heat.level - heatValue, heat.lastAmbushed);
                }
            }
            else {
                if (!FactionHeat.ActiveFactions.Contains(victimFaction)) {
                    Plugin.Log(LogSystem.Wanted, LogLevel.Warning, $"Attempted to load non-active faction heat data: {Enum.GetName(victimFaction)}");
                    return;
                }

                // reset the last ambushed time now they have a higher wanted level so that they can be ambushed again
                var newLastAmbushed = DateTime.Now - TimeSpan.FromSeconds(ambush_interval);
                UpdatePlayerHeat(userEntity, victimFaction, heatData.heat[victimFaction].level + heatValue, newLastAmbushed);
            }

            // Update the heatCache with the new data
            Database.PlayerHeat[steamID] = heatData;

            LogHeatData(steamID, heatData, userEntity, "kill");
        }

        public static void PlayerDied(Entity victimEntity) {
            var player = entityManager.GetComponentData<PlayerCharacter>(victimEntity);
            var userEntity = player.UserEntity;
            var user = entityManager.GetComponentData<User>(userEntity);
            var steamID = user.PlatformId;
            var preferences = Database.PlayerPreferences[steamID];

            // Reset player heat based on config settings
            var heatData = Database.PlayerHeat[steamID];
            
            foreach (var (faction, heat) in heatData.heat)
            {
                var heatLost = (int)Math.Ceiling(heat.level * HeatPercentageLostOnDeath / 100f);
                var newHeatLevel = Math.Max(heat.level - heatLost, 0); // No negative heat!
                if (newHeatLevel == 0)
                {
                    heatData.heat.Remove(faction);
                }
                else
                {
                    heatData.heat[faction] = new PlayerHeatData.Heat
                    {
                        lastAmbushed = DateTime.Now,
                        level = newHeatLevel
                    };
                }
                ClientActionHandler.SendWantedData(user, faction, newHeatLevel, preferences.Language);
            }
            
            Database.PlayerHeat[steamID] = heatData;
            LogHeatData(steamID, heatData, userEntity, "died");
        }

        private struct AllyHeat {
            public Alliance.ClosePlayer player;
            public PlayerHeatData heat;

            public AllyHeat(Alliance.ClosePlayer player, PlayerHeatData heat) {
                this.player = player;
                this.heat = heat;
            }
        }

        // This is expected to only be called at the start of combat
        public static void CheckForAmbush(Entity triggeringPlayerEntity) {
            var useGroup = ExperienceSystem.GroupMaxDistance > 0;
            var triggerLocation = Plugin.Server.EntityManager.GetComponentData<LocalToWorld>(triggeringPlayerEntity);
            var closeAllies = Alliance.GetClosePlayers(
                triggerLocation.Position, triggeringPlayerEntity, ExperienceSystem.GroupMaxDistance, true, useGroup, LogSystem.Wanted);
            var alliesInCombat = false;
            // Check if there are close allies in combat (we don't want ALL close allies to trigger an ambush at the same time!)
            foreach (var ally in closeAllies) {
                if (ally.isTrigger) continue;
                var inCombat = Cache.GetCombatStart(ally.steamID) > Cache.GetCombatEnd(ally.steamID);
                alliesInCombat = inCombat || alliesInCombat;
            }

            // Leave processing
            if (alliesInCombat) return;

            // Leave processing if we cannot spawn where we are.
            if (!CanSpawn(triggerLocation.Position)) return;

            // Check for ambush-able factions
            // Note: We could do this in the loop above, but it is likely quicker to iterate over them separately if
            // alliesInCombat is true.
            var heatList = new List<AllyHeat>();
            var ambushFactions = new Dictionary<Faction, int>();
            foreach (var ally in closeAllies) {
                HeatManager(ally.userEntity, out var heatData, out var steamID);
                heatList.Add(new AllyHeat(ally, heatData));

                foreach (var faction in FactionHeat.ActiveFactions) {
                    if (heatData.heat.TryGetValue(faction, out var heat))
                    {
                        TimeSpan timeSinceAmbush = DateTime.Now - heat.lastAmbushed;
                        var wantedLevel = FactionHeat.GetWantedLevel(heat.level);

                        if (timeSinceAmbush.TotalSeconds > ambush_interval && wantedLevel > 0) {
                            Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"{faction} can ambush");

                            // If there is no stored wanted level yet, or if this ally's wanted level is higher, then set it.
                            if (!ambushFactions.TryGetValue(faction, out var highestWantedLevel) || wantedLevel > highestWantedLevel) {
                                ambushFactions[faction] = wantedLevel;
                            }
                        }
                    }
                }
            }
            
            // Check for ambush
            // (sort for wanted level and only have 1 faction ambush)
            var sortedFactionList = ambushFactions.ToList();
            // Sort DESC so that we prioritise the highest wanted level
            sortedFactionList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            bool isAmbushing = false;
            var ambushingFaction = Faction.Unknown;
            var ambushingTime = DateTime.Now;
            foreach (var faction in sortedFactionList) {
                if (rand.Next(0, 100) <= ambush_chance) {
                    FactionHeat.Ambush(triggerLocation.Position, closeAllies, faction.Key, faction.Value);
                    isAmbushing = true;
                    ambushingFaction = faction.Key;
                    // Only need 1 ambush at a time!
                    break;
                }
            }

            // If we are ambushing, update all allies heat data.
            if (isAmbushing) {
                foreach (var allyHeat in heatList) {
                    var heatData = allyHeat.heat;

                    var factionHeat = heatData.heat[ambushingFaction];
                    factionHeat.lastAmbushed = ambushingTime;
                    heatData.heat[ambushingFaction] = factionHeat;

                    Database.PlayerHeat[allyHeat.player.steamID] = heatData;
            
                    LogHeatData(allyHeat.player.steamID, heatData, allyHeat.player.userEntity, "check");
                }
            }
        }

        public static PlayerHeatData GetPlayerHeat(Entity userEntity) {
            // Ensure that the user has the up-to-date heat data and return the value
            HeatManager(userEntity, out var heatData, out var steamID);
            LogHeatData(steamID, heatData, userEntity, "get");
            return heatData;
        }

        public static PlayerHeatData SetPlayerHeat(Entity userEntity, Faction heatFaction, int value, DateTime lastAmbushed) {
            HeatManager(userEntity, out var heatData, out var steamID);

            heatData = UpdatePlayerHeat(userEntity, heatFaction, value, lastAmbushed);
            LogHeatData(steamID, heatData, userEntity, "set");
            
            return heatData;
        }
        
        private static PlayerHeatData UpdatePlayerHeat(Entity userEntity, Faction heatFaction, int value, DateTime lastAmbushed) {
            HeatManager(userEntity, out var heatData, out var steamID);

            // Update faction heat
            var heat = heatData.heat[heatFaction];
            
            var oldWantedLevel = FactionHeat.GetWantedLevel(heat.level);
            var newWantedLevel = FactionHeat.GetWantedLevel(value);
            
            heat.level = Math.Max(0, value);
            heat.lastAmbushed = lastAmbushed;
            heatData.heat[heatFaction] = heat;

            Database.PlayerHeat[steamID] = heatData;
            if (newWantedLevel != oldWantedLevel)
            {
                var message = newWantedLevel < oldWantedLevel
                    ? L10N.Get(L10N.TemplateKey.WantedHeatDecrease)
                    : L10N.Get(L10N.TemplateKey.WantedHeatIncrease);
                message.AddField("{factionStatus}", FactionHeat.GetFactionStatus(heatFaction, heat.level));
                var colourIndex = Math.Clamp(newWantedLevel - 1, 0, FactionHeat.ColourGradient.Length - 1);
                Output.SendMessage(userEntity, message, $"#{FactionHeat.ColourGradient[colourIndex]}");
            }
            // Make sure the cooldown timer has started
            heatData.StartCooldownTimer(steamID);
            return heatData;
        }

        public static bool CanSpawn(float3 position)
        {
            var em = Plugin.Server.EntityManager;
            var query = em.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<VBloodUnit>(),
                },
                Options = EntityQueryOptions.IncludeDisabled | EntityQueryOptions.IncludeDestroyTag
            });

            var farEnoughFromBoss = true;
            var vbloodUnits = query.ToEntityArray(Allocator.Temp);
            foreach (var vblood in vbloodUnits)
            {
                var prefab = DebugTool.GetPrefabName(vblood);
                if (em.TryGetComponentData<LocalTransform>(vblood, out var localTransform))
                {
                    var distance = math.distance(position.xz, localTransform.Position.xz);
                    if (distance <= RequiredDistanceFromVBlood)
                    {
                        Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"{prefab}: distance to boss: {distance}m");
                        farEnoughFromBoss = false;
                    }
                }
            }

            return farEnoughFromBoss;
        }

        private static void HeatManager(Entity userEntity, out PlayerHeatData heatData, out ulong steamID) {
            steamID = entityManager.GetComponentData<User>(userEntity).PlatformId;

            if (!Database.PlayerHeat.TryGetValue(steamID, out heatData)) {
                heatData = new PlayerHeatData();
            }
        }

        public static bool CanCooldownHeat(DateTime lastCombatStart, DateTime lastCombatEnd) {
            // If we have started combat more recently than we have finished, then we are in combat.
            // There are some edge cases (such as player disconnecting during this period) that can mean the combat end
            // was never set correctly. As combat start should be logged about once every 10s, if we are well past this point
            // without a new combat start, just consider it ended.
            var inCombat = lastCombatStart >= lastCombatEnd && lastCombatStart + TimeSpan.FromSeconds(15) > DateTime.Now;
            Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"Heat CD period: combat: {inCombat}");

            return !inCombat && (lastCombatEnd + TimeSpan.FromSeconds(20)) < DateTime.Now;
        }

        private static string HeatDataString(PlayerHeatData heatData, bool useColor) {
            var activeHeat =
                heatData.heat.Where(faction => faction.Value.level > 0)
                    .Select(faction =>
                        useColor ? $"{Enum.GetName(faction.Key)}: <color={Output.White}>{faction.Value.level.ToString()}</color>" :
                            $"{Enum.GetName(faction.Key)}: {faction.Value.level.ToString()}"
                        );
            var sb = new StringBuilder();
            sb.AppendJoin(" | ", activeHeat);
            return sb.ToString();
        }

        private static void LogHeatData(ulong steamID, PlayerHeatData heatData, Entity userEntity, string origin)
        {
            var preferences = Database.PlayerPreferences[steamID];
            if (preferences.LoggingWanted)
            {
                var heatDataString = HeatDataString(heatData, true);
                Output.SendMessage(userEntity,
                    heatDataString == ""
                        ? L10N.Get(L10N.TemplateKey.WantedHeatDataEmpty)
                        : new L10N.LocalisableString(heatDataString));
            }
            Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"Heat({origin}): {HeatDataString(heatData, false)}");
            
            if (Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user))
            {
                foreach (var (faction, heat) in heatData.heat)
                {
                    ClientActionHandler.SendWantedData(user, faction, heat.level, preferences.Language);
                }
            }
        }
    }
}
