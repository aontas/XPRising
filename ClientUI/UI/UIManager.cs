using BepInEx.Logging;
using ClientUI.UI.Panel;
using Il2CppSystem.Text.RegularExpressions;
using UnityEngine;
using UIBase = ClientUI.UniverseLib.UI.UIBase;
using UniversalUI = ClientUI.UniverseLib.UI.UniversalUI;

namespace ClientUI.UI;

public static class UIManager
{
    // Resources.GetBuiltinResource<Font>("Arial.ttf");
    public enum Panels
    {
        Base,
        Progress,
        Actions,
    }
    
    public static bool IsInitialised { get; private set; }
    
    internal static void Initialize()
    {
        // const float startupDelay = 3f;
        // UniverseLib.Config.UniverseLibConfig config = new()
        // {
        //     Disable_EventSystem_Override = false, // or null
        //     Force_Unlock_Mouse = false, // or null
        //     Allow_UI_Selection_Outside_UIBase = true,
        //     Unhollowed_Modules_Folder = Path.Combine(BepInEx.Paths.BepInExRootPath, "interop") // or null
        // };
        //
        UniversalUI.Init(); //startupDelay, OnInitialized, LogHandler, config);
    }

    internal static GameObject CanvasRoot;
    private static void CreateRootCanvas()
    {
        CanvasRoot = new GameObject("ClientUICanvas");
        UnityEngine.Object.DontDestroyOnLoad(CanvasRoot);
        CanvasRoot.hideFlags |= HideFlags.HideAndDontSave;
        CanvasRoot.layer = 5;
        CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

        // CanvasRoot.SetActive(false);

        // EventSys = CanvasRoot.AddComponent<EventSystem>();
        // EventSystemHelper.AddUIModule();
        // EventSys.enabled = false;

        CanvasRoot.SetActive(true);
    }

    private const string ID = "UI";
    private const int TOP_SORTORDER = 30000;
    private static GameObject RootObject;
    private static RectTransform RootRect;
    private static Canvas Canvas;
    // private static GameObject MakeRoot()
    // {
    //     RootObject = UIFacts.CreateUIObject($"{ID}_Root", CanvasRoot);
    //     RootObject.SetActive(false);
    //
    //     RootRect = RootObject.GetComponent<RectTransform>();
    //
    //     Canvas = RootObject.AddComponent<Canvas>();
    //     Canvas.renderMode = RenderMode.ScreenSpaceCamera;
    //     Canvas.referencePixelsPerUnit = 100;
    //     Canvas.sortingOrder = TOP_SORTORDER;
    //     Canvas.overrideSorting = true;
    //
    //     var scaler = RootObject.AddComponent<CanvasScaler>();
    //     scaler.referenceResolution = new Vector2(1920, 1080);
    //     scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    //
    //     RootObject.AddComponent<GraphicRaycaster>();
    //
    //     var uiRect = RootObject.GetComponent<RectTransform>();
    //     uiRect.anchorMin = Vector2.zero;
    //     uiRect.anchorMax = Vector2.one;
    //     uiRect.pivot = new Vector2(0.5f, 0.5f);
    //
    //     RootObject.SetActive(true);
    //     return RootObject;
    // }

    public static UIBase UiBase { get; private set; }
    public static GameObject UIRoot => UiBase?.RootObject;
    public static ContentPanel ContentPanel { get; private set; }
    public static ProgressBarPanel ProgressBarPanel { get; private set; }
    public static ButtonPanel ButtonPanel { get; private set; }

    public static void OnInitialized()
    {
        UiBase = UniversalUI.RegisterUI(MyPluginInfo.PLUGIN_GUID, UiUpdate);
        
        ContentPanel = new ContentPanel(UiBase);
        ContentPanel.SetActive(false);
        
        ProgressBarPanel = new ProgressBarPanel(UiBase);
        ProgressBarPanel.SetActive(false);
        
        // ButtonPanel = new ButtonPanel(UiBase);
        // ButtonPanel.SetActive(false);
        Plugin.LoadUI = true;
        
        SetActive(true);
    }

    public static void DoThing()
    {
        // CreateRootCanvas();
        // var root = MakeRoot();
        // var thing = UIFacts.CreateButton(root, "test name", "some text", Color.grey);
        // UIFactory.SetLayoutElement(thing.GameObject, ignoreLayout: true);
        // thing.ButtonText.fontSize = 10;
        // thing.Transform.Translate(Vector3.right * 100);
        // thing.Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100);
        // thing.Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30);
        // thing.OnClick = () =>
        // {
        //     Plugin.Log(LogLevel.Info, "clicked the button");
        // };

        //ContentPanel.DoThing();

        //UniversalUI.CanvasRoot.SetActive(false);
    }

    public static void SetActive(bool active)
    {
        if (ContentPanel == null) return;
        
        ContentPanel.SetActive(active);
        ProgressBarPanel.SetActive(active);
        // ButtonPanel.SetActive(active);
        
        IsInitialised = true;
    }

    public static void Reset()
    {
        ContentPanel.Reset();
        ProgressBarPanel.Reset();
        // ButtonPanel.Reset();
    }

    private static void UiUpdate()
    {
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