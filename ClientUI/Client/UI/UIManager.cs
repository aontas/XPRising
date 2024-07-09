using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UniverseLib.UI;
using UniverseLib;
using ClientUI.Client.UI.Panel;
using System.IO;
using BepInEx.Logging;
using ClientUI.Transport.Handlers;
using Il2CppSystem.Text.RegularExpressions;
using UnityEngine.UI;

namespace ClientUI.Client.UI
{
    internal class UIManager
    {
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
            ClientMessageActions.loadUI = true;
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
}
