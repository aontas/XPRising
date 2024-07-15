using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using ClientUI.Hooks;
using ClientUI.UI;
using ClientUI.UI.Panel;
using HarmonyLib;
using Unity.Entities;
using XPShared;
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
                    var psmData = MessageRegistry.DeserialiseMessage<ProgressSerialisedMessage>(message);
                    Log(LogLevel.Debug, $"Got {psmData.Label} message. Instance valid?: {ProgressBarPanel.Instance != null}");
                    if (ProgressBarPanel.Instance != null)
                    {
                        ProgressBarPanel.Instance.ChangeProgress(psmData);
                    }
                    break;
                case MessageRegistry.MessageTypes.ActionSerialisedMessage:
                    var asmData = MessageRegistry.DeserialiseMessage<ActionSerialisedMessage>(message);
                    Log(LogLevel.Debug, $"Got {asmData.Label} message. Instance valid?: {ButtonPanel.Instance != null}");
                    if (ButtonPanel.Instance != null)
                    {
                        ButtonPanel.Instance.SetButton(asmData);
                    }
                    break;
                case MessageRegistry.MessageTypes.Unknown:
                default:
                    return;
            }
            
            if (LoadUI)
            {
                UIManager.ActivateUI();
                LoadUI = false;
            }
        }

        public static void GameDataOnInitialize(World world)
        {
            if (VWorld.IsClient)
            {
                _timer = new FrameTimer();

                _timer.Initialise(() =>
                {
                    Log(LogLevel.Info, "Starting UI...");
                    MessageHandler.ClientSendToServer(Utils.UserConnectAction());
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