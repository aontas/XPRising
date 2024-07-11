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
        try
        {
            if (!__instance.GameDataInitialized) return;
            Plugin.GameDataOnInitialize(__instance.World);
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Error, ex.ToString());
        }
    }
}