using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ClientUI.Hooks;
using ClientUI.UI;
using HarmonyLib;
using Unity.Entities;
using XPShared;
using XPShared.Services;
using XPShared.Transport.Messages;
using GameManangerPatch = ClientUI.Hooks.GameManangerPatch;

namespace ClientUI
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("XPRising.XPShared")]
    public class Plugin : BasePlugin
    {
        private static ManualLogSource _logger;
        internal static Plugin Instance { get; private set; }
        internal static bool LoadUI = false;
        
        private static FrameTimer _uiInitialisedTimer = new();
        private static FrameTimer _connectUiTimer;
        private static Harmony _harmonyBootPatch;
        private static Harmony _harmonyCanvasPatch;
        private static Harmony _harmonyMenuPatch;
        internal static Harmony _harmonyVersionStringPatch;

        public override void Load()
        {
            Instance = this;

            // Ensure the logger is accessible in static contexts.
            _logger = base.Log;
            
            if (XPShared.Plugin.IsServer)
            {
                Log(LogLevel.Warning, $"Plugin {MyPluginInfo.PLUGIN_GUID} is a client plugin only. Not continuing to load on server.");
                return;
            }
            
            // GameData.OnInitialize += GameDataOnInitialize;
            // GameData.OnDestroy += GameDataOnDestroy;

            UIManager.Initialize();
            
            _harmonyBootPatch = Harmony.CreateAndPatchAll(typeof(GameManangerPatch));
            _harmonyMenuPatch = Harmony.CreateAndPatchAll(typeof(EscapeMenuPatch));
            _harmonyCanvasPatch = Harmony.CreateAndPatchAll(typeof(UICanvasSystemPatch));
            _harmonyVersionStringPatch = Harmony.CreateAndPatchAll(typeof(VersionStringPatch));
            
            // Timer for initialising our client connection. This will be started as part of our UI initialisation timer.
            _connectUiTimer = new FrameTimer();
            _connectUiTimer.Initialise(() =>
                {
                    ChatUtils.SendInitialisation();
                    Log(LogLevel.Info, $"Sending client initialisation...");
                },
                TimeSpan.FromSeconds(1),
                5);
                
            // Register all the messages we want to support
            RegisterMessages();
            
            // Don't use this when connecting to a game as it has extra elements to test features in the UI.
            // AddTestUI();

            Log(LogLevel.Info, $"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
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
            if (XPShared.Plugin.IsClient)
            {
                _uiInitialisedTimer.Initialise(() =>
                    {
                        if (!UIManager.IsInitialised) UIManager.OnInitialized();
                        _connectUiTimer.Start();
                    },
                    TimeSpan.FromSeconds(5),
                    1).Start();
            }
        }

        private static void GameDataOnDestroy()
        {
            //Logger.LogInfo("GameDataOnDestroy");
        }

        private static void RegisterMessages()
        {
            ChatService.RegisterType<ProgressSerialisedMessage>((message, steamId) =>
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
            ChatService.RegisterType<ActionSerialisedMessage>((message, steamId) =>
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
            ChatService.RegisterType<NotificationMessage>((message, steamId) =>
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
            ChatService.RegisterType<ConnectedMessage>((message, steamId) =>
            {
                // We have received acknowledgement that we have connected. We can stop trying to connect now.
                _connectUiTimer.Stop();
                Log(LogLevel.Info, $"Client initialisation successful");
            });
        }
        
        public new static void Log(LogLevel level, string message)
        {
            _logger.Log(level, $"{DateTime.Now:u}: {message}");
        }
        
        // The following variables and function can be used to test out features of the UI.
        private const string TurboColourMap = "@#30123B@#445ACD@#3E9BFE@#18D6CB@#46F783@#A2FC3C@#E1DC37@#FDA531@#EF5A11@#C42502@#7A0402";
        private float _testValue = 0;
        private FrameTimer _testTimer1;
        private FrameTimer _testTimer2;
        private bool _buttonEnabled = false;
        private int _testValue2 = 0;
        private void AddTestUI()
        {
            UIManager.OnInitialized();
            _testTimer1 = new FrameTimer();
            _testTimer2 = new FrameTimer();
            UIManager.ContentPanel.ChangeProgress(new ProgressSerialisedMessage()
            {
                Group = "Test1",
                Label = "TEST BAR",
                Colour = "red",
                Active = ProgressSerialisedMessage.ActiveState.Active,
                Change = "",
                Header = "1X",
                ProgressPercentage = 0.45f,
                Tooltip = "Test Progress",
            });
            UIManager.ContentPanel.ChangeProgress(new ProgressSerialisedMessage()
            {
                Group = "Test",
                Label = "TEST BAR 2",
                Colour = TurboColourMap,
                Active = ProgressSerialisedMessage.ActiveState.Active,
                Change = "",
                Header = "00",
                ProgressPercentage = 0,
                Tooltip = "Test Progress 2",
                Flash = false
            });
            UIManager.ContentPanel.SetButton(new ActionSerialisedMessage()
            {
                Group = "Test",
                ID = "TestButton2",
                Label = "Test disabled",
                Enabled = _buttonEnabled
            }, () => {});
            UIManager.ContentPanel.SetButton(new ActionSerialisedMessage()
            {
                Group = "Test",
                ID = "TestButton",
                Label = "Test colour and enable",
                Colour = "blue",
                Enabled = true
            }, () =>
            {
                _buttonEnabled = !_buttonEnabled;
                UIManager.ContentPanel.SetButton(new ActionSerialisedMessage()
                {
                    Group = "Test",
                    ID = "TestButton2",
                    Label = _buttonEnabled ? "Test enabled" : "Test disabled",
                    Enabled = _buttonEnabled
                });
            });

            _testTimer1.Initialise(() =>
                {
                    var increment = 0.0125f;
                    bool flash = _testValue % 1 > (_testValue + increment) % 1 && _testValue % 2 < 1;
                    _testValue = (_testValue + increment) % 100.0f;
                    UIManager.ContentPanel.ChangeProgress(new ProgressSerialisedMessage()
                    {
                        Group = "Test",
                        Label = "TEST BAR 2",
                        Colour = TurboColourMap,
                        Active = ProgressSerialisedMessage.ActiveState.Active,
                        Change = "whoop",
                        Header = $"{(int)_testValue:D2}",
                        ProgressPercentage = _testValue % 1.0f,
                        Tooltip = "Test Progress 2",
                        Flash = flash
                    });
                },
                TimeSpan.FromMilliseconds(50),
                -1).Start();
            
            _testTimer2.Initialise(() =>
                {
                    UIManager.ContentPanel.AddMessage(new NotificationMessage()
                    {
                        ID = "testMessage",
                        Message = $"{DateTime.Now:u}: This is a test message",
                        Severity = LogLevel.Warning
                    });
                    _testValue2++;
                    if (_testValue2 % 3 == 0)
                    {
                        _testValue2 = 0;
                        UIManager.TextPanel.AddText($"{DateTime.Now:u}: This is a bunch of text to be added to the scrollable panel.\n\n");
                    }
                },
                TimeSpan.FromSeconds(3),
                -1).Start();
            UIManager.ContentPanel.ChangeProgress(new ProgressSerialisedMessage()
            {
                Group = "Test",
                Label = "TEST BAR 3",
                Colour = "white",
                Active = ProgressSerialisedMessage.ActiveState.Active,
                Change = "",
                Header = "Xx",
                ProgressPercentage = 0.65f,
                Tooltip = "Test text colour",
            });
            
            UIManager.TextPanel.SetText("Test text panel", "This is some multiline text and this text should be able to be scrollable.\n\nThis is some more text\n\n");
            
            UIManager.ContentPanel.SetButton(new ActionSerialisedMessage()
            {
                Group = "Test",
                ID = "TestButton3",
                Label = "Show test text panel",
                Colour = "white",
                Enabled = true
            }, () =>
            {
                UIManager.TextPanel.SetText("Test text panel", "Restarting the multiline, scrollable, test text panel.\n\n");
            });
        }
    }
}