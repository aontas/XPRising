using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using Unity.Collections;
using Unity.Entities;
using XPRising.Systems;
using XPShared;

namespace XPRising.Hooks;

[HarmonyPatch]
public class StatChangeSystemHook
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StatChangeSystem), nameof(StatChangeSystem.ApplyStatChanges))]
    private static void ApplyStatChangesPostfix(
        StatChangeSystem __instance,
        NativeArray<Entity> statChangeEntities,
        NativeArray<StatChangeEvent> statChanges)
    {        
        for (var i = 0; i < statChangeEntities.Length; i++)
        {
            var statChangeEvent = statChanges[i];
            var statChangeEntity = statChangeEntities[i];

            switch (statChangeEvent.Reason)
            {
                case StatChangeReason.BehaviourTreeSystem_0:
                case StatChangeReason.BehaviourTreeSystem_1:
                    // Ignore these
                    break;
                case StatChangeReason.DealDamageSystem_0:
                    // If the target entity does not have movement, it isn't a unit that will give XP (likely a tree/ore/wall etc)
                    if (!statChangeEvent.Entity.Has<Movement>()) break;
                    WeaponMasterySystem.HandleDamageEvent(statChangeEvent.Source, statChangeEvent.Entity);
                    break;
                case StatChangeReason.Any:
                case StatChangeReason.Default:
                case StatChangeReason.HandleGameplayEventsBase_0:
                case StatChangeReason.HandleGameplayEventsBase_1:
                case StatChangeReason.HandleGameplayEventsBase_2:
                case StatChangeReason.HandleGameplayEventsBase_3:
                case StatChangeReason.HandleGameplayEventsBase_4:
                case StatChangeReason.HandleGameplayEventsBase_5:
                case StatChangeReason.HandleGameplayEventsBase_6:
                case StatChangeReason.HandleGameplayEventsBase_7:
                case StatChangeReason.HandleGameplayEventsBase_8:
                case StatChangeReason.HandleGameplayEventsBase_9:
                case StatChangeReason.HandleGameplayEventsBase_10:
                case StatChangeReason.HandleGameplayEventsBase_11:
                case StatChangeReason.FeedableInventorySystem_Update_0:
                case StatChangeReason.StatChangeSystem_0:
                case StatChangeReason.StatChangeSystem_1:
                case StatChangeReason.ServerGameManager_0:
                case StatChangeReason.ServerGameManager_1:
                case StatChangeReason.ServerGameManager_2:
                case StatChangeReason.ServerGameManager_3:
                case StatChangeReason.ServerGameManager_4:
                case StatChangeReason.BloodConsumeBuffDestroySystem_0:
                case StatChangeReason.BloodConsumeBuffDestroySystem_1:
                case StatChangeReason.VerifyingRepairAbilitySystem_0:
                case StatChangeReason.CastleBuffsTickSystem_0:
                case StatChangeReason.HealingBuffSystem_0:
                case StatChangeReason.UpdatePrisonSystem_0:
                case StatChangeReason.ServantReactToDestroySystem_0:
                case StatChangeReason.VariousMigratedDebugEventsSystem_0:
                case StatChangeReason.DebugEventsSystem_0:
                case StatChangeReason.DebugEventsSystem_1:
                case StatChangeReason.DebugEventsSystem_2:
                case StatChangeReason.TakeDamageInSunSystem_0:
                case StatChangeReason.Script_BloodBuff_Brute_RecoverOnKill_0:
                case StatChangeReason.LifeTimeSystem_0:
                case StatChangeReason.LinkMinionToOwnerOnSpawnSystem_0:
                case StatChangeReason.KillAllMinionsEventSystem_0:
                case StatChangeReason.KillAndDisableInactivePlayerAfterDuration_0:
                case StatChangeReason.CastleHeartEventSystem_0:
                case StatChangeReason.CastleHeartEventSystem_1:
                case StatChangeReason.Script_FollowerStationaryBuff_0:
                case StatChangeReason.Script_SharedHealthPoolBuff_0:
                case StatChangeReason.ReviveCancelEventSystem_0:
                case StatChangeReason.KillEventSystem_0:
                case StatChangeReason.KillEventSystem_1:
                case StatChangeReason.KillEventSystem_2:
                case StatChangeReason.KillEventSystem_3:
                case StatChangeReason.UnitSpawnerOnDestroySystem_0:
                case StatChangeReason.UnitBudgetSystem_0:
                case StatChangeReason.WarEventClear_0:
                case StatChangeReason.KillVbloodConsoleCommand:
                    Plugin.Log(Plugin.LogSystem.Core, LogLevel.Debug, $"stat change not handled: {statChangeEvent.StatType} {statChangeEvent.Reason}");
                    break;
            }
        }
    }
}