using System;
using System.Collections.Generic;
using System.Linq;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands {
    [CommandGroup("mastery", "m")]
    public static class MasteryCommands {
        private static void CheckMasteryActive(ChatCommandContext ctx)
        {
            if (!Plugin.WeaponMasterySystemActive && !Plugin.BloodlineSystemActive)
            {
                var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                    .AddField("{system}", "Mastery");
                throw Output.ChatError(ctx, message);
            }
        }

        [Command("get", "g", "[masteryType]", "Display your current mastery progression for your active or specified mastery type")]
        public static void GetMastery(ChatCommandContext ctx, string masteryTypeInput = "")
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;

            if (!Database.PlayerMastery.ContainsKey(steamID)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryNoValue));
                return;
            }

            var masteriesToPrint = new List<GlobalMasterySystem.MasteryType>();
            if (string.IsNullOrEmpty(masteryTypeInput))
            {
                var activeWeaponMastery = WeaponMasterySystem.WeaponToMasteryType(WeaponMasterySystem.GetWeaponType(ctx.Event.SenderCharacterEntity, out _));
                var activeBloodMastery = BloodlineSystem.BloodMasteryType(ctx.Event.SenderCharacterEntity);
                masteriesToPrint.Add(activeWeaponMastery);
                masteriesToPrint.Add(activeBloodMastery);

                if (!GlobalMasterySystem.SpellMasteryRequiresUnarmed ||
                    activeWeaponMastery == GlobalMasterySystem.MasteryType.WeaponUnarmed)
                {
                    masteriesToPrint.Add(GlobalMasterySystem.MasteryType.Spell);
                }
            }
            else if (!GlobalMasterySystem.KeywordToMasteryMap.TryGetValue(masteryTypeInput.ToLower(), out var lookupMasteryType))
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }
            else
            {
                masteriesToPrint.Add(lookupMasteryType);
            }
            
            var wd = Database.PlayerMastery[steamID];

            Output.ChatReply(ctx,
                L10N.Get(L10N.TemplateKey.MasteryHeader),
                masteriesToPrint.Select(masteryType =>
            {
                MasteryData data = wd[masteryType];
                return new L10N.LocalisableString(GetMasteryDataStringForType(masteryType, data));
            }).ToArray());
        }

        [Command("get-all", "ga", "", "Displays your current mastery progression in for all types that have progression (zero progression masteries are not shown).")]
        public static void GetAllMastery(ChatCommandContext ctx)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;
            
            if (!Database.PlayerMastery.ContainsKey(steamID)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryNoValue));
                return;
            }

            var playerMastery = Database.PlayerMastery[steamID];
            Output.ChatReply(ctx,
                L10N.Get(L10N.TemplateKey.MasteryHeader),
                playerMastery.Where(data => data.Value.Mastery > 0).Select(data => new L10N.LocalisableString(GetMasteryDataStringForType(data.Key, data.Value))).ToArray());
        }

        private static string GetMasteryDataStringForType(GlobalMasterySystem.MasteryType type, MasteryData data)
        {
            var name = Enum.GetName(type);
            var mastery = data.Mastery;
            var effectiveness = GlobalMasterySystem.EffectivenessSubSystemEnabled ? $" (E: {data.Effectiveness * 100:F3}%, G: {data.Growth * 100:F3}%)" : "";
            
            return $"{name}: <color={Output.White}>{mastery:F3}%</color>{effectiveness}";
        }

        [Command("add", "a", "<type> <amount>", "Adds the amount to the mastery of the specified type", adminOnly: false)]
        public static void AddMastery(ChatCommandContext ctx, string weaponType, double amount)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;
            var charName = ctx.Event.User.CharacterName.ToString();
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            if (!GlobalMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }

            GlobalMasterySystem.ModMastery(steamID, masteryType, amount);
            Output.ChatReply(
                ctx,
                L10N.Get(L10N.TemplateKey.MasteryAdjusted)
                    .AddField("{masteryType}", Enum.GetName(masteryType))
                    .AddField("{playerName}", charName)
                    .AddField("{value}", amount.ToString()));
            BuffUtil.ApplyStatBuffOnDelay(ctx.User, userEntity, charEntity);
        }
        
        [Command("set", "s", "<playerName> <masteryType> <masteryValue>", "Sets the specified player's mastery to a specific value", adminOnly: false)]
        public static void SetMastery(ChatCommandContext ctx, string name, string weaponType, double value)
        {
            CheckMasteryActive(ctx);
            ulong steamID = PlayerCache.GetSteamIDFromName(name);
            if (steamID == 0) {
                var message = L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound)
                    .AddField("{playerName}", name);
                throw Output.ChatError(ctx, message);
            }

            if (!GlobalMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                return;
            }

            GlobalMasterySystem.ModMastery(steamID, masteryType, -100000);
            GlobalMasterySystem.ModMastery(steamID, masteryType, value);
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
            var loggingData = Database.PlayerPreferences[steamID];
            loggingData.LoggingMastery = !loggingData.LoggingMastery;
            
            var message = loggingData.LoggingMastery
                ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
                : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
            Output.ChatReply(ctx, message.AddField("{system}", "Mastery system"));
            Database.PlayerPreferences[steamID] = loggingData;
        }

        [Command("reset", "r", "", "Resets all weapon mastery to gain more power.", adminOnly: false)]
        public static void ResetMastery(ChatCommandContext ctx)
        {
            CheckMasteryActive(ctx);
            var steamID = ctx.Event.User.PlatformId;
            GlobalMasterySystem.ResetMastery(steamID, GlobalMasterySystem.MasteryCategory.Weapon);
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryReset).AddField("{masteryType}", Enum.GetName(GlobalMasterySystem.MasteryCategory.Weapon)));
        }
    }
}
