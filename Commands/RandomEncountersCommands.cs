using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    [CommandGroup("re")]
    internal class RandomEncountersCommands
    {
        [Command("start", usage: "", description: "Starts an encounter for a random online user.", adminOnly: false)]
        public static void StartCommand(ChatCommandContext ctx)
        {
            RandomEncountersSystem.StartEncounter();
            ctx.Reply("The hunt has begun...");
            return;
        }

        [Command("me", usage: "", description: "Starts an encounter for the admin who sends the command.", adminOnly: false)]
        public static void MeCommand(ChatCommandContext ctx)
        {
            if (!Cache.SteamPlayerCache.TryGetValue(ctx.User.PlatformId, out var playerData))
            {
                throw ctx.Error("For some reason your user is not in the player cache...");
            }
            
            RandomEncountersSystem.StartEncounter(playerData);
            ctx.Reply("Prepare for the fight...");
            return;
        }

        [Command("player", usage: "<PlayerName>", description: "Starts an encounter for the given player.", adminOnly: false)]
        public static void PlayerCommand(ChatCommandContext ctx, string playerName)
        {
            if (!Cache.NamePlayerCache.TryGetValue(playerName.ToLower(), out var playerData))
            {
                throw ctx.Error($"Player not found");
            }
            if(!playerData.IsOnline)
            {
                throw ctx.Error($"Could not find an online player with name {playerName}");
            }
            RandomEncountersSystem.StartEncounter(playerData);
            ctx.Reply($"Sending an ambush to {playerName}.");
        }

        [Command("enable", usage: "", description: "Enables the random encounter timer.", adminOnly: false)]
        public static void EnableCommand(ChatCommandContext ctx)
        {
            if (Plugin.RandomEncountersSystemActive)
            {
                throw ctx.Error("Already enabled.");
            }
            Plugin.RandomEncountersSystemActive = true;
            RandomEncounters.StartEncounterTimer();
            ctx.Reply($"Enabled");
        }

        [Command("disable", usage: "", description: "Disables the random encounter timer.", adminOnly: false)]
        public static void DisableCommand(ChatCommandContext ctx)
        {
            if (!Plugin.RandomEncountersSystemActive)
            {
                throw ctx.Error("Already disabled.");
            }
            Plugin.RandomEncountersSystemActive = false;
            RandomEncounters.EncounterTimer.Stop();
            ctx.Reply("Disabled.");
        }
    }
}
