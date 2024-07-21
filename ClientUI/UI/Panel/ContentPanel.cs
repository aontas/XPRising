using BepInEx.Logging;
using ClientUI.UI.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using XPShared.Transport.Messages;
using RectTransform = UnityEngine.RectTransform;
using UIBase = ClientUI.UniverseLib.UI.UIBase;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class ContentPanel : ResizeablePanelBase
{
    public override string Name => "ClientUIContent";
    public override int MinWidth => 440;
    public override int MinHeight => 50;
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 1f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 1f);

    private const string ExpandText = "+";
    private const string ContractText = "\u2212"; // Using unicode instead of "-" as it centers better
    private TextMeshProUGUI _messageText;
    private ClientUI.UniverseLib.UI.Models.ButtonRef _expandButton;
    private ActionPanel _actionPanel;
    
    public ContentPanel(UIBase owner) : base(owner)
    {
    }

    protected override UIManager.Panels PanelType => UIManager.Panels.Base;

    protected override void ConstructPanelContent()
    {
        // Remove the rect mask so that items (e.g. floating text) can appear outside the panel
        RemoveDefaultPanelImageAndMask();
        
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
        actionRect.Translate(Vector3.left * 10 + Vector3.down * 35);
        
        _actionPanel = new ActionPanel(actionContentHolder);
        _actionPanel.Active = false;
    }

    internal override void Reset()
    {
        // TODO
        _expandButton.GameObject.SetActive(false);
        _actionPanel.Reset();
    }

    internal void SetButton(ActionSerialisedMessage data)
    {
        _expandButton.GameObject.SetActive(true);
        _actionPanel.SetButton(data);
    }

    private void ToggleActionPanel()
    {
        _actionPanel.Active = !_actionPanel.Active;
        _expandButton.ButtonText.text = _actionPanel.Active ? ContractText : ExpandText;
    }

    public void DoThing()
    {
        // Plugin.Log(LogLevel.Info, "setting expand button parent");
        // _expandButton.Transform.SetParent(UIFacts.PlayerHUDCanvas.transform, true);
        // Plugin.Log(LogLevel.Info, "set");
    }
}