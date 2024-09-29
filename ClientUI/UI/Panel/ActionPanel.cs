using UnityEngine;
using UnityEngine.UI;
using XPShared.Transport.Messages;
using ButtonRef = ClientUI.UniverseLib.UI.Models.ButtonRef;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class ActionPanel
{
    private readonly GameObject _contentRoot;
    public ActionPanel(GameObject root)
    {
        _contentRoot = root;
    }

    private readonly Dictionary<string, GameObject> _buttonGroups = new();
    private readonly Dictionary<string, ButtonRef> _buttons = new();

    public bool Active
    {
        get => _contentRoot.active;
        set => _contentRoot.SetActive(value);
    }

    public void SetButton(ActionSerialisedMessage data, Action onClick = null)
    {
        if (!_buttons.TryGetValue(data.ID, out var button))
        {
            button = AddButton(data.Group, data.ID, data.Label, data.Colour);
            _buttons[data.ID] = button;
            if (onClick == null)
            {
                button.OnClick = () =>
                {
                    XPShared.Transport.MessageHandler.ClientSendToServer(new ClientAction(ClientAction.ActionType.ButtonClick, data.ID));
                };
            }
            else
            {
                button.OnClick = onClick;
            }
        }
        
        button.ButtonText.text = data.Label;
        button.ButtonText.color = data.Enabled ? Color.white : Color.gray;
        button.Component.interactable = data.Enabled;
    }

    internal void Reset()
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
            buttonGroup = UIFactory.CreateUIObject(group, _contentRoot);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(buttonGroup, false, false, true, true, 3);
            _buttonGroups.Add(group, buttonGroup);
        }
        Color? normalColour = ColorUtility.TryParseHtmlString(colour, out var onlyColour) ? onlyColour : null;
        var button = UIFactory.CreateButton(buttonGroup, id, text, normalColour);
        UIFactory.SetLayoutElement(button.Component.gameObject, minHeight: 25, minWidth: 200, flexibleWidth: 0, flexibleHeight: 0);
        
        return button;
    }
}