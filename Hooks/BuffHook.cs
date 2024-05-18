using System;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Entities;
using Unity.Collections;
using ProjectM.Network;
using ProjectM;
using Stunlock.Core;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Hooks;

[HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
public class ModifyUnitStatBuffSystem_Spawn_Patch
{
    private static void Prefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        EntityManager entityManager = __instance.EntityManager;
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            var prefabGuid = entityManager.GetComponentData<PrefabGUID>(entity);
            DebugTool.LogPrefabGuid(prefabGuid, "Buff:", LogSystem.Buff);
            if (prefabGuid.GuidHash == Helper.ForbiddenBuffGuid)
            {
                Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Forbidden buff found with GUID of " + prefabGuid.GuidHash);
            }
            else if (prefabGuid == Helper.AppliedBuff)
            {
                ApplyBuffs(entity, entityManager);
            }
        }
    }

    private static ModifyUnitStatBuff_DOTS makeModifyUnitStatBuff_DOTS(UnitStatType type, float value,
        ModificationType modType)
    {
        if (Helper.multiplierStats.Contains(type))
        {
            modType = ModificationType.Multiply;
        }

        return new ModifyUnitStatBuff_DOTS()
        {
            StatType = type,
            Value = value,
            ModificationType = modType,
            Modifier = 1,
            Id = ModificationId.NewId(0)
        };
    }

    private static void ApplyBuffs(Entity entity, EntityManager entityManager)
    {

        Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Applying XPRising Buffs");
        var owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
        Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Owner found, hash: " + owner.GetHashCode());
        if (!entityManager.TryGetComponentData<PlayerCharacter>(owner, out var playerCharacter)) return;
        if (!entityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var user))
            Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, $"has no user");

        if (!entityManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(entity, out var buffer))
        {
            Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Error, "entity did not have buffer");
            return;
        }

        //Gotta clear the buffer before applying more stats as it is persistent
        buffer.Clear();

        // Should this be stored rather than calculated each time?
        LazyDictionary<UnitStatType, float> statusBonus = new();
        // Plugin.Log(LogSystem.Buff, LogLevel.Info, "Now doing Weapon Mastery System Buff Receiver");
        // if (Plugin.WeaponMasterySystemActive) WeaponMasterySystem.BuffReceiver(buffer, owner, user.PlatformId);
        // Plugin.Log(LogSystem.Buff, LogLevel.Info, "Now doing Bloodline Buff Receiver");
        // if (Plugin.BloodlineSystemActive) BloodlineSystem.BuffReceiver(buffer, owner, user.PlatformId);
        Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Now doing XP System Buff Receiver");
        if (ExperienceSystem.LevelRewardsOn && Plugin.ExperienceSystemActive)
            ExperienceSystem.BuffReceiver(ref statusBonus, owner, user.PlatformId);

        foreach (var bonus in statusBonus)
        {
            buffer.Add(makeModifyUnitStatBuff_DOTS(bonus.Key, bonus.Value, ModificationType.Add));
        }

        Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Done Adding, Buffer length: " + buffer.Length);
    }

    private static void Postfix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        EntityManager entityManager = __instance.EntityManager;
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var prefabGuid = entityManager.GetComponentData<PrefabGUID>(entity);

            if (!__instance.EntityManager.TryGetComponentData<EntityOwner>(entity, out var owner) ||
                !__instance.EntityManager.TryGetComponentData<PlayerCharacter>(owner.Owner, out var playerCharacter) ||
                !__instance.EntityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var user))
            {
                DebugTool.LogPrefabGuid(prefabGuid, "Not PC buff (spawn):", LogSystem.Buff);
                continue;
            }
            
            ExperienceSystem.SetLevel(owner.Owner, playerCharacter.UserEntity, user.PlatformId);
        }
    }
}
[HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Destroy), nameof(ModifyUnitStatBuffSystem_Destroy.OnUpdate))]
public class ModifyUnitStatBuffSystem_Destroy_Patch
{
    private static void Postfix(ModifyUnitStatBuffSystem_Destroy __instance)
    {
        EntityManager entityManager = __instance.EntityManager;
        NativeArray<Entity> entities = __instance.__query_1735840524_0.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var prefabGuid = entityManager.GetComponentData<PrefabGUID>(entity);
            var itemEquipped = Helper.IsItemEquipBuff(prefabGuid);
            
            if (!__instance.EntityManager.TryGetComponentData<EntityOwner>(entity, out var owner) ||
                !__instance.EntityManager.TryGetComponentData<PlayerCharacter>(owner.Owner, out var playerCharacter) ||
                !__instance.EntityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var user))
            {
                // Item equipped on a non-pc entity.
                DebugTool.LogPrefabGuid(prefabGuid, "Not PC buff (destroy):", LogSystem.Buff);
                continue;
            }
            
            ExperienceSystem.SetLevel(owner.Owner, playerCharacter.UserEntity, user.PlatformId);

            if (itemEquipped) DebugTool.LogPrefabGuid(prefabGuid, "Destroy:", LogSystem.Buff);
            
            if (!entityManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(entity, out var buffer))
            {
                Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Destroy: entity did not have buffer");
                return;
            }
            
            DebugTool.LogStatsBuffer(buffer, "Destroy:", LogSystem.Buff);
        }
    }
}

[HarmonyPatch(typeof(BuffDebugSystem), nameof(BuffDebugSystem.OnUpdate))]
public class DebugBuffSystem_Patch
{
    private static void Prefix(BuffDebugSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_401358786_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities) {
            var guid = __instance.EntityManager.GetComponentData<PrefabGUID>(entity);
            DebugTool.LogPrefabGuid(guid, "BuffDebugSystem:");

            var combatStart = false;
            var combatEnd = false;
            var newPlayer = false;
            var addingBloodBuff = false;
            switch (guid.GuidHash)
            {
                case (int)Buffs.Buff_InCombat:
                    combatStart = true;
                    break;
                case (int)Buffs.Buff_OutOfCombat:
                    combatEnd = true;
                    break;
                case (int)Effects.AB_Interact_TombCoffinSpawn_Travel:
                    newPlayer = true;
                    break;
                case (int)Effects.AB_BloodBuff_VBlood_0:
                    addingBloodBuff = true;
                    break;
                default:
                    continue;
            }

            // Get entity owner: This will be the entity that actually gets the buff
            var ownerEntity = __instance.EntityManager.GetComponentData<EntityOwner>(entity).Owner;
            // If the owner is not a player character, ignore this entity
            if (!__instance.EntityManager.TryGetComponentData(ownerEntity, out PlayerCharacter playerCharacter)) continue;
            
            var userEntity = playerCharacter.UserEntity;
            var userData = __instance.EntityManager.GetComponentData<User>(userEntity);
            var steamID = userData.PlatformId;
            
            if (newPlayer) Helper.UpdatePlayerCache(userEntity, userData);
            if (combatStart || combatEnd) TriggerCombatUpdate(ownerEntity, steamID, combatStart, combatEnd);
            if (addingBloodBuff)
            {
                // We are intending to use the AB_BloodBuff_VBlood_0 buff as our internal adding stats buff, but
                // it doesn't usually have a unit stat mod buffer. Add this buffer now.
                if (__instance.EntityManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(entity, out var buffer)) continue;
                __instance.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(entity);
            }
        }
    }

    private static void TriggerCombatUpdate(Entity ownerEntity, ulong steamID, bool combatStart, bool combatEnd)
    {
        // Update player combat status
        // Notes:
        // - only update combatStart if we are not already in combat. It gets sent multiple times as
        //   mobs refresh their combat state with the PC
        // - Buff_OutOfCombat only seems to be sent once.
        var inCombat = Cache.GetCombatStart(steamID) > Cache.GetCombatEnd(steamID);
        if (combatStart && !inCombat) {
            Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, $"{steamID}: Combat start");
            Cache.playerCombatStart[steamID] = DateTime.Now;

            // Actions to check on combat start
            if (Plugin.WantedSystemActive) WantedSystem.CheckForAmbush(ownerEntity);
        } else if (combatEnd) {
            Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, $"{steamID}: Combat end");
            Cache.playerCombatEnd[steamID] = DateTime.Now;
        }
    }
}