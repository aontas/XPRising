using BepInEx.Logging;
using XPRising.Systems;
using XPRising.Transport;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using XPShared;

namespace XPRising.Models;

public class PlayerHeatData {
    public struct Heat {
        public int level { get; set; }
        public DateTime lastAmbushed { get; set; }
    }
    
    // init is used here for loading via JSON
    public LazyDictionary<Faction, Heat> heat { get; init; } = new();
    private readonly FrameTimer _cooldownTimer = new();
    private ulong _steamID = 0;

    public PlayerHeatData()
    {
        _cooldownTimer.Initialise(RunCooldown, TimeSpan.FromMilliseconds(CooldownTickLengthMs), -1);
    }

    public void Clear()
    {
        _cooldownTimer.Stop();
        heat.Clear();
    }

    private static double CooldownPerSecond => WantedSystem.heat_cooldown < 1 ? 1 / 6f : WantedSystem.heat_cooldown / 60f;
    private static int CooldownTickLengthMs => (int)Math.Max(1000, 1000 / CooldownPerSecond);

    private void RunCooldown()
    {
        var lastCombatStart = Cache.GetCombatStart(_steamID);
        var lastCombatEnd = Cache.GetCombatEnd(_steamID);
        
        Plugin.Log(Plugin.LogSystem.Wanted, LogLevel.Info, $"Heat CD: Combat (S:{lastCombatStart:u}|E:{lastCombatEnd:u})");
        
        var userLanguage = Database.PlayerPreferences[_steamID].Language;

        if (WantedSystem.CanCooldownHeat(lastCombatStart, lastCombatEnd)) {
            var cooldownValue = (int)Math.Round(CooldownTickLengthMs * 0.001f * CooldownPerSecond);
            Plugin.Log(Plugin.LogSystem.Wanted, LogLevel.Info, $"Heat cooldown: {cooldownValue} ({CooldownPerSecond:F1}c/s)");

            // Update all heat levels
            foreach (var faction in heat.Keys) {
                var factionHeat = heat[faction];
                
                if (factionHeat.level > 0)
                {
                    var newHeatLevel = Math.Max(factionHeat.level - cooldownValue, 0);
                    factionHeat.level = newHeatLevel;
                    heat[faction] = factionHeat;
                
                    if (PlayerCache.FindPlayer(_steamID, true, out _, out _, out var user))
                    {
                        ClientActionHandler.SendWantedData(user, faction, factionHeat.level, userLanguage);
                    }
                }
                else
                {
                    heat.Remove(faction);
                }
            }

            if (heat.Count == 0) _cooldownTimer.Stop();
        }
    }

    public void StartCooldownTimer(ulong steamID)
    {
        if (_steamID == 0)
        {
            _steamID = steamID;
        }
        
        if (!_cooldownTimer.Enabled) _cooldownTimer.Start();
    }
}