using System;
using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    public static class PermissionCommands
    {
        [Command(name: "permission add admin", shortHand: "paa", usage: "", description: "Gives the current user the max privilege level. Requires user to be admin.", adminOnly: true)]
        public static void PermissionAddAdmin(ChatCommandContext ctx)
        {
            Database.UserPermission[ctx.User.PlatformId] = PermissionSystem.HighestPrivilege;
            ctx.Reply($"Added \"{ctx.Name}\" permission is now set to <color={Output.White}>{PermissionSystem.HighestPrivilege}</color>.");
        }
        
        [Command(name: "permission", shortHand: "p", usage: "<command | user>", description: "Display current privilege levels for users or commands.")]
        public static void PermissionList(ChatCommandContext ctx, string option = "user")
        {
            switch (option)
            {
                case "user":
                    PermissionSystem.UserPermissionList(ctx);
                    break;
                case "command":
                    PermissionSystem.CommandPermissionList(ctx);
                    break;
                default:
                    throw ctx.Error($"Option not recognised ({option}). Use either \"user\" or \"command\".");
            }
        }
        
        [Command(name: "permission set user", shortHand: "psu", usage: "<playerName> <0-100>", description: "Sets the privilege level for a user.")]
        public static void PermissionSetUser(ChatCommandContext ctx, string playerName, int level)
        {
            level = Math.Clamp(level, PermissionSystem.LowestPrivilege, PermissionSystem.HighestPrivilege);

            var steamID = PlayerCache.GetSteamIDFromName(playerName);
            if (steamID == ctx.User.PlatformId) throw ctx.Error($"You cannot modify your own privilege level.");
            if (steamID == 0) throw ctx.Error($"Could not find specified player \"{playerName}\".");
            if (level == PermissionSystem.LowestPrivilege) Database.UserPermission.Remove(steamID);
            else Database.UserPermission[steamID] = level;
            ctx.Reply($"Player \"{playerName}\" permission is now set to <color={Output.White}>{level}</color>.");
        }
        
        [Command(name: "permission set command", shortHand: "psc", usage: "<command> <0-100>", description: "Sets the required privilege level for a command.")]
        public static void PermissionSetCommand(ChatCommandContext ctx, string command, int level)
        {
            var maxPrivilege = PermissionSystem.GetUserPermission(ctx.User.PlatformId);
            if (level > maxPrivilege)
            {
                throw ctx.Error($"You cannot set a command's privilege higher than your own");
            }
            level = Math.Clamp(level, PermissionSystem.LowestPrivilege, maxPrivilege);
            if (!Database.CommandPermission.ContainsKey(command))
            {
                throw ctx.Error($"Command ({command}) is not recognised as a valid command.");
            }

            Database.CommandPermission[command] = level;
            ctx.Reply($"Command \"{command}\" required privilege is now set to <color={Output.White}>{level}</color>.");
        }
    }
}