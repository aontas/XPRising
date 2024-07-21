using ClientUI.UI;
using HarmonyLib;
using ProjectM.UI;

namespace ClientUI.Hooks;

public class EscapeMenuPatch
{
    [HarmonyPatch(typeof (EscapeMenuView), "OnDestroy")]
    [HarmonyPrefix]
    private static void EscapeMenuViewOnDestroyPrefix()
    {
        if (!UIManager.IsInitialised) return;
        
        // User has left the server. Reset all ui as the next server might be a different one
        UIManager.Reset();
    }
}