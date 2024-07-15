﻿using BepInEx.Logging;
using Bloodstone.API;
using ProjectM.Network;
using XPShared.Transport.Messages;

namespace XPShared.Transport;

public static class MessageRegistry
{
    public enum MessageTypes
    {
        Unknown,
        ProgressSerialisedMessage,
        ActionSerialisedMessage,
    }
    
    private const string HeaderDelimiter = "--";
    public static string GetMessageHeader(MessageTypes type, string userNonce)
    {
        return $"{userNonce}{HeaderDelimiter}{(int)type}{HeaderDelimiter}";
    }

    public static bool ReadMessageHeader(string input, out string userNonce, out MessageTypes type, out string message)
    {
        type = MessageTypes.Unknown;
        message = "";
        userNonce = "";
            
        var result = input.Split(HeaderDelimiter, 3);
        if (result.Length < 3) return false;
        
        userNonce = result[0];
        type = Enum.Parse<MessageTypes>(result[1]);
        message = result[2];
        return true;
    }

    public static string SerialiseMessage<T>(T msg) where T : ISerialisableChatMessage
    {
        using var stream = new MemoryStream();
        using var bw = new BinaryWriter(stream);
        
        msg.Serialize(bw);
        return Convert.ToBase64String(stream.ToArray());
    }

    public static T DeserialiseMessage<T>(string message) where T : ISerialisableChatMessage, new()
    {
        var bytes = Convert.FromBase64String(message);
        
        using var stream = new MemoryStream(bytes);
        using var br = new BinaryReader(stream);

        var newT = new T();
        newT.Deserialize(br);
        return newT;
    }
        
    public static void RegisterMessage()
    {
        Plugin.Log(LogLevel.Debug, "Registering message");
        VNetworkRegistry.RegisterBiDirectional<ClientAction>(
            // invoked when the server sends a message to the client
            (_) => { }, // Only messages from client to server are working with this mechanism.
            // invoked when a client sends a message to the server
            (fromCharacter, msg) =>
            {
                Plugin.Log(LogLevel.Debug, "onMessageFromClient");
                var user = VWorld.Server.EntityManager.GetComponentData<User>(fromCharacter.User);
                MessageHandler.ServerReceiveFromClient(user, msg);
            }
        );
    }

    public static void UnregisterMessages()
    {
        VNetworkRegistry.Unregister<ClientAction>();
    }
}