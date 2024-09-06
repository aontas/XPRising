using ProjectM;
using Unity.Collections;
using Unity.Entities;
using XPRising.Models;
using XPRising.Systems;

namespace XPRising.Utils;

using PlayerMasteryData = LazyDictionary<GlobalMasterySystem.MasteryType, MasteryData>;
public static class Cache
{
    //-- Cache (Wiped on plugin reload, server restart, and shutdown.)

    //-- -- Player Cache
    public static readonly LazyDictionary<FixedString64Bytes, PlayerData> NamePlayerCache = new();
    public static readonly LazyDictionary<ulong, PlayerData> SteamPlayerCache = new();
    public static readonly LazyDictionary<ulong, bool> PlayerClientUICache = new();
        
    //-- -- Combat
    public static LazyDictionary<ulong, DateTime> playerCombatStart = new();
    public static LazyDictionary<ulong, DateTime> playerCombatEnd = new();

    //-- -- Experience System
    public static LazyDictionary<ulong, float> player_level = new();
        
    //-- -- Alliance System
    public static LazyDictionary<Entity, Guid> AlliancePlayerToGroupId = new();
    public static LazyDictionary<Guid, Alliance.PlayerGroup> AlliancePlayerGroups = new();
    public static LazyDictionary<Entity, Alliance.PlayerGroup> AllianceAutoPlayerAllies = new();
    public static LazyDictionary<Entity, HashSet<AlliancePendingInvite>> AlliancePendingInvites = new();

    // Buff data
    public static LazyDictionary<Entity, LazyDictionary<UnitStatType, float>> PlayerToStatBonuses = new();
        
    //-- -- CustomNPC Spawner
    public static SizedDictionaryAsync<float, SpawnNpcListen> spawnNPC_Listen = new(500);

    public static DateTime GetCombatStart(ulong steamID) {
        if (!playerCombatStart.TryGetValue(steamID, out var start)) {
            start = DateTime.MinValue;
        }

        return start;
    }
    public static DateTime GetCombatEnd(ulong steamID) {
        if (!playerCombatEnd.TryGetValue(steamID, out var end)) {
            end = DateTime.MinValue;
        }

        return end;
    }

    public static bool PlayerInCombat(ulong steamID)
    {
        return GetCombatStart(steamID) > GetCombatEnd(steamID);
    }
}

public static class Database
{
    //-- Dynamic Database (Saved on a JSON file on plugin reload, server restart, and shutdown.)
    //-- Initialization for the data loading is on each command or related CS file.

    // Player preferences
    public static LazyDictionary<ulong, PlayerPreferences> PlayerPreferences = new();

    //-- -- Commands
    public static LazyDictionary<string, WaypointData> Waypoints = new();
    public static LazyDictionary<ulong, int> UserPermission = new();
    public static LazyDictionary<string, int> CommandPermission = new();
    public static LazyDictionary<ulong, PowerUpData> PowerUpList = new();

    //-- -- EXP System
    public static LazyDictionary<ulong, int> PlayerExperience = new();

    //-- -- Wanted System
    public static LazyDictionary<ulong, PlayerHeatData> PlayerHeat = new();

    /// <summary>
    /// Ability points awarded per level.
    /// </summary>
    public static LazyDictionary<ulong, int> PlayerAbilityIncrease = new();

    /// <summary>
    /// Buff stat bonuses from leveling
    /// </summary>
    public static LazyDictionary<ulong, LazyDictionary<UnitStatType, float>> PlayerLevelStats = new();

    /// <summary>
    /// A configuration database of class stats per ability point spent.
    /// </summary>
    public static LazyDictionary<string, LazyDictionary<UnitStatType, float>> ExperienceClassStats = new();

    public static LazyDictionary<ulong, DateTime> PlayerLogout = new();

    //-- -- Mastery System
    public static LazyDictionary<ulong, PlayerMasteryData> PlayerMastery = new();
}