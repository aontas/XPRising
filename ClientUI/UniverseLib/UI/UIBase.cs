using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using PanelManager = ClientUI.UniverseLib.UI.Panels.PanelManager;

namespace ClientUI.UniverseLib.UI;

/// <summary>
/// A simple wrapper to handle a UI created with <see cref="ClientUI.UniverseLib.UI.UniversalUI.RegisterUI"/>.
/// </summary>
public class UIBase
{
    public string ID { get; }
    public GameObject RootObject { get; }
    public RectTransform RootRect { get; }
    public Canvas Canvas { get; }
    public Action UpdateMethod { get; }

    public PanelManager Panels { get; }

    internal static readonly int TOP_SORTORDER = 30000;

    /// <summary>
    /// Whether this UI is currently being displayed or not. Disabled UIs will not receive Update calls.
    /// </summary>
    public bool Enabled
    {
        get => RootObject && RootObject.activeSelf;
        set => UniversalUI.SetUIActive(ID, value);
    }

    public UIBase(string id, Action updateMethod)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Cannot register a UI with a null or empty id!");

        if (UniversalUI.registeredUIs.ContainsKey(id))
            throw new ArgumentException($"A UI with the id '{id}' is already registered!");

        ID = id;
        UpdateMethod = updateMethod;

        RootObject = UIFactory.CreateUIObject($"{id}_Root", UniversalUI.CanvasRoot);
        RootObject.SetActive(false);

        RootRect = RootObject.GetComponent<RectTransform>();

        Canvas = RootObject.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        Canvas.referencePixelsPerUnit = 100;
        Canvas.sortingOrder = TOP_SORTORDER;
        Canvas.overrideSorting = true;

        CanvasScaler scaler = RootObject.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        RootObject.AddComponent<GraphicRaycaster>();

        RectTransform uiRect = RootObject.GetComponent<RectTransform>();
        uiRect.anchorMin = Vector2.zero;
        uiRect.anchorMax = Vector2.one;
        uiRect.pivot = new Vector2(0.5f, 0.5f);

        Panels = CreatePanelManager();

        RootObject.SetActive(true);

        UniversalUI.registeredUIs.Add(id, this);
        UniversalUI.uiBases.Add(this);
    }

    /// <summary>
    /// Can be overridden if you want a different type of PanelManager implementation.
    /// </summary>
    protected virtual PanelManager CreatePanelManager() => new(this);


    /// <summary>
    /// Set this UIBase to be on top of all others.
    /// </summary>
    public void SetOnTop()
    {
        RootObject.transform.SetAsLastSibling();

        foreach (UIBase ui in UniversalUI.uiBases)
        {
            int offset = UniversalUI.CanvasRoot.transform.childCount - ui.RootRect.GetSiblingIndex();
            ui.Canvas.sortingOrder = TOP_SORTORDER - offset;
        }

        // Sort UniversalUI dictionary so update order is correct
        UniversalUI.uiBases.Sort((a, b) => b.RootObject.transform.GetSiblingIndex().CompareTo(a.RootObject.transform.GetSiblingIndex()));
    }

    internal void Update()
    {
        try
        {
            Panels.Update();

            UpdateMethod?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Warning, $"Exception invoking update method for {ID}: {ex}");
        }
    }
}