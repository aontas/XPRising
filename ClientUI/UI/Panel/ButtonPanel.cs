using UnityEngine;
using UnityEngine.UI;
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
    private const int BasePadding = 4;
    private const int HPadding = 2;
    private const int Border = 2;
    
    public override string Name => "XPRising.Buttons";
    public override int MinWidth => BaseWidth + HPadding * 2 + Border + BasePadding;
    public override int MinHeight => ButtonHeight;
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 1f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 1f);
    public override Vector2 DefaultPosition => new Vector2(850, -225);
    protected override UIManager.Panels PanelType => UIManager.Panels.Actions;
    public override bool CanDragAndResize => true;

    private GameObject _buttonGroup;
    private readonly Dictionary<string, ButtonRef> _buttons = new();
    
    protected override void ConstructPanelContent()
    {
        Instance = this;
        RemoveDefaultPanelImageAndMask();

        _buttonGroup = UIFactory.CreateUIObject("ModeButtons", ContentRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_buttonGroup, false, false, true, true, 3);
        UIFactory.SetLayoutElement(_buttonGroup, minHeight: 25, minWidth: 30, flexibleHeight: 9999, flexibleWidth: 9999);
        
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
    }

    public void SetButton(ActionSerialisedMessage data)
    {
        if (!_buttons.TryGetValue(data.ID, out var button))
        {
            var newButton = AddButton(data.ID, data.Label, data.Colour);
            _buttons[data.ID] = newButton;
            newButton.OnClick = () =>
            {
                XPShared.Transport.MessageHandler.ClientSendToServer(new ClientAction(ClientAction.ActionType.ButtonClick, data.ID));
            };
        }
        else
        {
            button.ButtonText.text = data.Label;
            button.ButtonText.color = data.Enabled ? Color.white : Color.gray;
            button.Component.interactable = data.Enabled;
        }
    }

    private ButtonRef AddButton(string id, string text, string colour)
    {
        Color? normalColour = ColorUtility.TryParseHtmlString(colour, out var onlyColour) ? onlyColour : null;
        var button = UIFactory.CreateButton(_buttonGroup, id, text, normalColour);
        UIFactory.SetLayoutElement(button.Component.gameObject, minHeight: 25, minWidth: 200, flexibleWidth: 0, flexibleHeight: 0);
        var cb = button.Component.colors;
        cb.disabledColor = cb.normalColor * 0.4f;
        
        return button;
    }
}