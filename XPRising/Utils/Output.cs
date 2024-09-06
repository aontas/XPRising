using System.Text;
using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Transport;

namespace XPRising.Utils
{
    public static class Output
    {
        public const string White = "white";
        public const string Green = "#75ff33";
        public const string Gray = "#8d8d8d";
        public const string DarkYellow = "#ffb700";
        public const string LightYellow = "#ffff00";
        public const string DarkRed = "#9f0000";

        private static readonly PlayerPreferences DebugPreferences = new()
        {
            Language = L10N.DefaultLanguage,
            TextSize = 10,
            UIProgressDisplay = Actions.BarState.Active
        };
        
        private static void SendMessage(User user, L10N.LocalisableString message, PlayerPreferences preferences, LogLevel logLevel, string colourOverride = "")
        {
            if (!user.IsConnected) return;
            
            var printableMessage = message.Build(preferences.Language);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, $"<size={preferences.TextSize}>{printableMessage}");

            if (Cache.PlayerHasUINotifications(user.PlatformId))
            {
                XPShared.Transport.Utils.ServerSendNotification(user, "X", printableMessage, logLevel, colourOverride);
            }
        }
        
        public static void DebugMessage(Entity userEntity, string message)
        {
            if (Plugin.IsDebug && Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user))
            {
                SendMessage(user, new L10N.LocalisableString(message), DebugPreferences, LogLevel.Debug);
            }
        }
        
        public static void DebugMessage(ulong steamID, string message)
        {
            if (Plugin.IsDebug && PlayerCache.FindPlayer(steamID, true, out _, out _, out var user))
            {
                SendMessage(user, new L10N.LocalisableString(message), DebugPreferences, LogLevel.Debug);
            }
        }
        
        public static void SendMessage(Entity userEntity, L10N.LocalisableString message, string colourOverride = "")
        {
            if (!Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user)) return;

            var preferences = Database.PlayerPreferences[user.PlatformId];
            SendMessage(user, message, preferences, LogLevel.Info, colourOverride);
        }
        
        public static void SendMessage(ulong steamID, L10N.LocalisableString message, string colourOverride = "")
        {
            if (!PlayerCache.FindPlayer(steamID, true, out _, out _, out var user)) return;
            
            var preferences = Database.PlayerPreferences[user.PlatformId];
            SendMessage(user, message, preferences, LogLevel.Info, colourOverride);
        }

        public static void SendMessages(ulong steamID, L10N.LocalisableString header, L10N.LocalisableString[] messages)
        {
            if (!PlayerCache.FindPlayer(steamID, true, out _, out _, out var user)) return;
            
            SendMessages(Send, steamID, header, messages);
            return;

            void Send(string message)
            {
                ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
            }
        }
        
        public static void ChatReply(ChatCommandContext ctx, L10N.LocalisableString message)
        {
            var preferences = Database.PlayerPreferences[ctx.User.PlatformId];
            ctx.Reply($"<size={preferences.TextSize}>{message.Build(preferences.Language)}");
        }

        // This is based on the MAX_MESSAGE_SIZE from VCF.
        private const int MaxCharacterCount = 450;
        public static void ChatReply(ChatCommandContext ctx, L10N.LocalisableString header, params L10N.LocalisableString[] messages)
        {
            SendMessages(ctx.Reply, ctx.User.PlatformId, header, messages);
        }
        
        public static CommandException ChatError(ChatCommandContext ctx, L10N.LocalisableString message)
        {
            var preferences = Database.PlayerPreferences[ctx.User.PlatformId];
            return ctx.Error($"<size={preferences.TextSize}>{message.Build(preferences.Language)}");
        }

        private static void SendMessages(Action<string> send, ulong steamID, L10N.LocalisableString header, params L10N.LocalisableString[] messages)
        {
            var preferences = Database.PlayerPreferences[steamID];

            var headerValue = $"<size={preferences.TextSize}>{header.Build(preferences.Language)}";
            var sBuilder = new StringBuilder();
            foreach (var message in messages)
            {
                var compiledMessage = message.Build(preferences.Language);
                if (sBuilder.Length == 0)
                {
                    sBuilder.AppendLine(headerValue);
                    sBuilder.AppendLine(compiledMessage);
                }
                else
                {
                    // Check if this message would take the packet over the limit
                    if (sBuilder.Length + compiledMessage.Length > MaxCharacterCount)
                    {
                        // If so, send the current message and start another page
                        send(sBuilder.ToString());
                        sBuilder.Clear();
                        sBuilder.AppendLine(headerValue);
                    }
                    sBuilder.AppendLine(compiledMessage);
                }
            }
            
            // Send any remaining messages
            if (sBuilder.Length > 0) send(sBuilder.ToString());
        }
    }
}
