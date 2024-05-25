using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bloodstone.API;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XPRising.Systems;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Utils; 

public class Alliance {
    public struct ClosePlayer {
        public Entity userEntity;
        public User userComponent;
        public int currentXp;
        public int playerLevel;
        public ulong steamID;
        public float3 position;
        public bool isTrigger;
    }
    
    public struct PlayerGroup()
    {
        public HashSet<Entity> Allies { get; } = new();
        public HashSet<Entity> Enemies { get; } = new();
        public DateTime TimeStamp { get; } = DateTime.Now;

        public string PrintAllies()
        {
            List<string> names = new();
            foreach (var ally in Allies)
            {
                if (!Plugin.Server.EntityManager.TryGetComponentData(ally, out PlayerCharacter pc)) {
                    Plugin.Log(Plugin.LogSystem.Alliance, LogLevel.Error, "Player Character Component unavailable, but should be.");
                    continue;
                }

                names.Add(pc.Name.ToString());
            }

            if (names.Count == 0)
            {
                return "Group has no members.";
            }

            return $"Group members:\n{string.Join(", ", names)}";
        }
    }
    
    private static bool ConvertToClosePlayer(Entity entity, float3 position, Plugin.LogSystem system, out ClosePlayer player) {
        if (!Plugin.Server.EntityManager.TryGetComponentData(entity, out PlayerCharacter pc)) {
            Plugin.Log(system, LogLevel.Info, "Player Character Component unavailable, available components are: " + Plugin.Server.EntityManager.Debug.GetEntityInfo(entity));
            player = new ClosePlayer();
            return false;
        }
        var user = pc.UserEntity;
        if (!Plugin.Server.EntityManager.TryGetComponentData(user, out User userComponent)) {
            Plugin.Log(system, LogLevel.Info, "User Component unavailable, available components from pc.UserEntity are: " + Plugin.Server.EntityManager.Debug.GetEntityInfo(user));
            // Can't really do anything at this point
            player = new ClosePlayer();
            return false;
        }
                        
        var steamID = userComponent.PlatformId;
        var currentXp = ExperienceSystem.GetXp(steamID);
        var playerLevel = ExperienceSystem.GetLevel(steamID);
                        
        player = new ClosePlayer() {
            currentXp = currentXp,
            playerLevel = playerLevel,
            steamID = steamID,
            userEntity = user,
            userComponent = userComponent,
            position = position
        };
        return true;
    }
    
    // Determines the units close to the entity in question.
    // This will always include the entity that triggered this call, even if they are greater than groupMaxDistance away.
    public static List<ClosePlayer> GetClosePlayers(float3 position, Entity triggerEntity, float groupMaxDistance,
        bool areAllies, bool useGroup, Plugin.LogSystem system) {
        var maxDistanceSq = groupMaxDistance * groupMaxDistance;
        //-- Must be executed from main thread
        Plugin.Log(system, LogLevel.Info, "Fetching allies...");
        List<ClosePlayer> closePlayers = new();
        if (!useGroup) {
            // If we are not using the group, then the trigger entity is the only ally
            if (ConvertToClosePlayer(triggerEntity, position, system, out var closePlayer)) {
                closePlayer.isTrigger = true;
                closePlayers.Add(closePlayer);
            }
        }
        else {
            GetPlayerClanAllies(triggerEntity, system, out var playerClan);
            
            Plugin.Log(system, LogLevel.Info, $"Getting close players");

            var playerList = new HashSet<Entity>(areAllies ? playerClan.Allies : playerClan.Enemies);
            if (areAllies && Cache.AlliancePlayerToGroupId.TryGetValue(triggerEntity, out var groupId))
            {
                var playerGroup = Cache.AlliancePlayerGroups[groupId];
                playerList.UnionWith(playerGroup.Allies);
            }
            
            foreach (var player in playerList) {
                Plugin.Log(system, LogLevel.Info, "Iterating over players, entity is " + player.GetHashCode());
                var isTrigger = triggerEntity.Equals(player);
                var playerPosition = Plugin.Server.EntityManager.GetComponentData<LocalToWorld>(player).Position;
                
                if (!isTrigger) {
                    Plugin.Log(system, LogLevel.Info, "Got entity Position");
                    var distance = math.distancesq(position.xz, playerPosition.xz);
                    Plugin.Log(system, LogLevel.Info, "DistanceSq is " + distance + ", Max DistanceSq is " + maxDistanceSq);
                    if (!(distance <= maxDistanceSq)) continue;
                }

                Plugin.Log(system, LogLevel.Info, "Converting entity to player...");

                if (ConvertToClosePlayer(player, playerPosition, system, out var closePlayer)) {
                    closePlayer.isTrigger = isTrigger;
                    closePlayers.Add(closePlayer);
                }
            }
        }
        
        //-- ---------------------------------
        Plugin.Log(system, LogLevel.Info, $"Close players fetched (are Allies: {areAllies}), Total player count of {closePlayers.Count}");
        return closePlayers;
    }
    
    // Get allies/enemies for PlayerCharacter, cached for 30 seconds
    // The list of allies includes PlayerCharacter.
    private static readonly int CacheAgeLimit = 30;
    
    private static EntityQuery _connectedPlayerCharactersQuery = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
    {
        All = new[]
        {
            ComponentType.ReadOnly<PlayerCharacter>(),
            ComponentType.ReadOnly<IsConnected>()
        },
        Options = EntityQueryOptions.IncludeDisabled
    });
    
    public static void GetPlayerClanAllies(Entity playerCharacter, Plugin.LogSystem system, out PlayerGroup playerGroup) {
        if (Cache.AllianceAutoPlayerAllies.TryGetValue(playerCharacter, out playerGroup)) {
            Plugin.Log(system, LogLevel.Info, $"Player found in cache, cache timestamp is {playerGroup.TimeStamp:u}");
            var cacheAge = DateTime.Now - playerGroup.TimeStamp;
            if (cacheAge.TotalSeconds < CacheAgeLimit) return;
            Plugin.Log(system, LogLevel.Info, $"Cache is too old, refreshing cached data");
        }

        playerGroup = new PlayerGroup();
        
        if (!Plugin.Server.EntityManager.HasComponent<PlayerCharacter>(playerCharacter)) {
            Plugin.Log(system, LogLevel.Info, $"Entity is not user: {playerCharacter}");
            Plugin.Log(system, LogLevel.Info, $"Components for Player Character are: {Plugin.Server.EntityManager.Debug.GetEntityInfo(playerCharacter)}");
            return;
        }
        
        // Check if the player has a team
        var hasTeam = false;
        var teamValue = 0;
        if (Plugin.Server.EntityManager.TryGetComponentData(playerCharacter, out Team playerTeam)) {
            Plugin.Log(system, LogLevel.Info, $"Player Character found team: {playerTeam.Value} - Faction Index: {playerTeam.FactionIndex}");
            hasTeam = true;
            teamValue = playerTeam.Value;
        }
        else {
            Plugin.Log(system, LogLevel.Info, $"Player Character has no team: all other PCs are marked as enemies.");
        }

        Plugin.Log(system, LogLevel.Info, $"Beginning To Parse Player Group");

        var playerEntityBuffer = _connectedPlayerCharactersQuery.ToEntityArray(Allocator.Temp);
        Plugin.Log(system, LogLevel.Info, $"got connected PC entities buffer of length {playerEntityBuffer.Length}");
        
        foreach (var entity in playerEntityBuffer) {
            Plugin.Log(system, LogLevel.Info, "got Entity " + entity);
            if (Plugin.Server.EntityManager.HasComponent<PlayerCharacter>(entity)) {
                Plugin.Log(system, LogLevel.Info, "Entity is User " + entity);
                if (entity.Equals(playerCharacter)) {
                    Plugin.Log(system, LogLevel.Info, "Entity is self");
                    // We are our own ally.
                    playerGroup.Allies.Add(entity);
                    continue;
                }

                // If the playerCharacter doesn't have a team, then all other PC entities are enemies
                if (!hasTeam) {
                    Plugin.Log(system, LogLevel.Info, $"Entity defaults to enemy: {entity}");
                    playerGroup.Enemies.Add(entity);
                }

                var allies = false;
                try {
                    Plugin.Log(system, LogLevel.Info, "Trying to get entity teams");
                    if (Plugin.Server.EntityManager.TryGetComponentData(entity, out Team entityTeam))
                    {
                        // Team has been found
                        Plugin.Log(system, LogLevel.Info, $"Team Value:{entityTeam.Value} - Faction Index: {entityTeam.FactionIndex}");
                        
                        // Check if the playerCharacter is on the same team as entity
                        allies = entityTeam.Value == teamValue;
                    }
                    else {
                        Plugin.Log(system, LogLevel.Info, $"Could not get team for entity: {entity}");
                        Plugin.Log(system, LogLevel.Info, "Components for entity are: " + Plugin.Server.EntityManager.Debug.GetEntityInfo(entity));
                    }
                }
                catch (Exception e) {
                    Plugin.Log(system, LogLevel.Info, "GetPlayerTeams failed " + e.Message);
                }

                if (allies) {
                    Plugin.Log(system, LogLevel.Info, $"Allies: {playerCharacter} - {entity}");
                    playerGroup.Allies.Add(entity);
                }
                else {
                    Plugin.Log(system, LogLevel.Info, $"Enemies: {playerCharacter} - {entity}");
                    playerGroup.Enemies.Add(entity);
                }
            }
            else {
                // Should never get here as the query should only return PlayerCharacter entities
                Plugin.Log(system, LogLevel.Info, "No Associated User!");
            }
        }
        
        Cache.AllianceAutoPlayerAllies[playerCharacter] = playerGroup;
    }
    
    public static void GetLocalPlayers(Entity playerCharacter, Plugin.LogSystem system, out PlayerGroup playerGroup) {
        playerGroup = new PlayerGroup();
        
        if (!Plugin.Server.EntityManager.HasComponent<PlayerCharacter>(playerCharacter)) {
            Plugin.Log(system, LogLevel.Info, $"Entity is not user: {playerCharacter}");
            return;
        }
        Plugin.Log(system, LogLevel.Info, $"LP: Beginning To Parse Player Group");

        var playerEntityBuffer = _connectedPlayerCharactersQuery.ToEntityArray(Allocator.Temp);
        Plugin.Log(system, LogLevel.Info, $"LP: got connected PC entities buffer of length {playerEntityBuffer.Length}");
        
        foreach (var entity in playerEntityBuffer) {
            Plugin.Log(system, LogLevel.Info, "got Entity " + entity);
            if (Plugin.Server.EntityManager.HasComponent<PlayerCharacter>(entity)) {
                Plugin.Log(system, LogLevel.Info, "Entity is User " + entity);
                if (entity.Equals(playerCharacter)) {
                    Plugin.Log(system, LogLevel.Info, "Entity is self");
                    // We are our own ally.
                    playerGroup.Allies.Add(entity);
                    continue;
                }

                Plugin.Log(system, LogLevel.Info, $"Adding player to group: {playerCharacter} - {entity}");
                playerGroup.Allies.Add(entity);
            }
            else {
                // Should never get here as the query should only return PlayerCharacter entities
                Plugin.Log(system, LogLevel.Info, "No Associated User!");
            }
        }
    }

    public static void RemoveUserOnLogout(Entity playerCharacter, string playerName)
    {
        // Clear any pending invites as well
        Cache.AlliancePendingInvites.Remove(playerCharacter);
        
        if (!Cache.AlliancePlayerToGroupId.Remove(playerCharacter, out var groupId))
        {
            return;
        }

        if (!Cache.AlliancePlayerGroups.TryGetValue(groupId, out var group))
        {
            // This should never happen, but just in case we can just skip.
            return;
        }

        group.Allies.Remove(playerCharacter);

        var em = VWorld.Server.EntityManager;
        foreach (var ally in group.Allies)
        {
            var allyUserEntity = em.GetComponentData<PlayerCharacter>(ally).UserEntity;
            Output.SendMessage(allyUserEntity, $"{playerName} has logged out and left your group.");
        }
    }
}