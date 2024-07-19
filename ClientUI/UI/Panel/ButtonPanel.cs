using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using XPShared.Transport.Messages;

namespace ClientUI.UI.Panel;

public class ButtonPanel : ResizeablePanelBase
{
    public ButtonPanel(UIBase owner) : base(owner) { }

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

    private readonly Dictionary<string, GameObject> _buttonGroups = new();
    private readonly Dictionary<string, ButtonRef> _buttons = new();
    
    protected override void ConstructPanelContent()
    {
        RemoveDefaultPanelImageAndMask();
        
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
    }

    public void SetButton(ActionSerialisedMessage data)
    {
        if (!_buttons.TryGetValue(data.ID, out var button))
        {
            var newButton = AddButton(data.Group, data.ID, data.Label, data.Colour);
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

    public void Reset()
    {
        foreach (var (_, buttonGroup) in _buttonGroups)
        {
            GameObject.Destroy(buttonGroup);
        }
        _buttonGroups.Clear();
        _buttons.Clear();
    }

    private ButtonRef AddButton(string group, string id, string text, string colour)
    {
        if (!_buttonGroups.TryGetValue(group, out var buttonGroup))
        {
            buttonGroup = UIFactory.CreateUIObject("group", ContentRoot);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(buttonGroup, false, false, true, true, 3);
            UIFactory.SetLayoutElement(buttonGroup, minHeight: 25, minWidth: 30, flexibleHeight: 9999, flexibleWidth: 9999);
            _buttonGroups.Add(group, buttonGroup);
        }
        Color? normalColour = ColorUtility.TryParseHtmlString(colour, out var onlyColour) ? onlyColour : null;
        var button = UIFactory.CreateButton(buttonGroup, id, text, normalColour);
        UIFactory.SetLayoutElement(button.Component.gameObject, minHeight: 25, minWidth: 200, flexibleWidth: 0, flexibleHeight: 0);
        var cb = button.Component.colors;
        cb.disabledColor = cb.normalColor * 0.4f;
        
        return button;
    }
}