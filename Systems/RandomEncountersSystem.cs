using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using System.Collections.Concurrent;
using BepInEx.Logging;
using Bloodstone.API;
using XPRising.Utils.Prefabs;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Transforms;
using XPRising.Configuration;
using XPRising.Models;
using XPRising.Models.RandomEncounters;
using XPRising.Utils;
using XPRising.Utils.RandomEncounters;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Systems
{
    internal static class RandomEncountersSystem
    {
        private static readonly ConcurrentDictionary<ulong, ConcurrentDictionary<int, ItemDataModel>> RewardsMap = new();

        private static readonly ConcurrentDictionary<int, ulong> NpcPlayerMap = new();

        private static readonly Entity StationEntity = new();
        private static float Lifetime => RandomEncountersConfig.EncounterLength.Value;
        private static string MessageTemplate => RandomEncountersConfig.EncounterMessageTemplate.Value;

        internal static Dictionary<long, (float actualDuration, Action<Entity> Actions)> PostActions = new();

        private static System.Random Random = new System.Random();
        private const Plugin.LogSystem LoggingSystem = Plugin.LogSystem.RandomEncounter;

        internal static void StartEncounter()
        {
            var validUsers = Cache.NamePlayerCache.Values
                .Where(data =>
                {
                    return data.IsOnline &&
                           !Helper.IsInCastle(data.UserEntity) &&
                           !Cache.PlayerInCombat(data.SteamID);
                });

            if (validUsers.Any())
            {
                var randomPlayer = validUsers.MinBy(_ => Random.Next());
                StartEncounter(randomPlayer);
            }
            
            Plugin.Log(LoggingSystem, LogLevel.Message, "Could not find any eligible players for a random encounter...");
        }
        
        internal static void StartEncounter(PlayerData user)
        {
            var world = Plugin.Server;

            var userLevel = ExperienceSystem.GetLevel(user.SteamID);
            var npc = DataFactory.GetRandomNpc(userLevel);
            var npcPrefab = new PrefabGUID((int)npc.type);
            Plugin.Log(LoggingSystem, LogLevel.Message, $"Attempting to start a new encounter for {user.CharacterName} with {DebugTool.GetPrefabName(npcPrefab)}");
            var minSpawnDistance = RandomEncountersConfig.MinSpawnDistance.Value;
            var maxSpawnDistance = RandomEncountersConfig.MaxSpawnDistance.Value;
            try
            {
                NpcPlayerMap[(int)npc.type] = user.SteamID;
                var localToWorld = world.EntityManager.GetComponentData<LocalToWorld>(user.UserEntity);
                var spawnPosition = localToWorld.Position;

                world.GetExistingSystemManaged<UnitSpawnerUpdateSystem>()
                    .SpawnUnit(StationEntity, npcPrefab, spawnPosition, 1, minSpawnDistance, maxSpawnDistance, Lifetime);

            }
            catch (Exception ex)
            {
                Plugin.Log(LoggingSystem, LogLevel.Error, $"RE Failed spawning unit {ex}");
                // Suppress
            }
        }

        internal static void ServerEvents_OnUnitSpawned(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<PrefabGUID>(entity))
            {
                return;
            }

            var prefabGuid = entityManager.GetComponentData<PrefabGUID>(entity);
            if (!NpcPlayerMap.TryGetValue(prefabGuid.GuidHash, out var steamID))
            {
                return;
            }
            if (!entityManager.HasComponent<LifeTime>(entity))
            {
                return;
            }
            var lifeTime = entityManager.GetComponentData<LifeTime>(entity);
            if (Math.Abs(lifeTime.Duration - Lifetime) > 0.001)
            {
                return;
            }

            NpcPlayerMap.TryRemove(prefabGuid.GuidHash, out _);

            if (!RewardsMap.ContainsKey(steamID))
            {
                RewardsMap[steamID] = new ConcurrentDictionary<int, ItemDataModel>();
            }

            var npcName = DebugTool.GetPrefabName(prefabGuid);

            var message = string.Format(MessageTemplate, npcName, Lifetime);

            var user = Cache.SteamPlayerCache[steamID];

            Output.SendLore(user.UserEntity, message);
            Plugin.Log(LoggingSystem, LogLevel.Info, $"Encounters started: {user.CharacterName} vs. {npcName}");

            if (RandomEncountersConfig.NotifyAdminsAboutEncountersAndRewards.Value)
            {
                var onlineAdmins = DataFactory.GetOnlineAdmins();
                foreach (var onlineAdmin in onlineAdmins)
                {
                    Output.SendLore(onlineAdmin.UserEntity, $"Encounter started: {user.CharacterName} vs. {npcName}");
                }
            }
            RewardsMap[steamID][entity.Index] = DataFactory.GetRandomItem();
        }

        internal static void ServerEvents_OnDeath(DeathEvent deathEvent, User userModel)
        {
            if (RewardsMap.TryGetValue(userModel.PlatformId, out var bounties) &&
                bounties.TryGetValue(deathEvent.Died.Index, out var itemModel))
            {
                var itemGuid = new PrefabGUID(itemModel.Id);
                var quantity = RandomEncountersConfig.Items[itemModel.Id];
                if (!Helper.TryGiveItem(deathEvent.Killer, new PrefabGUID(itemModel.Id), quantity.Value, out _))
                {
                    Helper.DropItemNearby(deathEvent.Killer, itemGuid, quantity.Value);
                }
                var message = string.Format(RandomEncountersConfig.RewardMessageTemplate.Value, itemModel.Color, itemModel.Name);
                userModel.SendSystemMessage(message);
                bounties.TryRemove(deathEvent.Died.Index, out _);
                Plugin.Log(LoggingSystem, LogLevel.Info, $"{userModel.CharacterName} earned reward: {itemModel.Name}");
                var globalMessage = string.Format(RandomEncountersConfig.RewardAnnouncementMessageTemplate.Value,
                    userModel.CharacterName, itemModel.Color, itemModel.Name);
                if (RandomEncountersConfig.NotifyAllPlayersAboutRewards.Value)
                {
                    var onlineUsers = Cache.NamePlayerCache.Values
                        .Where(data => data.IsOnline && data.SteamID != userModel.PlatformId);
                    foreach (var player in onlineUsers)
                    {
                        Output.SendLore(player.UserEntity, globalMessage);
                    }

                }
                else if (RandomEncountersConfig.NotifyAdminsAboutEncountersAndRewards.Value)
                {
                    var onlineAdmins = Cache.NamePlayerCache.Values
                        .Where(data => data.IsOnline && data.IsAdmin && data.SteamID != userModel.PlatformId);
                    foreach (var onlineAdmin in onlineAdmins)
                    {
                        Output.SendLore(onlineAdmin.UserEntity, $"{userModel.CharacterName} earned an encounter reward: <color={itemModel.Color}>{itemModel.Name}</color>");
                    }
                }
            }
        }
    }
}
