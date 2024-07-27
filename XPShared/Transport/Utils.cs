using BepInEx.Logging;
using Bloodstone.Network;
using ProjectM.Network;
using XPShared.Transport.Messages;

namespace XPShared.Transport;

public static class Utils
{
    public static void ServerSetBarData(User playerCharacter, string barGroup, string bar, int level, float progressPercentage, string tooltip, ProgressSerialisedMessage.ActiveState activeState, string colour, string change = "")
    {
        var msg = new ProgressSerialisedMessage()
        {
            Group = barGroup,
            Label = bar,
            ProgressPercentage = progressPercentage,
            Level = level,
            Tooltip = tooltip,
            Active = activeState,
            Colour = colour,
            Change = change,
            Flash = change != ""
        };
        MessageHandler.ServerSendToClient(playerCharacter, msg);
    }
    
    public static void ServerSetAction(User playerCharacter, string group, string id, string label, string colour = "#808080")
    {
        var msg = new ActionSerialisedMessage()
        {
            Group = group,
            ID = id,
            Label = label,
            Colour = colour,
        };
        MessageHandler.ServerSendToClient(playerCharacter, msg);
    }
    
    public static void ServerSendNotification(User playerCharacter, string id, string message, LogLevel severity, string colourOverride = "")
    {
        var msg = new NotificationMessage()
        {
            ID = id,
            Message = message,
            Severity = severity,
            Colour = colourOverride
        };
        MessageHandler.ServerSendToClient(playerCharacter, msg);
    }

    public static void SendClientInitialisation()
    {
        MessageUtils.InitialiseClient();
    }
}