using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    [CommandGroup("bloodline", "bl")]
    public static class BloodlineCommands
    {
        private static EntityManager entityManager = Plugin.Server.EntityManager;

        private static string BloodlineToPrint(BloodlineSystem.BloodType type, MasteryData masteryData, List<StatConfig> bloodlineConfig)
        {
            var name = BloodlineSystem.GetBloodTypeName(type);
            return $"{name}: <color={Output.White}>{masteryData.Mastery:F3}%</color>";

            // var statStrings = bloodlineConfig.Select(statConfig => 
            // {
            //     if (masteryData.Mastery >= statConfig.strength)
            //     {
            //         var val = Helper.CalcBuffValue(masteryData.Mastery, masteryData.Effectiveness, statConfig.rate, statConfig.type);
            //         // If the value was inverted for the buff calculation, un-invert it here.
            //         if (Helper.percentageStats.Contains(statConfig.type)) {
            //             return $"{Helper.CamelCaseToSpaces(statConfig.type)} <color={Output.Green}>{val/100:F3}%</color>";
            //         }
            //         return $"{Helper.CamelCaseToSpaces(statConfig.type)} <color={Output.Green}>{val:F3}</color>";
            //     }
            //     return $"{Helper.CamelCaseToSpaces(statConfig.type)} <color={Output.Gray}>Not enough strength</color>";
            // });
            //
            // return $"{name}: <color={Output.White}>{masteryData.Mastery:F3}%</color> ({string.Join("\n",statStrings)}) Effectiveness: {masteryData.Effectiveness * 100}%";
        }
        
        [Command("get", "g", "", "Display your current bloodline progression")]
        public static void GetBloodline(ChatCommandContext ctx) {
            if (!Plugin.BloodlineSystemActive) {
                ctx.Reply("Bloodline system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;
            
            var blood = entityManager.GetComponentData<Blood>(ctx.Event.SenderCharacterEntity);
            var bloodType = (BloodlineSystem.BloodType)blood.BloodType.GuidHash;
            if (!Enum.IsDefined(bloodType))
            {
                ctx.Reply($"Unknown user blood type: {blood.BloodType.GuidHash}.");
                return;
            }

            if (!Database.PlayerBloodline.TryGetValue(steamID, out var bld) ||
                !bld.TryGetValue(bloodType, out var masteryData)) {
                ctx.Reply("You haven't developed any bloodline...");
                return;
            }

            var bloodlineConfig = Database.BloodlineStatConfig[bloodType];
            ctx.Reply($"-- <color={Output.White}>Bloodlines</color> --");
            
            ctx.Reply(BloodlineToPrint(bloodType, masteryData, bloodlineConfig));
        }
        
        [Command("get-all", "ga", "", "Display all your bloodline progressions")]
        public static void GetAllBloodlines(ChatCommandContext ctx) {
            if (!Plugin.BloodlineSystemActive) {
                ctx.Reply("Bloodline system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;

            var blood = entityManager.GetComponentData<Blood>(ctx.Event.SenderCharacterEntity);
            var bloodType = (BloodlineSystem.BloodType)blood.BloodType.GuidHash;
            if (!Enum.IsDefined(bloodType))
            {
                ctx.Reply($"Unknown user blood type: {blood.BloodType.GuidHash}.");
                return;
            }

            if (!Database.PlayerBloodline.TryGetValue(steamID, out var bld)) {
                ctx.Reply("You haven't developed any bloodline...");
                return;
            }

            var bloodlineConfig = Database.BloodlineStatConfig[bloodType];
            ctx.Reply($"-- <color={Output.White}>Bloodlines</color> --");

            foreach (var data in bld)
            {
                ctx.Reply(BloodlineToPrint(data.Key, data.Value, bloodlineConfig));
            }
        }

        [Command("add", "a", "<BloodlineName> <amount>", "Adds amount to the specified bloodline. able to use default names, bloodtype names, or the configured names.", adminOnly: false)]
        public static void AddBloodlineValue(ChatCommandContext ctx, string masteryType, double amount)
        {
            var steamID = ctx.User.PlatformId;
            var name = ctx.User.CharacterName.ToString();
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;
            if (!BloodlineSystem.KeywordToBloodMap.TryGetValue(masteryType.ToLower(), out var type))
            {
                throw ctx.Error($"{type} Bloodline not found! Did you typo?");
            }
            BloodlineSystem.ModBloodline(steamID, type, amount / BloodlineSystem.MasteryGainMultiplier);
            ctx.Reply($"{BloodlineSystem.GetBloodTypeName(type)} bloodline for \"{name}\" adjusted by <color={Output.White}>{amount}%</color>");
            Helper.ApplyBuff(userEntity, charEntity, Helper.AppliedBuff);
        }

        [Command("set", "s", "<playerName> <bloodline> <value>", "Sets the specified players bloodline to a specific value", adminOnly: false)]
        public static void SetBloodline(ChatCommandContext ctx, string playerName, string bloodlineName, double value) {
            if (!Plugin.BloodlineSystemActive) {
                ctx.Reply("Bloodline system is not enabled.");
                return;
            }
            var steamID = PlayerCache.GetSteamIDFromName(playerName);
            if (steamID == 0) {
                throw ctx.Error($"Could not find specified player \"{playerName}\".");
            }

            if (!BloodlineSystem.KeywordToBloodMap.TryGetValue(bloodlineName, out var type)) {
                throw ctx.Error($"{bloodlineName} Bloodline not found! Did you typo?");
            }
            BloodlineSystem.ModBloodline(steamID, type, -100000);
            BloodlineSystem.ModBloodline(steamID, type, value / BloodlineSystem.MasteryGainMultiplier);
            ctx.Reply($"{BloodlineSystem.GetBloodTypeName(type)} bloodline for \"{playerName}\" set to <color={Output.White}>{value}%</color>");
        }

        [Command("log", "l", "", "Toggles logging of bloodlineXP gain.", adminOnly: false)]
        public static void LogBloodline(ChatCommandContext ctx)
        {
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerLogConfig[steamID];
            loggingData.LoggingBloodline = !loggingData.LoggingBloodline;
            ctx.Reply(loggingData.LoggingBloodline
                ? "Bloodline gain is now being logged."
                : $"Bloodline gain is no longer being logged.");
            Database.PlayerLogConfig[steamID] = loggingData;
        }

        [Command("reset", "r", "<bloodline>", "Resets a bloodline to gain more power with it.", adminOnly: false)]
        public static void ResetBloodline(ChatCommandContext ctx, string bloodlineName)
        {
            var steamID = ctx.User.PlatformId;
            if (!BloodlineSystem.KeywordToBloodMap.TryGetValue(bloodlineName, out var type)) {
                throw ctx.Error($"{bloodlineName} Bloodline not found! Did you typo?");
            }
            ctx.Reply($"Resetting {BloodlineSystem.GetBloodTypeName(type)} bloodline.");
            BloodlineSystem.ResetBloodline(steamID, type);
        }
    }
}
