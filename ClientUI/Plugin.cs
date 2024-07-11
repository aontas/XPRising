using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using ClientUI.Client;
using ClientUI.Hooks;
using ClientUI.UI;
using ClientUI.UI.Panel;
using HarmonyLib;
using Unity.Entities;
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
        
        private static TimerClient _timer;
        private static Harmony _harmonyBootPatch;

        public override void Load()
        {
            Instance = this;

            // Ensure the logger is accessible in static contexts.
            _logger = base.Log;
            
            // GameData.OnInitialize += GameDataOnInitialize;
            // GameData.OnDestroy += GameDataOnDestroy;

            UIManager.Initialize();
            
            _harmonyBootPatch = Harmony.CreateAndPatchAll(typeof(GameManangerPatch));

            MessageHandler.OnClientMessageEvent += ReceivedMessage;

            Plugin.Log(LogLevel.Info, $"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        public override bool Unload()
        {
            // GameData.OnDestroy -= GameDataOnDestroy;
            // GameData.OnInitialize -= GameDataOnInitialize;
            
            MessageHandler.OnClientMessageEvent -= ReceivedMessage;
            _harmonyBootPatch.UnpatchSelf();
            
            return true;
        }

        public static void ReceivedMessage(MessageRegistry.MessageTypes type, string message)
        {
            switch (type)
            {
                case MessageRegistry.MessageTypes.ProgressSerialisedMessage:
                    var data = MessageRegistry.DeserialiseMessage<ProgressSerialisedMessage>(message);
                    Log(LogLevel.Debug, $"Got {data.Label} message. Instance valid?: {ProgressPanelBase.Instance != null}");
                    if (ProgressPanelBase.Instance != null)
                    {
                        ProgressPanelBase.Instance.ChangeProgress(data.Label, data.Level, data.ProgressPercentage, data.Tooltip);
                    }
                    break;
                case MessageRegistry.MessageTypes.Unknown:
                default:
                    return;
            }
            
            if (LoadUI)
            {
                UIManager.ProgressPanel.SetActive(true);
                LoadUI = false;
            }
        }

        public static void GameDataOnInitialize(World world)
        {
            if (VWorld.IsClient)
            {
                _timer = new TimerClient();

                _timer.Start(
                world =>
                {
                    Plugin.Log(LogLevel.Info, "Starting UI...");
                    MessageHandler.ClientSendToServer(Utils.UserConnectAction());
                    _timer.Stop();
                },
                input =>
                {
                    if (input is not int secondAutoUIr)
                    {
                        Plugin.Log(LogLevel.Error, "Starting UI timer delay function parameter is not a valid integer");
                        return TimeSpan.MaxValue;
                    }

                    var seconds = 5;
                    Plugin.Log(LogLevel.Info, $"Next Starting UI will start in {seconds} seconds.");
                    return TimeSpan.FromSeconds(seconds);
                });
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