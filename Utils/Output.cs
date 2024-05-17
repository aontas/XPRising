using ProjectM;
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
        public static void SendLore(Entity userEntity, string message)
        {
            var user = Plugin.Server.EntityManager.GetComponentData<ProjectM.Network.User>(userEntity);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
        }
    }
}
