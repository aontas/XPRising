using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ProjectM.Network;
using XPRising.Systems;
using XPRising.Transport;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using XPShared;

namespace XPRising.Models;

public struct PlayerHeatData {
    public struct Heat {
        public int level { get; set; }
        public DateTime lastAmbushed { get; set; }
    }
        
    public Dictionary<Faction, Heat> heat { get; } = new();
    private FrameTimer _cooldownTimer = new FrameTimer();
    private ulong _steamID = 0;
    private User user;

    public PlayerHeatData() {
        foreach (Faction faction in FactionHeat.ActiveFactions) {
            heat[faction] = new();
        }
    }
    
    private static double CooldownPerSecond => WantedSystem.heat_cooldown / 60f;
    private static int CooldownTickLengthMs => CooldownPerSecond == 0 ? 0 : (int)Math.Max(1000, 1000 / CooldownPerSecond);

    private void RunCooldown()
    {
        var lastCombatStart = Cache.GetCombatStart(_steamID);
        var lastCombatEnd = Cache.GetCombatEnd(_steamID);
        
        Plugin.Log(Plugin.LogSystem.Wanted, LogLevel.Info, $"Heat CD: Combat (S:{lastCombatStart:u}|E:{lastCombatEnd:u})");

        if (WantedSystem.CanCooldownHeat(lastCombatStart, lastCombatEnd)) {
            var cooldownValue = (int)Math.Round(CooldownTickLengthMs * 0.001f * CooldownPerSecond);
            Plugin.Log(Plugin.LogSystem.Wanted, LogLevel.Info, $"Heat cooldown: {cooldownValue} ({CooldownPerSecond:F1}c/s)");

            // Update all heat levels
            foreach (var faction in heat.Keys) {
                var factionHeat = heat[faction];
                var newHeatLevel = Math.Max(factionHeat.level - cooldownValue, 0);
                factionHeat.level = newHeatLevel;
                heat[faction] = factionHeat;
                
                ClientActionHandler.SendWantedData(user, faction, factionHeat.level);
            }
        }
    }

    public void StartCooldownTimer(ulong steamID)
    {
        if (_cooldownTimer != null) return;
        
        _steamID = steamID;
        if (!PlayerCache.FindPlayer(steamID, true, out _, out var userEntity) &&
            !Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out user))
        {
            return;
        }
        
        _cooldownTimer = new FrameTimer().Initialise(RunCooldown, TimeSpan.FromMilliseconds(CooldownTickLengthMs), false);
        _cooldownTimer.Start();
    }
}