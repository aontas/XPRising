using System.Linq;
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
            var user = Plugin.Server.EntityManager.GetComponentData<User>(userEntity);
            if (Plugin.IsDebug) ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
        }
        
        public static void DebugMessage(ulong steamID, string message)
        {
            PlayerCache.FindPlayer(steamID, true, out _, out var userEntity);
            DebugMessage(userEntity, message);
        }
        
        public static void SendMessage(Entity userEntity, L10N.LocalisableString message)
        {
            if (!Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user)) return;

            var language = L10N.GetUserLanguage(user.PlatformId);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message.Build(language));
        }
        
        public static void SendMessage(ulong steamID, L10N.LocalisableString message)
        {
            PlayerCache.FindPlayer(steamID, true, out _, out var userEntity);
            SendMessage(userEntity, message);
        }
        
        public static void ChatReply(ChatCommandContext ctx, params L10N.LocalisableString[] messages)
        {
            var language = L10N.GetUserLanguage(ctx.User.PlatformId);
            if (messages.Length > 1)
            {
                // Make bigger messages smaller
                ctx.Reply($"<size=10>{string.Join("\n", messages.Select(m => m.Build(language)))}</size>");
            }
            else if (messages.Length == 1)
            {
                ctx.Reply(messages[0].Build(language));
            }
        }
        
        public static CommandException ChatError(ChatCommandContext ctx, L10N.LocalisableString message)
        {
            var language = L10N.GetUserLanguage(ctx.User.PlatformId);
            return ctx.Error(message.Build(language));
        }
    }
}
