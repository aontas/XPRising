using BepInEx.Logging;
using ProjectM.Network;
using XPShared.Transport.Messages;

namespace XPShared.Transport;

public static class Utils
{
    public static void ServerSetBarData(User playerCharacter, string barGroup, string bar, string header, float progressPercentage, string tooltip, ProgressSerialisedMessage.ActiveState activeState, string colour, string change = "", bool flash = false)
    {
        var msg = new ProgressSerialisedMessage()
        {
            Group = barGroup,
            Label = bar,
            ProgressPercentage = progressPercentage,
            Header = header,
            Tooltip = tooltip,
            Active = activeState,
            Colour = colour,
            Change = change,
            Flash = flash
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

    public static void ServerSendText(User playerCharacter, string group, string id, string title, string text)
    {
        var msg = new DisplayTextMessage()
        {
            Group = group,
            ID = id,
            Title = title,
            Text = text,
            Reset = true
        };
        MessageHandler.ServerSendToClient(playerCharacter, msg);
    }
    
    public static void ServerSendText(User playerCharacter, string group, string id, string title, List<string> text)
    {
        foreach (var msg in text.Select((message, index) => new DisplayTextMessage()
                 {
                     Group = group,
                     ID = id,
                     Title = title,
                     Text = message,
                     Reset = index == 0
                 }))
        {
            MessageHandler.ServerSendToClient(playerCharacter, msg);
        }
    }
}