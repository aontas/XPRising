using ProjectM;
using ProjectM.Network;
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
        public static readonly Dictionary<PrefabGUID, GlobalMasterySystem.MasteryType> BuffToBloodTypeMap = new()
        {
            // Seems like we only need to check against the T1 options
            { new PrefabGUID((int)Effects.AB_BloodBuff_Worker_Tier1), GlobalMasterySystem.MasteryType.BloodWorker },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Warrior_Tier1), GlobalMasterySystem.MasteryType.BloodWarrior },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Scholar_Tier1), GlobalMasterySystem.MasteryType.BloodScholar },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Rogue_Tier1), GlobalMasterySystem.MasteryType.BloodRogue },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Mutant_Tier1), GlobalMasterySystem.MasteryType.BloodMutant },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Draculin_Tier1), GlobalMasterySystem.MasteryType.BloodDraculin },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Dracula_Tier1), GlobalMasterySystem.MasteryType.BloodDracula },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Creature_Tier1), GlobalMasterySystem.MasteryType.BloodCreature },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Corruption_Tier1), GlobalMasterySystem.MasteryType.BloodCorruption },
            { new PrefabGUID((int)Effects.AB_BloodBuff_Brute_Tier1), GlobalMasterySystem.MasteryType.BloodBrute }
        };
        
        private static EntityManager _em = Plugin.Server.EntityManager;
        private static Random _random = new Random();

        public static bool MercilessBloodlines = true;
        public const int BloodTypeCount = 10;
        public static int VBloodAddsXTypes = BloodTypeCount;

        public static double VBloodMultiplier = 15;
        public static double MasteryGainMultiplier = 1.0;

        public static void UpdateBloodline(Entity killer, Entity victim, bool killOnly)
        {
            if (killer == victim) return;
            if (_em.HasComponent<Minion>(victim)) return;

            var victimLevel = _em.GetComponentData<UnitLevel>(victim);
            var killerUserEntity = _em.GetComponentData<PlayerCharacter>(killer).UserEntity;
            var killerUserComponent = _em.GetComponentData<User>(killerUserEntity);
            var steamID = killerUserComponent.PlatformId;
            Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Updating bloodline mastery for {steamID}");
            
            double growthVal = Math.Clamp(victimLevel.Level.Value - ExperienceSystem.GetLevel(steamID), 1, 10);
            
            GlobalMasterySystem.MasteryType killerBloodType;
            if (_em.TryGetComponentData<Blood>(killer, out var killerBlood)){
                if (!GuidToBloodType(killerBlood.BloodType, out killerBloodType)) return;
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
                isVBlood = Helper.IsVBlood(victimBlood);
                if (isVBlood)
                {
                    // When running the kill only step for VBloods, only add to the current bloodline, not multi-bloodlines
                    if (VBloodAddsXTypes > 0 && !killOnly)
                    {
                        var baseGrowthVal = growthVal * 0.05 * MasteryGainMultiplier * VBloodMultiplier;
                        var pmd = Database.PlayerMastery[steamID];
                        if (VBloodAddsXTypes >= BloodTypeCount)
                        {
                            Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Adding V Blood bonus ({baseGrowthVal}) to all blood types");
                            foreach (var bloodType in BuffToBloodTypeMap.Values)
                            {
                                GlobalMasterySystem.BankMastery(steamID, victim, bloodType, baseGrowthVal * pmd[killerBloodType].Growth);
                            }
                        }
                        else
                        {
                            var selectedBloodTypes =
                                BuffToBloodTypeMap.Values.OrderBy(x => _random.Next()).Take(VBloodAddsXTypes);
                            Plugin.Log(LogSystem.Bloodline, LogLevel.Info, () => $"Adding V Blood bonus ({baseGrowthVal}) to {VBloodAddsXTypes} blood types: {string.Join(",", selectedBloodTypes)}");
                            foreach (var bloodType in selectedBloodTypes)
                            {
                                GlobalMasterySystem.BankMastery(steamID, victim, bloodType, baseGrowthVal * pmd[killerBloodType].Growth);
                            }
                        }
                        return;
                    }
                    else
                    {
                        victimBloodType = killerBloodType;
                        victimBloodQuality = 100f;
                    }
                }
                else
                {
                    GuidToBloodType(victimBlood.UnitBloodType, out victimBloodType);
                }
            }
            else
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"victim does not have blood: Killer ({killer}), Victim ({victim}");
                return;
            }

            if (victimBloodType == GlobalMasterySystem.MasteryType.None)
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"victim has frail blood, not modifying: Killer ({killer}), Victim ({victim})");
                return;
            }
            
            var growthModifier = killOnly ? 0.4 : isVBlood ? VBloodMultiplier : 1.0;
            
            var playerMasterydata = Database.PlayerMastery[steamID];
            var bloodlineMastery = playerMasterydata[victimBloodType];
            growthVal *= bloodlineMastery.Growth * growthModifier * (victimBloodQuality * 0.01f);
            
            if (MercilessBloodlines && victimBloodQuality <= bloodlineMastery.Mastery)
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                    $"merciless bloodlines exit: victim blood quality less than killer mastery: Killer ({bloodlineMastery.Mastery}), Victim ({victimBloodQuality})");
                if (Cache.PlayerHasUINotifications(steamID))
                {
                    var message = L10N.Get(L10N.TemplateKey.BloodlineMercilessErrorWeak);
                    var preferences = Database.PlayerPreferences[steamID];
                    XPShared.Transport.Utils.ServerSendNotification(killerUserComponent, "bloodline", message.Build(preferences.Language), LogLevel.Warning);
                }
                else if (Database.PlayerPreferences[steamID].LoggingMastery)
                {
                    var message = L10N.Get(L10N.TemplateKey.BloodlineMercilessErrorWeak);
                    Output.SendMessage(killerUserEntity, message);
                }
                return;
            }

            if (_em.HasComponent<PlayerCharacter>(victim))
            {
                var victimGear = _em.GetComponentData<Equipment>(victim);
                var bonusMastery = victimGear.ArmorLevel + victimGear.WeaponLevel + victimGear.SpellLevel;
                growthVal *= (1 + (bonusMastery * 0.01));
                
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Bonus bloodline mastery {bonusMastery:F3}]");
            }
            
            Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                () => $"Blood growth {Enum.GetName(victimBloodType)}: [{growthVal:F3} * 0.05 * {MasteryGainMultiplier:F3} => {growthVal * 0.05 * MasteryGainMultiplier:F3}]");
            growthVal *= 0.05 * MasteryGainMultiplier;
            
            GlobalMasterySystem.BankMastery(steamID, victim, victimBloodType, growthVal);
        }

        public static GlobalMasterySystem.MasteryType BloodMasteryType(Entity entity)
        {
            var bloodType = GlobalMasterySystem.MasteryType.None;
            if (_em.TryGetComponentData<Blood>(entity, out var entityBlood))
            {
                GuidToBloodType(entityBlood.BloodType, out bloodType);
            }
            return bloodType;
        }

        private static bool GuidToBloodType(PrefabGUID guid, out GlobalMasterySystem.MasteryType bloodType)
        {
            bloodType = GlobalMasterySystem.MasteryType.None;
            if (guid.GuidHash == (int)Remainders.BloodType_VBlood || guid.GuidHash == (int)Remainders.BloodType_GateBoss)
                return false;
            if(!Enum.IsDefined(typeof(GlobalMasterySystem.MasteryType), guid.GuidHash)) {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Warning, $"Bloodline not found for guid {guid.GuidHash}", true);
                return false;
            }

            bloodType = (GlobalMasterySystem.MasteryType)guid.GuidHash;
            return true;
        }
    }
}
