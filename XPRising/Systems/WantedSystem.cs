using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XPRising.Models;
using XPRising.Utils;
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

        public static void PlayerKillEntity(List<Alliance.ClosePlayer> closeAllies, Entity victimEntity, bool isVBlood)
        {
            var unit = Helper.ConvertGuidToUnit(Helper.GetPrefabGUID(victimEntity));
            if (!entityManager.TryGetComponentData<FactionReference>(victimEntity, out var victim))
            {
                Plugin.Log(LogSystem.Faction, LogLevel.Warning, () => $"Player killed: Entity: {unit}, but it has no faction");
                return;
            }
            
            var victimFaction = victim.FactionGuid._Value;
            var faction = Helper.ConvertGuidToFaction(victimFaction);

            FactionHeat.GetActiveFactionHeatValue(faction, unit, isVBlood, out var heatValue, out var activeFaction);
            Plugin.Log(
                LogSystem.Faction,
                LogLevel.Warning,
                () => $"Player killed: [{Helper.ConvertGuidToUnit(Helper.GetPrefabGUID(victimEntity))}, {Enum.GetName(faction)} ({victimFaction.GuidHash})]",
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
                foreach (var (key, value) in heatData.heat) {
                    var heat = value;
                    var oldHeatLevel = FactionHeat.GetWantedLevel(heat.level);
                    heat.level = Math.Max(0, heat.level - heatValue);
                    var newHeatLevel = FactionHeat.GetWantedLevel(heat.level);
                    heatData.heat[key] = heat;

                    if (newHeatLevel < oldHeatLevel) {
                        // User has decreased in wanted level
                        var message =
                            L10N.Get(L10N.TemplateKey.WantedHeatDecrease)
                                .AddField("{factionStatus}", FactionHeat.GetFactionStatus(key, heat.level));
                        Output.SendMessage(userEntity, message);
                    }
                }
            }
            else {
                if (!heatData.heat.TryGetValue(victimFaction, out var heat)) {
                    Plugin.Log(LogSystem.Wanted, LogLevel.Warning, $"Attempted to load non-active faction heat data: {Enum.GetName(victimFaction)}");
                    return;
                }

                // Update the heat value for this faction
                var randHeatValue = rand.Next(1, heatValue);
                var oldHeatLevel = FactionHeat.GetWantedLevel(heat.level);
                heat.level += randHeatValue;
                var newHeatLevel = FactionHeat.GetWantedLevel(heat.level);

                if (newHeatLevel > oldHeatLevel) {
                    // User has increased in wanted level, so send them an ominous message
                    var message =
                        L10N.Get(L10N.TemplateKey.WantedHeatIncrease)
                            .AddField("{factionStatus}", FactionHeat.GetFactionStatus(victimFaction, heat.level));
                    Output.SendMessage(userEntity, message);
                    // and reset their last ambushed time so that they can be ambushed again
                    heat.lastAmbushed = DateTime.Now - TimeSpan.FromSeconds(ambush_interval);
                }
                
                heatData.heat[victimFaction] = heat;
            }

            // Update the heatCache with the new data
            Cache.heatCache[steamID] = heatData;

            LogHeatData(steamID, heatData, userEntity, "kill");
        }

        public static void PlayerDied(Entity victimEntity) {
            var player = entityManager.GetComponentData<PlayerCharacter>(victimEntity);
            var userEntity = player.UserEntity;
            var user = entityManager.GetComponentData<User>(userEntity);
            var steamID = user.PlatformId;

            // Reset player heat to 0
            var heatData = new PlayerHeatData();
            Cache.heatCache[steamID] = heatData;
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
                    var heat = heatData.heat[faction];
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

                    Cache.heatCache[allyHeat.player.steamID] = heatData;
            
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

            // Update faction heat
            var heat = heatData.heat[heatFaction];
            heat.level = value;
            heat.lastAmbushed = lastAmbushed;
            heatData.heat[heatFaction] = heat;

            Cache.heatCache[steamID] = heatData;
            LogHeatData(steamID, heatData, userEntity, "set");
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
            var cooldownPerSecond = (double)heat_cooldown / 60;

            if (!Cache.heatCache.TryGetValue(steamID, out heatData)) {
                heatData = new PlayerHeatData();
            }
            
            // If there has been less than 5 seconds since the last heat manager update, just skip calculations
            if ((DateTime.Now - heatData.lastCooldown).TotalSeconds < 5) return;

            var lastCombatStart = Cache.GetCombatStart(steamID);
            var lastCombatEnd = Cache.GetCombatEnd(steamID);

            var elapsedTime = CooldownPeriod(heatData.lastCooldown, lastCombatStart, lastCombatEnd);
            Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"Heat CD period: {elapsedTime:F1}s (L:{heatData.lastCooldown:u}|S:{lastCombatStart:u}|E:{lastCombatEnd:u})");

            if (elapsedTime > 0) {
                var cooldownValue = (int)Math.Floor(elapsedTime * cooldownPerSecond);
                Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"Heat cooldown: {cooldownValue} ({cooldownPerSecond:F1}c/s)");

                // Update all heat levels
                foreach (var faction in FactionHeat.ActiveFactions) {
                    var heat = heatData.heat[faction];
                    var newHeatLevel = Math.Max(heat.level - cooldownValue, 0);
                    heat.level = newHeatLevel;
                    heatData.heat[faction] = heat;
                }
            }

            heatData.lastCooldown = DateTime.Now;
                
            // Store updated heatData
            Cache.heatCache[steamID] = heatData;
        }

        private static double CooldownPeriod(DateTime lastCooldown, DateTime lastCombatStart, DateTime lastCombatEnd) {
            // If we have started combat more recently than we have finished, then we are in combat.
            var inCombat = lastCombatStart >= lastCombatEnd;
            Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"Heat CD period: combat: {inCombat}");
            
            // cdPeriodStart is the max of (lastCooldown, lastCombatEnd + offset)
            var cdPeriodStartAfterCombat = lastCombatEnd + TimeSpan.FromSeconds(20);
            var cdPeriodStart = lastCooldown > cdPeriodStartAfterCombat ? lastCooldown : cdPeriodStartAfterCombat;

            // If we are in combat, cdPeriodEnd is the start of combat;
            var cdPeriodEnd = inCombat ? lastCombatStart : DateTime.Now;
            
            return (cdPeriodEnd - cdPeriodStart).TotalSeconds;
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

        private static void LogHeatData(ulong steamID, PlayerHeatData heatData, Entity userEntity, string origin) {
            if (Database.PlayerLogConfig[steamID].LoggingWanted)
            {
                var heatDataString = HeatDataString(heatData, true);
                Output.SendMessage(userEntity,
                    heatDataString == ""
                        ? L10N.Get(L10N.TemplateKey.WantedHeatDataEmpty)
                        : new L10N.LocalisableString(heatDataString));
            }
            Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"Heat({origin}): {HeatDataString(heatData, false)}");
        }
    }
}
