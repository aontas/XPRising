using ClientUI.UniverseLib.UI.Panels;
using UnityEngine;
using UnityEngine.UI;
using XPShared.Transport.Messages;
using RectTransform = UnityEngine.RectTransform;
using UIBase = ClientUI.UniverseLib.UI.UIBase;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class ContentPanel : ResizeablePanelBase
{
    public override string Name => "ClientUIContent";
    public override int MinWidth => 340;
    public override int MinHeight => 25;
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
    public override Vector2 DefaultPivot => new Vector2(0.5f, 1f);
    private bool _canDragAndResize = true;
    public override bool CanDrag => _canDragAndResize;
    public override PanelDragger.ResizeTypes CanResize =>
        _canDragAndResize ? PanelDragger.ResizeTypes.Horizontal : PanelDragger.ResizeTypes.None;

    private GameObject _uiAnchor;
    private ActionPanel _actionPanel;
    private ProgressBarPanel _progressBarPanel;
    private NotificationPanel _notificationsPanel;
    private UIScaleSettingButton _screenScale;
    private ToggleDraggerSettingButton _toggleDrag;

    public ContentPanel(UIBase owner) : base(owner)
    {
    }

    protected override UIManager.Panels PanelType => UIManager.Panels.Base;

    protected override void ConstructPanelContent()
    {
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);

        _uiAnchor = UIFactory.CreateVerticalGroup(ContentRoot, "UIAnchor", true, true, true, true);
        
        var text = UIFactory.CreateLabel(_uiAnchor, "UIAnchorText", "Drag me");
        UIFactory.SetLayoutElement(text.gameObject, 0, 25, 1, 1);
        
        Dragger.DraggableArea = Rect;
        Dragger.OnEndResize();

        var actionContentHolder = UIFactory.CreateUIObject("ActionGroupButtonContent", ContentRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(actionContentHolder, false, false, true, true);
        UIFactory.SetLayoutElement(actionContentHolder, ignoreLayout: true);
        
        // Set anchor/pivot to top left so panel can expand out left
        var actionsRect = actionContentHolder.GetComponent<RectTransform>();
        actionsRect.SetAnchors(RectExtensions.PivotPresets.TopLeft);
        actionsRect.SetPivot(RectExtensions.PivotPresets.TopLeft);
        actionsRect.Translate(Vector3.left * 10);
        
        _actionPanel = new ActionPanel(actionContentHolder);
        _actionPanel.Active = false;
        
        var progressBarHolder = UIFactory.CreateUIObject("ProgressBarContent", ContentRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(progressBarHolder, false, false, true, true);
        UIFactory.SetLayoutElement(progressBarHolder, ignoreLayout: true);
        var progressRect = progressBarHolder.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = Vector2.right;
        progressRect.pivot = new Vector2(0.5f, 1);
        
        _progressBarPanel = new ProgressBarPanel(progressBarHolder);
        _progressBarPanel.Active = false;
        
        var notificationsHolder = UIFactory.CreateUIObject("NotificationContent", ContentRoot, new Vector2(0, 200));
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(notificationsHolder, false, false, true, true, childAlignment: TextAnchor.LowerCenter);
        UIFactory.SetLayoutElement(notificationsHolder, ignoreLayout: true);
        var notificationRect = notificationsHolder.GetComponent<RectTransform>();
        notificationRect.anchorMin = Vector2.up;
        notificationRect.anchorMax = Vector2.one;
        notificationRect.pivot = new Vector2(0.5f, 0);
        notificationRect.Translate(Vector3.up * 10);
        
        _notificationsPanel = new NotificationPanel(notificationsHolder);
        _notificationsPanel.Active = false;
        
        // Added scale UI button now so that the panel is scaled correctly within this frame
        _screenScale = new UIScaleSettingButton();
        
        // Add the toggle drag button now that the panel content has been sized appropriately
        _toggleDrag = new ToggleDraggerSettingButton(ToggleDragging);
    }
    
    protected override void LateConstructUI()
    {
        base.LateConstructUI();
        
        // Update the buttons now that the panel is set up correctly.
        _screenScale.UpdateButton();
        _toggleDrag.UpdateButton();
    }

    public override void Update()
    {
        base.Update();
        // Call update on the panels that need it
        _progressBarPanel.Update();
    }

    internal override void Reset()
    {
        _actionPanel.Reset();
        _progressBarPanel.Reset();
        _notificationsPanel.Reset();

        // Run LateConstructUI so all the panels are set up as they were at the start
        LateConstructUI();
    }

    internal void SetButton(ActionSerialisedMessage data, Action onClick = null)
    {
        _actionPanel.Active = true;
        _actionPanel.SetButton(data, onClick);
    }

    internal void ChangeProgress(ProgressSerialisedMessage data)
    {
        _progressBarPanel.Active = true;
        _progressBarPanel.ChangeProgress(data);
    }

    internal void AddMessage(NotificationMessage data)
    {
        _notificationsPanel.Active = true;
        _notificationsPanel.AddNotification(data);
    }

    internal void OpenActionPanel(string group)
    {
        _actionPanel.Active = true;
        _actionPanel.ShowGroup(group);
    }

    internal void CloseActionPanel()
    {
        _actionPanel.HideGroup();
    }

    private void ToggleDragging(bool active)
    {
        _uiAnchor.SetActive(active);
        _canDragAndResize = active;
        Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, active ? MinHeight : 2);
    }
}