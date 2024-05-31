using ProjectM;
using ProjectM.Network;
using Unity.Entities;
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
        
        public static void SendMessage(Entity userEntity, LocalisationSystem.LocalisableString message)
        {
            var user = Plugin.Server.EntityManager.GetComponentData<User>(userEntity);

            var language = LocalisationSystem.GetUserLanguage(user.PlatformId);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message.Build(language));
        }
        
        public static void SendMessage(ulong steamID, LocalisationSystem.LocalisableString message)
        {
            PlayerCache.FindPlayer(steamID, true, out _, out var userEntity);
            SendMessage(userEntity, message);
        }
    }
}
