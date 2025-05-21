using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using XPRising.Systems;
using XPRising.Transport;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using XPShared;
using EntityOwner = ProjectM.EntityOwner;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Hooks;

[HarmonyPatch]
public class BuffSystemSpawnServerPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
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

[HarmonyPatch]
public class BuffDebugSystemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BuffDebugSystem), nameof(BuffDebugSystem.OnUpdate))]
    private static void Prefix(BuffDebugSystem __instance)
    {
        var entities = __instance.__query_401358787_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities) {
            var guid = __instance.EntityManager.GetComponentData<PrefabGUID>(entity);
            DebugTool.LogPrefabGuid(guid, "BuffDebugSystemPre:", LogSystem.Buff);

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

            if (newPlayer)
            {
                PlayerCache.PlayerOnline(userEntity, userData);
                if (Plugin.ExperienceSystemActive) ExperienceSystem.CheckAndApplyLevel(ownerEntity, userEntity, steamID);
            }
            if (combatStart || combatEnd) TriggerCombatUpdate(ownerEntity, steamID, combatStart, combatEnd);
            if (addingBloodBuff && Plugin.ShouldApplyBuffs)
            {
                Output.DebugMessage(userEntity, "Applying XPRising stat buff");
                
                // We are intending to use the AB_BloodBuff_VBlood_0 buff as our internal adding stats buff, but
                // it doesn't usually have a unit stat mod buffer. Add this buffer now if it does not exist.
                if (!entity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
                {
                    buffer = entity.AddBuffer<ModifyUnitStatBuff_DOTS>();
                }
            
                // Clear the buffer so it doesn't double up on bonuses
                buffer.Clear();

                // Should this be stored rather than calculated each time?
                var statusBonus = Helper.GetAllStatBonuses(steamID, ownerEntity);
                foreach (var bonus in statusBonus)
                {
                    buffer.Add(Helper.MakeModifyUnitStatBuff_DOTS(bonus.Key, bonus.Value, ModificationType.Add));
                }

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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuffDebugSystem), nameof(BuffDebugSystem.OnUpdate))]
    private static void Postfix(BuffDebugSystem __instance)
    {
        if (Plugin.ExperienceSystemActive)
        {
            var entities = __instance.__query_401358787_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var guid = __instance.EntityManager.GetComponentData<PrefabGUID>(entity);
                DebugTool.LogPrefabGuid(guid, "BuffDebugSystemPost:", LogSystem.Buff);

                if (BloodlineSystem.BuffToBloodTypeMap.TryGetValue(guid, out _))
                {
                    // If we have gained a blood type, update the stat bonus
                    ApplyPlayerBloodType(__instance.EntityManager, entity);
                    continue;
                }

                switch (guid.GuidHash)
                {
                    // Detect equipping a spell source (ring/necklace) so we can reapply the player level correctly
                    case (int)Items.Item_EquipBuff_MagicSource_BloodKey_T01:
                    case (int)Items.Item_EquipBuff_MagicSource_General:
                    case (int)Items.Item_EquipBuff_MagicSource_Soulshard_Dracula:
                    case (int)Items.Item_EquipBuff_MagicSource_Soulshard_Manticore:
                    case (int)Items.Item_EquipBuff_MagicSource_Soulshard_Morgana:
                    case (int)Items.Item_EquipBuff_MagicSource_Soulshard_Solarus:
                    case (int)Items.Item_EquipBuff_MagicSource_Soulshard_TheMonster:
                    case (int)Items.Item_EquipBuff_MagicSource_T06_Blood:
                    case (int)Items.Item_EquipBuff_MagicSource_T06_Chaos:
                    case (int)Items.Item_EquipBuff_MagicSource_T06_Frost:
                    case (int)Items.Item_EquipBuff_MagicSource_T06_Illusion:
                    case (int)Items.Item_EquipBuff_MagicSource_T06_Storm:
                    case (int)Items.Item_EquipBuff_MagicSource_T06_Unholy:
                    case (int)Items.Item_EquipBuff_MagicSource_T08_Blood:
                    case (int)Items.Item_EquipBuff_MagicSource_T08_Chaos:
                    case (int)Items.Item_EquipBuff_MagicSource_T08_Frost:
                    case (int)Items.Item_EquipBuff_MagicSource_T08_Illusion:
                    case (int)Items.Item_EquipBuff_MagicSource_T08_Storm:
                    case (int)Items.Item_EquipBuff_MagicSource_T08_Unholy:
                    case (int)Items.Item_EquipBuff_Shared_General:
                        ApplyPlayerLevel(__instance.EntityManager, entity);
                        continue;
                    default:
                        continue;
                }
            }
        }
    }

    private static void ApplyPlayerBloodType(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var entityOwner) ||
            !entityManager.TryGetComponentData<PlayerCharacter>(entityOwner.Owner, out var playerCharacter) ||
            !entityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var user)) return;
        
        BuffUtil.ApplyStatBuffOnDelay(user, playerCharacter.UserEntity, entityOwner);
        var currentBlood = BloodlineSystem.BloodMasteryType(entityOwner);
        ClientActionHandler.SendActiveBloodMasteryData(user, currentBlood);
    }

    private static void ApplyPlayerLevel(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var entityOwner) ||
            !entityManager.TryGetComponentData<PlayerCharacter>(entityOwner.Owner, out var playerCharacter) ||
            !entityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var user)) return;
        // As this is a player, re-apply the player level
        ExperienceSystem.ApplyLevel(entityOwner.Owner, ExperienceSystem.GetLevel(user.PlatformId));
    }

    private static void TriggerCombatUpdate(Entity ownerEntity, ulong steamID, bool combatStart, bool combatEnd)
    {
        // Update player combat status
        // Notes:
        // - only update combatStart if we are not already in combat. It gets sent multiple times as
        //   mobs refresh their combat state with the PC
        // - Buff_OutOfCombat only seems to be sent once.
        var inCombat = Cache.GetCombatStart(steamID) > Cache.GetCombatEnd(steamID);
        var timeNow = DateTime.Now;
        if (combatStart && !inCombat) {
            Plugin.Log(LogSystem.Buff, LogLevel.Info, $"{steamID}: Combat start");
            Output.DebugMessage(steamID, $"Combat start: {timeNow:u}");
            Cache.playerCombatStart[steamID] = timeNow;

            // Actions to check on combat start
            if (Plugin.WantedSystemActive) WantedSystem.CheckForAmbush(ownerEntity);
        } else if (combatEnd) {
            Plugin.Log(LogSystem.Buff, LogLevel.Info, $"{steamID}: Combat end");
            Output.DebugMessage(steamID, $"Combat end: {timeNow:u}: {(timeNow - Cache.GetCombatStart(steamID)).TotalSeconds:N1} s");
            Cache.playerCombatEnd[steamID] = timeNow;

            if (Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive)
            {
                GlobalMasterySystem.ExitCombat(steamID);
            }
        }
    }
}