using System;
using System.Text;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Systems;

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

        public static void DebugMessage(Entity userEntity, string message)
        {
            if (Plugin.IsDebug && Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user))
            {
                ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, $"<size=10>{message}</size>");
            }
        }
        
        public static void DebugMessage(ulong steamID, string message)
        {
            if (Plugin.IsDebug && PlayerCache.FindPlayer(steamID, true, out _, out var userEntity))
            {
                DebugMessage(userEntity, message);
            }
        }
        
        public static void SendMessage(Entity userEntity, L10N.LocalisableString message)
        {
            if (!Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user)) return;

            var language = L10N.GetUserLanguage(user.PlatformId);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, $"<size={Plugin.TextSize}>{message.Build(language)}");
        }
        
        public static void SendMessage(ulong steamID, L10N.LocalisableString message)
        {
            PlayerCache.FindPlayer(steamID, true, out _, out var userEntity);
            SendMessage(userEntity, message);
        }

        public static void SendMessages(ulong steamID, L10N.LocalisableString header, L10N.LocalisableString[] messages)
        {
            PlayerCache.FindPlayer(steamID, true, out _, out var userEntity);
            if (!Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user)) return;
            
            SendMessages(Send, steamID, header, messages);
            return;

            void Send(string message)
            {
                ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
            }
        }
        
        public static void ChatReply(ChatCommandContext ctx, L10N.LocalisableString message)
        {
            var language = L10N.GetUserLanguage(ctx.User.PlatformId);
            ctx.Reply($"<size={Plugin.TextSize}>{message.Build(language)}");
        }

        // This is based on the MAX_MESSAGE_SIZE from VCF.
        private const int MaxCharacterCount = 450;
        public static void ChatReply(ChatCommandContext ctx, L10N.LocalisableString header, params L10N.LocalisableString[] messages)
        {
            SendMessages(ctx.Reply, ctx.User.PlatformId, header, messages);
        }
        
        public static CommandException ChatError(ChatCommandContext ctx, L10N.LocalisableString message)
        {
            var language = L10N.GetUserLanguage(ctx.User.PlatformId);
            return ctx.Error($"<size={Plugin.TextSize}>{message.Build(language)}");
        }

        private static void SendMessages(Action<string> send, ulong steamID, L10N.LocalisableString header, params L10N.LocalisableString[] messages)
        {
            var language = L10N.GetUserLanguage(steamID);

            var headerValue = $"<size={Plugin.TextSize}>{header.Build(language)}";
            var sBuilder = new StringBuilder();
            foreach (var message in messages)
            {
                var compiledMessage = message.Build(language);
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
