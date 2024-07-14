using System;
using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Unity.Collections;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Hooks;

[HarmonyPatch]
public class ScriptSpawnServerHook
{
    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        if (!Plugin.BloodlineSystemActive) return;
        
        var entities = __instance.__query_1231292176_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                var prefabGuid = Helper.GetPrefabGUID(entity);
                if (prefabGuid != BuffUtil.AppliedBuff &&
                    Plugin.Server.EntityManager.HasComponent<BloodBuff>(entity) &&
                    Plugin.Server.EntityManager.TryGetComponentData<EntityOwner>(entity, out var entityOwner) &&
                    Plugin.Server.EntityManager.TryGetComponentData<PlayerCharacter>(entityOwner, out var playerCharacter) &&
                    Plugin.Server.EntityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var userData))
                {
                    // If we have gained a blood type, update the stat bonus
                    if (BloodlineSystem.BuffToBloodTypeMap.TryGetValue(prefabGuid, out _))
                    {
                        BuffUtil.ApplyStatBuffOnDelay(userData, playerCharacter.UserEntity, entityOwner);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log(Plugin.LogSystem.Bloodline, LogLevel.Error, $"Error in ScriptSpawnServerHook: {e.Message}", true);
        }
        finally
        {
            entities.Dispose();
        }
    }

}