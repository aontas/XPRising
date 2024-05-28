using Il2CppSystem.Text;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

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
        
        public static void SendMessage(Entity userEntity, string message)
        {
            var user = Plugin.Server.EntityManager.GetComponentData<ProjectM.Network.User>(userEntity);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
        }
        
        public static void SendMessage(User user, string message)
        {
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
        }
        
        public static void SendMessage(ulong steamID, string message)
        {
            PlayerCache.FindPlayer(steamID, true, out _, out var userEntity);
            var user = Plugin.Server.EntityManager.GetComponentData<ProjectM.Network.User>(userEntity);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
        }
    }

    public class MessageTemplate(string template)
    {
        private readonly StringBuilder _stringBuilder = new(template);

        public void Add(string field, string replacement)
        {
            _stringBuilder.Replace(field, replacement);
        }

        public string Build()
        {
            return _stringBuilder.ToString();
        }
    }
}
