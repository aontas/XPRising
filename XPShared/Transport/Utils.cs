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
            Change = change
        };
        MessageHandler.ServerSendToClient(playerCharacter, msg);
    }

    public static ClientAction UserConnectAction()
    {
        return new ClientAction(ClientAction.ActionType.Connect, $"{Plugin.ClientNonce}");
    }
    
    public static ClientAction UserDisconnectAction()
    {
        return new ClientAction(ClientAction.ActionType.Disconnect, $"{Plugin.ClientNonce}");
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
}