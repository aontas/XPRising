using System;
using System.Collections.Generic;
using BepInEx;
using VampireCommandFramework;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using ProjectM;
using Unity.Entities;
using UnityEngine;
using Stunlock.Core;
using XPRising.Commands;
using XPRising.Components.RandomEncounters;
using XPRising.Configuration;
using XPRising.Systems;
using XPRising.Utils;
using XPRising.Utils.Prefabs;

namespace XPRising
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.Bloodstone")]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    public class Plugin : BasePlugin
    {
        public static Harmony harmony;

        internal static Plugin Instance { get; private set; }

        public static bool IsInitialized = false;
        public static bool BloodlineSystemActive = false;
        public static bool ExperienceSystemActive = true;
        public static bool PlayerGroupsActive = true;
        public static bool PowerUpCommandsActive = false;
        public static bool RandomEncountersSystemActive = false;
        public static bool WeaponMasterySystemActive = false;
        public static bool WantedSystemActive = true;
        public static bool WaypointsActive = false;

        private static bool _adminCommandsRequireAdmin = false;

        private static ManualLogSource _logger;
        private static World _serverWorld;
        public static World Server
        {
            get
            {
                if (_serverWorld != null) return _serverWorld;

                _serverWorld = GetWorld("Server")
                    ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
                return _serverWorld;
            }
        }

        public static bool IsServer => Application.productName == "VRisingServer";

        private static World GetWorld(string name)
        {
            foreach (var world in World.s_AllWorlds)
            {
                if (world.Name == name)
                {
                    return world;
                }
            }

            return null;
        }

        public void InitCoreConfig()
        {
            Helper.buffGUID = Config.Bind("Core", "Buff GUID", (int)Effects.AB_BloodBuff_VBlood_0, "The GUID of the buff that gets used when mastery, bloodline, etc changes.\nDefault is now boneguard set bonus 2, but you can set anything else too.\nThe only reason to change this is if it clashes with another mod.").Value;
            Helper.AppliedBuff = new PrefabGUID(Helper.buffGUID);
            Helper.ForbiddenBuffGuid = Config.Bind("Core", "Forbidden Buff GUID", Helper.ForbiddenBuffGuid, "The GUID of the buff that prohibits you from getting mastery buffs\nDefault is boneguard set bonus 1. If this is the same value as Buff GUID, then none will get buffs.\nThe only reason to change this is if it clashes with another mod.").Value;
            Helper.humanReadablePercentageStats = Config.Bind("Core", "Human Readable Percentage Stats", true, "Determines if rates for percentage stats should be read as out of 100 instead of 1.").Value;
            Helper.inverseMultipersDisplayReduction = Config.Bind("Core", "Inverse Multipliers Display Reduction", true, "Determines if inverse multiplier stats display their reduction, or the final value.").Value;
            
            _adminCommandsRequireAdmin = Config.Bind("Admin", "Admin commands require admin", true, "When set to false, commands marked as requiring admin, no longer require admin.").Value;

            BloodlineSystemActive = Config.Bind("System", "Enable Bloodline Mastery system", false,  "Enable/disable the bloodline mastery system.").Value;
            ExperienceSystemActive = Config.Bind("System", "Enable Experience system", true,  "Enable/disable the experience system.").Value;
            PlayerGroupsActive = Config.Bind("System", "Enable Player Groups", true,  "Enable/disable the player group system.").Value;
            // Disabling this for now as it needs more attention.
            //RandomEncountersSystemActive = Config.Bind("System", "Enable Random Encounters system", false,  "Enable/disable the random encounters system.").Value;
            WeaponMasterySystemActive = Config.Bind("System", "Enable Weapon Mastery system", false,  "Enable/disable the weapon mastery system.").Value;
            WantedSystemActive = Config.Bind("System", "Enable Wanted system", false,  "Enable/disable the wanted system.").Value;
            
            // I only want to keep waypoints around as it makes it easier to test.
            //WaypointsActive = Config.Bind("Core", "Enable Wanted system", false,  "Enable/disable waypoints.").Value;

            if (WaypointsActive)
            {
                WaypointCommands.WaypointLimit = Config.Bind("Config", "Waypoint Limit", 2, "Set a waypoint limit for per non-admin user.").Value;
            }

            Config.SaveOnConfigSet = true;
            var autoSaveFrequency = Config.Bind("Auto-save", "Frequency", 10, "Request the frequency for auto-saving the database. Value is in minutes. Minimum is 2.");
            var backupSaveFrequency = Config.Bind("Auto-save", "Backup", 0, "Enable and request the frequency for saving to the backup folder. Value is in minutes. 0 to disable.");
            if (autoSaveFrequency.Value < 2) autoSaveFrequency.Value = 10;
            if (backupSaveFrequency.Value < 0) backupSaveFrequency.Value = 0;
            
            // Save frequency is set to a TimeSpan of 30s less than specified, so that the auto-save won't miss being triggered by seconds.
            AutoSaveSystem.AutoSaveFrequency = TimeSpan.FromMinutes(autoSaveFrequency.Value * 60 - 30);
            AutoSaveSystem.BackupFrequency = backupSaveFrequency.Value < 1 ? TimeSpan.Zero : TimeSpan.FromMinutes(backupSaveFrequency.Value * 60 - 30);
        }

        public override void Load()
        {
            // Ensure the logger is accessible in static contexts.
            _logger = base.Log;
            if(!IsServer)
            {
                Plugin.Log(LogSystem.Core, LogLevel.Warning, $"This is a server plugin. Not continuing to load on client.", true);
                return;
            }
            
            InitCoreConfig();
            
            Instance = this;
            GameFrame.Initialize();
            
            // Load command registry for systems that are active
            // Note: Displaying these in alphabetical order for ease of maintenance
            Command.AddCommandType(typeof(AllianceCommands), PlayerGroupsActive);
            Command.AddCommandType(typeof(BloodlineCommands), BloodlineSystemActive);
            Command.AddCommandType(typeof(CacheCommands));
            Command.AddCommandType(typeof(ExperienceCommands), ExperienceSystemActive);
            Command.AddCommandType(typeof(MasteryCommands), WeaponMasterySystemActive);
            Command.AddCommandType(typeof(PermissionCommands));
            Command.AddCommandType(typeof(PlayerInfoCommands));
            Command.AddCommandType(typeof(WantedCommands), WantedSystemActive);
            
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Plugin.Log(LogSystem.Core, LogLevel.Info, $"Plugin is loaded [version: {MyPluginInfo.PLUGIN_VERSION}]", true);
        }

        public override bool Unload()
        {
            Config.Clear();
            harmony.UnpatchSelf();
            return true;
        }

        public static void Initialize()
        {
            Plugin.Log(LogSystem.Core, LogLevel.Warning, $"Trying to Initialize {MyPluginInfo.PLUGIN_NAME}: isInitialized == {IsInitialized}", IsInitialized);
            if (IsInitialized) return;
            Plugin.Log(LogSystem.Core, LogLevel.Info, $"Initializing {MyPluginInfo.PLUGIN_NAME}...", true);
            
            //-- Initialize System
            // Pre-initialise some constants
            Helper.GetServerGameManager(out _);
            
            // Ensure that there is a consistent starting level to the server settings
            if (ExperienceSystemActive)
            {
                var serverSettings = Plugin.Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
                var startingXpLevel = serverSettings.Settings.StartingProgressionLevel;
                ExperienceSystem.StartingExp = ExperienceSystem.ConvertLevelToXp(startingXpLevel) + 1; // Add 1 to make it show start of this level, rather than end of last.
                Plugin.Log(LogSystem.Xp, LogLevel.Info, $"Starting XP level set to {startingXpLevel} to match server settings", true);
            }
            
            DebugLoggingConfig.Initialize();
            if (BloodlineSystemActive) BloodlineConfig.Initialize();
            if (ExperienceSystemActive) ExperienceConfig.Initialize();
            if (WeaponMasterySystemActive) MasteryConfig.Initialize();
            if (WantedSystemActive) WantedConfig.Initialize();

            //-- Apply configs
            
            Plugin.Log(LogSystem.Core, LogLevel.Info, "Initialising player cache and internal database...");
            Helper.CreatePlayerCache();
            AutoSaveSystem.LoadOrInitialiseDatabase();
            
            // Validate any potential change in permissions
            var commands = Command.GetAllCommands();
            Command.ValidatedCommandPermissions(commands);
            // Note for devs: To regenerate Command.md and PermissionSystem.DefaultCommandPermissions, uncomment the following:
            // Command.GenerateCommandMd(commands);
            // Command.GenerateDefaultCommandPermissions(commands);
            var assemblyConfigurationAttribute = typeof(Plugin).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
            var buildConfigurationName = assemblyConfigurationAttribute?.Configuration;
            if (buildConfigurationName == "Debug")
            {
                Plugin.Log(LogSystem.Core, LogLevel.Info, $"****** WARNING ******* Build configuration: {buildConfigurationName}", true);
                Plugin.Log(LogSystem.Core, LogLevel.Info, $"THIS IS ADDING SOME DEBUG COMMANDS. JUST SO THAT YOU ARE AWARE.", true);
                
                PowerUpCommandsActive = true;
                RandomEncountersSystemActive = true;
                WaypointsActive = true;
                Command.AddCommandType(typeof(PowerUpCommands), PowerUpCommandsActive);
                Command.AddCommandType(typeof(RandomEncountersCommands), RandomEncountersSystemActive);
                Command.AddCommandType(typeof(WaypointCommands), WaypointsActive);
                // Reload DB to ensure these commands work as intended.
                AutoSaveSystem.LoadOrInitialiseDatabase();
            }
            
            Plugin.Log(LogSystem.Core, LogLevel.Info, $"Setting CommandRegistry middleware");
            if (!_adminCommandsRequireAdmin)
            {
                Plugin.Log(LogSystem.Core, LogLevel.Info, "Removing admin privilege requirements");
                CommandRegistry.Middlewares.Clear();                
            }
            CommandRegistry.Middlewares.Add(new Command.PermissionMiddleware());

            if (RandomEncountersSystemActive)
            {
                RandomEncounters.GameData_OnInitialize();
                RandomEncounters.EncounterTimer = new Timer();
                RandomEncounters.StartEncounterTimer();
            }
            
            Plugin.Log(LogSystem.Core, LogLevel.Info, "Finished initialising", true);

            IsInitialized = true;
        }

        public enum LogSystem
        {
            Alliance,
            Bloodline,
            Buff,
            Core,
            Death,
            Debug,
            Faction,
            Mastery,
            PowerUp,
            RandomEncounter,
            SquadSpawn,
            Wanted,
            Xp
        }
        
        public new static void Log(LogSystem system, LogLevel logLevel, string message, bool forceLog = false)
        {
            var isLogging = forceLog || DebugLoggingConfig.IsLogging(system);
            if (isLogging) _logger.Log(logLevel, ToLogMessage(system, message));
        }
        
        // Log overload to allow potentially more computationally expensive logs to be hidden when not being logged
        public new static void Log(LogSystem system, LogLevel logLevel, Func<string> messageGenerator, bool forceLog = false)
        {
            var isLogging = forceLog || DebugLoggingConfig.IsLogging(system);
            if (isLogging) _logger.Log(logLevel, ToLogMessage(system, messageGenerator()));
        }
        
        // Log overload to allow enumerations to only be iterated over if logging
        public new static void Log(LogSystem system, LogLevel logLevel, IEnumerable<string> messages, bool forceLog = false)
        {
            var isLogging = forceLog || DebugLoggingConfig.IsLogging(system);
            if (!isLogging) return;
            foreach (var message in messages)
            {
                _logger.Log(logLevel, ToLogMessage(system, message));
            }
        }

        private static string ToLogMessage(LogSystem logSystem, string message)
        {
            return $"{DateTime.Now:u}: [{Enum.GetName(logSystem)}] {message}";
        }
    }
}
