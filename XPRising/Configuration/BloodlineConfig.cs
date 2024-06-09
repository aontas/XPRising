using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Configuration;

public static class BloodlineConfig
{
    private static ConfigFile _configFile;
    
    public static void Initialize()
    {
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, "Loading Bloodline config");
        var configPath = AutoSaveSystem.ConfirmFile(AutoSaveSystem.ConfigPath, "BloodlineConfig.cfg");
        _configFile = new ConfigFile(configPath, true);
        
        // Currently, we are never updating and saving the config file in game, so just load the values.
        BloodlineSystem.MercilessBloodlines = _configFile.Bind("Bloodlines", "Merciless Bloodlines", true, "Causes bloodlines to only grow when you kill something with a matching bloodline of that has a quality higher than the bloodline strength. Finally, a reward when you accidentally kill that 100% blood you found").Value;
        BloodlineSystem.MasteryGainMultiplier = _configFile.Bind("Bloodlines", "Mastery Gain Multiplier", 1.0, "Multiply the gained mastery value by this amount.").Value;
        var inactiveMultiplier = 0.1;//_configFile.Bind("Bloodlines", "Inactive multiplier", 0.1, "The amount of bloodline mastery to apply for each of the inactive bloodlines. 0 is Off, 1 is 100%.").Value;
        BloodlineSystem.InactiveMultiplier = Math.Clamp(inactiveMultiplier, 0, 1); // Validate multiplier
        
        BloodlineSystem.VBloodMultiplier = _configFile.Bind("Bloodlines", "Bloodline VBlood Multiplier", 15.0, "The multiplier applied to the effective level of VBlood enemies for bloodline gains.").Value;
        
        BloodlineSystem.IsDecaySystemEnabled = _configFile.Bind("Bloodlines", "Enable Bloodline Decay", false, "Enable/disable the decay of bloodline over time.").Value;
        BloodlineSystem.DecayInterval = _configFile.Bind("Bloodlines", "Decay Interval", 60, "Amount of seconds per decay tick.").Value;
        BloodlineSystem.OfflineDecayValue = _configFile.Bind("Bloodlines", "Decay Value", 0.1, "Bloodlines will decay by this amount for every decay tick.(1 -> 0.001%)").Value;
        BloodlineSystem.OnlineDecayValue = BloodlineSystem.OfflineDecayValue;
        
        BloodlineSystem.EffectivenessSubSystemEnabled = _configFile.Bind("Bloodlines", "Enable Effectiveness Subsystem", true, "Enables the Effectiveness bloodline subsystem, which lets you reset your bloodline to gain a multiplier to the effects of the matching bloodline.").Value;
        BloodlineSystem.GrowthPerEffectiveness = _configFile.Bind("Bloodlines", "Growth per effectiveness", 1.0, "Used to determine how bloodline mastery growth is affected at higher effectiveness levels. When enabled, more effectiveness will mean that it is slower to gain bloodline mastery (at 1, 200% effectiveness gives a growth rate of 50%). A smaller number here will reduce the growth rate faster. Use a value of 0 to disable. Negative values are converted to positive.").Value;
        BloodlineSystem.MaxBloodlineEffectiveness = _configFile.Bind("Bloodlines", "Maximum Effectiveness", 5.0, "The maximum bloodline effectiveness where 1 is 100%.").Value;
    }
}