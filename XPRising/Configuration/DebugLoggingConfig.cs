using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using XPRising.Utils;

namespace XPRising.Configuration;

public static class DebugLoggingConfig
{
    private static ConfigFile _configFile;
    private static readonly bool[] LoggingInfo = new bool[Enum.GetNames<Plugin.LogSystem>().Length]; 
    
    public static void Initialize()
    {
        var configPath = AutoSaveSystem.ConfirmFile(AutoSaveSystem.ConfigPath, "DebugLoggingConfig.cfg");
        _configFile = new ConfigFile(configPath, true);
        
        // Currently, we are never updating and saving the config file in game, so just load the values.
        foreach (var system in Enum.GetValues<Plugin.LogSystem>())
        {
            if (system == Plugin.LogSystem.Debug)
            {
                LoggingInfo[(int)system] = Plugin.IsDebug;
            }
            else
            {
                LoggingInfo[(int)system] = _configFile.Bind(
                    "Debug",
                    $"{Enum.GetName(system)} system logging",
                    false,
                    "Logs detailed information about the system in your console. Enable before sending errors with this system.").Value;
            }
            // Let the log know which systems are actually logging.
            Plugin.Log(system, LogLevel.Info, $"is logging.");
        }
    }

    public static bool IsLogging(Plugin.LogSystem system)
    {
        return LoggingInfo[(int)system];
    }
}