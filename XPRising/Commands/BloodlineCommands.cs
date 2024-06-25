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
