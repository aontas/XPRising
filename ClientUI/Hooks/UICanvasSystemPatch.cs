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
        hudEnabled = canvas.CharacterHUDs.gameObject.active;
        UIManager.SetActive(hudEnabled);
        Plugin.Log(LogLevel.Error, $"UICanvasSystem: [UpdateHideIfDisabled] enabled: {hudEnabled}");
    }
}