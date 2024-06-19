using ProjectM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx.Logging;
using ProjectM.Network;
using Unity.Entities;
using XPRising.Configuration;
using XPRising.Extensions;
using XPRising.Models;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using GlobalMasteryConfig = XPRising.Models.GlobalMasteryConfig;

namespace XPRising.Systems;

public static class GlobalMasterySystem
{
    private static EntityManager _em = Plugin.Server.EntityManager;

    public static bool EffectivenessSubSystemEnabled = false;
    public static bool DecaySubSystemEnabled = false;
    public static bool SpellMasteryRequiresUnarmed = false;
    public static int DecayInterval = 60;
    public static string MasteryConfigPreset = "none";
    public static readonly string CustomPreset = "custom";
        
    public enum MasteryType
    {
        None = 0,
        WeaponUnarmed,
        WeaponSpear,
        WeaponSword,
        WeaponScythe,
        WeaponCrossbow,
        WeaponMace,
        WeaponSlasher,
        WeaponAxe,
        WeaponFishingPole,
        WeaponRapier,
        WeaponPistol,
        WeaponGreatSword,
        WeaponLongBow,
        WeaponWhip,
        Spell,
        BloodNone = Remainders.BloodType_None,
        BloodBrute = Remainders.BloodType_Brute,
        BloodCreature = Remainders.BloodType_Creature,
        // BloodDracula = Remainders.BloodType_DraculaTheImmortal,
        BloodDraculin = Remainders.BloodType_Draculin,
        BloodMutant = Remainders.BloodType_Mutant,
        BloodRogue = Remainders.BloodType_Rogue,
        BloodScholar = Remainders.BloodType_Scholar,
        BloodWarrior = Remainders.BloodType_Warrior,
        BloodWorker = Remainders.BloodType_Worker,
    }

    [Flags]
    public enum MasteryCategory
    {
        None = 0,
        Blood = 0b01,
        Weapon = 0b10,
        All = 0b11
    }
    
    // This is a "potential" name to mastery map. Multiple keywords map to the same mastery
    public static readonly Dictionary<string, MasteryType> KeywordToMasteryMap = new()
    {
        { "spell", MasteryType.Spell },
        { "magic", MasteryType.Spell },
        { "unarmed", MasteryType.WeaponUnarmed },
        { "spear", MasteryType.WeaponSpear },
        { "crossbow", MasteryType.WeaponCrossbow },
        { "slashers", MasteryType.WeaponSlasher },
        { "slasher", MasteryType.WeaponSlasher },
        { "scythe", MasteryType.WeaponScythe },
        { "reaper", MasteryType.WeaponScythe },
        { "sword", MasteryType.WeaponSword },
        { "fishingpole", MasteryType.WeaponFishingPole },
        { "mace", MasteryType.WeaponMace },
        { "axe", MasteryType.WeaponAxe },
        { "greatsword", MasteryType.WeaponGreatSword },
        { "rapier", MasteryType.WeaponRapier },
        { "pistol", MasteryType.WeaponPistol },
        { "dagger", MasteryType.WeaponSword },
        { "longbow", MasteryType.WeaponLongBow },
        { "xbow", MasteryType.WeaponCrossbow },
        { "whip", MasteryType.WeaponWhip },
        { "frail", MasteryType.BloodNone },
        { "none", MasteryType.BloodNone },
        { "mutant", MasteryType.BloodMutant },
        { "creature", MasteryType.BloodCreature },
        { "warrior", MasteryType.BloodWarrior },
        { "rogue", MasteryType.BloodRogue },
        { "brute", MasteryType.BloodBrute },
        { "scholar", MasteryType.BloodScholar },
        { "worker", MasteryType.BloodWorker },
        { "dracula", MasteryType.BloodDraculin },
        { "draculin", MasteryType.BloodDraculin }
    };
    
    private static LazyDictionary<MasteryType, GlobalMasteryConfig.MasteryConfig> _masteryConfig;
    private static List<GlobalMasteryConfig.SkillTree> _skillTrees;
    private static LazyDictionary<ulong, LazyDictionary<Entity, LazyDictionary<MasteryType, double>>> _masteryBank;
    
    public static MasteryCategory GetMasteryCategory(MasteryType type)
    {
        switch (type)
        {
            case MasteryType.None:
                return MasteryCategory.None;
            case MasteryType.WeaponUnarmed:
            case MasteryType.WeaponSpear:
            case MasteryType.WeaponSword:
            case MasteryType.WeaponScythe:
            case MasteryType.WeaponCrossbow:
            case MasteryType.WeaponMace:
            case MasteryType.WeaponSlasher:
            case MasteryType.WeaponAxe:
            case MasteryType.WeaponFishingPole:
            case MasteryType.WeaponRapier:
            case MasteryType.WeaponPistol:
            case MasteryType.WeaponGreatSword:
            case MasteryType.WeaponLongBow:
            case MasteryType.WeaponWhip:
            case MasteryType.Spell:
                return MasteryCategory.Weapon;
            case MasteryType.BloodNone:
            case MasteryType.BloodBrute:
            case MasteryType.BloodCreature:
            case MasteryType.BloodDraculin:
            case MasteryType.BloodMutant:
            case MasteryType.BloodRogue:
            case MasteryType.BloodScholar:
            case MasteryType.BloodWarrior:
            case MasteryType.BloodWorker:
                return MasteryCategory.Blood;
        }

        return MasteryCategory.None;
    }
    public static bool SetMasteryConfig(GlobalMasteryConfig globalMasteryConfig)
    {
        // Load skill trees
        _skillTrees = globalMasteryConfig.SkillTrees ?? new List<GlobalMasteryConfig.SkillTree>();
        _masteryConfig = new LazyDictionary<MasteryType, GlobalMasteryConfig.MasteryConfig>();
        _masteryBank = new LazyDictionary<ulong, LazyDictionary<Entity, LazyDictionary<MasteryType, double>>>();

        // Load mastery data
        foreach (var masteryType in Enum.GetValues<MasteryType>())
        {
            if (masteryType == MasteryType.None || masteryType == MasteryType.BloodNone) continue;
            
            var masteryData = new GlobalMasteryConfig.MasteryConfig();
            var masteryCategory = GetMasteryCategory(masteryType);
            try
            {
                if (masteryCategory == MasteryCategory.Weapon)
                {
                    globalMasteryConfig.DefaultWeaponMasteryConfig.CopyTo(ref masteryData);
                }
                else if (masteryCategory == MasteryCategory.Blood)
                {
                    globalMasteryConfig.DefaultBloodMasteryConfig.CopyTo(ref masteryData);
                }
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Error, $"Error loading default config: {e}", true);
            }

            try
            {
                if (globalMasteryConfig.Mastery != null && globalMasteryConfig.Mastery.TryGetValue(masteryType, out var config))
                {
                    // Apply any templates (in order) first
                    if (globalMasteryConfig.MasteryTemplates != null)
                    {
                        config.Templates?.ForEach(template =>
                        {
                            // If the template does not match anything, skip trying to apply it.
                            if (!globalMasteryConfig.MasteryTemplates.TryGetValue(template, out var templateData)) return;

                            // Copy over any template values
                            templateData.CopyTo(ref masteryData);
                        });
                    }

                    // Apply overrides
                    config.CopyTo(ref masteryData);
                }
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Error, $"Error applying overrides: {e}", true);
            }

            try
            {
                // Check to see if the points system has correct config
                masteryData.Points ??= new List<GlobalMasteryConfig.PointsData>();

                for (var i = 0; i < masteryData.Points.Count; i++)
                {
                    var pointsData = masteryData.Points[i];
                    pointsData.AllowedSkillTrees = pointsData.AllowedSkillTrees?.FindAll(treeName =>
                    {
                        if (_skillTrees.FindIndex(tree => tree.Name == treeName) < 0)
                        {
                            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Warning,
                                $"{masteryType} points are being pushed to a skill tree that is not configured ({treeName}). Dropping this tree requirement.");
                            return false;
                        }

                        return true;
                    });
                    masteryData.Points[i] = pointsData;
                }
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Error, $"Error validating points: {e}", true);
            }

            _masteryConfig[masteryType] = masteryData;
        }

        if (DebugLoggingConfig.IsLogging(Plugin.LogSystem.Mastery))
        {
            try
            {
                AutoSaveSystem.EnsureFile(AutoSaveSystem.ConfigPath, "LoadedGlobalMasteryConfig.json", () => JsonSerializer.Serialize(_masteryConfig, AutoSaveSystem.PrettyJsonOptions));
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Warning, $"Could not write out loaded mastery config: {e}");
            }
        }

        return true;
    }

    public static void DecayMastery(Entity userEntity, DateTime lastDecay)
    {
        var steamID = _em.GetComponentData<User>(userEntity).PlatformId;
        var elapsedTime = DateTime.Now - lastDecay;
        if (elapsedTime.TotalSeconds < DecayInterval) return;

        var decayTicks = (int)Math.Floor(elapsedTime.TotalSeconds / DecayInterval);
        if (decayTicks <= 0) return;
        
        var playerMastery = Database.PlayerMastery[steamID];

        var maxDecayValue = 0d;
        foreach (var (masteryType, masteryConfig) in _masteryConfig)
        {
            var decayValue = decayTicks * masteryConfig.DecayValue;
            var realDecay = ModMastery(steamID, playerMastery, masteryType, -decayValue);
            maxDecayValue = Math.Max(maxDecayValue, realDecay);
        }
        if (maxDecayValue > 0)
        {
            var message =
                L10N.Get(L10N.TemplateKey.MasteryDecay)
                    .AddField("{duration}", $"{elapsedTime.TotalMinutes:F1}")
                    .AddField("{decay}", $"{maxDecayValue:F3}");
            Output.SendMessage(steamID, message);
        }

        Database.PlayerMastery[steamID] = playerMastery;
    }

    public static void BankMastery(ulong steamID, Entity targetEntity, MasteryType type, double changeInMastery)
    {
        // Ignore any mastery "gained" when not in combat. This is likely not an entity we want to gain combat from.
        if (!Cache.PlayerInCombat(steamID)) return;
        _masteryBank[steamID][targetEntity][type] += changeInMastery;
    }

    public static void KillEntity(List<Alliance.ClosePlayer> closeAllies, Entity targetEntity)
    {
        foreach (var player in closeAllies)
        {
            var loggingMastery = Database.PlayerLogConfig[player.steamID].LoggingMastery;
            if (!_masteryBank[player.steamID].TryRemove(targetEntity, out var masteryToStore)) continue;
            foreach (var (masteryType, changeInMastery) in masteryToStore)
            {
                if (loggingMastery)
                {
                    if (ModMastery(player.steamID, masteryType, changeInMastery) == 0)
                    {
                        var currentMastery = Database.PlayerMastery[player.steamID][masteryType].Mastery;
                        var message =
                            L10N.Get(L10N.TemplateKey.MasteryFull)
                                .AddField("{masteryType}", $"{Enum.GetName(masteryType)}")
                                .AddField("{currentMastery}", $"{currentMastery:F2}");
                        Output.SendMessage(player.steamID, message);
                    }
                    else
                    {
                        var currentMastery = Database.PlayerMastery[player.steamID][masteryType].Mastery;
                        var message =
                            L10N.Get(L10N.TemplateKey.MasteryGainOnKill)
                                .AddField("{masteryChange}", $"{changeInMastery:+##.###;-##.###;0}")
                                .AddField("{masteryType}", $"{Enum.GetName(masteryType)}")
                                .AddField("{currentMastery}", $"{currentMastery:F2}");
                        Output.SendMessage(player.steamID, message);
                    }
                }
            }
        }
    }

    public static void ExitCombat(ulong steamID)
    {
        if (!_masteryBank.TryRemove(steamID, out var data)) return;
        
        var lostMastery = data.Aggregate(0d, (a, b) => a + b.Value.Aggregate(0d, (c, d) => c + d.Value));
        Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Mastery lost on combat exit: {steamID}: {lostMastery}");
    }

    /// <summary>
    /// Applies the change in mastery to the mastery type of the specified player.
    /// </summary>
    /// <param name="steamID"></param>
    /// <param name="type"></param>
    /// <param name="changeInMastery"></param>
    /// <returns>Whether the amount of change actually applied to the mastery</returns>
    public static double ModMastery(ulong steamID, MasteryType type, double changeInMastery)
    {
        var playerMastery = Database.PlayerMastery[steamID];
        
        return ModMastery(steamID, playerMastery, type, changeInMastery);
    }
    
    private static double ModMastery(ulong steamID, LazyDictionary<MasteryType, MasteryData> playerMastery, MasteryType type, double changeInMastery)
    {
        var mastery = playerMastery[type];
        var currentMastery = mastery.Mastery;
        mastery.Mastery += mastery.CalculateBaseMasteryGrowth(changeInMastery);
        Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Mastery changed: {steamID}: {Enum.GetName(type)}: {mastery.Mastery}");
        playerMastery[type] = mastery;

        return mastery.Mastery - currentMastery;
    }
    
    public static void ResetMastery(ulong steamID, MasteryCategory category) {
        if (!EffectivenessSubSystemEnabled) {
            Output.SendMessage(steamID, L10N.Get(L10N.TemplateKey.SystemEffectivenessDisabled).AddField("{system}", "mastery"));
            return;
        }
        if (Database.PlayerMastery.TryGetValue(steamID, out var playerMastery))
        {
            foreach (var (masteryType, masteryData) in playerMastery)
            {
                var masteryCategory = GetMasteryCategory(masteryType);
                // Reset mastery if the category matches and the mastery is actually above 0.
                if (masteryData.Mastery > 0 && (category & masteryCategory) != MasteryCategory.None)
                {
                    var config = _masteryConfig[masteryType];
                    playerMastery[masteryType] = masteryData.ResetMastery(config.MaxEffectiveness, config.GrowthPerEffectiveness);
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Mastery reset: {Enum.GetName(masteryType)}: {masteryData}");
                    Output.SendMessage(steamID, L10N.Get(L10N.TemplateKey.MasteryReset).AddField("{masteryType}", Enum.GetName(masteryType)));
                }
                Database.PlayerMastery[steamID] = playerMastery;
            }
        }
    }

    public static void BuffReceiver(ref LazyDictionary<UnitStatType, float> statBonus, Entity owner, ulong steamID)
    {
        var activeWeaponMastery = WeaponMasterySystem.WeaponToMasteryType(WeaponMasterySystem.GetWeaponType(owner, out var weaponEntity));
        var activeBloodMastery = BloodlineSystem.BloodMasteryType(owner);
        var playerMastery = Database.PlayerMastery[steamID];
    
        foreach (var (masteryType, masteryData) in playerMastery)
        {
            // Skip trying to add a buff if there is no config for it
            if (!MasteryConfig(masteryType, out var config)) continue;
            
            var masteryPercentage = masteryData.Mastery / 100 * (EffectivenessSubSystemEnabled ? masteryData.Effectiveness : 1);
            var isMasteryActive = false;
            if (masteryType == activeWeaponMastery)
            {
                if (config.ActiveBonus?.Count > 0)
                {
                    if (_em.TryGetBuffer<ModifyUnitStatBuff_DOTS>(weaponEntity, out var statBuffer))
                    {
                        foreach (var statModifier in statBuffer)
                        {
                            var bonus = CalculateActiveBonus(config.ActiveBonus, statModifier, (float)masteryPercentage);
                            statBonus[statModifier.StatType] += bonus;
                        }
                    }
                }

                isMasteryActive = true;
            } else if (masteryType == activeBloodMastery)
            {
                // TODO bonus to active blood mastery
                isMasteryActive = true;
            } else if (masteryType == MasteryType.Spell)
            {
                isMasteryActive = !SpellMasteryRequiresUnarmed || activeWeaponMastery == MasteryType.WeaponUnarmed;
            }
            
            if (config.BaseBonus?.Count > 0)
            {
                if (isMasteryActive)
                {
                    foreach (var data in config.BaseBonus.Where(data => masteryData.Mastery >= data.RequiredMastery))
                    {
                        statBonus[data.StatType] += data.BonusType == GlobalMasteryConfig.BonusData.Type.Fixed
                            ? data.Value
                            : data.Value * (float)masteryPercentage;
                    }
                }
                else
                {
                    foreach (var data in config.BaseBonus.Where(data => masteryData.Mastery >= data.RequiredMastery))
                    {
                        statBonus[data.StatType] += (data.BonusType == GlobalMasteryConfig.BonusData.Type.Fixed
                            ? data.Value
                            : data.Value * (float)masteryPercentage) * data.InactiveMultiplier;
                    }
                }
            }
        }

        foreach (var tree in _skillTrees)
        {
            // TODO apply skill trees
        }
    }

    private static bool MasteryConfig(MasteryType type, out GlobalMasteryConfig.MasteryConfig config)
    {
        if (_masteryConfig.ContainsKey(type))
        {
            config = _masteryConfig[type];
            return true;
        }

        config = new GlobalMasteryConfig.MasteryConfig();
        return false;
    }

    private static float CalculateActiveBonus(List<GlobalMasteryConfig.ActiveBonusData> activeBonusData, ModifyUnitStatBuff_DOTS statBuffDots, float masteryPercentage)
    {
        var statBonus = 0f;
        activeBonusData.ForEach(data =>
        {
            var applyBonus = false;
            switch (data.StatCategory)
            {
                case UnitStatTypeExtensions.Category.None:
                    break;
                case UnitStatTypeExtensions.Category.Offensive:
                    applyBonus = statBuffDots.StatType.IsOffensiveStat();
                    break;
                case UnitStatTypeExtensions.Category.Defensive:
                    applyBonus = statBuffDots.StatType.IsDefensiveStat();
                    break;
                case UnitStatTypeExtensions.Category.Resource:
                    applyBonus = statBuffDots.StatType.IsResourceStat();
                    break;
                case UnitStatTypeExtensions.Category.Other:
                    break;
                case UnitStatTypeExtensions.Category.Any:
                    applyBonus = true;
                    break;
            }

            if (applyBonus)
            {
                statBonus += data.BonusType == GlobalMasteryConfig.BonusData.Type.Fixed
                    ? data.Value
                    : data.Value * statBuffDots.Value * masteryPercentage;
            }
        });
        
        return statBonus;
    }

    public static GlobalMasteryConfig DefaultMasteryConfig()
    {
        switch (MasteryConfigPreset)
        {
            case "basic":
                return DefaultBasicMasteryConfig();
            case "fixed":
                return DefaultFixedMasteryConfig();
            case "decay":
                return DefaultDecayMasteryConfig();
            case "decay-op":
                return DefaultOPDecayMasteryConfig();
            case "none":
            default:
                return DefaultNoneMasteryConfig();
        }
    }
    
    public static GlobalMasteryConfig DefaultNoneMasteryConfig()
    {
        return new GlobalMasteryConfig {};
    }

    public static GlobalMasteryConfig DefaultBasicMasteryConfig()
    {
        return new GlobalMasteryConfig
        {
            Mastery = new LazyDictionary<MasteryType, GlobalMasteryConfig.MasteryConfig>
            {
                {
                    MasteryType.Spell, new GlobalMasteryConfig.MasteryConfig 
                    {
                        BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                        {
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellPower, RequiredMastery = 0, Value = 30, InactiveMultiplier = 0.1f},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeDamage, RequiredMastery = 30, Value = 30, InactiveMultiplier = 0.1f},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeChance, RequiredMastery = 60, Value = 10, InactiveMultiplier = 0.1f},
                        }
                    }
                }
            },
            DefaultWeaponMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.PhysicalPower, RequiredMastery = 0, Value = 30, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.PhysicalCriticalStrikeDamage, RequiredMastery = 30, Value = 30, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.PhysicalCriticalStrikeChance, RequiredMastery = 60, Value = 10, InactiveMultiplier = 0.1f},
                },
                ActiveBonus = new List<GlobalMasteryConfig.ActiveBonusData>
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatCategory = UnitStatTypeExtensions.Category.Any, Value = 5}
                }
            },
            DefaultBloodMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.MovementSpeed, RequiredMastery = 30, Value = 10, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.PrimaryAttackSpeed, RequiredMastery = 10, Value = 2, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.PrimaryAttackSpeed, RequiredMastery = 50, Value = 3, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.CooldownRecoveryRate, RequiredMastery = 70, Value = 10, InactiveMultiplier = 0.1f},
                }
            }
        };
    }
    
    public static GlobalMasteryConfig DefaultFixedMasteryConfig()
    {
        return new GlobalMasteryConfig
        {
            Mastery = new LazyDictionary<MasteryType, GlobalMasteryConfig.MasteryConfig>
            {
                {
                    MasteryType.Spell, new GlobalMasteryConfig.MasteryConfig 
                    {
                        BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                        {
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellPower, RequiredMastery = 0, Value = 30, InactiveMultiplier = 0.1f},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeDamage, RequiredMastery = 30, Value = 30, InactiveMultiplier = 0.1f},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeChance, RequiredMastery = 60, Value = 5, InactiveMultiplier = 0.1f},
                        }
                    }
                }
            },
            DefaultWeaponMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                ActiveBonus = new List<GlobalMasteryConfig.ActiveBonusData>
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatCategory = UnitStatTypeExtensions.Category.Any, Value = 5}
                }
            },
            DefaultBloodMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.MovementSpeed, RequiredMastery = 0, Value = 10, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.PrimaryAttackSpeed, RequiredMastery = 10, Value = 2, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.PrimaryAttackSpeed, RequiredMastery = 50, Value = 3, InactiveMultiplier = 0.1f},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.CooldownRecoveryRate, RequiredMastery = 70, Value = 10, InactiveMultiplier = 0.1f},
                }
            }
        };
    }
    
    public static GlobalMasteryConfig DefaultDecayMasteryConfig()
    {
        return new GlobalMasteryConfig
        {
            Mastery = new LazyDictionary<MasteryType, GlobalMasteryConfig.MasteryConfig>
            {
                {
                    MasteryType.Spell, new GlobalMasteryConfig.MasteryConfig 
                    {
                        BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                        {
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellPower, RequiredMastery = 0, Value = 50},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeDamage, RequiredMastery = 0, Value = 40},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeChance, RequiredMastery = 0, Value = 10},
                        },
                        DecayValue = 0.1f,
                        MaxEffectiveness = 1,
                        GrowthPerEffectiveness = 1
                    }
                }
            },
            DefaultWeaponMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                ActiveBonus = new List<GlobalMasteryConfig.ActiveBonusData>
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatCategory = UnitStatTypeExtensions.Category.Any, Value = 20}
                },
                DecayValue = 0.1f,
                MaxEffectiveness = 1,
                GrowthPerEffectiveness = 1
            },
            DefaultBloodMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.MovementSpeed, RequiredMastery = 0, Value = 10},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.PrimaryAttackSpeed, RequiredMastery = 10, Value = 2},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.PrimaryAttackSpeed, RequiredMastery = 50, Value = 3},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Fixed, StatType = UnitStatType.CooldownRecoveryRate, RequiredMastery = 70, Value = 10},
                },
                DecayValue = 0.1f,
                MaxEffectiveness = 1,
                GrowthPerEffectiveness = 1
            }
        };
    }
    
    public static GlobalMasteryConfig DefaultOPDecayMasteryConfig()
    {
        return new GlobalMasteryConfig
        {
            Mastery = new LazyDictionary<MasteryType, GlobalMasteryConfig.MasteryConfig>
            {
                {
                    MasteryType.Spell, new GlobalMasteryConfig.MasteryConfig 
                    {
                        BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                        {
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellPower, RequiredMastery = 0, Value = 50},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeDamage, RequiredMastery = 30, Value = 50},
                            new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.SpellCriticalStrikeChance, RequiredMastery = 60, Value = 15},
                        },
                        DecayValue = 0.1f,
                        MaxEffectiveness = 5,
                        GrowthPerEffectiveness = 1
                    }
                }
            },
            DefaultWeaponMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.PhysicalPower, RequiredMastery = 0, Value = 50},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.PhysicalCriticalStrikeDamage, RequiredMastery = 30, Value = 50},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.PhysicalCriticalStrikeChance, RequiredMastery = 60, Value = 15},
                },
                ActiveBonus = new List<GlobalMasteryConfig.ActiveBonusData>
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatCategory = UnitStatTypeExtensions.Category.Any, Value = 20}
                },
                DecayValue = 0.1f,
                MaxEffectiveness = 5,
                GrowthPerEffectiveness = 1
            },
            DefaultBloodMasteryConfig = new GlobalMasteryConfig.MasteryConfig()
            {
                BaseBonus = new List<GlobalMasteryConfig.BonusData>()
                {
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.MovementSpeed, RequiredMastery = 0, Value = 10},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.PrimaryAttackSpeed, RequiredMastery = 30, Value = 10},
                    new(){BonusType = GlobalMasteryConfig.BonusData.Type.Ratio, StatType = UnitStatType.CooldownRecoveryRate, RequiredMastery = 60, Value = 20},
                },
                DecayValue = 0.1f,
                MaxEffectiveness = 5,
                GrowthPerEffectiveness = 1
            }
        };
    }
}