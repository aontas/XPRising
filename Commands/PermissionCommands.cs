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
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.PermissionPlayerSet)
                .AddField("{playerName}", ctx.Name)
                .AddField("{value}", PermissionSystem.HighestPrivilege.ToString()));
        }
        
        [Command(name: "permission command", shortHand: "p c", usage: "", description: "Display current privilege levels for commands.")]
        public static void PermissionListCommand(ChatCommandContext ctx)
        {
            PermissionSystem.CommandPermissionList(ctx);
        }
        
        [Command(name: "permission user", shortHand: "p u", usage: "", description: "Display current privilege levels for users.")]
        public static void PermissionListUser(ChatCommandContext ctx, string option = "user")
        {
            PermissionSystem.UserPermissionList(ctx);
        }
        
        [Command(name: "permission set user", shortHand: "psu", usage: "<playerName> <0-100>", description: "Sets the privilege level for a user.")]
        public static void PermissionSetUser(ChatCommandContext ctx, string playerName, int level)
        {
            level = Math.Clamp(level, PermissionSystem.LowestPrivilege, PermissionSystem.HighestPrivilege);

            var steamID = PlayerCache.GetSteamIDFromName(playerName);
            if (steamID == ctx.User.PlatformId) throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.PermissionModifySelfError));
            var maxPrivilege = PermissionSystem.GetUserPermission(ctx.User.PlatformId);
            if (level > maxPrivilege) throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.PermissionModifyHigherError));
            if (steamID == 0) throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound).AddField("{playerName}", playerName));
            if (level == PermissionSystem.LowestPrivilege) Database.UserPermission.Remove(steamID);
            else Database.UserPermission[steamID] = level;
            
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.PermissionPlayerSet)
                .AddField("{playerName}", playerName)
                .AddField("{value}", level.ToString()));
        }
        
        [Command(name: "permission set command", shortHand: "psc", usage: "<command> <0-100>", description: "Sets the required privilege level for a command.")]
        public static void PermissionSetCommand(ChatCommandContext ctx, string command, int level)
        {
            var maxPrivilege = PermissionSystem.GetUserPermission(ctx.User.PlatformId);
            if (level > maxPrivilege)
            {
                throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.PermissionModifyHigherError));
            }
            level = Math.Clamp(level, PermissionSystem.LowestPrivilege, maxPrivilege);
            if (!Database.CommandPermission.ContainsKey(command))
            {
                throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.PermissionCommandUnknown).AddField("{command}", command));
            }

            Database.CommandPermission[command] = level;
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.PermissionCommandSet)
                .AddField("{command}", command)
                .AddField("{value}", level.ToString()));
        }
    }
}