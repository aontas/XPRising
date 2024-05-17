using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Transforms;
using XPRising.Systems;
using XPRising.Utils;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Hooks;

[HarmonyPatch]
public class DeathEventListenerSystem_Patch {
    [HarmonyPatch(typeof(DeathEventListenerSystem), "OnUpdate")]
    public static void Postfix(DeathEventListenerSystem __instance) {
        NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);
        foreach (DeathEvent ev in deathEvents) {
            DebugTool.LogEntity(ev.Died, "Death Event occured for:", LogSystem.Death);

            //-- Player Creature Kill Tracking
            var killer = ev.Killer;
            
            //-- Check if victim is a minion
            var ignoreAsKill = false;
            if (__instance.EntityManager.HasComponent<Minion>(ev.Died)) {
                Plugin.Log(LogSystem.Death, LogLevel.Info, "Minion killed, ignoring as kill");
                ignoreAsKill = true;
            }
            
            //-- Check victim has a level
            if (!__instance.EntityManager.HasComponent<UnitLevel>(ev.Died)) {
                Plugin.Log(LogSystem.Death, LogLevel.Info, "Has no level, ignoring as kill");
                ignoreAsKill = true;
            }

            if (!__instance.EntityManager.HasComponent<Movement>(ev.Died))
            {
                Plugin.Log(LogSystem.Death, LogLevel.Info, "Entity doesn't move, ignoring as kill");
                ignoreAsKill = true;
            }

            // If the entity killing is a minion, switch the killer to the owner of the minion.
            if (__instance.EntityManager.HasComponent<Minion>(killer))
            {
                Plugin.Log(LogSystem.Death, LogLevel.Info, $"Minion killed entity. Getting owner...");
                if (__instance.EntityManager.TryGetComponentData<EntityOwner>(killer, out var entityOwner))
                {
                    killer = entityOwner.Owner;
                    Plugin.Log(LogSystem.Death, LogLevel.Info, $"Owner found, switching killer to owner.");
                }
            }
            Plugin.Log(LogSystem.Death, LogLevel.Info, () => $"[{ev.Source},{ev.Killer},{ev.Died}] => [{DebugTool.GetPrefabName(ev.Source)},{DebugTool.GetPrefabName(ev.Killer)},{DebugTool.GetPrefabName(ev.Died)}]");
            
            // If the killer is the victim, then we can skip trying to add xp, heat, mastery, bloodline.
            if (!ignoreAsKill && !killer.Equals(ev.Died))
            {
                if (__instance.EntityManager.HasComponent<PlayerCharacter>(killer))
                {
                    Plugin.Log(LogSystem.Death, LogLevel.Info,
                        $"Killer ({killer}) is a player, running xp and heat and the like");

                    if (Plugin.ExperienceSystemActive || Plugin.WantedSystemActive)
                    {
                        var isVBlood = Plugin.Server.EntityManager.TryGetComponentData(ev.Died, out BloodConsumeSource victimBlood) && Helper.IsVBlood(victimBlood);

                        var useGroup = ExperienceSystem.GroupMaxDistance > 0;

                        var triggerLocation = Plugin.Server.EntityManager.GetComponentData<LocalToWorld>(ev.Died);
                        var closeAllies = Alliance.GetClosePlayers(
                            triggerLocation.Position, killer, ExperienceSystem.GroupMaxDistance, true, useGroup,
                            LogSystem.Death);

                        // If you get experience for the kill, you get heat for the kill
                        if (Plugin.ExperienceSystemActive) ExperienceSystem.ExpMonitor(closeAllies, ev.Died, isVBlood);
                        if (Plugin.WantedSystemActive) WantedSystem.PlayerKillEntity(closeAllies, ev.Died, isVBlood);
                    }

                    if (Plugin.WeaponMasterySystemActive) WeaponMasterySystem.UpdateMastery(killer, ev.Died);
                    if (Plugin.BloodlineSystemActive) BloodlineSystem.UpdateBloodline(killer, ev.Died);
                    
                    //-- Random Encounters
                    if (Plugin.RandomEncountersSystemActive && Plugin.IsInitialized)
                    {
                        var userEntity = Plugin.Server.EntityManager.GetComponentData<PlayerCharacter>(killer).UserEntity;
                        var userModel = Plugin.Server.EntityManager.GetComponentData<User>(userEntity);
                        RandomEncountersSystem.ServerEvents_OnDeath(ev, userModel);
                    }
                }
            }
            
            // Player death
            if (__instance.EntityManager.TryGetComponentData<RespawnCharacter>(ev.Died, out var respawnData))
            {
                Plugin.Log(LogSystem.Death, LogLevel.Info, $"the deceased ({ev.Died}) is a player, running xp loss and heat dumping");
                if (Plugin.WantedSystemActive) WantedSystem.PlayerDied(ev.Died);
                if (Plugin.ExperienceSystemActive) ExperienceSystem.DeathXpLoss(ev.Died, respawnData.KillerEntity._Entity);
            }
        }
    }
}