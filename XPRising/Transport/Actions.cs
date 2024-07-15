using ProjectM.Network;
using XPRising.Utils;

namespace XPRising.Transport;

public static class Actions
{
    public enum BarState
    {
        None,
        Active,
        All
    }

    public static void BarStateChanged(User user)
    {
        var preferences = Database.PlayerPreferences[user.PlatformId];
        
        switch (preferences.UIProgressDisplay)
        {
            case BarState.None:
                preferences.UIProgressDisplay = BarState.Active;
                break;
            case BarState.Active:
            default:
                preferences.UIProgressDisplay = BarState.All;
                break;
            case BarState.All:
                preferences.UIProgressDisplay = BarState.None;
                break;
        }

        Database.PlayerPreferences[user.PlatformId] = preferences;
    }
}