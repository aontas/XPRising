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
        
        [Command("challenge list", shortHand: "cl", usage: "", description: "Lists available challenges and their progress", adminOnly: false)]
        public static void ChallengeListCommand(ChatCommandContext ctx)
        {
            CheckChallengeSystemActive(ctx);
            var challenges = ChallengeSystem.ListChallenges(ctx.User.PlatformId);
            var output = challenges.Select(challenge =>
                new L10N.LocalisableString($"{challenge.challenge.Label}: {challenge.status}"));
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.ChallengeListHeader), output.ToArray());
        }
        
        [Command("challenge toggle", shortHand: "ct", usage: "<index>", description: "Accepts or resets the challenge at the specified index", adminOnly: false)]
        public static void ChallengeToggleCommand(ChatCommandContext ctx, int challengeIndex)
        {
            CheckChallengeSystemActive(ctx);
            ChallengeSystem.ToggleChallenge(ctx.User.PlatformId, challengeIndex);
        }
        
        [Command("challenge leaderboard", shortHand: "clb", usage: "<index>", description: "Shows the leaderboard for the challenge at the specified index", adminOnly: false)]
        public static void ChallengeStats(ChatCommandContext ctx, int challengeIndex)
        {
            CheckChallengeSystemActive(ctx);
            var stats = ChallengeSystem.ListChallengeStats(challengeIndex, 5, out var challenge);
            var output = stats.Count == 0 ?
                [L10N.Get(L10N.TemplateKey.ChallengeLeaderboardEmpty)] :
                stats.Select((stat, index) =>
                {
                    var scoreStrings = new List<string>();
                    if (stat.Item2.FastestTime > TimeSpan.Zero) scoreStrings.Add($"{FormatTimeSpan(stat.Item2.FastestTime),14}");
                    if (stat.Item2.Score > 0) scoreStrings.Add($"{stat.Item2.Score:D6}");
                    scoreStrings.Add(PlayerCache.GetNameFromSteamID(stat.Item1));
                    
                    return new L10N.LocalisableString($"{index + 1,3:D}: {string.Join(" - ", scoreStrings)}");
                }).ToArray();
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.ChallengeLeaderboard).AddField("{challenge}", challenge.Label), output);
        }

        [Command("challenge log", "clog", "", "Toggles logging of challenges.", adminOnly: false)]
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
        
        private static string FormatTimeSpan(TimeSpan ts)
        {
            return ts.TotalHours >= 1 ? $@"{ts.TotalHours:F0}h {ts:mm\m\ ss\.ff\s}" : $@"{ts:mm\m\ ss\.ff\s}";
        }
    }
}
