using System;
using BepInEx.Logging;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using XPRising.Models;

namespace XPRising.Utils;

public static class PlayerCache
{
    private static Entity empty_entity = new Entity();

    public static void CreatePlayerCache() {

        Cache.NamePlayerCache.Clear();
        Cache.SteamPlayerCache.Clear();
        EntityQuery query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc() {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<User>()
            },
            Options = EntityQueryOptions.IncludeDisabled
        });
        var userEntities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in userEntities) {
            var userData = Plugin.Server.EntityManager.GetComponentData<User>(entity);
            var playerData = new PlayerData(
                userData.CharacterName,
                userData.PlatformId,
                userData.IsConnected,
                userData.IsAdmin,
                entity,
                userData.LocalCharacter._Entity);

            Cache.NamePlayerCache.TryAdd(Helper.GetTrueName(userData.CharacterName.ToString().ToLower()), playerData);
            Cache.SteamPlayerCache.TryAdd(userData.PlatformId, playerData);

        }

        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, "Player Cache Created.");
    }
    
    public static void PlayerOnline(Entity userEntity, User userData)
    {
        PlayerData playerData = new PlayerData(
            userData.CharacterName,
            userData.PlatformId,
            userData.IsConnected,
            userData.IsAdmin,
            userEntity,
            userData.LocalCharacter._Entity);

        Cache.NamePlayerCache[Helper.GetTrueName(userData.CharacterName.ToString().ToLower())] = playerData;
        Cache.SteamPlayerCache[userData.PlatformId] = playerData;
    }
    
    public static void PlayerOffline(ulong steamID)
    {
        var playerData = Cache.SteamPlayerCache[steamID];
        playerData.IsOnline = false;

        Cache.NamePlayerCache[Helper.GetTrueName(playerData.CharacterName.ToString().ToLower())] = playerData;
        Cache.SteamPlayerCache[steamID] = playerData;
        Cache.playerCombatEnd[steamID] = DateTime.Now;
        
        Database.PlayerLogout[steamID] = DateTime.Now;
        Alliance.RemoveUserOnLogout(playerData.CharEntity, playerData.CharacterName.ToString());
    }

    public static string GetNameFromSteamID(ulong steamID)
    {
        if (Cache.SteamPlayerCache.TryGetValue(steamID, out var data))
        {
            return data.CharacterName.ToString();
        }
        else
        {
            return null;
        }
    }

    public static ulong GetSteamIDFromName(string name)
    {
        if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out var data))
        {
            return data.SteamID;
        }
        else
        {
            return 0;
        }
    }
        
    public static bool FindPlayer(ulong steamID, bool mustOnline, out Entity playerEntity, out Entity userEntity)
    {
        EntityManager entityManager = Plugin.Server.EntityManager;

        //-- Way of the Cache
        if (Cache.SteamPlayerCache.TryGetValue(steamID, out var data))
        {
            playerEntity = data.CharEntity;
            userEntity = data.UserEntity;
            if (mustOnline)
            {
                var userComponent = entityManager.GetComponentData<User>(userEntity);
                if (!userComponent.IsConnected)
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            playerEntity = empty_entity;
            userEntity = empty_entity;
            return false;
        }
    }

    public static bool FindPlayer(string name, bool mustOnline, out Entity playerEntity, out Entity userEntity)
    {
        EntityManager entityManager = Plugin.Server.EntityManager;

        //-- Way of the Cache
        if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out var data))
        {
            playerEntity = data.CharEntity;
            userEntity = data.UserEntity;
            if (mustOnline)
            {
                var userComponent = entityManager.GetComponentData<User>(userEntity);
                if (!userComponent.IsConnected)
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            playerEntity = empty_entity;
            userEntity = empty_entity;
            return false;
        }
    }
}