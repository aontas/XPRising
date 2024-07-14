using BepInEx.Logging;
using Bloodstone.API;
using ProjectM;
using ProjectM.Network;
using XPShared.Transport.Messages;

namespace XPShared.Transport;

public delegate void ClientMessageHandler(MessageRegistry.MessageTypes type, string message);
public delegate void ServerMessageHandler(User fromCharacter, ClientAction msg);

public class MessageHandler
{
    public static event ClientMessageHandler OnClientMessageEvent;
    public static event ServerMessageHandler OnServerMessageEvent;
    
    private static Dictionary<ulong, string> supportedUsers = new();

    public static void ClientReceiveFromServer(MessageRegistry.MessageTypes type, string message)
    {
        Plugin.Log(LogLevel.Debug, $"[CLIENT] [RECEIVED] SerialisableChatMessage {type} {message}");
        
        OnClientMessageEvent?.Invoke(type, message);
    }

    public static void ClientSendToServer(ClientAction message)
    {
        Plugin.Log(LogLevel.Debug, $"[CLIENT] [SEND] ClientAction: [{message.Action}] [{message.Value}]");

        VNetwork.SendToServer(message);
    }
    
    public static void ServerReceiveFromClient(User fromCharacter, ClientAction msg)
    {
        Plugin.Log(LogLevel.Debug, $"[SERVER] [RECEIVED] ClientAction {msg.Action} {msg.Value}");
        
        // Internal handling for connection messages
        switch (msg.Action)
        {
            case ClientAction.ActionType.Connect:
                supportedUsers[fromCharacter.PlatformId] = msg.Value;
                break;
            case ClientAction.ActionType.Disconnect:
                if (supportedUsers.TryGetValue(fromCharacter.PlatformId, out var existingNonce) &&
                    existingNonce == msg.Value)
                {
                    supportedUsers.Remove(fromCharacter.PlatformId);
                }
                break;
            default:
                break;
        }
        
        OnServerMessageEvent?.Invoke(fromCharacter, msg);
    }
    
    public static void ServerSendToClient(User toCharacter, ProgressSerialisedMessage msg)
    {
        Plugin.Log(LogLevel.Debug, $"[SERVER] [SEND] ProgressSerialisedMessage {MessageRegistry.SerialiseMessage(msg)}");

        // Note: Bloodstone currently doesn't support sending custom server messages to the client :(
        // VNetwork.SendToClient(toCharacter, msg);
            
        // ... instead we are going to send the user a chat message, as long as we have them in our initialised list.
        if (supportedUsers.TryGetValue(toCharacter.PlatformId, out var userNonce))
        {
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, toCharacter,
                $"{MessageRegistry.GetMessageHeader(msg.Type(), userNonce)}{MessageRegistry.SerialiseMessage(msg)}");
        }
        else
        {
            Plugin.Log(LogLevel.Debug, $"user nonce not present in supportedUsers");
        }
    }
}