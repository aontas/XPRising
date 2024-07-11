using BepInEx.Logging;
using ClientUI.UI.Panel;
using Il2CppSystem.Text.RegularExpressions;
using UnityEngine;
using UniverseLib;
using UniverseLib.UI;

namespace ClientUI.UI;

public static class UIManager
{
    public enum Panels
    {
        Progress,
    }
    
    internal static void Initialize()
    {
        const float startupDelay = 3f;
        UniverseLib.Config.UniverseLibConfig config = new()
        {
            Disable_EventSystem_Override = false, // or null
            Force_Unlock_Mouse = false, // or null
            Allow_UI_Selection_Outside_UIBase = true,
            Unhollowed_Modules_Folder = Path.Combine(BepInEx.Paths.BepInExRootPath, "interop") // or null
        };

        Universe.Init(startupDelay, OnInitialized, LogHandler, config);
    }

    public static UIBase UiBase { get; private set; }
    public static GameObject UIRoot => UiBase?.RootObject;
    public static ProgressPanelBase ProgressPanel { get; private set; }

    static void OnInitialized()
    {
        UiBase = UniversalUI.RegisterUI(MyPluginInfo.PLUGIN_GUID, UiUpdate);

        ProgressPanel = new ProgressPanelBase(UiBase);
        ProgressPanel.SetActive(false);
        Plugin.LoadUI = true;
    }

    private static void UiUpdate()
    {
        //XPPanel.changeProgress();
        // Called once per frame when your UI is being displayed.
    }
        
    private const string TypeErrorRegex = @"Can't cache type named (.+) Error: .+'(.+)' from assembly[\s\S]+";
    private const string TypeErrorReplace = "Type not found: $1 from ($2)";

    private static void LogHandler(string message, LogType type)
    {
        var logLevel = type switch
        {
            LogType.Error => LogLevel.Error,
            LogType.Assert or LogType.Exception => LogLevel.Fatal,
            LogType.Warning => LogLevel.Warning,
            LogType.Log => LogLevel.Message,
            _ => LogLevel.Info
        };
            
        Plugin.Log(logLevel, Regex.Replace(message, TypeErrorRegex, TypeErrorReplace));
    }
}