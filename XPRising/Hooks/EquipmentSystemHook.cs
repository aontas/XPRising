using HarmonyLib;
using ProjectM.Gameplay.Systems;
using Unity.Collections;
using ProjectM;
using BepInEx.Logging;
using ProjectM.Network;
using XPRising.Utils;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Hooks;

[HarmonyPatch]
public class ItemLevelSystemSpawnPatch
{
    /*
     * WeaponLevelSystem_Spawn and ArmorLevelSystem_Spawn can be used to set the level granted for items to 0, so that
     * we can manually set the level as required.
     *
     * Unfortunately, SpellLevelSystem_Spawn doesn't work the same so we cannot use it as well. The BuffHook forcibly
     * sets the user level when we gain/lose a SpellLevel buff (which should be true for all rings/amulets).
     */
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
    private static void WeaponSpawn(WeaponLevelSystem_Spawn __instance)
    {
        if (Plugin.ExperienceSystemActive)
        {
            Plugin.Log(LogSystem.Buff, LogLevel.Info, "WeaponLevelSystem spawn");
            var entityManager = __instance.EntityManager;
            var entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                // Remove any weapon level, as we set the levels to generate the player level.
                if (entityManager.TryGetComponentData<WeaponLevel>(entity, out var weaponLevel))
                {
                    weaponLevel.Level = 0;
                    entityManager.SetComponentData(entity, weaponLevel);
                }
            }
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
    private static void ArmorSpawn(ArmorLevelSystem_Spawn __instance)
    {
        if (Plugin.ExperienceSystemActive)
        {
            Plugin.Log(LogSystem.Buff, LogLevel.Info, "ArmorLevelSystem spawn");
            var entityManager = __instance.EntityManager;
            var entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                // Remove any armor level, as we set the levels to generate the player level.
                if (entityManager.TryGetComponentData<ArmorLevel>(entity, out var armorLevel))
                {
                    armorLevel.Level = 0;
                    entityManager.SetComponentData(entity, armorLevel);
                }
            }
        }
    }

    /*
     * These WeaponLevelSystem Postfix patches attempt to ensure that the weapon mastery buffs get reapplied correctly.
     */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
    private static void WeaponSpawnPostfix(WeaponLevelSystem_Spawn __instance)
    {
        if (Plugin.ShouldApplyBuffs)
        {
            Plugin.Log(LogSystem.Buff, LogLevel.Info, "WeaponLevelSystem spawn POST");
            var entityManager = __instance.EntityManager;
            var entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var entityOwner) ||
                    !entityManager.TryGetComponentData<PlayerCharacter>(entityOwner.Owner, out var playerCharacter) ||
                    !entityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out _))
                {
                    continue;
                }

                Helper.ApplyBuff(playerCharacter.UserEntity, entityOwner, Helper.AppliedBuff);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
    private static void WeaponDestoryPostfix(WeaponLevelSystem_Destroy __instance)
    {
        if (Plugin.ShouldApplyBuffs)
        {
            Plugin.Log(LogSystem.Buff, LogLevel.Info, "WeaponLevelSystem spawn POST");
            var entityManager = __instance.EntityManager;
            var entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!entityManager.TryGetComponentData<EntityOwner>(entity, out var entityOwner) ||
                    !entityManager.TryGetComponentData<PlayerCharacter>(entityOwner.Owner, out var playerCharacter) ||
                    !entityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out _))
                {
                    continue;
                }

                Helper.ApplyBuff(playerCharacter.UserEntity, entityOwner, Helper.AppliedBuff);
            }
        }
    }
}