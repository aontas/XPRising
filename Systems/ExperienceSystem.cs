using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using Unity.Entities;
using System.Linq;
using BepInEx.Logging;
using Unity.Collections;
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

        public static float ExpMultiplier = 1.5f;
        public static float VBloodMultiplier = 15;
        public static int MaxLevel = 100;
        public static float GroupMaxDistance = 50;
        public static bool ShouldAllowGearLevel = true;
        public static bool LevelRewardsOn = true;

        public static float PvpXpLossPercent = 0;
        public static float PveXpLossPercent = 10;

        /*
         * The following values have been tweaked to have the following stats:
         * Total xp: 355,085
         * Last level xp: 7,7765
         *
         * Assuming:
         * - ExpMultiplier = 1.5
         * - Ignoring VBlood bonus (2x in last column)
         * - MaxLevel = 100 (and assuming that the max mob level matches)
         *
         *    mob level=> | same   | +5   |  -5  | +5 => -5 | same (VBlood only) |
         * _______________|________|______|______|__________|____________________|
         * Total kills    | 4258   | 2720 | 8644 | 4891     | 2129               |
         * lvl 0 kills    | 10     | 2    | 10   | 2        | 5                  |
         * Last lvl kills | 52     | 52   | 164  | 91       | 26                 |
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
        private const float ExpLevelDiffMultiplier = 0.08f;

        // This is updated on server start-up to match server settings start level
        public static int StartingExp = 0;
        private static int MinLevel => ConvertXpToLevel(StartingExp);
        private static int MaxXp => ConvertLevelToXp(MaxLevel);

        private static HashSet<Units> _noExpUnits = new(
            FactionUnits.farmNonHostile.Select(u => u.type).Union(FactionUnits.farmFood.Select(u => u.type)));

        private static HashSet<Units> _minimalExpUnits = new()
        {
            Units.CHAR_Farmlands_Nun_Servant,
            Units.CHAR_Mutant_RatHorror
        };

        // Encourage group play by buffing XP for groups
        private const double GroupXpBuffGrowth = 0.2;
        private const double MaxGroupXpBuff = 1.5;
        
        // We can add various mobs/groups/factions here to reduce or increase XP gain
        private static float ExpValueMultiplier(Entity entity, bool isVBlood)
        {
            if (isVBlood) return VBloodMultiplier;
            var unit = Helper.ConvertGuidToUnit(Helper.GetPrefabGUID(entity));
            if (_noExpUnits.Contains(unit)) return 0;
            if (_minimalExpUnits.Contains(unit)) return 0.1f;
            
            return 1;
        }
        
        public static bool IsPlayerLoggingExperience(ulong steamId)
        {
            return Database.PlayerLogConfig[steamId].LoggingExp;
        }

        public static void ExpMonitor(List<Alliance.ClosePlayer> closeAllies, Entity victimEntity, bool isVBlood)
        {
            var unitLevel = _entityManager.GetComponentData<UnitLevel>(victimEntity);
            var multiplier = ExpValueMultiplier(victimEntity, isVBlood);
            
            var sumGroupLevel = (double)closeAllies.Sum(x => x.playerLevel);
            var avgGroupLevel = (int)Math.Ceiling(closeAllies.Average(x => x.playerLevel));

            // Calculate an XP bonus that grows as groups get larger
            var baseGroupXpBonus = Math.Min(Math.Pow(1 + GroupXpBuffGrowth, closeAllies.Count - 1), MaxGroupXpBuff);

            Plugin.Log(LogSystem.Xp, LogLevel.Info, "Running Assign EXP for all close allied players");
            foreach (var teammate in closeAllies) {
                Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Assigning EXP to {teammate.steamID}: LVL: {teammate.playerLevel}, IsKiller: {teammate.isTrigger}, IsVBlood: {isVBlood}");
                
                // Calculate the portion of the total XP that this player should get.
                var groupMultiplier = GroupMaxDistance > 0 ? baseGroupXpBonus * (teammate.playerLevel / sumGroupLevel) : 1.0;
                AssignExp(teammate, avgGroupLevel, unitLevel.Level, multiplier * groupMultiplier);
            }
        }

        private static void AssignExp(Alliance.ClosePlayer player, int groupLevel, int mobLevel, double multiplier) {
            if (player.currentXp >= ConvertLevelToXp(MaxLevel)) return;
            
            var xpGained = CalculateXp(groupLevel, mobLevel, multiplier);

            var newXp = Math.Max(player.currentXp, 0) + xpGained;
            SetXp(player.steamID, newXp);

            GetLevelAndProgress(newXp, out _, out var earned, out var needed);
            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Gained {xpGained} from Lv.{mobLevel} [{earned}/{needed} (total {newXp})]");
            if (IsPlayerLoggingExperience(player.steamID))
            {
                Output.SendMessage(player.userEntity, $"<color={Output.LightYellow}>You gain {xpGained} XP by slaying a Lv.{mobLevel} enemy.</color> [ XP: <color={Output.White}>{earned}</color>/<color={Output.White}>{needed}</color> ]");
            }
            
            SetLevel(player.userComponent.LocalCharacter._Entity, player.userEntity, player.steamID);
        }

        private static int CalculateXp(int playerLevel, int mobLevel, double multiplier) {
            var levelDiff = mobLevel - playerLevel;

            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"--- Max(1, {mobLevel} * {multiplier} * (1 + {levelDiff} * 0.1))*{ExpMultiplier}");
            return (int)(Math.Max(1, mobLevel * multiplier * (1 + levelDiff * ExpLevelDiffMultiplier))*ExpMultiplier);
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
            
            var calculatedNewXp = exp * (1 - xpLossPercent/100);

            // The minimum our XP is allowed to drop to
            var minXp = ConvertLevelToXp(ConvertXpToLevel(exp)) + 1; // 1 more XP than the start of the level
            var currentXp = Math.Max((int)Math.Ceiling(calculatedNewXp), minXp);
            var xpLost = exp - currentXp;
            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Calculated XP: {steamID}: {currentXp} = Max({exp} * {xpLossPercent/100}, {minXp}) [lost {xpLost}]");
            SetXp(steamID, currentXp);

            SetLevel(playerEntity, userEntity, steamID);
            GetLevelAndProgress(currentXp, out _, out var earned, out var needed);
            Output.SendMessage(userEntity, $"You've been defeated, <color={Output.White}>{xpLost}</color> XP is lost. [ XP: <color={Output.White}>{earned}</color>/<color={Output.White}>{needed}</color> ]");
        }

        public static void SetLevel(Entity entity, Entity user, ulong steamID)
        {
            var level = ConvertXpToLevel(GetXp(steamID));
            if (level < MinLevel)
            {
                level = MinLevel;
                SetXp(steamID, ConvertLevelToXp(level));
            }
            else if (level > MaxLevel)
            {
                level = MaxLevel;
                SetXp(steamID, ConvertLevelToXp(MaxLevel));
            }

            if (Cache.player_level.TryGetValue(steamID, out var storedLevel))
            {
                if (storedLevel < level)
                {
                    Helper.ApplyBuff(user, entity, Helper.LevelUp_Buff);
                    if (IsPlayerLoggingExperience(steamID))
                    {
                        Output.SendMessage(user,
                            $"<color={Output.LightYellow}>Level up! You're now level</color> <color={Output.White}>{level}</color><color={Output.LightYellow}>!</color>");
                    }
                }

                Plugin.Log(LogSystem.Xp, LogLevel.Info,
                    $"Set player level: LVL: {level} (stored: {storedLevel}) XP: {GetXp(steamID)}");
            }
            else
            {
                Plugin.Log(LogSystem.Xp, LogLevel.Info,
                    $"Player logged in: LVL: {level} (stored: {storedLevel}) XP: {GetXp(steamID)}");
                SetXp(steamID, ConvertLevelToXp(level));
            }

            Cache.player_level[steamID] = level;

            Equipment equipment = _entityManager.GetComponentData<Equipment>(entity);
            Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Current gear levels: A:{equipment.ArmorLevel.Value} W:{equipment.WeaponLevel.Value} S:{equipment.SpellLevel.Value}");
            var halfOfLevel = level / 2f;
            equipment.ArmorLevel._Value = MathF.Floor(halfOfLevel);
            equipment.WeaponLevel._Value = MathF.Ceiling(halfOfLevel);
            equipment.SpellLevel._Value = 0;

            _entityManager.SetComponentData(entity, equipment);
        }

        /// <summary>
        /// For use with the LevelUpRewards buffing system.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="owner"></param>
        /// <param name="steamID"></param>
        public static void BuffReceiver(ref LazyDictionary<UnitStatType, float> statBonus, Entity owner, ulong steamID)
        {
            if (!LevelRewardsOn) return;
            float multiplier = 1;
            try
            {
                // foreach (var gearType in Database.PlayerLevelStats[steamID])
                // {
                //     // TODO is this still true?
                //     //we have to hack unequipped players and give them double bonus because the buffer array does not contain the buff, but they get an additional 
                //     //buff of the same type when they are equipped! This will make them effectively the same effect, equipped or not.
                //     //Maybe im just dumb, but I checked the array and tried that approach thinking i was double buffing due to logical error                    
                //     if (WeaponMasterySystem.GetWeaponType(owner) == WeaponType.None) multiplier = 2; 
                //
                //     buffer.Add(new ModifyUnitStatBuff_DOTS()
                //     {
                //         StatType = gearType.Key,
                //         Value = gearType.Value * multiplier,
                //         ModificationType = ModificationType.AddToBase,
                //         Id = ModificationId.NewId(0)
                //     });
                // }
                var playerLevel = GetLevel(steamID);
                var healthBuff = 2f * playerLevel * multiplier;
                statBonus[UnitStatType.MaxHealth] = healthBuff;
            }
            catch (Exception ex)
            {
                Plugin.Log(LogSystem.Xp, LogLevel.Error, $"Could not apply XP buff!\n{ex} ");
            }
        }

        public static int ConvertXpToLevel(int xp)
        {
            // Shortcut for exceptional cases
            if (xp < 1) return 1;
            // Level = CONSTANT * (xp)^1/POWER
            int lvl = 1 + (int)Math.Floor(ExpConstant * Math.Pow(xp, 1 / ExpPower));
            return lvl;
        }

        public static int ConvertLevelToXp(int level)
        {
            // Shortcut for exceptional cases
            if (level < 1) return 1;
            // XP = (Level / CONSTANT) ^ POWER
            int xp = (int)Math.Pow((level - 1) / ExpConstant, ExpPower);
            return xp;
        }

        public static int GetXp(ulong steamID)
        {
            return Math.Max(Database.PlayerExperience.GetValueOrDefault(steamID, StartingExp), StartingExp);
        }
        
        public static void SetXp(ulong steamID, int exp)
        {
            Database.PlayerExperience[steamID] = Math.Min(exp, MaxXp);
        }

        public static int GetLevel(ulong steamID)
        {
            return ConvertXpToLevel(GetXp(steamID));
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