using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Configuration;

public static class MasteryConfig
{
    private static ConfigFile _configFile;
    
    public static void Initialize()
    {
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, "Loading Weapon Mastery config");
        var configPath = AutoSaveSystem.ConfirmFile(AutoSaveSystem.ConfigPath, "MasteryConfig.cfg");
        _configFile = new ConfigFile(configPath, true);
        
        // Currently, we are never updating and saving the config file in game, so just load the values.
        WeaponMasterySystem.MasteryCombatTick = _configFile.Bind("Mastery", "Mastery Value/Combat Ticks", 5, "Configure the amount of mastery gained per combat ticks. (5 -> 0.005%)").Value;
        WeaponMasterySystem.MaxCombatTick = _configFile.Bind("Mastery", "Max Combat Ticks", 12, "Mastery will no longer increase after this many ticks is reached in combat. (1 tick = 5 seconds)").Value;
        WeaponMasterySystem.MasteryGainMultiplier = _configFile.Bind("Mastery", "Mastery Gain Multiplier", 1.0, "Multiply the gained mastery value by this amount.").Value;
        var inactiveMultiplier = 0.1;//_configFile.Bind("Mastery", "Inactive multiplier", 0.1, "The amount of mastery to apply for each unequipped weapon. 0 is Off, 1 is 100%.").Value;
        WeaponMasterySystem.InactiveMultiplier = Math.Clamp(inactiveMultiplier, 0, 1); // Validate multiplier
        
        WeaponMasterySystem.VBloodMultiplier = _configFile.Bind("Mastery", "VBlood Mastery Multiplier", 15.0, "Multiply Mastery gained from VBlood kill.").Value;
        
        WeaponMasterySystem.IsDecaySystemEnabled = _configFile.Bind("Mastery", "Enable Mastery Decay", false, "Enable/disable the decay of weapon mastery over time.").Value;
        WeaponMasterySystem.DecayInterval = _configFile.Bind("Mastery", "Decay Interval", 60, "Amount of seconds per decay tick.").Value;
        WeaponMasterySystem.OfflineDecayValue = _configFile.Bind("Mastery", "Decay Value", 0.1, "Mastery will decay by this amount for every decay tick.(1 -> 0.001%)").Value;
        WeaponMasterySystem.OnlineDecayValue = WeaponMasterySystem.OfflineDecayValue;
        
        WeaponMasterySystem.EffectivenessSubSystemEnabled = _configFile.Bind("Mastery", "Enable Effectiveness Subsystem", true, "Enables the Effectiveness mastery subsystem, which lets you reset your mastery to gain a multiplier to the effects of the matching mastery.").Value;
        WeaponMasterySystem.GrowthPerEffectiveness = _configFile.Bind("Mastery", "Growth per effectiveness", 1.0, "Used to determine how mastery growth is affected at higher effectiveness levels. When enabled, more effectiveness will mean that it is slower to gain mastery (at 1, 200% effectiveness gives a growth rate of 50%). A smaller number here will reduce the growth rate faster. Use a value of 0 to disable. Negative values are converted to positive.").Value;
        WeaponMasterySystem.MaxEffectiveness = _configFile.Bind("Mastery", "Maximum Effectiveness", 5.0, "The maximum mastery effectiveness where 1 is 100%.").Value;
        WeaponMasterySystem.OffensiveStatIncreaseFactor = _configFile.Bind("Mastery", "Offensive Stat Increase Factor", 10, "The percentage increase in offensive stats for every 100% mastery").Value;
        WeaponMasterySystem.OffMasteryMultiplier = _configFile.Bind("Mastery", "Off-Mastery Multiplier", 0.5, "The multiplier for buffs from primary mastery applied to off-mastery.").Value;
    }
}