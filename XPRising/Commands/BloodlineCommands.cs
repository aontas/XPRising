using System;
using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    [CommandGroup("bloodline", "bl")]
    public static class BloodlineCommands
    {
        private static void CheckBloodlineSystemActive(ChatCommandContext ctx)
        {
            if (!Plugin.BloodlineSystemActive)
            {
                var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                    .AddField("{system}", "Bloodline");
                throw Output.ChatError(ctx, message);
            }
        }

        [Command("log", "l", "", "Toggles logging of bloodlineXP gain.", adminOnly: false)]
        public static void LogBloodline(ChatCommandContext ctx)
        {
            CheckBloodlineSystemActive(ctx);
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerLogConfig[steamID];
            loggingData.LoggingBloodline = !loggingData.LoggingBloodline;
            var message = loggingData.LoggingBloodline
                ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
                : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
            Output.ChatReply(ctx, message.AddField("{system}", "Bloodline system"));
            Database.PlayerLogConfig[steamID] = loggingData;
        }

        [Command("reset", "r", "", "Resets all bloodlines to gain more power with them.", adminOnly: false)]
        public static void ResetBloodline(ChatCommandContext ctx)
        {
            CheckBloodlineSystemActive(ctx);
            var steamID = ctx.User.PlatformId;

            GlobalMasterySystem.ResetMastery(steamID, GlobalMasterySystem.MasteryCategory.Blood);
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryReset).AddField("{masteryType}", Enum.GetName(GlobalMasterySystem.MasteryCategory.Blood)));
        }
    }
}
