using System;
using BepInEx.Logging;
using HarmonyLib;
using ProjectM;

namespace ClientUI.Hooks;

public class GameManangerPatch
{
    [HarmonyPatch(typeof (GameDataManager), "OnUpdate")]
    [HarmonyPostfix]
    private static void GameDataManagerOnUpdatePostfix(GameDataManager __instance)
    {
        // if (ClientEvents._onGameDataInitializedTriggered)
        //     return;
        try
        {
            if (!__instance.GameDataInitialized)
                return;
            // ClientEvents._onGameDataInitializedTriggered = true;
            Plugin.GameDataOnInitialize(__instance.World);
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Error, ex.ToString());
        }
    }
}