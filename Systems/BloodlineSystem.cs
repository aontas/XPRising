using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Unity.Entities;
using Stunlock.Core;
using XPRising.Models;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Systems
{
    using BloodlineMasteryData =  LazyDictionary<BloodlineSystem.BloodType,MasteryData>;
    public class BloodlineSystem
    {
        private static EntityManager _em = Plugin.Server.EntityManager;

        public static bool MercilessBloodlines = false;
        public static double MasteryGainMultiplier = 1.0;
        public static double InactiveMultiplier = 0.1;
        public static double VBloodMultiplier = 15;
        
        public static bool IsDecaySystemEnabled = false;
        public static int DecayInterval = 60;
        public static double OnlineDecayValue = 0;
        public static double OfflineDecayValue = 1;

        public static bool EffectivenessSubSystemEnabled = true;
        public static double GrowthPerEffectiveness = 1.0;
        public static double MaxBloodlineEffectiveness = 5;

        public enum BloodType
        {
            None = Remainders.BloodType_None,
            Brute = Remainders.BloodType_Brute,
            Creature = Remainders.BloodType_Creature,
            Dracula = Remainders.BloodType_DraculaTheImmortal,
            Draculin = Remainders.BloodType_Draculin,
            GateBoss = Remainders.BloodType_GateBoss,
            Mutant = Remainders.BloodType_Mutant,
            Rogue = Remainders.BloodType_Rogue,
            Scholar = Remainders.BloodType_Scholar,
            VBlood = Remainders.BloodType_VBlood,
            Warrior = Remainders.BloodType_Warrior,
            Worker = Remainders.BloodType_Worker,
        }

        // This is a "potential" name to blood type map. Multiple keywords map to the same blood type
        public static readonly Dictionary<string, BloodType> KeywordToBloodMap = new()
        {
            { "frail", BloodType.None },
            { "none", BloodType.None },
            { "mutant", BloodType.Mutant },
            { "creature", BloodType.Creature },
            { "warrior", BloodType.Warrior },
            { "rogue", BloodType.Rogue },
            { "brute", BloodType.Brute },
            { "scholar", BloodType.Scholar },
            { "worker", BloodType.Worker }
        };

        private static readonly Random Rand = new Random();

        public static void UpdateBloodline(Entity killer, Entity victim, bool killOnly)
        {
            if (killer == victim) return;
            if (_em.HasComponent<Minion>(victim)) return;

            var victimLevel = _em.GetComponentData<UnitLevel>(victim);
            var killerUserEntity = _em.GetComponentData<PlayerCharacter>(killer).UserEntity;
            var steamID = _em.GetComponentData<User>(killerUserEntity).PlatformId;
            Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Updating bloodline mastery for {steamID}");
            
            double growthVal = Math.Clamp(victimLevel.Level.Value - ExperienceSystem.GetLevel(steamID), 1, 10);
            
            BloodType killerBloodType;
            if (_em.TryGetComponentData<Blood>(killer, out var killerBlood)){
                if (!GuidToBloodType(killerBlood.BloodType, true, out killerBloodType)) return;
            }
            else {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"killer does not have blood: Killer ({killer}), Victim ({victim})");
                return; 
            }

            BloodType victimBloodType;
            float victimBloodQuality;
            bool isVBlood;
            if (_em.TryGetComponentData<BloodConsumeSource>(victim, out var victimBlood)) {
                victimBloodQuality = victimBlood.BloodQuality;
                if (!GuidToBloodType(victimBlood.UnitBloodType, false, out victimBloodType)) return;
                isVBlood = Helper.IsVBlood(victimBlood);
                
                // If the killer is consuming the target and it is not VBlood, the blood type will be changing to the victims.
                if (!isVBlood && !killOnly) killerBloodType = victimBloodType;
            }
            else
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"victim does not have blood: Killer ({killer}), Victim ({victim}");
                return;
            }

            if (killerBloodType == BloodType.None)
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"killer has frail blood, not modifying: Killer ({killer}), Victim ({victim})");
                if (Database.PlayerLogConfig[steamID].LoggingBloodline)
                {
                    var message =
                        LocalisationSystem.Get(LocalisationSystem.TemplateKey.BloodlineMercilessFailBlood);
                    Output.SendMessage(killerUserEntity, message);
                }
                return;
            }
            
            var bld = Database.PlayerBloodline[steamID];
            var bloodlineMastery = bld[killerBloodType];
            growthVal *= bloodlineMastery.Growth;
            
            if (MercilessBloodlines)
            {
                if (!isVBlood) // VBlood is allowed to boost all blood types
                {
                    if (killerBloodType != victimBloodType)
                    {
                        Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                            $"merciless bloodlines exit: Blood types are different: Killer ({Enum.GetName(killerBloodType)}), Victim ({Enum.GetName(victimBloodType)})");
                        if (Database.PlayerLogConfig[steamID].LoggingBloodline)
                        {
                            var message =
                                LocalisationSystem.Get(LocalisationSystem.TemplateKey.BloodlineMercilessUnmatchedBlood);
                            Output.SendMessage(killerUserEntity, message);
                        }
                        return;
                    }
                    
                    if (victimBloodQuality <= bloodlineMastery.Mastery)
                    {
                        Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                            $"merciless bloodlines exit: victim blood quality less than killer mastery: Killer ({bloodlineMastery.Mastery}), Victim ({victimBloodQuality})");
                        if (Database.PlayerLogConfig[steamID].LoggingBloodline)
                        {
                            var message =
                                LocalisationSystem.Get(LocalisationSystem.TemplateKey.BloodlineMercilessFailWeak);
                            Output.SendMessage(killerUserEntity, message);
                        }
                        return;
                    }
                }

                var modifier = killOnly ? 0.4 : isVBlood ? VBloodMultiplier : 1.0;

                growthVal *= (1 + Math.Clamp((victimBloodQuality - bloodlineMastery.Mastery)/100, 0.0, 0.5))*5*modifier;
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                    $"Merciless growth {GetBloodTypeName(killerBloodType)}: [{victimBloodQuality:F3},{bloodlineMastery.Mastery:F3},{growthVal:F3}]");
            }
            else if (isVBlood)
            {
                growthVal *= VBloodMultiplier;
            }
            else if (killOnly)
            {
                growthVal *= 0.4;
            }

            growthVal *= Math.Max(Rand.NextDouble() * 0.2, 0.1);

            if (_em.HasComponent<PlayerCharacter>(victim))
            {
                var victimGear = _em.GetComponentData<Equipment>(victim);
                var bonusMastery = victimGear.ArmorLevel + victimGear.WeaponLevel + victimGear.SpellLevel;
                growthVal *= (1 + (bonusMastery * 0.01));
                
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Bonus bloodline mastery {bonusMastery:F3}]");
            }

            var updatedMastery = ModBloodline(steamID, killerBloodType, growthVal);

            if (Database.PlayerLogConfig[steamID].LoggingBloodline)
            {
                var updatedValue = updatedMastery.Mastery;
                var bloodTypeName = GetBloodTypeName(killerBloodType);
                var message =
                    LocalisationSystem.Get(LocalisationSystem.TemplateKey.BloodlineMasteryGain)
                        .AddField("{growth}", $"{growthVal:F3}")
                        .AddField("{bloodType}", bloodTypeName)
                        .AddField("{total}", $"{updatedValue:F3}");
                Output.SendMessage(killerUserEntity, message);
            }
        }
        
        public static void DecayBloodline(Entity userEntity, DateTime lastDecay)
        {
            var steamID = _em.GetComponentData<User>(userEntity).PlatformId;
            var elapsedTime = DateTime.Now - lastDecay;
            if (elapsedTime.TotalSeconds < DecayInterval) return;

            var decayTicks = (int)Math.Floor(elapsedTime.TotalSeconds / DecayInterval);
            if (decayTicks > 0)
            {
                var decayValue = OfflineDecayValue * decayTicks * -1;

                var message =
                    LocalisationSystem.Get(LocalisationSystem.TemplateKey.BloodlineDecay)
                        .AddField("{duration}", $"{elapsedTime.TotalMinutes}")
                        .AddField("{decay}", $"{decayValue * 0.001:F3)}");
                Output.SendMessage(steamID, message);
                
                var bld = Database.PlayerBloodline[steamID];

                foreach (var type in Enum.GetValues<BloodType>())
                {
                    bld = ModBloodline(steamID, bld, type, decayValue);
                }

                Database.PlayerBloodline[steamID] = bld;
            }
        }
        
        public static void ResetBloodline(ulong steamID, BloodType type) {
            if (!EffectivenessSubSystemEnabled) {
                Output.SendMessage(steamID, LocalisationSystem.Get(LocalisationSystem.TemplateKey.EffectivenessDisabled).AddField("{system}", "bloodline"));
                return;
            }

            var bld = Database.PlayerBloodline[steamID];
            var bloodMastery = bld[type];
            
            // If it is already 0, then this won't have much effect.
            if (bloodMastery.Mastery > 0)
            {
                bld[type] = bloodMastery.ResetMastery(MaxBloodlineEffectiveness, GrowthPerEffectiveness);
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Bloodline reset: {GetBloodTypeName(type)}: {bloodMastery}");
            }

            Database.PlayerBloodline[steamID] = bld;
        }
        
        public static void BuffReceiver(ref LazyDictionary<UnitStatType, float> statBonus, Entity owner, ulong steamID) {
            if (!_em.TryGetComponentData<Blood>(owner, out var bloodline) ||
                !GuidToBloodType(bloodline.BloodType, false, out var bloodType))
            {
                return;
            }
            
            var bld = Database.PlayerBloodline[steamID];

            if (InactiveMultiplier > 0)
            {
                foreach (var (type, mastery) in bld)
                {
                    var multiplier = type == bloodType ? 1.0 : InactiveMultiplier;
                    var effectiveness = (EffectivenessSubSystemEnabled ? mastery.Effectiveness : 1) * multiplier;
                    var masteryValue = Math.Max(mastery.Mastery, 0);
                    var config = Database.BloodlineStatConfig[type];
                    foreach (var statConfig in config)
                    {
                        // Skip if we don't have enough mastery for this bonus
                        if (mastery.Mastery < statConfig.strength) continue;
                        var value = Helper.CalcBuffValue(masteryValue, effectiveness, statConfig.rate, statConfig.type);
                        statBonus[statConfig.type] += (float)value;
                    }
                }
            }
            else
            {
                var bloodlineMastery = bld[bloodType];
                var effectiveness = EffectivenessSubSystemEnabled ? bloodlineMastery.Effectiveness : 1;
                var masteryValue = Math.Max(bloodlineMastery.Mastery, 0);
                var config = Database.BloodlineStatConfig[bloodType];
                foreach (var statConfig in config)
                {
                    if (bloodlineMastery.Mastery < statConfig.strength) continue;
                    
                    var value = Helper.CalcBuffValue(masteryValue, effectiveness, statConfig.rate, statConfig.type);
                    statBonus[statConfig.type] += (float)value;
                }
            }
        }

        public static MasteryData ModBloodline(ulong steamID, BloodType type, double changeInMastery)
        {
            var bloodlineMasteryData = Database.PlayerBloodline[steamID];
            bloodlineMasteryData = ModBloodline(steamID, bloodlineMasteryData, type, changeInMastery);

            Database.PlayerBloodline[steamID] = bloodlineMasteryData;
            return bloodlineMasteryData[type];
        }
        
        private static BloodlineMasteryData ModBloodline(ulong steamID, BloodlineMasteryData bloodlineMasteryData, BloodType type, double changeInMastery)
        {
            var bloodMastery = bloodlineMasteryData[type];

            bloodMastery.Mastery += changeInMastery * MasteryGainMultiplier;
            Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Mastery changed: {steamID}: {GetBloodTypeName(type)}: {bloodMastery.Mastery}");
            bloodlineMasteryData[type] = bloodMastery;

            return bloodlineMasteryData;
        }

        private static bool GuidToBloodType(PrefabGUID guid, bool isKiller, out BloodType bloodType)
        {
            bloodType = BloodType.None;
            if(!Enum.IsDefined(typeof(BloodType), guid.GuidHash)) {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Warning, $"Bloodline not found for guid {guid.GuidHash}. isKiller ({isKiller})", true);
                return false;
            }

            bloodType = (BloodType)guid.GuidHash;
            return true;
        }

        public static string GetBloodTypeName(BloodType type)
        {
            return Enum.GetName(type);
        }

        public static LazyDictionary<BloodType, List<StatConfig>> DefaultBloodlineConfig()
        {
            return new LazyDictionary<BloodType, List<StatConfig>>()
            {
                { BloodType.None, new List<StatConfig>() },
                { BloodType.Creature, new List<StatConfig>
                {
                    new(UnitStatType.HolyResistance, 0, 0.25),
                    new(UnitStatType.MovementSpeed, 50, 0.005),
                    new(UnitStatType.DamageVsHumans, 100, 0.0025)
                } },
                { BloodType.Brute, new List<StatConfig>
                {
                    new(UnitStatType.SilverResistance, 0, 0.25),
                    new(UnitStatType.PhysicalCriticalStrikeDamage, 50, 0.01),
                    new(UnitStatType.DamageVsUndeads, 100, 0.0025)
                } },
                { BloodType.Mutant, new List<StatConfig>
                {
                    new(UnitStatType.SpellCriticalStrikeChance, 0, 0.005),
                    new(UnitStatType.MovementSpeed, 50, 0.005),
                    new(UnitStatType.DamageVsHumans, 100, 0.0025)
                } },
                { BloodType.Rogue, new List<StatConfig>
                {
                    new(UnitStatType.SunResistance, 0, 0.25),
                    new(UnitStatType.PhysicalCriticalStrikeChance, 50, 0.001),
                    new(UnitStatType.DamageVsVampires, 100, 0.0025)
                } },
                { BloodType.Scholar, new List<StatConfig>
                {
                    new(UnitStatType.SpellPower, 0, 0.1),
                    new(UnitStatType.WeaponCooldownRecoveryRate, 50, 0.01),
                    new(UnitStatType.DamageVsDemons, 100, 0.0025)
                } },
                { BloodType.VBlood, new List<StatConfig>() },
                { BloodType.Warrior, new List<StatConfig>
                {
                    new(UnitStatType.FireResistance, 0, 0.25),
                    new(UnitStatType.PhysicalPower, 50, 0.1),
                    new(UnitStatType.DamageVsBeasts, 100, 0.0025)
                } },
                { BloodType.Worker, new List<StatConfig>
                {
                    new(UnitStatType.GarlicResistance, 0, 0.25),
                    new(UnitStatType.ResourceYield, 50, 0.01),
                    new(UnitStatType.DamageVsMineral, 100, 0.0025),
                    new(UnitStatType.DamageVsVegetation, 100, 0.0025),
                    new(UnitStatType.DamageVsWood, 100, 0.0025)
                } }
            };
        }
    }
}
