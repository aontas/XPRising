using System;
using System.Threading.Tasks;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Entities;
using Unity.Collections;
using ProjectM.Network;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using Stunlock.Core;
using XPRising.Systems;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using EntityOwner = ProjectM.EntityOwner;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Hooks;

[HarmonyPatch]
public class ModifyUnitStatBuffSystemPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Destroy), nameof(ModifyUnitStatBuffSystem_Destroy.OnUpdate))]
    private static void MUSBS_D_Post_FixSpellLevel(ModifyUnitStatBuffSystem_Destroy __instance)
    {
        if (!Plugin.ShouldApplyBuffs) return;
        
        var entityManager = __instance.EntityManager;
        var entities = __instance.__query_1735840524_0.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            DebugTool.LogEntity(entity, "ModStats_Destroy POST:", LogSystem.Buff);
            if (!entityManager.HasComponent<SpellLevel>(entity)) continue;

            EnforceSpellLevel(entityManager, entity);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    private static void MUSBS_S_Post_FixSpellLevel(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Plugin.ShouldApplyBuffs) return;
        
        var entityManager = __instance.EntityManager;
        var entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            DebugTool.LogEntity(entity, "ModStats_Spawn POST:", LogSystem.Buff);
            if (!entityManager.HasComponent<SpellLevel>(entity)) continue;

            EnforceSpellLevel(entityManager, entity);
        }
    }

    private static void EnforceSpellLevel(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var entityOwner) ||
            !entityManager.TryGetComponentData<PlayerCharacter>(entityOwner.Owner, out var playerCharacter) ||
            !entityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var user)) return;

        Task.Delay(50).ContinueWith(t =>
            ExperienceSystem.ApplyLevel(entityOwner.Owner, ExperienceSystem.GetLevel(user.PlatformId)));
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    private static void SpawnPrefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Plugin.ShouldApplyBuffs) return;
        
        EntityManager entityManager = __instance.EntityManager;
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            //not sure if this goes here
            if (Plugin.WeaponMasterySystemActive) WeaponMasterySystem.ApplyWeaponBonuses(entity, entityManager);
            
            var prefabGuid = entityManager.GetComponentData<PrefabGUID>(entity);
            DebugTool.LogPrefabGuid(prefabGuid, "ModStats_Spawn Pre:", LogSystem.Buff);
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

        // Clear the buffer before applying more stats as it is persistent
        buffer.Clear();

        // Should this be stored rather than calculated each time?
        var statusBonus = Helper.GetAllStatBonuses(user.PlatformId, owner);
        foreach (var bonus in statusBonus)
        {
            buffer.Add(makeModifyUnitStatBuff_DOTS(bonus.Key, bonus.Value, ModificationType.Add));
        }

        Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Done Adding, Buffer length: " + buffer.Length);
    }
}

[HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
public class BuffSystem_Spawn_Server_Patch {
    private static void Prefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Plugin.BloodlineSystemActive) return;
        
        var entities = __instance.__query_401358634_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities) {
            var prefabGuid = DebugTool.GetAndLogPrefabGuid(entity, "BuffSystem_Spawn_Server:", LogSystem.Buff);
            
            switch (prefabGuid.GuidHash)
            {
                case (int)Effects.AB_Feed_02_Bite_Abort_Trigger:
                    SendPlayerUpdate(__instance.EntityManager, entity, true);
                    break;
                case (int)Effects.AB_Feed_03_Complete_Trigger:
                case (int)Effects.AB_FeedBoss_03_Complete_Trigger:
                    SendPlayerUpdate(__instance.EntityManager, entity, false);
                    break;
            }
        }
    }

    private static void SendPlayerUpdate(EntityManager em, Entity entity, bool killOnly)
    {
        if (em.TryGetComponentData<SpellTarget>(entity, out var target))
        {
            // If the owner is not a player character, ignore this entity
            if (!em.TryGetComponentData<EntityOwner>(entity, out var entityOwner)) return;
            if (!em.TryGetComponentData<PlayerCharacter>(entityOwner.Owner, out var playerCharacter)) return;

            PlayerCache.FindPlayer(playerCharacter.Name.ToString(), true, out _, out var userEntity);
            // target.BloodConsumeSource can buff/debuff the blood quality
            Output.DebugMessage(userEntity, $"{(killOnly ? "Killed" : "Consumed")}: {DebugTool.GetPrefabName(target.Target._Entity)}");
            BloodlineSystem.UpdateBloodline(entityOwner.Owner, target.Target._Entity, killOnly);
        }
    }
}

[HarmonyPatch(typeof(BuffDebugSystem), nameof(BuffDebugSystem.OnUpdate))]
public class BuffDebugSystem_Patch
{
    private static void Prefix(BuffDebugSystem __instance)
    {
        var entities = __instance.__query_401358786_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities) {
            var guid = __instance.EntityManager.GetComponentData<PrefabGUID>(entity);
            DebugTool.LogPrefabGuid(guid, "BuffDebugSystem:", LogSystem.Buff);

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
            
            // Very useful for debugging things.
            Output.DebugMessage(userEntity, DebugTool.GetPrefabName(entity));

            if (newPlayer)
            {
                PlayerCache.PlayerOnline(userEntity, userData);
                if (Plugin.ExperienceSystemActive) ExperienceSystem.CheckAndApplyLevel(ownerEntity, userEntity, steamID);
            }
            if (combatStart || combatEnd) TriggerCombatUpdate(ownerEntity, steamID, combatStart, combatEnd);
            if (addingBloodBuff && Plugin.ShouldApplyBuffs)
            {
                // We are intending to use the AB_BloodBuff_VBlood_0 buff as our internal adding stats buff, but
                // it doesn't usually have a unit stat mod buffer. Add this buffer now.
                if (__instance.EntityManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(entity, out var buffer)) continue;
                __instance.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(entity);

                // Remove the drain increase factor, to normalise what the buff would be.
                if (__instance.EntityManager.TryGetComponentData<BloodBuff_VBlood_0_DataShared>(entity,
                        out var dataShared))
                {
                    dataShared.DrainIncreaseFactor = 1.0f;
                    __instance.EntityManager.SetComponentData(entity, dataShared);
                }
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