using BepInEx.Configuration;
using XPShared.Transport.Messages;

namespace ClientUI.UI.Panel;

public abstract class SettingsButtonBase
{
    private const string Group = "UISettings";
    private readonly string _id;

    protected string State;
    
    private readonly ConfigEntry<string> _setting;

    protected SettingsButtonBase(string id)
    {
        this._id = id;
        _setting = Plugin.Instance.Config.Bind("UISettings", $"{_id}", "");
        State = _setting.Value;
    }

    // Implementers to use this to set/toggle/perform action
    // This should return the new config that can be stored in the config file
    protected abstract string PerformAction();

    // Gets the label that should be displayed on the button due to the current state
    protected abstract string Label();
    
    private void OnToggle()
    {
        var newState = PerformAction();
        
        UpdateButton();
        
        _setting.Value = newState;
    }

    public void UpdateButton()
    {
        // Update the label on the button
        UIManager.ContentPanel.SetButton(new ActionSerialisedMessage()
        {
            Group = Group,
            ID = _id,
            Label = Label(),
            Enabled = true
        }, OnToggle);
    }
}