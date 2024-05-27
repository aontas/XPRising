using BepInEx.Logging;
using ProjectM.Network;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    public static class PlayerInfoCommands
    {
        private static EntityManager _entityManager = Plugin.Server.EntityManager;

        [Command(name: "playerinfo", shortHand: "pi", adminOnly: false, usage: "", description: "Display the player's information details.")]
        public static void PlayerInfoCommand(ChatCommandContext ctx)
        {
            var user = ctx.Event.User;

            var name = Helper.GetTrueName(user.CharacterName.ToString().ToLower());
            var foundPlayer = Cache.NamePlayerCache.TryGetValue(name, out var playerData);
            if (!foundPlayer)
            {
                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"Current user not appearing in cache. Probably not good. [{name},{ctx.User.PlatformId}]");
                playerData = new PlayerData(
                    user.CharacterName,
                    user.PlatformId,
                    true,
                    user.IsAdmin,
                    ctx.Event.SenderUserEntity,
                    ctx.Event.SenderCharacterEntity);
            }
            
            PrintPlayerInfo(ctx, playerData);
        }
        
        [Command(name: "playerinfo", shortHand: "pi", adminOnly: false, usage: "<PlayerName>", description: "Display the requested player's information details.")]
        public static void PlayerInfoCommand(ChatCommandContext ctx, string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                throw ctx.Error("You need to provide a playerName");
            }
            
            var name = Helper.GetTrueName(playerName.ToLower());
            var foundPlayer = Cache.NamePlayerCache.TryGetValue(name, out var playerData);
            if (!foundPlayer)
            {
                throw ctx.Error("No player found with that name");
            }
            
            PrintPlayerInfo(ctx, playerData);
        }

        private static void PrintPlayerInfo(ChatCommandContext ctx, PlayerData playerData)
        {
            var playerEntityString = $"{playerData.CharEntity.Index.ToString()}:{playerData.CharEntity.Version.ToString()}";
            var userEntityString = $"{playerData.UserEntity.Index.ToString()}:{playerData.UserEntity.Version.ToString()}";
            var ping = _entityManager.GetComponentData<Latency>(playerData.CharEntity).Value;
            var position = _entityManager.GetComponentData<LocalTransform>(playerData.CharEntity).Position;

            ctx.Reply($"Name: <color={Output.White}>{playerData.CharacterName.ToString()}</color>");
            ctx.Reply($"SteamID: <color={Output.White}>{playerData.SteamID:D}</color>");
            ctx.Reply($"Latency: <color={Output.White}>{ping:F3}</color>s");
            ctx.Reply($"Admin: <color={Output.White}>{playerData.IsAdmin.ToString()}</color>");
            if (playerData.IsOnline)
            {
                ctx.Reply("-- Position --");
                ctx.Reply($"X: <color={Output.White}>{position.x:F2}</color> " +
                          $"Y: <color={Output.White}>{position.y:F2}</color> " +
                          $"Z: <color={Output.White}>{position.z:F2}</color>");
                ctx.Reply($"-- <color={Output.White}>Entities</color> --");
                ctx.Reply($"Char Entity: <color={Output.White}>{playerEntityString}");
                ctx.Reply($"User Entity: <color={Output.White}>{userEntityString}");
            }
            else
            {
                ctx.Reply("-- Position --");
                ctx.Reply($"<color={Color.Red}>Offline</color>");
            }
            if (Plugin.ExperienceSystemActive)
            {
                var currentXp = ExperienceSystem.GetXp(playerData.SteamID);
                var currentLevel = ExperienceSystem.ConvertXpToLevel(currentXp);
                ExperienceSystem.GetLevelAndProgress(currentXp, out _, out var xpEarned, out var xpNeeded);
                ctx.Reply($"-- <color={Output.White}>Experience</color> --");
                ctx.Reply($"Level: <color={Output.White}>{currentLevel.ToString()}</color> [<color={Output.White}>{xpEarned.ToString()}</color>/<color={Output.White}>{xpNeeded.ToString()}</color>]");
            }
            
            // Get buffs for user
            var statusBonus = Helper.GetAllStatBonuses(playerData.SteamID, playerData.CharEntity);
            ctx.Reply($"-- <color={Output.White}>Stat buffs</color> --");
            if (statusBonus.Count > 0)
            {
                foreach (var pair in statusBonus)
                {
                    ctx.Reply($"{Helper.CamelCaseToSpaces(pair.Key)}: {pair.Value:F2}");
                }
            }
            else
            {
                ctx.Reply("None");
            }
        }
    }
}
