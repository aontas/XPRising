using BepInEx.Logging;
using Bloodstone.API;
using ProjectM.Network;
using XPShared.BloodstoneExtensions;
using XPShared.Transport.Messages;

namespace XPShared.Transport;

public delegate void ServerMessageHandler(User fromCharacter, ClientAction msg);

public class MessageHandler
{
    /// <summary>
    /// Required to be called by both server and client during Plugin.Load (or other initialisation phase).
    /// </summary>
    public static void RegisterClientAction()
    {
        Plugin.Log(LogLevel.Warning, "Registering ClientAction");
        VNetworkRegistry.RegisterServerbound<ClientAction>((fromCharacter, msg) =>
        {
            var user = VWorld.Server.EntityManager.GetComponentData<User>(fromCharacter.User);
            MessageHandler.ServerReceiveFromClient(user, msg);
        });
    }

    /// <summary>
    /// Should be called by the client and server during a Plugin.Unload (to ensure any potential hot reloading is correct)
    /// </summary>
    public static void UnregisterClientAction()
    {
        Plugin.Log(LogLevel.Debug, "Unregistering ClientAction");
        VNetworkRegistry.Unregister<ClientAction>();
    }

    /// <summary>
    /// Event for the server to subscribe to messages sent from the client
    /// </summary>
    public static event ServerMessageHandler OnServerMessageEvent;
    
    /// <summary>
    /// Send a ClientAction to the server.
    /// </summary>
    /// <param name="message"></param>
    public static void ClientSendToServer(ClientAction message)
    {
        Plugin.Log(LogLevel.Debug, $"[CLIENT] [SEND] ClientAction: [{message.Action}] [{message.Value}]");

        VNetwork.SendToServer(message);
    }
    
    /// <summary>
    /// Send a VNetworkChatMessage based message to the specified user client.
    /// </summary>
    /// <param name="toCharacter"></param>
    /// <param name="msg"></param>
    /// <typeparam name="T"></typeparam>
    public static void ServerSendToClient<T>(User toCharacter, T msg) where T : VNetworkChatMessage
    {
        Plugin.Log(LogLevel.Debug, $"[SERVER] [SEND] {msg.GetType()}");
        
        MessageUtils.SendToClient(toCharacter, msg);
    }
    
    internal static void ServerReceiveFromClient(User fromCharacter, ClientAction msg)
    {
        Plugin.Log(LogLevel.Debug, $"[SERVER] [RECEIVED] ClientAction {msg.Action} {msg.Value}");
        
        OnServerMessageEvent?.Invoke(fromCharacter, msg);
    }
}