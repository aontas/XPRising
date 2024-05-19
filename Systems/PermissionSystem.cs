using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Utils;

namespace XPRising.Systems
{
    public static class PermissionSystem
    {
        private static EntityManager em = Plugin.Server.EntityManager;

        public static int HighestPrivilege = 100;
        public static int LowestPrivilege = 0;

        public static int GetUserPermission(ulong steamID)
        {
            return Database.UserPermission.GetValueOrDefault(steamID, LowestPrivilege);
        }

        public static int GetCommandPermission(string command)
        {
            return Database.CommandPermission.GetValueOrDefault(command, HighestPrivilege);
        }

        private static object SendPermissionList(ChatCommandContext ctx, List<string> messages)
        {
            foreach(var m in messages)
            {
                ctx.Reply(m);
            }
            return new object();
        }

        public static void UserPermissionList(ChatCommandContext ctx)
        {
            var sortedPermission = Database.UserPermission.ToList();
            // Sort by privilege descending
            sortedPermission.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            ctx.Reply($"===================================");
            if (sortedPermission.Count == 0) ctx.Reply($"<color={Output.White}>No permissions</color>");
            else
            {
                foreach (var (item, index) in sortedPermission.Select((item, index) => (item, index)))
                {
                    ctx.Reply($"{index}. <color={Output.White}>{Helper.GetNameFromSteamID(item.Key)} : {item.Value}</color>");
                }
            }
            ctx.Reply($"===================================");
        }
        
        public static void CommandPermissionList(ChatCommandContext ctx)
        {
            var sortedPermission = Database.CommandPermission.ToList();
            // Sort by command name
            sortedPermission.Sort((pair1, pair2) => String.Compare(pair1.Key, pair2.Key, StringComparison.CurrentCultureIgnoreCase));
            ctx.Reply($"===================================");
            if (sortedPermission.Count == 0) ctx.Reply($"<color={Output.White}>No commands</color>");
            else
            {
                foreach (var (item, index) in sortedPermission.Select((item, index) => (item, index)))
                {
                    ctx.Reply($"{index}. <color={Output.White}>{item.Key} : {item.Value}</color>");
                }
            }
            ctx.Reply($"===================================");
        }

        public static LazyDictionary<string, int> DefaultCommandPermissions()
        {
            var permissions = new LazyDictionary<string, int>()
            {
                {"bloodline add [2]", 100},
                {"bloodline get", 0},
                {"bloodline get-all", 0},
                {"bloodline log", 0},
                {"bloodline reset [1]", 0},
                {"bloodline set [3]", 100},
                {"db load", 100},
                {"db save", 100},
                {"db wipe", 100},
                {"experience ability [2]", 0},
                {"experience ability reset", 50},
                {"experience ability show", 0},
                {"experience bump20", 0},
                {"experience get", 0},
                {"experience log", 0},
                {"experience set [2]", 100},
                {"group add", 0},
                {"group ignore", 0},
                {"group leave", 0},
                {"group no", 0},
                {"group show", 0},
                {"group wipe", 100},
                {"group yes", 0},
                {"mastery add [2]", 100},
                {"mastery get", 0},
                {"mastery get-all", 0},
                {"mastery log", 0},
                {"mastery reset [1]", 0},
                {"mastery set [3]", 100},
                {"permission", 100},
                {"permission set command [2]", 100},
                {"permission set user [2]", 100},
                {"playerinfo", 0},
                {"playerinfo [1]", 100},
                {"wanted fixminions", 100},
                {"wanted get", 0},
                {"wanted log", 0},
                {"wanted set [3]", 100},
                {"wanted trigger [1]", 100}
            };
            return permissions;
        }
    }
}
