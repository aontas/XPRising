namespace ClientUI.UI.Panel;

public class ToggleDraggerSettingButton : SettingsButtonBase
{
    private readonly Action<bool> _action;
    private bool currentState => State == "True";
    public ToggleDraggerSettingButton(Action<bool> action) : base("ShowDragAnchor")
    {
        action(currentState);
        _action = action;
    }

    protected override string PerformAction()
    {
        _action(!currentState);
        return $"{!currentState}";
    }

    protected override string Label()
    {
        var state = currentState ? "on" : "off";
        return $"Toggle drag anchor [{state}]";
    }
}