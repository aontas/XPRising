using BepInEx.Configuration;
using BepInEx.Logging;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Configuration;

public static class ExperienceConfig
{
    private static ConfigFile _configFile; 
    
    public static void Initialize()
    {
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, "Loading XP config");
        var configPath = AutoSaveSystem.ConfirmFile(AutoSaveSystem.ConfigPath, "ExperienceConfig.cfg");
        _configFile = new ConfigFile(configPath, true);
        
        // Currently, we are never updating and saving the config file in game, so just load the values.
        
        // TODO currently not supported
        // ExperienceSystem.ShouldAllowGearLevel = _configFile.Bind("Experience", "Allow Gear Level", false, "Enable/disable gear level adjustment.").Value;
        // ExperienceSystem.LevelRewardsOn = _configFile.Bind("Experience", "Enable Level Rewards", false, "Enable rewards per level.").Value;

        ExperienceSystem.MaxLevel = _configFile.Bind("Experience", "Max Level", 100, "Configure the experience system max level.").Value;
        ExperienceSystem.ExpMultiplier = _configFile.Bind("Experience", "Multiplier", 1.5f, "Multiply the EXP gained by player.\n" +
                "Ex.: 0.7f -> Will reduce the EXP gained by 30%\nFormula: BaseExpValue * EXPMultiplier").Value;
        ExperienceSystem.VBloodMultiplier = _configFile.Bind("Experience", "VBlood Multiplier", 15f, "Multiply EXP gained from VBlood kills.\n" +
                "Formula: EXPGained * VBloodMultiplier * EXPMultiplier").Value;
        ExperienceSystem.GroupMaxDistance = _configFile.Bind("Experience", "Group Range", 50f, "Set the maximum distance an ally (player) has to be from the player for them to share EXP with the player. Set this to 0 to disable groups.").Value;
        ExperienceSystem.MaxXpGainPercentage = _configFile.Bind("Experience", "Max XP Gain Percent", 50f,
            "Set the maximum XP a player can gain, based on the percentage of XP required for the current level. For example, if the player's level takes 300 XP, a value of 50% will result in the max XP gain for a single kill to be 150 XP. Set to 0 to disable.").Value;

        ExperienceSystem.PvpXpLossPercent = _configFile.Bind("Rates, Experience", "PvP XP Loss Percent", 0f, "Sets the percentage of XP to the next level lost on a PvP death").Value;
        ExperienceSystem.PveXpLossPercent = _configFile.Bind("Rates, Experience", "PvE XP Loss Percent", 10f, "Sets the percentage of XP to the next level lost on a PvE death").Value;

        ExperienceSystem.XpGainedMessageTemplate = _configFile.Bind("Templates", "XP Gained Message Template", ExperienceSystem.XpGainedMessageTemplate, "Sets the template to format the message displayed when the player gains XP points. Allowed placeholders: {xpGained}, {mobLevel}, {earned}, {needed}.").Value;
        ExperienceSystem.XpLostMessageTemplate = _configFile.Bind("Templates", "XP Lost Message Template", ExperienceSystem.XpLostMessageTemplate, "Sets the template to format the message displayed when the player loses XP points. Allowed placeholders: {xpLost}, {earned}, {needed}.").Value;
        ExperienceSystem.LevelUpMessageTemplate = _configFile.Bind("Templates", "Level Up Message Template", ExperienceSystem.LevelUpMessageTemplate, "Sets the template to format the message displayed when the player levels up. Allowed placeholders: {level}.").Value;
    }
}