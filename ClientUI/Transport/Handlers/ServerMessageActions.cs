using System;
using System.Collections.Generic;
using System.Text.Json;
using BepInEx.Logging;
using Bloodstone.API;
using ClientUI.Transport.Messages;
using ProjectM;
using ProjectM.Network;

namespace ClientUI.Transport.Handlers
{
    internal class ServerMessageActions
    {
        private static Dictionary<ulong, string> supportedUsers = new();
        
        public static void Received(User fromCharacter, ClientAction msg)
        {
            Plugin.Log(LogLevel.Info, $"[SERVER] [RECEIVED] ClientAction {msg.Action} {msg.Value}");

            switch (msg.Action)
            {
                case ClientAction.ActionType.Connect:
                    supportedUsers.Add(fromCharacter.PlatformId, msg.Value);
                    break;
                case ClientAction.ActionType.Disconnect:
                    if (supportedUsers.TryGetValue(fromCharacter.PlatformId, out var existingNonce) &&
                        existingNonce == msg.Value)
                    {
                        supportedUsers.Remove(fromCharacter.PlatformId);
                    }
                    break;
                default:
                    Plugin.Log(LogLevel.Warning, $"Received unknown client action type: {msg.Action}");
                    return;
            }
        }

        public static void Send(User toCharacter, ProgressSerialisedMessage msg)
        {
            Plugin.Log(LogLevel.Info, $"[SERVER] [SEND] ProgressSerialisedMessage");

            // Note: Bloodstone currently doesn't support sending server messages to the client.
            // VNetwork.SendToClient(toCharacter, msg);
            
            // We are instead going to send the user a chat message, as long as we have them in our initialised list.
            if (supportedUsers.TryGetValue(toCharacter.PlatformId, out var userNonce))
            {
                ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, toCharacter, $"{MessageRegistry.GetMessageHeader(msg.Type(), userNonce)}{MessageRegistry.SerialiseMessage(msg)}");
            }
        }
    }
}
