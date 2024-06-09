using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using XPRising.Configuration;
using XPRising.Models;
using XPRising.Models.RandomEncounters;

namespace XPRising.Utils.RandomEncounters
{
    internal static class DataFactory
    {
        private static readonly Random Random = new();
        private static List<ItemDataModel> _items;

        internal static void Initialize()
        {
            // var tsv = Encoding.UTF8.GetString(Resources.npcs);
            // tsv = Encoding.UTF8.GetString(Resources.items);
            // _items = tsv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Select(l => new ItemDataModel(l)).ToList();
            _items = new();
        }

        internal static FactionUnits.Unit GetRandomNpc(float playerLevel)
        {
            var lowestLevel = playerLevel - RandomEncountersConfig.EncounterMaxLevelDifferenceLower.Value;
            var highestLevel = playerLevel + RandomEncountersConfig.EncounterMaxLevelDifferenceUpper.Value;
            Plugin.Log(Plugin.LogSystem.RandomEncounter, LogLevel.Info, $"Searching an NPC between levels {lowestLevel} and {highestLevel}");
            var faction = FactionUnits.SupportedFactions.ToList().GetRandomElement();
            return FactionUnits.GetFactionUnits(faction, (int)highestLevel, 1).ToList().GetRandomElement();
        }

        internal static ItemDataModel GetRandomItem()
        {
            return _items
                .Where(n => RandomEncountersConfig.Items.TryGetValue(n.Id, out var itemSetting) && itemSetting.Value > 0).ToList()
                .GetRandomElement();
        }

        internal static int GetOnlineUsersCount()
        {
            return Cache.NamePlayerCache.Values.Count(data => data.IsOnline);
        }

        internal static List<ItemDataModel> GetAllItems()
        {
            return _items;
        }

        internal static List<PlayerData> GetOnlineAdmins()
        {
            return Cache.NamePlayerCache.Values.Where(data => data.IsOnline && data.IsAdmin).ToList();
        }

        private static T GetRandomElement<T>(this List<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return default;
            }

            return items[Random.Next(items.Count)];
        }
    }
}