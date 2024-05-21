using HarmonyLib;
using ProjectM.Gameplay.Systems;
using Unity.Entities;
using Unity.Collections;
using ProjectM.Network;
using ProjectM;
using BepInEx.Logging;
using XPRising.Systems;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Hooks;

[HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
public class ArmorLevelSystem_Spawn_Patch
{
    private static void Prefix(ArmorLevelSystem_Spawn __instance)
    {
        Plugin.Log(LogSystem.Buff, LogLevel.Info, "Armor System Patch Entry");
        if (Plugin.ExperienceSystemActive)
        {
            var entityManager = __instance.EntityManager;
            var entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities){
                // Remove any armor level, as we set this manually to generate the player level.
                // This ensures that the Effects.AB_BloodBuff_Brute_GearLevelBonus does not give us extra unintended
                // levels. (It does allow just the brute bonus, but nothing more).
                if (entityManager.TryGetComponentData<ArmorLevel>(entity, out var armorLevel))
                {
                    armorLevel.Level = 0;
                    entityManager.SetComponentData(entity, armorLevel);
                }
            }
        }
    }
    
    private static void Postfix(ArmorLevelSystem_Spawn __instance)
    {
        Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Post armour change");
        if (Plugin.ExperienceSystemActive)
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities){
                if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var owner) ||
                    !entityManager.TryGetComponentData<PlayerCharacter>(owner.Owner, out var playerCharacter))
                {
                    // Does not have owner or player character
                    continue;
                }
                
                var steamID = entityManager.GetComponentData<User>(playerCharacter.UserEntity).PlatformId;
                ExperienceSystem.SetLevel(owner, playerCharacter.UserEntity, steamID);
            }
        }
    }
}

[HarmonyPatch(typeof(ArmorLevelSystem_Destroy), nameof(ArmorLevelSystem_Destroy.OnUpdate))]
public class ArmorLevelSystem_Destroy_Patch
{
    private static void Postfix(ArmorLevelSystem_Destroy __instance)
    {
        Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Post armour change");
        if (Plugin.ExperienceSystemActive)
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__query_663986292_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities){
                if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var owner) ||
                    !entityManager.TryGetComponentData<PlayerCharacter>(owner.Owner, out var playerCharacter))
                {
                    // Does not have owner or player character
                    continue;
                }
                
                var steamID = entityManager.GetComponentData<User>(playerCharacter.UserEntity).PlatformId;
                ExperienceSystem.SetLevel(owner, playerCharacter.UserEntity, steamID);
            }
        }
    }
}

[HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
public class WeaponLevelSystem_Spawn_Patch
{
    private static void Prefix(WeaponLevelSystem_Spawn __instance)
    {
        // Plugin.Log(LogSystem.Buff, LogLevel.Info, "Weapon System Patch Entry");
        // if (Plugin.ExperienceSystemActive || Plugin.WeaponMasterySystemActive)
        // {
        //     var entityManager = __instance.EntityManager;
        //     var entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
        //
        //     foreach (var entity in entities)
        //     {
        //         Entity Owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
        //         Entity User = entityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
        //         if (Plugin.WeaponMasterySystemActive || ExperienceSystem.ShouldAllowGearLevel || ExperienceSystem.LevelRewardsOn)
        //         {
        //             Plugin.Log(LogSystem.Buff, LogLevel.Info, $"Applying Helper.AppliedBuff: {Helper.AppliedBuff.GuidHash}");
        //             Helper.ApplyBuff(User, Owner, Helper.AppliedBuff);
        //         }
        //     }
        // }
    }
    
    private static void Postfix(WeaponLevelSystem_Spawn __instance)
    {
        if (Plugin.ExperienceSystemActive)
        {
            var entityManager = Plugin.Server.EntityManager;
            NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var owner) ||
                    !entityManager.TryGetComponentData<PlayerCharacter>(owner.Owner, out var playerCharacter))
                {
                    // Does not have owner or player character
                    continue;
                }
                
                var steamID = entityManager.GetComponentData<User>(playerCharacter.UserEntity).PlatformId;
                ExperienceSystem.SetLevel(owner, playerCharacter.UserEntity, steamID);
            }
        }
    }
}

[HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
public class WeaponLevelSystem_Destroy_Patch
{
    private static void Prefix(WeaponLevelSystem_Destroy __instance)
    {
        // TODO do we want to buff weapons as they come in due to mastery?
        // if (Plugin.ExperienceSystemActive && (ExperienceSystem.LevelRewardsOn || ExperienceSystem.ShouldAllowGearLevel))
        // {
        //     EntityManager entityManager = __instance.EntityManager;
        //     NativeArray<Entity> entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
        //     foreach (var entity in entities)
        //     {
        //         Entity Owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
        //         Entity User = __instance.EntityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
        //         ulong SteamID = __instance.EntityManager.GetComponentData<User>(User).PlatformId;
        //         if (ExperienceSystem.ShouldAllowGearLevel) //experiment with buffing for equipment based on level.
        //         {
        //             if (!Cache.player_geartypedonned.ContainsKey(SteamID) || Cache.player_geartypedonned[SteamID] == null)
        //                 Cache.player_geartypedonned[SteamID] = new System.Collections.Generic.Dictionary<UnitStatType, float>();
        //             //we can accomplish gear bonuses per level using a similar buffing system approach as ability point buffs for leveling.
        //             //might need a better data structure...but should be fine in the cache only.
        //         }
        //         //reset buffs for being unarmed
        //         Helper.ApplyBuff(User, Owner, Helper.AppliedBuff);
        //     }
        // }
    }
    
    private static void Postfix(WeaponLevelSystem_Destroy __instance)
    {
        if (Plugin.ExperienceSystemActive)
        {
            var entityManager = Plugin.Server.EntityManager;
            NativeArray<Entity> entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var owner) ||
                    !entityManager.TryGetComponentData<PlayerCharacter>(owner.Owner, out var playerCharacter))
                {
                    // Does not have owner or player character
                    continue;
                }
                
                var steamID = entityManager.GetComponentData<User>(playerCharacter.UserEntity).PlatformId;
                ExperienceSystem.SetLevel(owner, playerCharacter.UserEntity, steamID);
            }
        }
    }
}