using System.Collections.Generic;
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

        [Command(name: "playerbuffs", shortHand: "pb", adminOnly: false, usage: "", description: "Display the player's buff details.")]
        public static void PlayerBuffCommand(ChatCommandContext ctx)
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
            
            var messages = new List<L10N.LocalisableString>();
            GenerateBuffStatus(playerData, ref messages);
            Output.ChatReply(ctx, messages.ToArray());
        }
        
        [Command(name: "playerinfo", shortHand: "pi", adminOnly: false, usage: "<PlayerName>", description: "Display the requested player's information details.")]
        public static void PlayerInfoCommand(ChatCommandContext ctx, string playerName)
        {
            var name = Helper.GetTrueName(playerName.ToLower());
            var foundPlayer = Cache.NamePlayerCache.TryGetValue(name, out var playerData);
            if (!foundPlayer)
            {
                var message = L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound).AddField("{playerName}", playerName);
                throw Output.ChatError(ctx, message);
            }
            
            PrintPlayerInfo(ctx, playerData);
        }

        public static void GeneratePlayerDebugInfo(PlayerData playerData, ref List<L10N.LocalisableString> messages)
        {
            var playerEntityString = $"{playerData.CharEntity.Index.ToString()}:{playerData.CharEntity.Version.ToString()}";
            var userEntityString = $"{playerData.UserEntity.Index.ToString()}:{playerData.UserEntity.Version.ToString()}";
            var ping = _entityManager.GetComponentData<Latency>(playerData.CharEntity).Value;
            var position = _entityManager.GetComponentData<LocalTransform>(playerData.CharEntity).Position;
            
            messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoName).AddField("{playerName}", playerData.CharacterName.ToString()));
            messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoSteamID).AddField("{steamID}", $"{playerData.SteamID:D}"));
            messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoLatency).AddField("{value}", $"{ping:F3}"));
            messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoAdmin).AddField("{admin}", playerData.IsAdmin.ToString()));
            if (playerData.IsOnline)
            {
                messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoPosition));
                messages.Add(new L10N.LocalisableString(
                    $"X: <color={Output.White}>{position.x:F2}</color> " +
                    $"Y: <color={Output.White}>{position.y:F2}</color> " +
                    $"Z: <color={Output.White}>{position.z:F2}</color>"));
                if (Plugin.IsDebug)
                {
                    messages.Add(new L10N.LocalisableString($"-- <color={Output.White}>Entities</color> --"));
                    messages.Add(new L10N.LocalisableString($"Char Entity: <color={Output.White}>{playerEntityString}</color>"));
                    messages.Add(new L10N.LocalisableString($"User Entity: <color={Output.White}>{userEntityString}</color>"));
                }
            }
            else
            {
                messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoPosition));
                messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoOffline));
            }
        }

        public static void GenerateXPStatus(PlayerData playerData, ref List<L10N.LocalisableString> messages)
        {
            var currentXp = ExperienceSystem.GetXp(playerData.SteamID);
            var currentLevel = ExperienceSystem.ConvertXpToLevel(currentXp);
            ExperienceSystem.GetLevelAndProgress(currentXp, out var progress, out var xpEarned, out var xpNeeded);
            var message = L10N.Get(L10N.TemplateKey.XpLevel)
                .AddField("{level}", currentLevel.ToString())
                .AddField("{progress}", progress.ToString())
                .AddField("{earned}", xpEarned.ToString())
                .AddField("{needed}", xpNeeded.ToString());
            messages.Add(message);
        }

        public static void GenerateBuffStatus(PlayerData playerData, ref List<L10N.LocalisableString> messages)
        {
            // Get buffs for user
            var statusBonus = Helper.GetAllStatBonuses(playerData.SteamID, playerData.CharEntity);
            messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoBuffs));
            if (statusBonus.Count > 0)
            {
                foreach (var pair in statusBonus)
                {
                    var valueString = Helper.percentageStats.Contains(pair.Key) ? $"{pair.Value / 100:F3}%" : $"{pair.Value:F2}";
                    messages.Add(new L10N.LocalisableString(
                        $"{Helper.CamelCaseToSpaces(pair.Key)}: <color={Output.White}>{valueString}</color>"));
                }
            }
            else
            {
                messages.Add(L10N.Get(L10N.TemplateKey.PlayerInfoNoBuffs));
            }
        }

        private static void PrintPlayerInfo(ChatCommandContext ctx, PlayerData playerData)
        {
            var messages = new List<L10N.LocalisableString>();
            GeneratePlayerDebugInfo(playerData, ref messages);
            
            if (Plugin.ExperienceSystemActive) GenerateXPStatus(playerData, ref messages);
            
            // Buffs can be large, so print and clear, then send print the buffs separately.
            Output.ChatReply(ctx, messages.ToArray());
            messages.Clear();
            GenerateBuffStatus(playerData, ref messages);
            Output.ChatReply(ctx, messages.ToArray());
        }
    }
}
