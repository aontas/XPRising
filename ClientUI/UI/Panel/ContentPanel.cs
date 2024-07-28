using ClientUI.UI.Util;
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
    public override int MinHeight => 50;
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 1f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 1f);

    private const string ExpandText = "+";
    private const string ContractText = "\u2212"; // Using unicode instead of "-" as it centers better
    private TextMeshProUGUI _messageText;
    private ClientUI.UniverseLib.UI.Models.ButtonRef _expandButton;
    private ActionPanel _actionPanel;
    private ProgressBarPanel _progressBarPanel;
    private NotificationPanel _notificationsPanel;

    public ContentPanel(UIBase owner) : base(owner)
    {
    }

    protected override UIManager.Panels PanelType => UIManager.Panels.Base;

    protected override void ConstructPanelContent()
    {
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);

        var group = UIFactory.CreateVerticalGroup(ContentRoot, "Messages", true, true, true, true);

        _messageText = UIFactory.CreateLabel(group, "MessageOfTheDay", "UI Rising: The modding");
        UIFactory.SetLayoutElement(_messageText.gameObject, 0, 0, 1, 1);
        
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
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(progressBarHolder, false, false, true, true, 0, 4);
        UIFactory.SetLayoutElement(progressBarHolder, ignoreLayout: true);
        var progressRect = progressBarHolder.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = Vector2.right;
        progressRect.pivot = new Vector2(0.5f, 1);
        
        _progressBarPanel = new ProgressBarPanel(progressBarHolder);
        _progressBarPanel.Active = false;
        
        var notificationsHolder = UIFactory.CreateUIObject("NotificationContent", ContentRoot, new Vector2(0, 200));
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(notificationsHolder, false, false, true, true, 0, childAlignment: TextAnchor.LowerCenter);
        UIFactory.SetLayoutElement(notificationsHolder, ignoreLayout: true);
        var notificationRect = notificationsHolder.GetComponent<RectTransform>();
        notificationRect.anchorMin = Vector2.up;
        notificationRect.anchorMax = Vector2.one;
        notificationRect.pivot = new Vector2(0.5f, 0);
        notificationRect.Translate(Vector3.up * 10);
        
        _notificationsPanel = new NotificationPanel(notificationsHolder);
        _notificationsPanel.Active = false;
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

    internal void SetButton(ActionSerialisedMessage data)
    {
        _expandButton.GameObject.SetActive(true);
        _actionPanel.SetButton(data);
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
}