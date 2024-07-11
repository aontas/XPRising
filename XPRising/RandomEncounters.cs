using BepInEx.Logging;
using XPRising.Components.RandomEncounters;
using XPRising.Configuration;
using XPRising.Systems;
using XPRising.Utils.RandomEncounters;

namespace XPRising
{
    internal static class RandomEncounters
    {
        private const Plugin.LogSystem LogSystem = Plugin.LogSystem.RandomEncounter;

        public static WorldTimer EncounterTimer;

        public static void Load()
        {
        }

        internal static void GameData_OnInitialize()
        {
            Plugin.Log(LogSystem, LogLevel.Info, "Loading main data RandomEncounters");
            DataFactory.Initialize();
            Plugin.Log(LogSystem, LogLevel.Info, "Binding configuration RandomEncounters");
            RandomEncountersConfig.Initialize();
        }

        public static void StartEncounterTimer()
        {
            EncounterTimer.Start(
                _ =>
                {
                    Plugin.Log(LogSystem, LogLevel.Info, $"Starting an encounter.");
                    RandomEncountersSystem.StartEncounter();
                },
                input =>
                {
                    if (input is not int onlineUsersCount)
                    {
                        Plugin.Log(LogSystem, LogLevel.Error, "Encounter timer delay function parameter is not a valid integer");
                        return TimeSpan.MaxValue;
                    }
                    if (onlineUsersCount < 1)
                    {
                        onlineUsersCount = 1;
                    }
                    var seconds = new Random().Next(RandomEncountersConfig.EncounterTimerMin.Value, RandomEncountersConfig.EncounterTimerMax.Value);
                    Plugin.Log(LogSystem, LogLevel.Info, $"Next encounter will start in {seconds / onlineUsersCount} seconds.");
                    return TimeSpan.FromSeconds(seconds) / onlineUsersCount;
                });
        }

        public static void Unload()
        {
            EncounterTimer?.Stop();
            GameFrame.Uninitialize();
            Plugin.Log(LogSystem, LogLevel.Info, $"RandomEncounters unloaded!");
        }
    }
}
