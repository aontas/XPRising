using HarmonyLib;
using ProjectM.Gameplay.Systems;
using Unity.Collections;
using ProjectM;
using BepInEx.Logging;
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
}