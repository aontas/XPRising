using System;
using System.Text.Json;
using BepInEx.Logging;
using Bloodstone.API;
using ClientUI.Client.UI;
using ClientUI.Client.UI.Panel;
using ClientUI.Transport.Messages;

namespace ClientUI.Transport.Handlers
{
    internal class ClientMessageActions
    {
        public static bool loadUI = false;
        
        public static void Send(ClientAction msg = null)
        {
            Plugin.Log(LogLevel.Info, $"[CLIENT] [SEND] ClientConnectedMessage");

            if (msg == null)
            {
                msg = new ClientAction();
            }

            VNetwork.SendToServer(msg);
        }

        public static bool HandleChatMessage(string message)
        {
            var validHeader = MessageRegistry.ReadMessageHeader(message, Plugin.ClientNonce, out var type, out var message2);
            // If this is not a valid message to be read by us, then just ignore it.
            if (!validHeader) return false;
            
            Plugin.Log(LogLevel.Info, $"[CLIENT] [RECEIVED] SerialisableChatMessage {type} {message2} ({message})");

            switch (type)
            {
                case MessageRegistry.MessageTypes.ProgressSerialisedMessage:
                    var data = MessageRegistry.DeserialiseMessage<ProgressSerialisedMessage>(message2);
                    Plugin.Log(LogLevel.Info, $"Got {data.Label} message. Instance valid?: {ProgressPanelBase.Instance != null}");
                    if (ProgressPanelBase.Instance != null)
                    {
                        ProgressPanelBase.Instance.ChangeProgress(data.Label, data.Level, data.ProgressPercentage, data.Tooltip);
                    }
                    break;
                case MessageRegistry.MessageTypes.Unknown:
                default:
                    return false;
            }

            if (loadUI) UIManager.ProgressPanel.SetActive(true);
            
            return false;
        }
    }
}
