using ClientUI.UI;
using HarmonyLib;
using ProjectM.UI;
using XPShared.Hooks;

namespace ClientUI.Hooks;

public static class MainMenuNewViewPatch
{
    // This gets called when reaching the main menu. Most players will hit this if they leave a server, though it might
    // be possible to skip it with console commands. This is good enough for now.
    [HarmonyPatch(typeof(MainMenuNewView), nameof(MainMenuNewView.SetConsoleReady))]
    [HarmonyPostfix]
    private static void SetConsoleReadyPostfix()
    {
        if (!UIManager.IsInitialised) return;
        
        // User has left the server. Reset all ui as the next server might be a different one
        ClientChatPatch.ResetUser();
        UIManager.Reset();
    }
}