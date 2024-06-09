using System;
using System.Collections.Generic;
using XPRising.Utils;
using XPRising.Utils.Prefabs;

namespace XPRising.Models;

public struct PlayerHeatData {
    public struct Heat {
        public int level { get; set; }
        public DateTime lastAmbushed { get; set; }
    }
        
    public Dictionary<Faction, Heat> heat { get; } = new();
    public DateTime lastCooldown { get; set; }

    public PlayerHeatData() {
        foreach (Faction faction in FactionHeat.ActiveFactions) {
            heat[faction] = new();
        }
    }
}