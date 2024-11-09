using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Transport;
using XPRising.Utils;

namespace XPRising.Commands {
    [CommandGroup("mastery", "m")]
    public static class MasteryCommands {
        private static void CheckSystemActive(ChatCommandContext ctx, bool active, string label)
        {
            if (!active)
            {
                var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                    .AddField("{system}", label);
                throw Output.ChatError(ctx, message);
            }
        }
        
        [Command("get", "g", "[masteryType]", "Display your current mastery progression for your active or specified mastery type")]
        public static void GetMastery(ChatCommandContext ctx, string masteryTypeInput = "")
        {
            CheckSystemActive(ctx, Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive, "Mastery");
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
                var options = string.Join(", ", GlobalMasterySystem.KeywordToMasteryMap.Keys);
                Output.ChatReply(ctx, new L10N.LocalisableString($"({options})"));
                return;
            }
            else
            {
                masteriesToPrint.Add(lookupMasteryType);
            }
            
            var wd = Database.PlayerMastery[steamID];
            var preferences = Database.PlayerPreferences[steamID];

            Output.ChatReply(ctx,
                L10N.Get(L10N.TemplateKey.MasteryHeader),
                masteriesToPrint.Select(masteryType =>
            {
                MasteryData data = wd[masteryType];
                return new L10N.LocalisableString(GetMasteryDataStringForType(ClientActionHandler.MasteryTooltip(masteryType, preferences.Language), data));
            }).ToArray());
        }

        [Command("get-all", "ga", "", "Displays your current mastery progression in for all types that have progression (zero progression masteries are not shown).")]
        public static void GetAllMastery(ChatCommandContext ctx)
        {
            CheckSystemActive(ctx, Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive, "Mastery");
            var steamID = ctx.Event.User.PlatformId;
            
            if (!Database.PlayerMastery.ContainsKey(steamID)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryNoValue));
                return;
            }

            var preferences = Database.PlayerPreferences[steamID];
            var playerMastery = Database.PlayerMastery[steamID];
            Output.ChatReply(ctx,
                L10N.Get(L10N.TemplateKey.MasteryHeader),
                playerMastery
                    .Where(data => data.Value.Mastery > 0)
                    .Select(data => new L10N.LocalisableString(GetMasteryDataStringForType(ClientActionHandler.MasteryTooltip(data.Key, preferences.Language), data.Value))).ToArray());
        }

        private static string GetMasteryDataStringForType(string name, MasteryData data)
        {
            var mastery = data.Mastery;
            var effectiveness = GlobalMasterySystem.EffectivenessSubSystemEnabled ? $" (E: {data.Effectiveness * 100:F3}%, G: {data.Growth * 100:F3}%)" : "";
            
            return $"{name}: <color={Output.White}>{mastery:F3}%</color>{effectiveness}";
        }

        [Command("add", "a", "<type> <amount>", "Adds the amount to the mastery of the specified type", adminOnly: false)]
        public static void AddMastery(ChatCommandContext ctx, string weaponType, double amount)
        {
            CheckSystemActive(ctx, Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive, "Mastery");
            var steamID = ctx.Event.User.PlatformId;
            var charName = ctx.Event.User.CharacterName.ToString();
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            if (!GlobalMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                var options = string.Join(", ", GlobalMasterySystem.KeywordToMasteryMap.Keys);
                Output.ChatReply(ctx, new L10N.LocalisableString($"({options})"));
                return;
            }

            var preferences = Database.PlayerPreferences[steamID];
            GlobalMasterySystem.ModMastery(steamID, masteryType, amount);
            Output.ChatReply(
                ctx,
                L10N.Get(L10N.TemplateKey.MasteryAdjusted)
                    .AddField("{masteryType}", ClientActionHandler.MasteryTooltip(masteryType, preferences.Language))
                    .AddField("{playerName}", charName)
                    .AddField("{value}", amount.ToString()));
            BuffUtil.ApplyStatBuffOnDelay(ctx.User, userEntity, charEntity);
        }
        
        [Command("set", "s", "<playerName> <masteryType> <masteryValue>", "Sets the specified player's mastery to a specific value", adminOnly: false)]
        public static void SetMastery(ChatCommandContext ctx, string name, string weaponType, double value)
        {
            CheckSystemActive(ctx, Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive, "Mastery");
            ulong steamID = PlayerCache.GetSteamIDFromName(name);
            if (steamID == 0) {
                var message = L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound)
                    .AddField("{playerName}", name);
                throw Output.ChatError(ctx, message);
            }

            if (!GlobalMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                var options = string.Join(", ", GlobalMasterySystem.KeywordToMasteryMap.Keys);
                Output.ChatReply(ctx, new L10N.LocalisableString($"({options})"));
                return;
            }

            GlobalMasterySystem.ModMastery(steamID, masteryType, -100000);
            GlobalMasterySystem.ModMastery(steamID, masteryType, value);
            var preferences = Database.PlayerPreferences[steamID];
            Output.ChatReply(
                ctx,
                L10N.Get(L10N.TemplateKey.MasterySet)
                    .AddField("{masteryType}", ClientActionHandler.MasteryTooltip(masteryType, preferences.Language))
                    .AddField("{playerName}", name)
                    .AddField("{value}", $"{value:F0}"));
        }

        [Command("log", "l", "", "Toggles logging of mastery gain.", adminOnly: false)]
        public static void LogMastery(ChatCommandContext ctx)
        {
            CheckSystemActive(ctx, Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive, "Mastery");
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerPreferences[steamID];
            loggingData.LoggingMastery = !loggingData.LoggingMastery;
            
            var message = loggingData.LoggingMastery
                ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
                : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
            Output.ChatReply(ctx, message.AddField("{system}", "Mastery system"));
            Database.PlayerPreferences[steamID] = loggingData;
        }

        [Command("reset-all", "ra", "[category]", "Resets all mastery to gain more power. Category can be used to reset all weapons vs all bloodlines.", adminOnly: false)]
        public static void ResetAllMastery(ChatCommandContext ctx, string categoryInput = "")
        {
            CheckSystemActive(ctx, Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive, "Mastery");
            CheckSystemActive(ctx, GlobalMasterySystem.EffectivenessSubSystemEnabled, "Effectiveness");
            var steamID = ctx.Event.User.PlatformId;

            if (!string.IsNullOrEmpty(categoryInput))
            {
                if (!GlobalMasterySystem.KeywordToMasteryCategoryMap.TryGetValue(categoryInput.ToLower(), out var lookupMasteryCategory))
                {
                    Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                    var options = string.Join(", ", GlobalMasterySystem.KeywordToMasteryCategoryMap.Keys);
                    Output.ChatReply(ctx, new L10N.LocalisableString($"({options})"));
                    return;
                }
                
                GlobalMasterySystem.ResetMastery(steamID, lookupMasteryCategory);
                return;
            }

            if (Plugin.WeaponMasterySystemActive)
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryReset).AddField("{masteryType}", Enum.GetName(GlobalMasterySystem.MasteryCategory.Weapon)));
                GlobalMasterySystem.ResetMastery(steamID, GlobalMasterySystem.MasteryCategory.Weapon);
            }

            if (Plugin.BloodlineSystemActive)
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryReset).AddField("{masteryType}", Enum.GetName(GlobalMasterySystem.MasteryCategory.Blood)));
                GlobalMasterySystem.ResetMastery(steamID, GlobalMasterySystem.MasteryCategory.Blood);
            }
        }
        
        [Command("reset", "r", "<masteryType>", "Resets mastery to gain more power with it.", adminOnly: false)]
        public static void ResetMastery(ChatCommandContext ctx, string masteryTypeInput)
        {
            CheckSystemActive(ctx, Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive, "Mastery");
            CheckSystemActive(ctx, GlobalMasterySystem.EffectivenessSubSystemEnabled, "Effectiveness");
            var steamID = ctx.User.PlatformId;

            if (!GlobalMasterySystem.KeywordToMasteryMap.TryGetValue(masteryTypeInput.ToLower(), out var lookupMasteryType))
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.MasteryType404));
                var options = string.Join(", ", GlobalMasterySystem.KeywordToMasteryMap.Keys);
                Output.ChatReply(ctx, new L10N.LocalisableString($"({options})"));
                return;
            }
            
            GlobalMasterySystem.ResetMastery(steamID, lookupMasteryType);
        }
    }
}
