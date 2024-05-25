using ProjectM;
using ProjectM.Shared;
using HarmonyLib;
using XPRising.Configuration;
using Stunlock.Core;
using XPRising.Systems;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using Prefabs = XPRising.Utils.Prefabs;

namespace XPRising.Hooks;

[HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
public static class UnitSpawnerReactSystemPatch
{
    public static bool listen = false;
    public static void Prefix(UnitSpawnerReactSystem __instance)
    {
        if (!(Plugin.WantedSystemActive || Plugin.RandomEncountersSystemActive)) return;
        
        var entities = __instance.__query_2099432189_0.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities) {
            if (!__instance.EntityManager.HasComponent<LifeTime>(entity)) return;

            var lifetime = __instance.EntityManager.GetComponentData<LifeTime>(entity);
            // If this successfully gets decoded, then this is a custom spawn... or just extremely lucky.
            if (SpawnUnit.DecodeLifetime(lifetime.Duration, out var level, out var faction))
            {
                if (faction != SpawnUnit.SpawnFaction.Default) {
                    // Change faction to Vampire Hunters for spawned units
                    var factionReference = __instance.EntityManager.GetComponentData<FactionReference>(entity);
                    factionReference.FactionGuid = new ModifiablePrefabGUID(new PrefabGUID((int)Prefabs.Faction.VampireHunters));
                    __instance.EntityManager.SetComponentData(entity, factionReference);
                }
                if (level > 0) {
                    // Set the spawned unit level to the provided level
                    __instance.EntityManager.SetComponentData(
                        entity,
                        new UnitLevel()
                        {
                            Level = new ModifiableInt(level)
                        });
                }
            }

            if (listen)
            {
                if (Cache.spawnNPC_Listen.TryGetValue(lifetime.Duration, out var content))
                {
                    content.EntityIndex = entity.Index;
                    content.EntityVersion = entity.Version;
                    if (content.Options.Process) content.Process = true;

                    Cache.spawnNPC_Listen[lifetime.Duration] = content;
                    listen = false;
                }
            }
            
            if(Plugin.RandomEncountersSystemActive && Plugin.IsInitialized)
            {
                RandomEncountersSystem.ServerEvents_OnUnitSpawned(__instance.EntityManager, entity);
            }
        }
    }
}

[HarmonyPatch(typeof(MinionSpawnSystem), nameof(MinionSpawnSystem.OnUpdate))]
public static class MinionSpawnSystem_Patch {
    public static void Prefix(MinionSpawnSystem __instance)
    {
        // This issue and fix only make sense in the context of the Wanted system being active.
        if (!Plugin.WantedSystemActive) return;
        
        var entities = __instance.__query_166459767_0.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities) {
            // Gloomrot spider-tanks spawn a gloomrot technician minion that does not despawn when the spider-tank gets destroyed
            // by the "Lifetime" component. This will check for that case and destroy the minion on load so it doesn't get stuck.
            // This does not impact the behaviour of the spider-tank (other than it does not drop the technician on death).
            if (Helper.ConvertGuidToUnit(Helper.GetPrefabGUID(entity)) != Units.CHAR_Gloomrot_Technician) continue;
            
            if (__instance.EntityManager.TryGetComponentData(entity, out FactionReference faction)) {
                if (__instance.EntityManager.TryGetComponentData(entity, out EntityOwner eo)) {
                     if (__instance.EntityManager.TryGetComponentData(eo.Owner, out LifeTime lt) && lt.EndAction != LifeTimeEndAction.Kill) {
                         if (SpawnUnit.DecodeLifetime(lt.Duration, out _, out _)) {
                             // Destroy initial minions as they don't transition properly when their parent is destroyed without being killed.
                             DestroyUtility.CreateDestroyEvent(Plugin.Server.EntityManager, entity, DestroyReason.Default, DestroyDebugReason.ByScript);
                             DestroyUtility.Destroy(Plugin.Server.EntityManager, entity);
                         }
                     }
                }
            }
        }
    }
}
