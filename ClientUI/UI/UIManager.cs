using BepInEx.Logging;
using ClientUI.UI.Panel;
using Il2CppSystem.Text.RegularExpressions;
using UnityEngine;
using UIBase = ClientUI.UniverseLib.UI.UIBase;
using UniversalUI = ClientUI.UniverseLib.UI.UniversalUI;

namespace ClientUI.UI;

public static class UIManager
{
    public enum Panels
    {
        Base,
        Progress,
        Actions,
    }
    
    public static bool IsInitialised { get; private set; }
    
    internal static void Initialize()
    {
        UniversalUI.Init(); //startupDelay, OnInitialized, LogHandler, config);
    }

    public static UIBase UiBase { get; private set; }
    public static GameObject UIRoot => UiBase?.RootObject;
    public static ContentPanel ContentPanel { get; private set; }
    public static ProgressBarPanel ProgressBarPanel { get; private set; }

    public static void OnInitialized()
    {
        UiBase = UniversalUI.RegisterUI(MyPluginInfo.PLUGIN_GUID, UiUpdate);
        
        ContentPanel = new ContentPanel(UiBase);
        ContentPanel.SetActive(false);
        
        ProgressBarPanel = new ProgressBarPanel(UiBase);
        ProgressBarPanel.SetActive(false);
        
        Plugin.LoadUI = true;
        
        SetActive(true);
    }

    public static void SetActive(bool active)
    {
        if (ContentPanel == null) return;
        
        ContentPanel.SetActive(active);
        ProgressBarPanel.SetActive(active);
        
        IsInitialised = true;
    }

    public static void Reset()
    {
        ContentPanel.Reset();
        ProgressBarPanel.Reset();
    }

    private static void UiUpdate()
    {
        // Called once per frame when your UI is being displayed.
    }
}