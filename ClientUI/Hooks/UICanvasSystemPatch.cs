using BepInEx.Logging;
using ClientUI.UI;
using ClientUI.UI.Util;
using ClientUI.UniverseLib.UI;
using HarmonyLib;
using ProjectM.UI;
using TMPro;

namespace ClientUI.Hooks;

public static class UICanvasSystemPatch
{
    private static bool hudEnabled = false;
    [HarmonyPatch(typeof (UICanvasSystem), "UpdateHideIfDisabled")]
    [HarmonyPostfix]
    private static void UICanvasSystemPostfix(UICanvasBase canvas)
    {
        if (!UIFactory.PlayerHUDCanvas)
        {
            UIFactory.PlayerHUDCanvas = canvas.CharacterHUDs.gameObject;
        }
        
        if (!UIManager.IsInitialised || canvas.CharacterHUDs.gameObject.active == hudEnabled) return;
        
        hudEnabled = canvas.CharacterHUDs.gameObject.active;
        UIManager.SetActive(hudEnabled);
    }
}