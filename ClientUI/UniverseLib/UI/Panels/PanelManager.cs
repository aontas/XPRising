#nullable enable

using BepInEx.Logging;
using ClientUI.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientUI.UniverseLib.UI.Panels;

/// <summary>
/// Handles updating, dragging and resizing all <see cref="ClientUI.UniverseLib.UI.Panels.PanelBase"/>s for the parent <see cref="UIBase"/>.
/// </summary>
public class PanelManager
{
    #region STATIC

    /// <summary>Is a panel currently being resized?</summary>
    public static bool Resizing { get; internal set; }

    /// <summary>Is the resize cursor being displayed?</summary>
    public static bool ResizePrompting =>
        resizeCursor != null &&
        resizeCursorUIBase != null &&
        resizeCursorUIBase.Enabled;

    protected internal static readonly List<PanelDragger> allDraggers = new();

    protected internal static UIBase? resizeCursorUIBase;
    protected internal static GameObject? resizeCursor;

    protected internal static bool focusHandledThisFrame;
    protected internal static bool draggerHandledThisFrame;
    protected internal static bool wasAnyDragging;

    /// <summary>Force any current Resizing to immediately end.</summary>
    public static void ForceEndResize()
    {
        if (resizeCursor == null || resizeCursorUIBase == null)
            return;

        resizeCursorUIBase.Enabled = false;
        resizeCursor.SetActive(false);
        wasAnyDragging = false;
        Resizing = false;

        foreach (PanelDragger instance in allDraggers)
        {
            instance.WasDragging = false;
            instance.WasResizing = false;
        }
    }

    protected static void CreateResizeCursorUI()
    {
        try
        {
            resizeCursorUIBase = UniversalUI.RegisterUI($"{MyPluginInfo.PLUGIN_GUID}.resizeCursor", null);
            GameObject parent = resizeCursorUIBase.RootObject;
            parent.transform.SetParent(UniversalUI.CanvasRoot.transform);

            TextMeshProUGUI text = UIFactory.CreateLabel(parent, "ResizeCursor", "↔", TextAlignmentOptions.Center, Color.white, 35);
            resizeCursor = text.gameObject;

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new(1, 1);

            RectTransform rect = resizeCursor.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);

            resizeCursorUIBase.Enabled = false;
        }
        catch (Exception e)
        {
            Plugin.Log(LogLevel.Warning, "Exception creating Resize Cursor UI!\r\n" + e);
        }
    }

    #endregion

    /// <summary>The UIBase which created this PanelManager.</summary>
    public UIBase Owner { get; }

    /// <summary>The GameObject which holds all of this PanelManager's Panels.</summary>
    public GameObject PanelHolder { get; }

    /// <summary>Invoked when the UIPanel heirarchy is reordered.</summary>
    public event Action? OnPanelsReordered;

    /// <summary>Invoked when the user clicks outside of all panels.</summary>
    public event Action? OnClickedOutsidePanels;

    protected readonly List<PanelBase> panelInstances = new();
    protected readonly Dictionary<int, PanelBase> transformIDToUIPanel = new();
    protected readonly List<PanelDragger> draggerInstances = new();

    public PanelManager(UIBase owner)
    {
        Owner = owner;
        PanelHolder = UIFactory.CreateUIObject("PanelHolder", owner.RootObject);
        RectTransform rect = PanelHolder.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
    }

    /// <summary>
    /// Determines if the PanelManager should update "focus" (ie. heirarchy order).
    /// By default, returns true if user is clicking.
    /// </summary>
    protected virtual bool ShouldUpdateFocus
    {
        get => MouseInTargetDisplay && (InputManager.Mouse.Button0 == MouseState.ButtonState.Down || InputManager.Mouse.Button1 == MouseState.ButtonState.Down);
    }

    /// <summary>
    /// The MousePosition which should be used for this PanelManager. By default, this is <see cref="InputManager.Mouse.Position"/>.
    /// </summary>
    protected internal virtual Vector3 MousePosition
    {
        get => InputManager.Mouse.Position;
    }

    /// <summary>
    /// The Screen dimensions which should be used for this PanelManager. By default, this is the <see cref="Owner.Scaler.referenceResolution"/>.
    /// </summary>
    protected internal virtual Vector2 ScreenDimensions
    {
        get => new(Screen.width, Screen.height);
    }

    /// <summary>
    /// Determines if the mouse is currently in the Display used by this PanelManager. By default, this always returns true.
    /// </summary>
    protected virtual bool MouseInTargetDisplay => true;

    private Vector3 previousMousePosition = Vector3.zero;
    private MouseState.ButtonState previousMouseButtonState = MouseState.ButtonState.Unknown;

    // invoked from UIPanel ctor
    protected internal virtual void AddPanel(PanelBase panel)
    {
        allDraggers.Add(panel.Dragger);
        draggerInstances.Add(panel.Dragger);

        panelInstances.Add(panel);
        transformIDToUIPanel.Add(panel.UIRoot.transform.GetInstanceID(), panel);
    }

    // invoked from UIPanel.Destroy
    protected internal virtual void RemovePanel(PanelBase panel)
    {
        allDraggers.Remove(panel.Dragger);
        draggerInstances.Remove(panel.Dragger);

        panelInstances.Remove(panel);
        transformIDToUIPanel.Remove(panel.UIRoot.transform.GetInstanceID());
    }

    // invoked from UIPanel.Enable
    protected internal virtual void InvokeOnPanelsReordered()
    {
        Owner.SetOnTop();
        SortDraggerHeirarchy();
        OnPanelsReordered?.Invoke();
    }

    // invoked from parent UIBase.Update
    protected internal virtual void Update()
    {
        if (!ResizePrompting && ShouldUpdateFocus)
            UpdateFocus();

        if (!draggerHandledThisFrame)
            UpdateDraggers();
    }

    protected virtual void UpdateFocus()
    {
        bool clickedInAny = false;

        // If another UIBase has already handled a user's click for focus, don't update it for this UIBase.
        if (!focusHandledThisFrame)
        {
            Vector3 mousePos = MousePosition;
            int count = PanelHolder.transform.childCount;

            for (int i = count - 1; i >= 0; i--)
            {
                // make sure this is a real recognized panel
                Transform transform = PanelHolder.transform.GetChild(i);
                if (!transformIDToUIPanel.TryGetValue(transform.GetInstanceID(), out PanelBase? panel)) continue;

                // check if our mouse is clicking inside the panel
                Vector3 pos = panel.Rect.InverseTransformPoint(mousePos);
                if (!panel.Enabled || !panel.Rect.rect.Contains(pos)) continue;

                // Panel was clicked in.
                focusHandledThisFrame = true;
                clickedInAny = true;

                Owner.SetOnTop();

                // if this is not the top panel, reorder and invoke the onchanged event
                if (transform.GetSiblingIndex() != count - 1)
                {
                    // Set the clicked panel to be on top
                    transform.SetAsLastSibling();

                    InvokeOnPanelsReordered();
                }

                break;
            }
        }

        if (!clickedInAny)
            OnClickedOutsidePanels?.Invoke();
    }

    // Resizing

    /// <summary>Invoked when panels are reordered.</summary>
    protected virtual void SortDraggerHeirarchy()
    {
        draggerInstances.Sort((a, b) => b.Rect.GetSiblingIndex().CompareTo(a.Rect.GetSiblingIndex()));
    }

    /// <summary>
    /// Updates all PanelDraggers owned by this PanelManager.
    /// </summary>
    protected virtual void UpdateDraggers()
    {
        if (!MouseInTargetDisplay)
            return;

        if (!resizeCursor)
            CreateResizeCursorUI();

        var state = InputManager.Mouse.Button0;
        var mousePos = MousePosition;

        // If the mouse hasn't changed, we don't need to do any more
        if (mousePos == previousMousePosition && state == previousMouseButtonState) return;

        previousMousePosition = mousePos;
        previousMouseButtonState = state;

        foreach (PanelDragger instance in draggerInstances)
        {
            if (!instance.Rect.gameObject.activeSelf)
                continue;

            instance.Update(state, mousePos);

            if (draggerHandledThisFrame)
                break;
        }

        if (wasAnyDragging && state.HasFlag(MouseState.ButtonState.Up))
        {
            foreach (PanelDragger instance in draggerInstances)
                instance.WasDragging = false;
            wasAnyDragging = false;
        }
    }

    /// <summary>
    /// Ensure that all panels are visible on the screen
    /// </summary>
    public void ValidatePanels()
    {
        foreach (var panel in panelInstances)
        {
            panel.EnsureValidSize();
            panel.EnsureValidPosition();
        }
    }
}
