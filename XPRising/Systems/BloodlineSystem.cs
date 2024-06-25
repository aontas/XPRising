using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Unity.Entities;
using Stunlock.Core;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Systems
{
    public class BloodlineSystem
    {
        private static EntityManager _em = Plugin.Server.EntityManager;

        public static bool MercilessBloodlines = false;

        public static double VBloodMultiplier = 15;
        public static double MasteryGainMultiplier = 1.0;

        public static readonly Dictionary<PrefabGUID, GlobalMasterySystem.MasteryType> BuffToBloodTypeMap = new()
        {
            { new PrefabGUID((int)Effects.AB_BloodBuff_Worker_IncreaseYield), GlobalMasterySystem.MasteryType.BloodWorker }, // yield bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_Warrior_PhysPowerBonus), GlobalMasterySystem.MasteryType.BloodWarrior }, // phys bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_Scholar_SpellPowerBonus), GlobalMasterySystem.MasteryType.BloodScholar }, // spell bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_Rogue_PhysCritChanceBonus), GlobalMasterySystem.MasteryType.BloodRogue }, // crit bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_Mutant_BloodConsumption), GlobalMasterySystem.MasteryType.BloodMutant }, // drain bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_Draculin_SpeedBonus), GlobalMasterySystem.MasteryType.BloodDraculin }, // speed bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_Dracula_PhysAndSpellPower), GlobalMasterySystem.MasteryType.BloodDracula }, // phys/spell bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_Creature_SpeedBonus), GlobalMasterySystem.MasteryType.BloodCreature }, // speed bonus
            { new PrefabGUID((int)Effects.AB_BloodBuff_PrimaryAttackLifeLeech), GlobalMasterySystem.MasteryType.BloodBrute } // primary life leech
        };

        public static void UpdateBloodline(Entity killer, Entity victim, bool killOnly)
        {
            if (killer == victim) return;
            if (_em.HasComponent<Minion>(victim)) return;

            var victimLevel = _em.GetComponentData<UnitLevel>(victim);
            var killerUserEntity = _em.GetComponentData<PlayerCharacter>(killer).UserEntity;
            var steamID = _em.GetComponentData<User>(killerUserEntity).PlatformId;
            Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Updating bloodline mastery for {steamID}");
            
            double growthVal = Math.Clamp(victimLevel.Level.Value - ExperienceSystem.GetLevel(steamID), 1, 10);
            
            GlobalMasterySystem.MasteryType killerBloodType;
            if (_em.TryGetComponentData<Blood>(killer, out var killerBlood)){
                if (!GuidToBloodType(killerBlood.BloodType, true, out killerBloodType)) return;
            }
            else {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"killer does not have blood: Killer ({killer}), Victim ({victim})");
                return; 
            }

            GlobalMasterySystem.MasteryType victimBloodType;
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

            if (killerBloodType == GlobalMasterySystem.MasteryType.None)
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"killer has frail blood, not modifying: Killer ({killer}), Victim ({victim})");
                if (Database.PlayerLogConfig[steamID].LoggingMastery)
                {
                    Output.SendMessage(killerUserEntity, L10N.Get(L10N.TemplateKey.BloodlineMercilessErrorBlood));
                }
                return;
            }
            
            var playerMasterydata = Database.PlayerMastery[steamID];
            var bloodlineMastery = playerMasterydata[killerBloodType];
            growthVal *= bloodlineMastery.Growth;
            
            if (MercilessBloodlines)
            {
                if (!isVBlood) // VBlood is allowed to boost all blood types
                {
                    if (killerBloodType != victimBloodType)
                    {
                        Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                            $"merciless bloodlines exit: Blood types are different: Killer ({Enum.GetName(killerBloodType)}), Victim ({Enum.GetName(victimBloodType)})");
                        if (Database.PlayerLogConfig[steamID].LoggingMastery)
                        {
                            var message =
                                L10N.Get(L10N.TemplateKey.BloodlineMercilessUnmatchedBlood);
                            Output.SendMessage(killerUserEntity, message);
                        }
                        return;
                    }
                    
                    if (victimBloodQuality <= bloodlineMastery.Mastery)
                    {
                        Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                            $"merciless bloodlines exit: victim blood quality less than killer mastery: Killer ({bloodlineMastery.Mastery}), Victim ({victimBloodQuality})");
                        if (Database.PlayerLogConfig[steamID].LoggingMastery)
                        {
                            var message =
                                L10N.Get(L10N.TemplateKey.BloodlineMercilessErrorWeak);
                            Output.SendMessage(killerUserEntity, message);
                        }
                        return;
                    }
                }

                var modifier = killOnly ? 0.4 : isVBlood ? VBloodMultiplier : 1.0;

                growthVal *= (1 + Math.Clamp((victimBloodQuality - bloodlineMastery.Mastery)/100, 0.0, 0.5))*5*modifier;
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                    $"Merciless growth {Enum.GetName(killerBloodType)}: [{victimBloodQuality:F3},{bloodlineMastery.Mastery:F3},{growthVal:F3}]");
            }
            else if (isVBlood)
            {
                growthVal *= VBloodMultiplier;
            }
            else if (killOnly)
            {
                growthVal *= 0.4;
            }

            if (_em.HasComponent<PlayerCharacter>(victim))
            {
                var victimGear = _em.GetComponentData<Equipment>(victim);
                var bonusMastery = victimGear.ArmorLevel + victimGear.WeaponLevel + victimGear.SpellLevel;
                growthVal *= (1 + (bonusMastery * 0.01));
                
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Bonus bloodline mastery {bonusMastery:F3}]");
            }
            
            Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                () => $"Blood growth {Enum.GetName(killerBloodType)}: [{growthVal:F3} * 0.01 * {MasteryGainMultiplier:F3} => {growthVal * 0.01 * MasteryGainMultiplier:F3}]");
            growthVal *= 0.05 * MasteryGainMultiplier;
            
            GlobalMasterySystem.BankMastery(steamID, victim, killerBloodType, growthVal);
        }

        public static GlobalMasterySystem.MasteryType BloodMasteryType(Entity entity)
        {
            var bloodType = GlobalMasterySystem.MasteryType.None;
            if (_em.TryGetComponentData<Blood>(entity, out var entityBlood))
            {
                GuidToBloodType(entityBlood.BloodType, true, out bloodType);
            }
            return bloodType;
        }

        private static bool GuidToBloodType(PrefabGUID guid, bool isKiller, out GlobalMasterySystem.MasteryType bloodType)
        {
            bloodType = GlobalMasterySystem.MasteryType.None;
            if (guid.GuidHash == (int)Remainders.BloodType_VBlood || guid.GuidHash == (int)Remainders.BloodType_GateBoss)
                return false;
            if(!Enum.IsDefined(typeof(GlobalMasterySystem.MasteryType), guid.GuidHash)) {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Warning, $"Bloodline not found for guid {guid.GuidHash}. isKiller ({isKiller})", true);
                return false;
            }

            bloodType = (GlobalMasterySystem.MasteryType)guid.GuidHash;
            return true;
        }
    }
}
