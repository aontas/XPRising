#nullable enable
using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using XPShared.Transport.Messages;

namespace XPShared.Services;

public delegate void ClientRegisterMessageHandler(ulong steamId);

public static class ChatService
{
    private static readonly Dictionary<string, ChatEventHandler> EventHandlers = new();
    
    private static readonly Dictionary<ulong, int> SupportedUsers = new();
    
    private static string DeriveKey(Type name) => name.ToString(); // FullName contains assembly info which we don't want
    private static string ClientRegisterKey = DeriveKey(typeof(ClientRegisterMessage));
    
    internal static void Register<T>(ChatEventHandler handler)
    {
        var key = DeriveKey(typeof(T));

        if (EventHandlers.ContainsKey(key))
            throw new Exception($"Network event {key} is already registered");

        EventHandlers.Add(key, handler);
    }

    internal static void Unregister<T>()
    {
        var key = DeriveKey(typeof(T));

        // don't throw if it doesn't exist
        EventHandlers.Remove(key);
    }

    /// <summary>
    /// This function registers the ClientRegisterMessage to ensure that it will pick up any clients attempting to
    /// register with this server.
    /// </summary>
    internal static void ListenForClientRegister()
    {
        if (Plugin.IsClient) return;
        RegisterType<ClientRegisterMessage>((message, steamId) =>
        {
            Plugin.Log(LogLevel.Warning, $"got successful registry: {message.SteamId} -> {message.ClientNonce}");
            SupportedUsers[message.SteamId] = message.ClientNonce;
            
            OnClientRegisterEvent?.Invoke(message.SteamId);
        });
    }

    /// <summary>
    /// The server should add an action on this event to be able to respond with any start-up requests now that the user
    /// is ready for messages.
    /// </summary>
    public static event ClientRegisterMessageHandler? OnClientRegisterEvent;

    /// <summary>
    /// Send a IChatMessage message to the client via the in-game chat mechanism.
    /// If the client has not yet been initialised (via `InitialiseClient`) then this will not send any message.
    /// 
    /// Note: If the client has not registered the IChatMessage type that we are sending, then they will not
    /// receive that message. 
    /// </summary>
    /// <param name="toCharacter">This is the user that the message will be sent to</param>
    /// <param name="msg">This is the data packet that will be sent to the user</param>
    /// <typeparam name="T"></typeparam>
    public static void SendToClient<T>(User toCharacter, T msg) where T : IChatMessage
    {
        Plugin.Log(LogLevel.Debug, "[SERVER] [SEND] IChatMessage");

        // Send the user a chat message, as long as we have them in our initialised list.
        if (SupportedUsers.TryGetValue(toCharacter.PlatformId, out var clientNonce))
        {
            FixedString512Bytes message = $"{SerialiseMessage(msg, clientNonce)}";
            ServerChatUtils.SendSystemMessageToClient(Plugin.World.EntityManager, toCharacter, ref message);
        }
        else
        {
            Plugin.Log(LogLevel.Debug, "user nonce not present in supportedUsers");
        }
    }

    public static void RegisterType<T>(Action<T, ulong> onMessageEvent) where T : IChatMessage, new()
    {
        Register<T>(new()
        {
            OnReceiveMessage = (binaryReader, steamId) =>
            {
                var msg = new T();
                msg.Deserialize(binaryReader);
                onMessageEvent.Invoke(msg, steamId);
            }
        });
    }

    /// <summary>
    /// Used by the client to register their nonce so that messages can be deserialised correctly in the client
    /// </summary>
    /// <param name="clientNonce"></param>
    public static void RegisterClientNonce(ulong steamId, int clientNonce)
    {
        if (Plugin.IsServer) return;
        SupportedUsers[steamId] = clientNonce;
    }

    public static string SerialiseMessage<T>(T msg, int clientNonce) where T : IChatMessage
    {
        using var stream = new MemoryStream();
        using var bw = new BinaryWriter(stream);

        IChatMessage.WriteHeader(bw, DeriveKey(msg.GetType()), clientNonce);

        msg.Serialize(bw);
        return Convert.ToBase64String(stream.ToArray());
    }

    internal static bool DeserialiseMessage(string message, ulong steamId)
    {
        var type = "";
        try
        {
            var bytes = Convert.FromBase64String(message);

            using var stream = new MemoryStream(bytes);
            using var br = new BinaryReader(stream);

            // If we can't read the header, it is likely not a IChatMessage
            if (!IChatMessage.ReadHeader(br, out var clientNonce, out type)) return false;
            
            var isRegistered = SupportedUsers.TryGetValue(steamId, out var expectedNonce);
            var isClientRegister = type == ClientRegisterKey;
            var isCorrectNonce = isRegistered && clientNonce == expectedNonce;

            if (isClientRegister || isCorrectNonce)
            {
                if (EventHandlers.TryGetValue(type, out var handler))
                {
                    handler.OnReceiveMessage(br, steamId);
                }
            }
            else if (!isCorrectNonce) {
                // This is a valid message, but not intended for us.
                Plugin.Log(LogLevel.Warning, $"ClientNonce did not match: [registered: {isRegistered}, actual: {clientNonce}, expected: {expectedNonce}, event: {type}]");
            }

            return true;
        }
        catch (FormatException)
        {
            Plugin.Log(LogLevel.Debug, "Invalid base64");
            return false;
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Error, $"Error handling incoming network event {type}:");
            Plugin.Log(LogLevel.Error, ex.ToString());

            return false;
        }
    }
}

internal class ChatEventHandler
{
#nullable disable
    internal Action<BinaryReader, ulong> OnReceiveMessage { get; init; }
#nullable enable
}