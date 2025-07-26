using ClientUI.UI.Util;
using UnityEngine;
using UnityEngine.UI;
using XPShared.Transport.Messages;
using ButtonRef = ClientUI.UniverseLib.UI.Models.ButtonRef;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class ActionPanel
{
    private const string ExpandText = "<";
    private const string ContractText = ">";
    private static readonly ColorBlock ClosedButtonColour = UIFactory.CreateColourBlock(Colour.SliderFill);
    private static readonly ColorBlock OpenButtonColour = UIFactory.CreateColourBlock(Colour.SliderHandle);
    
    private readonly GameObject _contentRoot;
    private readonly GameObject _actionsContent;
    private readonly GameObject _buttonsContent;

    private readonly Dictionary<string, (GameObject, ButtonRef)> _actionGroups = new();
    private readonly Dictionary<string, ButtonRef> _actions = new();

    private string _activeGroup = "";
    
    public ActionPanel(GameObject root)
    {
        _contentRoot = root;
        
        _buttonsContent = UIFactory.CreateUIObject("ButtonsContent", _contentRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_buttonsContent, false, false, true, true, 2, 0, 0, 0, 0, TextAnchor.UpperRight);
        UIFactory.SetLayoutElement(_buttonsContent, ignoreLayout: true);
        
        // Set anchor/pivot to top right so it attaches to the root from that side 
        var buttonGroupRect = _buttonsContent.GetComponent<RectTransform>();
        buttonGroupRect.SetAnchors(RectExtensions.PivotPresets.TopRight);
        buttonGroupRect.SetPivot(RectExtensions.PivotPresets.TopRight);
        
        _actionsContent = UIFactory.CreateUIObject("ActionsContent", _contentRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_actionsContent, false, false, true, true, 2, 0, 0, 0, 0, TextAnchor.UpperRight);
        UIFactory.SetLayoutElement(_actionsContent, ignoreLayout: true);
        var actionRect = _actionsContent.GetComponent<RectTransform>();
        actionRect.anchorMin = Vector2.up;
        actionRect.anchorMax = Vector2.up;
        actionRect.pivot = Vector2.one;
        actionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
        actionRect.Translate(Vector3.left * 50);
    }

    public bool Active
    {
        get => _contentRoot.active;
        set => _contentRoot.SetActive(value);
    }

    public void SetButton(ActionSerialisedMessage data, Action onClick = null)
    {
        if (!_actions.TryGetValue(data.ID, out var button))
        {
            button = AddButton(data.Group, data.ID, data.Label, data.Colour);
            _actions[data.ID] = button;
            if (onClick == null)
            {
                button.OnClick = () =>
                {
                    ChatUtils.SendToServer(new ClientAction(ClientAction.ActionType.ButtonClick, data.ID));
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
        foreach (var (_, group) in _actionGroups)
        {
            GameObject.Destroy(group.Item1);
            GameObject.Destroy(group.Item2.GameObject);
        }
        _actionGroups.Clear();
        _actions.Clear();
    }

    private ButtonRef AddButton(string group, string id, string text, string colour)
    {
        if (!_actionGroups.TryGetValue(group, out var buttonGroup))
        {
            // Set up the button that will open this group
            var groupButton = UIFactory.CreateButton(_buttonsContent, $"{group}-button", ContractText);
            UIFactory.SetLayoutElement(groupButton.GameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0, flexibleHeight: 0);
            groupButton.OnClick = () => ToggleGroup(group);
            
            // actionGroup parented to groupButton
            var actionGroup = UIFactory.CreateUIObject(group, groupButton.GameObject);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(actionGroup, false, false, true, true, 3, 0, 0, 0, 0, TextAnchor.UpperRight);
            UIFactory.SetLayoutElement(actionGroup, ignoreLayout: true);
            var actionRect = actionGroup.GetComponent<RectTransform>();
            actionRect.anchorMin = Vector2.up;
            actionRect.anchorMax = Vector2.up;
            actionRect.pivot = Vector2.one;
            actionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            actionRect.Translate(Vector3.left * 8);
            actionGroup.SetActive(false);
            
            buttonGroup = (actionGroup, groupButton);
            _actionGroups.Add(group, buttonGroup);
            
            // Make sure that the settings is the first button
            if (group == SettingsButtonBase.Group)
            {
                groupButton.Transform.SetAsFirstSibling();
            }
        }
        Color? normalColour = ColorUtility.TryParseHtmlString(colour, out var onlyColour) ? onlyColour : null;
        var actionButton = UIFactory.CreateButton(buttonGroup.Item1, id, text, normalColour);
        UIFactory.SetLayoutElement(actionButton.Component.gameObject, minHeight: 25, minWidth: 200, flexibleWidth: 0, flexibleHeight: 0);
        
        return actionButton;
    }

    public void ToggleGroup(string group)
    {
        // Deactivate any active group
        if (_activeGroup != "" && _actionGroups.TryGetValue(_activeGroup, out var previousActiveGroup))
        {
            previousActiveGroup.Item1.SetActive(false);
            previousActiveGroup.Item2.ButtonText.text = ContractText;
            previousActiveGroup.Item2.Component.colors = ClosedButtonColour;
        }
        
        // Only set the active group if we have a record of it
        _activeGroup = _activeGroup == group || !_actionGroups.ContainsKey(group) ? "" : group;

        // activate the new group as required
        if (_activeGroup != "" && _actionGroups.TryGetValue(_activeGroup, out var newActiveGroup))
        {
            newActiveGroup.Item1.SetActive(true);
            newActiveGroup.Item2.ButtonText.text = ExpandText;
            newActiveGroup.Item2.Component.colors = OpenButtonColour;
        }
    }

    public void ShowGroup(string group)
    {
        // Just ignore this if the group is already active, or blank, or we have no record of it
        if (_activeGroup == group || group == "" || !_actionGroups.TryGetValue(group, out var newActiveGroup)) return;
        
        _activeGroup = group;

        // activate the new group as required
        newActiveGroup.Item1.SetActive(true);
        newActiveGroup.Item2.ButtonText.text = ExpandText;
        newActiveGroup.Item2.Component.colors = OpenButtonColour;
    }
    
    public void HideGroup()
    {
        if (_activeGroup == "") return;

        if (_actionGroups.TryGetValue(_activeGroup, out var oldActiveGroup))
        {
            oldActiveGroup.Item1.SetActive(false);
            oldActiveGroup.Item2.ButtonText.text = ContractText;
            oldActiveGroup.Item2.Component.colors = ClosedButtonColour;
        }
        
        _activeGroup = "";
    }
}