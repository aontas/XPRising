using System;
using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Unity.Collections;
using Unity.Entities;
using XPRising.Systems;
using XPRising.Transport;
using XPRising.Utils;

namespace XPRising.Hooks;

[HarmonyPatch]
public class ScriptSpawnServerHook
{
    private static EntityManager EntityManager => Plugin.Server.EntityManager;
    
    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        if (!Plugin.BloodlineSystemActive) return;
        
        var entities = __instance.__query_1231292170_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                var prefabGuid = Helper.GetPrefabGUID(entity);
                if (prefabGuid != BuffUtil.AppliedBuff &&
                    EntityManager.HasComponent<BloodBuff>(entity) &&
                    EntityManager.TryGetComponentData<EntityOwner>(entity, out var entityOwner) &&
                    EntityManager.TryGetComponentData<PlayerCharacter>(entityOwner, out var playerCharacter) &&
                    EntityManager.TryGetComponentData<User>(playerCharacter.UserEntity, out var userData))
                {
                    // If we have gained a blood type, update the stat bonus
                    if (BloodlineSystem.BuffToBloodTypeMap.TryGetValue(prefabGuid, out _))
                    {
                        BuffUtil.ApplyStatBuffOnDelay(userData, playerCharacter.UserEntity, entityOwner);
                        var currentBlood = BloodlineSystem.BloodMasteryType(entityOwner);
                        ClientActionHandler.SendActiveBloodMasteryData(userData, currentBlood);
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