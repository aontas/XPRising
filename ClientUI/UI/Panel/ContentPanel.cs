using ClientUI.UniverseLib.UI.Panels;
using TMPro;
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

    private const string ExpandText = "+";
    private const string ContractText = "\u2212"; // Using unicode instead of "-" as it centers better
    private GameObject _uiAnchor;
    private ClientUI.UniverseLib.UI.Models.ButtonRef _expandButton;
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

        _expandButton = UIFactory.CreateButton(ContentRoot, "ExpandActionsButton", ExpandText);
        UIFactory.SetLayoutElement(_expandButton.GameObject, ignoreLayout: true);
        _expandButton.ButtonText.fontSize = 30;
        _expandButton.OnClick = ToggleActionPanel;
        _expandButton.Transform.anchorMin = Vector2.up;
        _expandButton.Transform.anchorMax = Vector2.up;
        _expandButton.Transform.pivot = Vector2.one;
        _expandButton.Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 30);
        _expandButton.Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30);
        _expandButton.ButtonText.overflowMode = TextOverflowModes.Overflow;
        _expandButton.Transform.Translate(Vector3.left * 10);
        _expandButton.GameObject.SetActive(false);
        
        var actionContentHolder = UIFactory.CreateUIObject("ActionsContent", ContentRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(actionContentHolder, false, false, true, true, 2, 2, 2, 2, 2, TextAnchor.UpperLeft);
        UIFactory.SetLayoutElement(actionContentHolder, ignoreLayout: true);
        var actionRect = actionContentHolder.GetComponent<RectTransform>();
        actionRect.anchorMin = Vector2.up;
        actionRect.anchorMax = Vector2.up;
        actionRect.pivot = Vector2.one;
        actionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
        actionRect.Translate(Vector3.left * 10 + Vector3.down * 45);
        
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
    }
    
    protected override void LateConstructUI()
    {
        base.LateConstructUI();
        AddSettingsButtons();
    }

    private void AddSettingsButtons()
    {
        // Added UI settings buttons
        _screenScale = new UIScaleSettingButton();
        _screenScale.UpdateButton();
        _toggleDrag = new ToggleDraggerSettingButton(ToggleDragging);
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
        _expandButton.GameObject.SetActive(false);
        _actionPanel.Reset();
        _progressBarPanel.Reset();
        _notificationsPanel.Reset();
    }

    internal void SetButton(ActionSerialisedMessage data, Action onClick = null)
    {
        _expandButton.GameObject.SetActive(true);
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

    private void ToggleActionPanel()
    {
        _actionPanel.Active = !_actionPanel.Active;
        _expandButton.ButtonText.text = _actionPanel.Active ? ContractText : ExpandText;
    }

    private void ToggleDragging(bool active)
    {
        _uiAnchor.SetActive(active);
        _canDragAndResize = active;
        Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, active ? MinHeight : 2);
    }
}