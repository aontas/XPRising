using BepInEx.Logging;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using XPShared.Hooks;
using XPShared.Transport.Messages;
using XPShared;
using XPShared.Services;

namespace ClientUI;

public static class ChatUtils
{
    private static readonly ComponentType[] NetworkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<ChatMessageEvent>())
    ];

    private static readonly NetworkEventType ChatEventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_ChatMessageEvent,
        IsDebugEvent = false,
    };
    
    private static readonly int ClientNonce = Random.Shared.Next();

    public static void SendInitialisation()
    {
        // Can't initialise yet as the users have not been set
        if (ClientChatPatch.LocalUser == Entity.Null) return;
        
        ChatService.RegisterClientNonce(ClientChatPatch.LocalSteamId, ClientNonce);
        SendToServer(new ClientRegisterMessage(ClientNonce, ClientChatPatch.LocalSteamId));
        
    }

    /// <summary>
    /// Send a IChatMessage message to the server via the in-game chat mechanism.
    /// If the client has not yet been initialised (via `InitialiseClient`) then this will not send any message.
    /// </summary>
    /// <param name="msg">This is the data packet that will be sent to the server</param>
    /// <typeparam name="T"></typeparam>
    public static void SendToServer<T>(T msg) where T : IChatMessage
    {
        Plugin.Log(LogLevel.Debug, "[CLIENT] [SEND] IChatMessage");
        var serialised = ChatService.SerialiseMessage(msg, ClientNonce);
        ChatMessageEvent chatMessageEvent = new()
        {
            MessageText = new FixedString512Bytes(serialised),
            MessageType = ChatMessageType.Local,
            ReceiverEntity = ClientChatPatch.LocalUser.Read<NetworkId>()
        };
        
        Entity networkEntity = XPShared.Plugin.World.EntityManager.CreateEntity(NetworkEventComponents);
        networkEntity.Write(new FromCharacter { Character = ClientChatPatch.LocalCharacter, User = ClientChatPatch.LocalUser });
        networkEntity.Write(ChatEventType);
        networkEntity.Write(chatMessageEvent);
    }
}