using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using ClientUI.Hooks;
using ClientUI.UI;
using HarmonyLib;
using Unity.Entities;
using XPShared;
using XPShared.BloodstoneExtensions;
using XPShared.Transport;
using XPShared.Transport.Messages;

namespace ClientUI
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.Bloodstone")]
    [BepInDependency("XPRising.XPShared")]
    public class Plugin : BasePlugin
    {
        private static ManualLogSource _logger;
        internal static Plugin Instance { get; private set; }
        internal static bool LoadUI = false;
        
        private static XPShared.FrameTimer _timer;
        private static Harmony _harmonyBootPatch;
        private static Harmony _harmonyCanvasPatch;
        private static Harmony _harmonyMenuPatch;
        internal static Harmony _harmonyVersionStringPatch;

        public override void Load()
        {
            Instance = this;

            // Ensure the logger is accessible in static contexts.
            _logger = base.Log;
            
            if (VWorld.IsServer)
            {
                Plugin.Log(LogLevel.Warning, $"Plugin {MyPluginInfo.PLUGIN_GUID} is a client plugin only. Not continuing to load on server.");
                return;
            }
            
            // GameData.OnInitialize += GameDataOnInitialize;
            // GameData.OnDestroy += GameDataOnDestroy;

            UIManager.Initialize();
            
            _harmonyBootPatch = Harmony.CreateAndPatchAll(typeof(GameManangerPatch));
            _harmonyMenuPatch = Harmony.CreateAndPatchAll(typeof(EscapeMenuPatch));
            _harmonyCanvasPatch = Harmony.CreateAndPatchAll(typeof(UICanvasSystemPatch));
            _harmonyVersionStringPatch = Harmony.CreateAndPatchAll(typeof(VersionStringPatch));
            
            MessageUtils.RegisterType<ProgressSerialisedMessage>(message =>
            {
                if (UIManager.ContentPanel != null)
                {
                    UIManager.ContentPanel.ChangeProgress(message);
                }
                if (LoadUI && UIManager.ContentPanel != null)
                {
                    UIManager.SetActive(true);
                    LoadUI = false;
                }
            });
            MessageUtils.RegisterType<ActionSerialisedMessage>(message =>
            {
                if (UIManager.ContentPanel != null)
                {
                    UIManager.ContentPanel.SetButton(message);
                }
                if (LoadUI && UIManager.ContentPanel != null)
                {
                    UIManager.SetActive(true);
                    LoadUI = false;
                }
            });
            MessageUtils.RegisterType<NotificationMessage>(message =>
            {
                if (UIManager.ContentPanel != null)
                {
                    UIManager.ContentPanel.AddMessage(message);
                }
                if (LoadUI && UIManager.ContentPanel != null)
                {
                    UIManager.SetActive(true);
                    LoadUI = false;
                }
            });

            Plugin.Log(LogLevel.Info, $"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        public override bool Unload()
        {
            // GameData.OnDestroy -= GameDataOnDestroy;
            // GameData.OnInitialize -= GameDataOnInitialize;
            
            _harmonyBootPatch.UnpatchSelf();
            _harmonyCanvasPatch.UnpatchSelf();
            _harmonyMenuPatch.UnpatchSelf();
            _harmonyVersionStringPatch.UnpatchSelf();
            
            return true;
        }

        public static void GameDataOnInitialize(World world)
        {
            if (VWorld.IsClient)
            {
                _timer = new FrameTimer();

                _timer.Initialise(() =>
                {
                    UIManager.OnInitialized();
                    Utils.SendClientInitialisation();
                    _timer.Stop();
                },
                TimeSpan.FromSeconds(5),
                true).Start();
            }
        }

        private static void GameDataOnDestroy()
        {
            //Logger.LogInfo("GameDataOnDestroy");
        }
        
        public new static void Log(LogLevel level, string message)
        {
            _logger.Log(level, $"{DateTime.Now:u}: {message}");
        }
    }
}