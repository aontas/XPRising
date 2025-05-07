using BepInEx.Logging;
using ProjectM.Network;
using XPShared.Services;
using XPShared.Transport.Messages;

namespace XPShared.Transport;

public delegate void ServerMessageHandler(User fromCharacter, ClientAction msg);

public class MessageHandler
{
    /// <summary>
    /// Event for the server to subscribe to messages sent from the client
    /// </summary>
    public static event ServerMessageHandler OnServerMessageEvent;
    
    /// <summary>
    /// Send a IChatMessage based message to the specified user client.
    /// </summary>
    /// <param name="toCharacter"></param>
    /// <param name="msg"></param>
    /// <typeparam name="T"></typeparam>
    public static void ServerSendToClient<T>(User toCharacter, T msg) where T : IChatMessage
    {
        Plugin.Log(LogLevel.Debug, $"[SERVER] [SEND] {msg.GetType()}");
        
        ChatService.SendToClient(toCharacter, msg);
    }
    
    internal static void ServerReceiveFromClient(User fromCharacter, ClientAction msg)
    {
        Plugin.Log(LogLevel.Debug, $"[SERVER] [RECEIVED] ClientAction {msg.Action} {msg.Value}");
        
        OnServerMessageEvent?.Invoke(fromCharacter, msg);
    }
}