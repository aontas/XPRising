using UnityEngine;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using XPShared.Transport.Messages;

namespace ClientUI.UI.Panel;

public class ButtonPanel : ResizeablePanelBase
{
    public ButtonPanel(UIBase owner) : base(owner) { }

    public static ButtonPanel Instance { get; private set; }

    private const int ButtonHeight = 30;
    private const int BaseWidth = 400;
    private const int MinLevelWidth = 30;
    private const int BarHeight = 22;
    private const int BasePadding = 4;
    private const int Spacing = 4;
    private const int VPadding = 2;
    private const int HPadding = 2;
    private const int Border = 2;
    private readonly Vector4 _paddingVector = new Vector4(VPadding, VPadding, HPadding, HPadding);
    
    public override string Name => "XPRising.Actions";
    public override int MinWidth => BaseWidth + HPadding * 2 + Border + BasePadding;
    public override int MinHeight => ButtonHeight;
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 1f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 1f);
    public override Vector2 DefaultPosition => new Vector2(850, -225);
    public override UIManager.Panels PanelType => UIManager.Panels.Actions;
    public override bool CanDragAndResize => true;

    private GameObject _buttonGroup;
    private readonly Dictionary<string, GameObject> _buttons = new();

    private static readonly Color DefaultBarColour = new Color(0.5f, 0.8f, 0.1f);
    private static readonly Color HighlightColour = Color.yellow;
    
    protected override void ConstructPanelContent()
    {
        Instance = this;

        _buttonGroup = UIFactory.CreateVerticalGroup(ContentRoot, "ModeButtons", false, false, true, true, 3);
        UIFactory.SetLayoutElement(_buttonGroup, minHeight: 25, minWidth: 30, flexibleHeight: 9999, flexibleWidth: 9999);
        var xpModeButton = AddButton("OnlyXPBar", "Only XP");
        xpModeButton.OnClick = () =>
        {
            XPShared.Transport.MessageHandler.ClientSendToServer(new ClientAction(ClientAction.ActionType.ButtonClick, "BarMode:XP"));
        };
        var activeModeButton = AddButton("ActiveBars", "Active");
        activeModeButton.OnClick = () =>
        {
            XPShared.Transport.MessageHandler.ClientSendToServer(new ClientAction(ClientAction.ActionType.ButtonClick, "BarMode:Active"));
        };
        var allModeButton = AddButton("AllBars", "All");
        allModeButton.OnClick = () =>
        {
            XPShared.Transport.MessageHandler.ClientSendToServer(new ClientAction(ClientAction.ActionType.ButtonClick, "BarMode:All"));
        };
            
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
        UpdatePanelSize();
    }

    private void UpdatePanelSize()
    {
        // // Update main size
        // Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, groupHeightSum);
        // Dragger.DragableArea = Rect;
        // Dragger.OnEndResize();
    }

    public void AddButton(ActionSerialisedMessage data)
    {
        
        
        // Update panel size
        UpdatePanelSize();
    }

    private ButtonRef AddButton(string name, string text)
    {
        var button = UIFactory.CreateButton(_buttonGroup, name, text);
        UIFactory.SetLayoutElement(button.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0, flexibleHeight: 0);
        RuntimeHelper.SetColorBlock(button.Component, new Color(0.33f, 0.32f, 0.31f));
        return button;
    }
}