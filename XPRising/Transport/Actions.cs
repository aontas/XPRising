using ProjectM.Network;
using XPRising.Utils;

namespace XPRising.Transport;

public static class Actions
{
    public enum BarState
    {
        XpOnly,
        Active,
        All
    }

    public static void BarStateChanged(User user, string stringState)
    {
        BarState state;
        switch (stringState)
        {
            case "BarMode:XP":
                state = BarState.XpOnly;
                break;
            case "BarMode:Active":
                state = BarState.Active;
                break;
            case "BarMode:All":
            default:
                state = BarState.All;
                break;
        }

        var preferences = Database.PlayerPreferences[user.PlatformId];
        preferences.UIProgressDisplay = state;
        Database.PlayerPreferences[user.PlatformId] = preferences;
    }
}