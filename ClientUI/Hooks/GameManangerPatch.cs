using BepInEx.Logging;
using HarmonyLib;
using ProjectM;

namespace ClientUI.Hooks;

public class GameManangerPatch
{
    private static bool hasInitialised = false;
    [HarmonyPatch(typeof (GameDataManager), "OnUpdate")]
    [HarmonyPostfix]
    private static void GameDataManagerOnUpdatePostfix(GameDataManager __instance)
    {
        try
        {
            if (hasInitialised == __instance.GameDataInitialized) return;
            hasInitialised = !hasInitialised;
            if (hasInitialised) Plugin.GameDataOnInitialize(__instance.World);
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Error, ex.ToString());
        }
    }
}