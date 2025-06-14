using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    public static class ChallengeCommands {
        private static void CheckChallengeSystemActive(ChatCommandContext ctx)
        {
            if (!Plugin.ChallengeSystemActive)
            {
                var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                    .AddField("{system}", "Challenge");
                throw Output.ChatError(ctx, message);
            }
        }
        
        [Command("challenges", usage: "", description: "Lists available challenges and their progress", adminOnly: false)]
        public static void ChallengeListCommand(ChatCommandContext ctx)
        {
            var challenges = ChallengeSystem.ListChallenges(ctx.User.PlatformId);
            var output = challenges.Select(challenge =>
                new L10N.LocalisableString($"{challenge.challenge.label}: {challenge.status}"));
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.ChallengeListHeader), output.ToArray());
        }
        
        [Command("challenge toggle", shortHand: "ct", usage: "<index>", description: "Accepts or resets the challenge at specified index", adminOnly: false)]
        public static void ChallengeToggleCommand(ChatCommandContext ctx, int challengeIndex)
        {
            ChallengeSystem.ToggleChallenge(ctx.User.PlatformId, challengeIndex);
        }

        [Command("challenge log", "cl", "", "Toggles logging of challenges.", adminOnly: false)]
        public static void LogChallenges(ChatCommandContext ctx)
        {
            CheckChallengeSystemActive(ctx);
        
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerPreferences[steamID];
            loggingData.LoggingChallenges = !loggingData.LoggingChallenges;
            var message = loggingData.LoggingChallenges
                ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
                : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
            Output.ChatReply(ctx, message.AddField("{system}", "Challenge"));
            Database.PlayerPreferences[steamID] = loggingData;
        }
    }
}
