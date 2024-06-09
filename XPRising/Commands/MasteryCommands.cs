using System;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands {
    [CommandGroup("mastery", "m")]
    public static class MasteryCommands {
        private static void CheckMasteryActive(ChatCommandContext ctx)
        {
            if (!Plugin.WeaponMasterySystemActive)
            {
                var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                    .AddField("{system}", "Mastery");
                throw Output.ChatError(ctx, message);
            }
        }

        [Command("get", "g", "[masteryType]", "Display your current mastery progression for your equipped or specified weapon type")]
        public static void GetMastery(ChatCommandContext ctx, string weaponType = "")
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;

            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryNoValue));
                return;
            }

            WeaponMasterySystem.MasteryType type;
            if (string.IsNullOrEmpty(weaponType))
            {
                type = WeaponMasterySystem.WeaponToMasteryType(WeaponMasterySystem.GetWeaponType(ctx.Event.SenderCharacterEntity, out _));
            }
            else if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out type))
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }
            
            var wd = Database.PlayerWeaponmastery[steamID];
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryHeader));

            MasteryData data = wd[type];
            ctx.Reply(GetMasteryDataStringForType(type, data));
        }
        
        [Command("set primary", "sp", "[masteryType]", "Set your primary mastery to the specified mastery type")]
        public static void SetPrimaryMastery(ChatCommandContext ctx, string weaponType = "")
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;

            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryNoValue));
                return;
            }

            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType))
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }

            Database.PlayerPrimaryMastery[steamID] = masteryType;
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryPrimarySet).AddField("{masteryType}", Enum.GetName(masteryType)));
        }
        
        [Command("details", "d", "", "Displays detailed information about benefits from mastery for your equipped weapon")]
        public static void GetDetails(ChatCommandContext ctx)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;

            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryNoValue));
                return;
            }
            
            PrintMasteryDetails(ctx);
        }

        [Command("get-all", "ga", "", "Display your current mastery progression in everything")]
        public static void GetAllMastery(ChatCommandContext ctx)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;
            
            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryNoValue));
                return;
            }

            var wd = Database.PlayerWeaponmastery[steamID];
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryHeader));

            foreach (var data in wd)
            {
                ctx.Reply(GetMasteryDataStringForType(data.Key, data.Value));
            }
        }

        private static string GetMasteryDataStringForType(WeaponMasterySystem.MasteryType type, MasteryData data)
        {
            var name = Enum.GetName(type);
            var mastery = data.Mastery;
            var effectiveness = WeaponMasterySystem.EffectivenessSubSystemEnabled ? data.Effectiveness : 1;
            var growth = data.Growth;
            
            return $"{name}: <color={Output.White}>{mastery:F2}%</color>";
            
            // var statData = Database.MasteryStatConfig[type].Select(config =>
            // {
            //     var val = Helper.CalcBuffValue(mastery, effectiveness, config.rate, config.type);
            //     
            //     if (Helper.percentageStats.Contains(config.type)) {
            //         return $"{Helper.CamelCaseToSpaces(config.type)} <color={Output.Green}>{val/100:F3}%</color>";
            //     }
            //
            //     return $"{Helper.CamelCaseToSpaces(config.type)} <color={Output.Green}>{val:F3}</color>";
            // });
            //
            // return $"{name}: <color={Output.White}>{mastery:F2}%</color> ({string.Join(",", statData)}) Effectiveness: {effectiveness * 100}%, Growth: {growth * 100}%";
        }

        private static void PrintMasteryDetails(ChatCommandContext ctx)
        {
            var masteryType = WeaponMasterySystem.WeaponToMasteryType(WeaponMasterySystem.GetWeaponType(ctx.Event.SenderCharacterEntity, out var weaponEntity));
            var weaponMasteryPrintableData = WeaponMasterySystem.GetWeaponMasteryPrintableData(ctx.User.PlatformId, masteryType, weaponEntity);
            var spellMasteryPrintableData = WeaponMasterySystem.GetSpellMasteryPrintableData(ctx.User.PlatformId);

            Output.ChatReply(ctx, weaponMasteryPrintableData.ToArray());
            Output.ChatReply(ctx, spellMasteryPrintableData.ToArray());
        }

        [Command("add", "a", "<weaponType> <amount>", "Adds the amount to the mastery of the specified weaponType", adminOnly: false)]
        public static void AddMastery(ChatCommandContext ctx, string weaponType, double amount)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;
            var charName = ctx.Event.User.CharacterName.ToString();
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }

            WeaponMasterySystem.ModMastery(steamID, masteryType, amount / WeaponMasterySystem.MasteryGainMultiplier);
            Output.ChatReply(
                ctx,
                L10N.Get(L10N.TemplateKey.MasteryAdjusted)
                    .AddField("{masteryType}", Enum.GetName(masteryType))
                    .AddField("{playerName}", charName)
                    .AddField("{value}", amount.ToString()));
            Helper.ApplyBuff(userEntity, charEntity, Helper.AppliedBuff);
        }
        
        [Command("set", "s", "<playerName> <weaponType> <masteryValue>", "Sets the specified player's mastery to a specific value", adminOnly: false)]
        public static void SetMastery(ChatCommandContext ctx, string name, string weaponType, double value)
        {
            CheckMasteryActive(ctx);
            ulong steamID = PlayerCache.GetSteamIDFromName(name);
            if (steamID == 0) {
                var message = L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound)
                    .AddField("{playerName}", name);
                throw Output.ChatError(ctx, message);
            }

            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }

            WeaponMasterySystem.ModMastery(steamID, masteryType, -100000);
            WeaponMasterySystem.ModMastery(steamID, masteryType, value / WeaponMasterySystem.MasteryGainMultiplier);
            Output.ChatReply(
                ctx,
                L10N.Get(L10N.TemplateKey.MasterySet)
                    .AddField("{masteryType}", Enum.GetName(masteryType))
                    .AddField("{playerName}", name)
                    .AddField("{value}", value.ToString()));
        }

        [Command("log", "l", "", "Toggles logging of mastery gain.", adminOnly: false)]
        public static void LogMastery(ChatCommandContext ctx)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerLogConfig[steamID];
            loggingData.LoggingMastery = !loggingData.LoggingMastery;
            
            var message = loggingData.LoggingMastery
                ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
                : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
            Output.ChatReply(ctx, message.AddField("{system}", "Mastery system"));
            Database.PlayerLogConfig[steamID] = loggingData;
        }


        [Command("reset", "r", "<weaponType>", "Resets a mastery to gain more power with it.", adminOnly: false)]
        public static void ResetMastery(ChatCommandContext ctx, string weaponType)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;
            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryReset).AddField("{masteryType}", Enum.GetName(masteryType)));
            WeaponMasterySystem.ResetMastery(steamID, masteryType);
        }
    }
}
