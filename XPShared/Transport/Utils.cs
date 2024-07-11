using ProjectM.Network;
using XPShared.Transport.Messages;

namespace XPShared.Transport;

public static class Utils
{
    public static void ServerSetBarData(User playerCharacter, string bar, int level, float progressPercentage, string tooltip)
    {
        var msg = new ProgressSerialisedMessage();
        msg.Label = bar;
        msg.ProgressPercentage = progressPercentage;
        msg.Level = level;
        msg.Tooltip = tooltip;
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