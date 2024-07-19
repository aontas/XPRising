using BepInEx.Logging;
using ClientUI.UI;
using HarmonyLib;
using ProjectM.UI;

namespace ClientUI.Hooks;

public static class UICanvasSystemPatch
{
    private static bool hudEnabled = false;
    [HarmonyPatch(typeof (UICanvasSystem), "UpdateHideIfDisabled")]
    [HarmonyPostfix]
    private static void UICanvasSystemPostfix(UICanvasBase canvas)
    {
        if (canvas.CharacterHUDs.gameObject.active == hudEnabled) return;
        
        Plugin.Log(LogLevel.Warning, $"UICanvasSystemPostfix: {UIManager.IsInitialised}");
        if (!UIManager.IsInitialised) return;
        
        hudEnabled = canvas.CharacterHUDs.gameObject.active;
        UIManager.SetActive(hudEnabled);
    }
    
    [HarmonyPatch(typeof (EscapeMenuView), "OnDestroy")]
    [HarmonyPrefix]
    private static void EscapeMenuViewOnDestroyPrefix()
    {
        Plugin.Log(LogLevel.Warning, "EscapeMenuViewOnDestroyPrefix");
        hudEnabled = false;
        UIManager.SetActive(hudEnabled);
        
        // User has left the server. Reset all ui as the next server might be a different one
        UIManager.Reset();
    }
}