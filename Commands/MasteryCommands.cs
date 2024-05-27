using System;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands {
    [CommandGroup("mastery", "m")]
    public static class MasteryCommands {
        private static EntityManager _entityManager = Plugin.Server.EntityManager;

        [Command("get", "g", "[masteryType]", "Display your current mastery progression for your equipped or specified weapon type")]
        public static void GetMastery(ChatCommandContext ctx, string weaponType = "") {
            if (!Plugin.WeaponMasterySystemActive) {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;

            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                ctx.Reply("You haven't even tried to master anything...");
                return;
            }

            WeaponMasterySystem.MasteryType type;
            if (string.IsNullOrEmpty(weaponType))
            {
                type = WeaponMasterySystem.WeaponToMasteryType(WeaponMasterySystem.GetWeaponType(ctx.Event.SenderCharacterEntity));
            }
            else if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out type))
            {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }
            
            var wd = Database.PlayerWeaponmastery[steamID];
            ctx.Reply($"-- <color={Output.White}>Weapon Mastery</color> --");

            MasteryData data = wd[type];
            ctx.Reply(GetMasteryDataStringForType(type, data));
        }

        [Command("get-all", "ga", "", "Display your current mastery progression in everything")]
        public static void GetAllMastery(ChatCommandContext ctx) {
            if (!Plugin.WeaponMasterySystemActive) {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;
            
            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                ctx.Reply("You haven't even tried to master anything...");
                return;
            }

            var wd = Database.PlayerWeaponmastery[steamID];
            ctx.Reply($"-- <color={Output.White}>Weapon Mastery</color> --");

            foreach (var data in wd)
            {
                ctx.Reply(GetMasteryDataStringForType(data.Key, data.Value));
            }
        }

        private static string GetMasteryDataStringForType(WeaponMasterySystem.MasteryType type, MasteryData data){
            var name = Enum.GetName(type);
            var mastery = data.Mastery;
            var effectiveness = WeaponMasterySystem.EffectivenessSubSystemEnabled ? data.Effectiveness : 1;
            var growth = data.Growth;
            
            return $"{name}: <color={Output.White}>{mastery:F2}%</color>";
            
            // var statData = Database.MasteryStatConfig[type].Select(config =>
            // {
            //     var val = Helper.CalcBuffValue(mastery, effectiveness, config.rate, config.type);
            //     
            //     if (Helper.percentageStats.Contains(config.type) && Helper.humanReadablePercentageStats) {
            //         return $"{Helper.CamelCaseToSpaces(config.type)} <color={Output.Green}>{val/100:F3}%</color>";
            //     }
            //
            //     return $"{Helper.CamelCaseToSpaces(config.type)} <color={Output.Green}>{val:F3}</color>";
            // });
            //
            // return $"{name}: <color={Output.White}>{mastery:F2}%</color> ({string.Join(",", statData)}) Effectiveness: {effectiveness * 100}%, Growth: {growth * 100}%";
        }

        [Command("add", "a", "<weaponType> <amount>", "Adds the amount to the mastery of the specified weaponType", adminOnly: false)]
        public static void AddMastery(ChatCommandContext ctx, string weaponType, double amount){
            if (!Plugin.WeaponMasterySystemActive)
            {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;
            var charName = ctx.Event.User.CharacterName.ToString();
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }

            WeaponMasterySystem.ModMastery(steamID, masteryType, amount / WeaponMasterySystem.MasteryGainMultiplier);
            ctx.Reply($"{Enum.GetName(masteryType)} Mastery for \"{charName}\" adjusted by <color={Output.White}>{amount:F2}%</color>");
            Helper.ApplyBuff(userEntity, charEntity, Helper.AppliedBuff);
        }
        
        [Command("set", "s", "<playerName> <weaponType> <masteryValue>", "Sets the specified player's mastery to a specific value", adminOnly: false)]
        public static void SetMastery(ChatCommandContext ctx, string name, string weaponType, double value) {
            if (!Plugin.WeaponMasterySystemActive) {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            ulong steamID = PlayerCache.GetSteamIDFromName(name);
            if (steamID == 0) {
                ctx.Reply($"Could not find specified player \"{name}\".");
                return;
            }

            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }

            WeaponMasterySystem.ModMastery(steamID, masteryType, -100000);
            WeaponMasterySystem.ModMastery(steamID, masteryType, value / WeaponMasterySystem.MasteryGainMultiplier);
            ctx.Reply($"{Enum.GetName(masteryType)} Mastery for \"{name}\" set to <color={Output.White}>{value:F2}%</color>");
        }

        [Command("log", "l", "", "Toggles logging of mastery gain.", adminOnly: false)]
        public static void LogMastery(ChatCommandContext ctx)
        {
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerLogConfig[steamID];
            loggingData.LoggingMastery = !loggingData.LoggingMastery;
            ctx.Reply(loggingData.LoggingMastery
                ? "Mastery gain is now being logged."
                : $"Mastery gain is no longer being logged.");
            Database.PlayerLogConfig[steamID] = loggingData;
        }


        [Command("reset", "r", "<weaponType>", "Resets a mastery to gain more power with it.", adminOnly: false)]
        public static void ResetMastery(ChatCommandContext ctx, string weaponType)
        {
            if (!Plugin.WeaponMasterySystemActive){
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;
            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }
            ctx.Reply($"Resetting {Enum.GetName(masteryType)} Mastery");
            WeaponMasterySystem.ResetMastery(steamID, masteryType);
        }
    }
}
