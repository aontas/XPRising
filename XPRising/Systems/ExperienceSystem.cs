using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using Unity.Entities;
using System.Linq;
using BepInEx.Logging;
using Stunlock.Core;
using XPRising.Models;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using Cache = XPRising.Utils.Cache;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Systems
{
    public class ExperienceSystem
    {
        private static EntityManager _entityManager = Plugin.Server.EntityManager;

        public static bool ShouldAllowGearLevel = true;
        public static bool LevelRewardsOn = true;
        
        public static float ExpMultiplier = 1.5f;
        public static float VBloodMultiplier = 15;
        public static int MaxLevel = 100;
        public static float GroupMaxDistance = 50;
        public static float MaxXpGainPercentage = 50f;

        public static float PvpXpLossPercent = 0;
        public static float PveXpLossPercent = 10;
        
        /*
         * The following values have been tweaked to have the following stats:
         * Total xp: 355,085
         * Last level xp: 7,7765
         *
         * Assuming:
         * - ExpMultiplier = 1.5
         * - Ignoring VBlood bonus (15x in last column)
         * - MaxLevel = 90 (Anything above 90 will be a slog as most mobs stop at 83)
         * - Max mob level = 83 (Dracula is 90, but ignoring him)
         * - Minimum allowed level difference = -12
         *
         *    mob level=> | same   | +5   |  -5  | +5 => -5 | same (VBlood only) |
         * _______________|________|______|______|__________|____________________|
         * Total kills    | 3930   | 2745 | 7220 | 4221     | 262                |
         * lvl 0 kills    | 10     | 2    | 10   | 2        | 1                  |
         * Last lvl kills | 108    | 108  | 108  | 108      | 8                  |
         *
         * +5/-5 offset to levels in the above table as still clamped to the range [1, 100].
         *
         * To increase the kill counts across the board, reduce ExpMultiplier.
         * If you want to tweak the curve, lowering ExpConstant and raising ExpPower can be done in tandem to flatten the
         * curve (attempting to ensure that the total kills or last lvl kills stay the same).
         *
         * VBlood entry in the table (naively) assumes that the player is killing a VBlood at the same level (when some
         * of those do not exist).
         *
         */
        private const float ExpConstant = 0.3f;
        private const float ExpPower = 2.2f;
        private const float ExpLevelDiffMultiplier = 0.07f;
        private const float MinAllowedLevelDiff = -12;

        // This is updated on server start-up to match server settings start level
        public static int StartingExp = 0;
        private static int MinLevel => ConvertXpToLevel(StartingExp);
        private static int MaxXp => ConvertLevelToXp(MaxLevel);

        private static HashSet<Units> _noExpUnits = new(
            FactionUnits.farmNonHostile.Select(u => u.type).Union(FactionUnits.farmFood.Select(u => u.type)));

        private static HashSet<Units> _minimalExpUnits = new()
        {
            Units.CHAR_Militia_Nun,
            Units.CHAR_Mutant_RatHorror
        };

        // Encourage group play by buffing XP for groups
        private const double GroupXpBuffGrowth = 0.2;
        private const double MaxGroupXpBuff = 1.5;
        
        // We can add various mobs/groups/factions here to reduce or increase XP gain
        private static float ExpValueMultiplier(PrefabGUID entityPrefab, bool isVBlood)
        {
            if (isVBlood) return VBloodMultiplier;
            var unit = Helper.ConvertGuidToUnit(entityPrefab);
            if (_noExpUnits.Contains(unit)) return 0;
            if (_minimalExpUnits.Contains(unit)) return 0.1f;
            
            return 1;
        }
        
        public static bool IsPlayerLoggingExperience(ulong steamId)
        {
            return Database.PlayerLogConfig[steamId].LoggingExp;
        }

        public static void ExpMonitor(List<Alliance.ClosePlayer> closeAllies, PrefabGUID victimPrefab, int victimLevel, bool isVBlood)
        {
            var multiplier = ExpValueMultiplier(victimPrefab, isVBlood);
            
            var sumGroupLevel = (double)closeAllies.Sum(x => x.playerLevel);
            var avgGroupLevel = closeAllies.Max(x => x.playerLevel);

            // Calculate an XP bonus that grows as groups get larger
            var baseGroupXpBonus = Math.Min(Math.Pow(1 + GroupXpBuffGrowth, closeAllies.Count - 1), MaxGroupXpBuff);

            Plugin.Log(LogSystem.Xp, LogLevel.Info, "Running Assign EXP for all close allied players");
            foreach (var teammate in closeAllies) {
                Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Assigning EXP to {teammate.steamID}: LVL: {teammate.playerLevel}, IsKiller: {teammate.isTrigger}, IsVBlood: {isVBlood}");
                
                // Calculate the portion of the total XP that this player should get.
                var groupMultiplier = GroupMaxDistance > 0 ? baseGroupXpBonus * (teammate.playerLevel / sumGroupLevel) : 1.0;
                Plugin.Log(LogSystem.Xp, LogLevel.Info, $"--- multipliers: {multiplier:F3}, base group: {baseGroupXpBonus:F3} * {teammate.playerLevel} / {sumGroupLevel} = {groupMultiplier:F3}");
                // Calculate the player level as the max of (playerLevel, avgGroupLevel). This has 2 results:
                // - Stop higher level players "reducing" their level for XP calculations
                // - Prevent lower level players from getting too much XP from killing higher level mobs
                var calculatedPlayerLevel = Math.Max(avgGroupLevel, teammate.playerLevel);
                // Assign XP to the player as if they were at the same level as the highest in the group.
                AssignExp(teammate, calculatedPlayerLevel, victimLevel, multiplier * groupMultiplier);
            }
        }

        private static void AssignExp(Alliance.ClosePlayer player, int calculatedPlayerLevel, int mobLevel, double multiplier) {
            if (player.currentXp >= ConvertLevelToXp(MaxLevel)) return;
            
            var xpGained = CalculateXp(calculatedPlayerLevel, mobLevel, multiplier);

            var newXp = Math.Max(player.currentXp, 0) + xpGained;
            SetXp(player.steamID, newXp);

            GetLevelAndProgress(newXp, out _, out var earned, out var needed);
            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Gained {xpGained} from Lv.{mobLevel} [{earned}/{needed} (total {newXp})]");
            if (IsPlayerLoggingExperience(player.steamID))
            {
                var message =
                    L10N.Get(L10N.TemplateKey.XpGain)
                        .AddField("{xpGained}", xpGained.ToString())
                        .AddField("{mobLevel}", mobLevel.ToString())
                        .AddField("{earned}", earned.ToString())
                        .AddField("{needed}", needed.ToString());
                
                Output.SendMessage(player.userEntity, message);
            }
            
            CheckAndApplyLevel(player.userComponent.LocalCharacter._Entity, player.userEntity, player.steamID);
        }

        private static int CalculateXp(int playerLevel, int mobLevel, double multiplier) {
            // Using a min level difference here to ensure that the user can get a basic level of XP
            var levelDiff = Math.Max(mobLevel - playerLevel, MinAllowedLevelDiff);
            
            var baseXpGain = (int)(Math.Max(1, mobLevel * multiplier * (1 + levelDiff * ExpLevelDiffMultiplier))*ExpMultiplier);
            var maxGain = MaxXpGainPercentage > 0 ? (int)Math.Ceiling((ConvertLevelToXp(playerLevel + 1) - ConvertLevelToXp(playerLevel)) * (MaxXpGainPercentage * 0.01f)) : int.MaxValue;

            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"--- Max(1, {mobLevel} * {multiplier:F3} * (1 + {levelDiff} * {ExpLevelDiffMultiplier}))*{ExpMultiplier}  => {baseXpGain} => Clamped between [1,{maxGain}]");
            // Clamp the XP gain to be at most half of the current level.
            return Math.Clamp(baseXpGain, 1, maxGain);
        }
        
        public static void DeathXpLoss(Entity playerEntity, Entity killerEntity) {
            var pvpKill = !playerEntity.Equals(killerEntity) && _entityManager.TryGetComponentData<PlayerCharacter>(killerEntity, out _);
            var xpLossPercent = pvpKill ? PvpXpLossPercent : PveXpLossPercent;
            
            if (xpLossPercent == 0)
            {
                Plugin.Log(LogSystem.Xp, LogLevel.Info, $"xpLossPercent is 0. No lost XP. (PvP: {pvpKill})");
                return;
            }
            
            var player = _entityManager.GetComponentData<PlayerCharacter>(playerEntity);
            var userEntity = player.UserEntity;
            var user = _entityManager.GetComponentData<User>(userEntity);
            var steamID = user.PlatformId;

            var exp = GetXp(steamID);
            
            GetLevelAndProgress(exp, out _, out _, out var needed);
            
            var calculatedNewXp = exp - needed * (xpLossPercent/100);

            // The minimum our XP is allowed to drop to
            var minXp = ConvertLevelToXp(ConvertXpToLevel(exp));
            var currentXp = Math.Max((int)Math.Ceiling(calculatedNewXp), minXp);
            var xpLost = exp - currentXp;
            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Calculated XP: {steamID}: {currentXp} = Max({exp} - {needed} * {xpLossPercent/100}, {minXp}) => Max({calculatedNewXp}, {minXp}) => [lost {xpLost}]");
            SetXp(steamID, currentXp);

            // We likely don't need to use ApplyLevel() here (as it shouldn't drop below the current level) but do it anyway as XP has changed.
            CheckAndApplyLevel(playerEntity, userEntity, steamID);
            GetLevelAndProgress(currentXp, out _, out var earned, out needed);

            var message =
                L10N.Get(L10N.TemplateKey.XpLost)
                    .AddField("{xpLost}", xpLost.ToString())
                    .AddField("{earned}", earned.ToString())
                    .AddField("{needed}", needed.ToString());

            Output.SendMessage(userEntity, message);
        }

        public static void CheckAndApplyLevel(Entity entity, Entity user, ulong steamID)
        {
            var level = ConvertXpToLevel(GetXp(steamID));
            if (level < MinLevel)
            {
                level = MinLevel;
                SetXp(steamID, StartingExp);
            }
            else if (level > MaxLevel)
            {
                level = MaxLevel;
                SetXp(steamID, MaxXp);
            }

            if (Cache.player_level.TryGetValue(steamID, out var storedLevel))
            {
                if (storedLevel < level)
                {
                    Helper.ApplyBuff(user, entity, Helper.LevelUp_Buff);
                    if (IsPlayerLoggingExperience(steamID))
                    {
                        var message =
                            L10N.Get(L10N.TemplateKey.XpLevelUp)
                                .AddField("{level}", level.ToString());
                        
                        Output.SendMessage(user, message);
                    }
                }

                Plugin.Log(LogSystem.Xp, LogLevel.Info,
                    $"Set player level: LVL: {level} (stored: {storedLevel}) XP: {GetXp(steamID)}");
            }
            else
            {
                Plugin.Log(LogSystem.Xp, LogLevel.Info,
                    $"Player logged in: LVL: {level} (stored: {storedLevel}) XP: {GetXp(steamID)}");
            }

            Cache.player_level[steamID] = level;

            ApplyLevel(entity, level);
            
            // Re-apply the buff now that we have set the level.
            Helper.ApplyBuff(user, entity, Helper.AppliedBuff);
        }
        
        public static void ApplyLevel(Entity entity, int level)
        {
            Equipment equipment = Plugin.Server.EntityManager.GetComponentData<Equipment>(entity);
            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Current gear levels: A:{equipment.ArmorLevel.Value} W:{equipment.WeaponLevel.Value} S:{equipment.SpellLevel.Value}");
            // Brute blood potentially modifies ArmorLevel, so set ArmorLevel 0 and apply the player level to the other stats.
            var halfOfLevel = level / 2f;
            equipment.ArmorLevel._Value = 0;
            equipment.WeaponLevel._Value = 0;
            equipment.SpellLevel._Value = level;

            Plugin.Server.EntityManager.SetComponentData(entity, equipment);
        }

        /// <summary>
        /// For use with the LevelUpRewards buffing system.
        /// </summary>
        /// <param name="statBonus"></param>
        /// <param name="steamID"></param>
        public static void BuffReceiver(ref LazyDictionary<UnitStatType, float> statBonus, ulong steamID)
        {
            if (!Plugin.ExperienceSystemActive || !LevelRewardsOn) return;
            const float multiplier = 1;
            var playerLevel = GetLevel(steamID);
            var healthBuff = 2f * playerLevel * multiplier;
            statBonus[UnitStatType.MaxHealth] += healthBuff;
        }

        public static int ConvertXpToLevel(int xp)
        {
            // Shortcut for exceptional cases
            if (xp < 1) return 0;
            // Level = CONSTANT * (xp)^1/POWER
            int lvl = (int)Math.Floor(ExpConstant * Math.Pow(xp, 1 / ExpPower));
            return lvl;
        }

        public static int ConvertLevelToXp(int level)
        {
            // Shortcut for exceptional cases
            if (level < 1) return 1;
            // XP = (Level / CONSTANT) ^ POWER
            int xp = (int)Math.Pow(level / ExpConstant, ExpPower);
            // Add 1 to make it show start of this level, rather than end of the previous level.
            return xp + 1;
        }

        public static int GetXp(ulong steamID)
        {
            return Plugin.ExperienceSystemActive ? Math.Max(Database.PlayerExperience.GetValueOrDefault(steamID, StartingExp), StartingExp) : 0;
        }
        
        public static void SetXp(ulong steamID, int exp)
        {
            Database.PlayerExperience[steamID] = Math.Clamp(exp, 0, MaxXp);
        }

        public static int GetLevel(ulong steamID)
        {
            if (Plugin.ExperienceSystemActive)
            {
                return ConvertXpToLevel(GetXp(steamID));
            }
            // Otherwise return the current gear score.
            if (!PlayerCache.FindPlayer(steamID, true, out var playerEntity, out _)) return 0;
            
            Equipment equipment = _entityManager.GetComponentData<Equipment>(playerEntity);
            return (int)(equipment.ArmorLevel.Value + equipment.WeaponLevel.Value + equipment.SpellLevel.Value);
        }

        public static void GetLevelAndProgress(int currentXp, out int progressPercent, out int earnedXp, out int neededXp) {
            var currentLevel = ConvertXpToLevel(currentXp);
            var currentLevelXp = ConvertLevelToXp(currentLevel);
            var nextLevelXp = ConvertLevelToXp(currentLevel + 1);

            neededXp = nextLevelXp - currentLevelXp;
            earnedXp = currentXp - currentLevelXp;
            
            progressPercent = (int)Math.Floor((double)earnedXp / neededXp * 100.0);
        }

        public static LazyDictionary<string, LazyDictionary<UnitStatType, float>> DefaultExperienceClassStats()
        {
            var classes = new LazyDictionary<string, LazyDictionary<UnitStatType, float>>();
            classes["health"] = new LazyDictionary<UnitStatType, float>() { { UnitStatType.MaxHealth, 0.5f } };
            classes["ppower"] = new LazyDictionary<UnitStatType, float>() { { UnitStatType.PhysicalPower, 0.75f } };
            classes["spower"] = new LazyDictionary<UnitStatType, float>() { { UnitStatType.SpellPower, 0.75f } };
            classes["presist"] = new LazyDictionary<UnitStatType, float>() { { UnitStatType.PhysicalResistance, 0.05f } };
            classes["sresist"] = new LazyDictionary<UnitStatType, float>() { { UnitStatType.SpellResistance, 0.05f } };
            classes["beasthunter"] = new LazyDictionary<UnitStatType, float>()
                { { UnitStatType.DamageVsBeasts, 0.04f }, { UnitStatType.ResistVsBeasts, 4f } };
            classes["undeadhunter"] = new LazyDictionary<UnitStatType, float>()
                { { UnitStatType.DamageVsUndeads, 0.02f }, { UnitStatType.ResistVsUndeads, 2 } };
            classes["manhunter"] = new LazyDictionary<UnitStatType, float>() { { UnitStatType.DamageVsHumans, 0.02f}, { UnitStatType.ResistVsHumans, 2 } };
            classes["demonhunter"] = new LazyDictionary<UnitStatType, float>() { { UnitStatType.DamageVsDemons, 0.02f}, { UnitStatType.ResistVsDemons, 2 } };
            classes["farmer"] = new LazyDictionary<UnitStatType, float>()
            {
                { UnitStatType.ResourceYield, 0.1f }, { UnitStatType.PhysicalPower, -1f },
                { UnitStatType.SpellPower, -0.5f }
            };
            return classes;
        }
    }
}