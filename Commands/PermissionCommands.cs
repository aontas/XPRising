using System;
using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    public static class PermissionCommands
    {
        [Command(name: "permission", shortHand: "p", usage: "<command | user>", description: "Display current privilege levels for users or commands.", adminOnly: true)]
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
        
        [Command(name: "permission set user", shortHand: "psu", usage: "<playerName> <0-100>", description: "Sets the privilege level for a user.", adminOnly: true)]
        public static void PermissionSetUser(ChatCommandContext ctx, string playerName, int level)
        {
            level = Math.Clamp(level, 0, 100);

            var steamID = PlayerCache.GetSteamIDFromName(playerName);
            if (steamID == ctx.User.PlatformId) throw ctx.Error($"You cannot modify your own privilege level.");
            if (steamID == 0) throw ctx.Error($"Could not find specified player \"{playerName}\".");
            if (level == 0) Database.UserPermission.Remove(steamID);
            else Database.UserPermission[steamID] = level;
            ctx.Reply($"Player \"{playerName}\" permission is now set to <color={Output.White}>{level}</color>.");
        }
        
        [Command(name: "permission set command", shortHand: "psc", usage: "<command> <0-100>", description: "Sets the required privilege level for a command.", adminOnly: true)]
        public static void PermissionSetCommand(ChatCommandContext ctx, string command, int level)
        {
            var maxPrivilege = PermissionSystem.GetUserPermission(ctx.User.PlatformId);
            if (level > maxPrivilege)
            {
                throw ctx.Error($"You cannot set a command's privilege higher than your own");
            }
            level = Math.Clamp(level, 0, maxPrivilege);
            if (!Database.CommandPermission.ContainsKey(command))
            {
                throw ctx.Error($"Command ({command}) is not recognised as a valid command.");
            }

            Database.CommandPermission[command] = level;
            ctx.Reply($"Command \"{command}\" required privilege is now set to <color={Output.White}>{level}</color>.");
        }
    }
}