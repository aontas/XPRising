using System;
using System.Text.Json;
using Bloodstone.API;
using ClientUI.Transport.Handlers;
using ClientUI.Transport.Messages;

using ProjectM.Network;

namespace ClientUI.Transport
{
    internal class MessageRegistry
    {
        internal enum MessageTypes
        {
            Unknown,
            ProgressSerialisedMessage
        }
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            IncludeFields = true
        };
        
        private const string HeaderDelimiter = "--";
        public static string GetMessageHeader(MessageTypes type, string userNonce)
        {
            return $"{userNonce}{HeaderDelimiter}{type}{HeaderDelimiter}";
        }

        public static bool ReadMessageHeader(string input, string userNonce, out MessageTypes type, out string message)
        {
            type = MessageTypes.Unknown;
            message = "";
            
            var result = input.Split(HeaderDelimiter, 3);
            if (result.Length < 3 || result[0] != userNonce) return false;
            
            type = Enum.Parse<MessageTypes>(result[1]);
            message = result[2];
            return true;
        }

        public static string SerialiseMessage<T>(T msg)
        {
            return JsonSerializer.Serialize(msg, JsonOptions);
        }

        public static T DeserialiseMessage<T>(string message)
        {
            return JsonSerializer.Deserialize<T>(message, JsonOptions);
        }
        
        public static void RegisterMessage()
        {
            VNetworkRegistry.RegisterBiDirectional<ClientAction>(
                // invoked when the server sends a message to the client
                (_) => { }, // Only messages from client to server are working with this mechanism.
                // invoked when a client sends a message to the server
                (fromCharacter, msg) =>
                {
                    var user = VWorld.Server.EntityManager.GetComponentData<User>(fromCharacter.User);
                    ServerMessageActions.Received(user, msg);
                }
            );
        }

        public static void UnregisterMessages()
        {
            VNetworkRegistry.Unregister<ClientAction>();
        }
    }
}
