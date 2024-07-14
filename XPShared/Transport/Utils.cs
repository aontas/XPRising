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
}