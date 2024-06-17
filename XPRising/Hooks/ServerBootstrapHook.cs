using System;
using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using Unity.Entities;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Hooks
{
    [HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.VerifyServerGameSettings))]
    public class ServerGameSetting_Patch
    {
        public static void Postfix()
        {
        }
    }

    [HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.Start))]
    public static class GameBootstrap_Patch
    {
        public static void Postfix()
        {
        }
    }

    [HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.OnApplicationQuit))]
    public static class GameBootstrapQuit_Patch
    {
        // This needs to be Postfix so that OnUserDisconnected has a chance to work before the database is saved.
        public static void Postfix()
        {
            // Save before we quit the server
            AutoSaveSystem.SaveDatabase(true, false);
            RandomEncounters.Unload();
        }
    }
    
    [HarmonyPatch(typeof(LoadPersistenceSystemV2), nameof(LoadPersistenceSystemV2.SetLoadState))]
    public static class LoadPersistenceSystem_Patch
    {
        public static void Postfix(ServerStartupState.State loadState, LoadPersistenceSystemV2 __instance)
        {
            try
            {
                if (loadState == ServerStartupState.State.SuccessfulStartup)
                {
                    //OnGameDataInitialized?.Invoke(__instance.World);
                    Plugin.Initialize();
                }
            }
            catch (Exception ex)
            {
                if (Plugin.IsDebug)
                {
                    Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, ex.ToString, true);
                }
                else
                {
                    Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, ex.Message, true);
                }
                
            }
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    public static class OnUserConnected_Patch
    {
        public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
        {
            try
            {
                var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
                var serverClient = __instance._ApprovedUsersLookup[userIndex];
                var userEntity = serverClient.UserEntity;
                var userData = __instance.EntityManager.GetComponentData<User>(userEntity);
                bool isNewVampire = userData.CharacterName.IsEmpty;

                if (!isNewVampire)
                {
                    PlayerCache.PlayerOnline(userEntity, userData);
                    if ((WeaponMasterySystem.IsDecaySystemEnabled && Plugin.WeaponMasterySystemActive) ||
                        BloodlineSystem.IsDecaySystemEnabled && Plugin.BloodlineSystemActive)
                    {
                        if (Database.PlayerLogout.TryGetValue(userData.PlatformId, out var playerLogout))
                        {
                            GlobalMasterySystem.DecayMastery(userEntity, playerLogout);
                        }
                    }

                    if (Plugin.ExperienceSystemActive)
                    {
                        // Enforce gear level changes on log in
                        FixEquipmentLevel(__instance.EntityManager, userData.LocalCharacter._Entity);

                        ExperienceSystem.CheckAndApplyLevel(userData.LocalCharacter._Entity, userEntity, userData.PlatformId);
                    }
                    else
                    {
                        // We don't need to do this when ExperienceSystemActive, as ApplyLevel() does it as well.
                        Helper.ApplyBuff(userEntity, userData.LocalCharacter._Entity, Helper.AppliedBuff);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"Failed OnUserConnected_Patch: {e.Message}", true);
            }
        }

        private static void FixEquipmentLevel(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.TryGetBuffer<BuffBuffer>(entity, out var buffer))
            {
                DebugTool.LogEntity(entity, "Failed to get BuffBuffer from entity:", Plugin.LogSystem.Core);
                return;
            }

            foreach (var buff in buffer)
            {
                if (entityManager.TryGetComponentData<ArmorLevel>(buff.Entity, out var armorLevel))
                {
                    armorLevel.Level = 0;
                    entityManager.SetComponentData(buff.Entity, armorLevel);
                }
                if (entityManager.TryGetComponentData<WeaponLevel>(buff.Entity, out var weaponLevel))
                {
                    weaponLevel.Level = 0;
                    entityManager.SetComponentData(buff.Entity, weaponLevel);
                }
                if (entityManager.TryGetComponentData<SpellLevel>(buff.Entity, out var spellLevel))
                {
                    spellLevel.Level = 0;
                    entityManager.SetComponentData(buff.Entity, spellLevel);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    public static class OnUserDisconnected_Patch
    {
        private static void Prefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId, ConnectionStatusChangeReason connectionStatusReason, string extraData)
        {
            try
            {
                var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
                var serverClient = __instance._ApprovedUsersLookup[userIndex];

                PlayerCache.PlayerOffline(serverClient.PlatformId);
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"OnUserDisconnected failed: {e.Message}", true);
            }
        }
    }
}