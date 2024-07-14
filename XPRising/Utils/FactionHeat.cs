using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using XPRising.Systems;
using XPRising.Utils.Prefabs;
using Faction = XPRising.Utils.Prefabs.Faction;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Utils;

using Faction = Prefabs.Faction;

public static class FactionHeat {
    public static readonly Faction[] ActiveFactions = {
        Faction.Bandits,
        Faction.Critters,
        Faction.Gloomrot,
        Faction.Legion,
        Faction.Militia,
        Faction.Undead,
        Faction.Werewolf
    };
    
    public static readonly string[] ColourGradient = { "fef001", "ffce03", "fd9a01", "fd6104", "ff2c05", "f00505" };

    public static readonly int[] HeatLevels = { 150, 250, 500, 1000, 1500, 3000 };
    
    // Units that generate extra heat.
    private static readonly HashSet<Units> ExtraHeatUnits = new HashSet<Units>(
        FactionUnits.farmNonHostile.Select(u => u.type)
            .Union(FactionUnits.farmFood.Select(u => u.type))
            .Union(FactionUnits.otherNonHostile.Select(u => u.type)));
    
    public static void GetActiveFactionHeatValue(Faction faction, Units victim, bool isVBlood, out int heatValue, out Faction activeFaction) {
        switch (faction) {
            // Bandit
            case Faction.Traders_T01:
                heatValue = 300; // Don't kill the merchants
                activeFaction = Faction.Bandits;
                break;
            case Faction.Bandits:
                heatValue = 10;
                activeFaction = Faction.Bandits;
                break;
            // Human
            case Faction.Militia:
                heatValue = 10;
                activeFaction = Faction.Militia;
                break;
            case Faction.ChurchOfLum_SpotShapeshiftVampire:
                heatValue = 25;
                activeFaction = Faction.Militia;
                break;
            case Faction.Traders_T02:
                heatValue = 300; // Don't kill the merchants
                activeFaction = Faction.Militia;
                break;
            case Faction.ChurchOfLum:
                heatValue = 15;
                activeFaction = Faction.Militia;
                break;
            case Faction.World_Prisoners:
                heatValue = 10;
                activeFaction = Faction.Militia;
                break;
            // Human: gloomrot
            case Faction.Gloomrot:
                heatValue = 10;
                activeFaction = Faction.Gloomrot;
                break;
            // Legion
            case Faction.Legion:
                heatValue = 15;
                activeFaction = Faction.Legion;
                break;
            // Nature
            case Faction.Bear:
            case Faction.Critters:
            case Faction.Wolves:
                heatValue = 10;
                activeFaction = Faction.Critters;
                break;
            // Undead
            case Faction.Undead:
                heatValue = 5;
                activeFaction = Faction.Undead;
                break;
            // Werewolves
            case Faction.Werewolf:
            case Faction.WerewolfHuman:
                heatValue = 20;
                activeFaction = Faction.Werewolf;
                break;
            case Faction.VampireHunters:
                heatValue = 3;
                activeFaction = Faction.VampireHunters;
                break;
            // Do nothing
            case Faction.ChurchOfLum_Slaves:
            case Faction.ChurchOfLum_Slaves_Rioters:
            case Faction.Cursed:
            case Faction.Elementals:
            case Faction.Ignored:
            case Faction.Harpy:
            case Faction.Mutants:
            case Faction.NatureSpirit:
            case Faction.Plants:
            case Faction.Players:
            case Faction.Players_Castle_Prisoners:
            case Faction.Players_Mutant:
            case Faction.Players_Shapeshift_Human:
            case Faction.Spiders:
            case Faction.Spiders_Shapeshifted:
            case Faction.Unknown:
            case Faction.Wendigo:
                heatValue = 0;
                activeFaction = Faction.Unknown;
                break;
            default:
                Plugin.Log(Plugin.LogSystem.Wanted, LogLevel.Warning, $"Faction not handled for GetActiveFactionHeatValue: {Enum.GetName(faction)}");
                heatValue = 0;
                activeFaction = Faction.Unknown;
                break;
        }
        
        if (isVBlood) heatValue *= WantedSystem.vBloodMultiplier;
        else if (ExtraHeatUnits.Contains(victim)) heatValue = (int)(heatValue * 1.5);
    }

    public static string GetFactionStatus(Faction faction, int heat) {
        var output = $"{Enum.GetName(faction)}: ";
        return HeatLevels.Aggregate(output, (current, t) => current + (heat < t ? "☆" : "★"));
    }

    public static int GetWantedLevel(int heat) {
        for (var i = 0; i < HeatLevels.Length; i++) {
            if (HeatLevels[i] > heat) return i;
        }

        return HeatLevels.Length;
    }

    public static void Ambush(Entity userEntity, float3 position, Faction faction, int wantedLevel) {
        if (wantedLevel < 1) return;
        
        var steamID = Plugin.Server.EntityManager.GetComponentData<User>(userEntity).PlatformId;
        var playerLevel = ExperienceSystem.GetLevel(steamID);
        
        var squadMessage = SquadList.SpawnSquad(playerLevel, position, faction, wantedLevel);
        var message = L10N.Get(L10N.TemplateKey.WantedFactionHeatStatus)
            .AddField("{colour}", ColourGradient[wantedLevel - 1])
            .AddField("{squadMessage}", squadMessage);
        Output.SendMessage(userEntity, message);
    }

    public static void Ambush(float3 position, List<Alliance.ClosePlayer> closeAllies, Faction faction, int wantedLevel) {
        if (wantedLevel < 1 || closeAllies.Count == 0) return;

        // Grab the player based on the highest player level
        var chosenAlly = closeAllies.MaxBy(ally => ally.playerLevel);
        var squadMessage = SquadList.SpawnSquad(chosenAlly.playerLevel, position, faction, wantedLevel);
        
        foreach (var ally in closeAllies) {
            var message = L10N.Get(L10N.TemplateKey.WantedFactionHeatStatus)
                .AddField("{colour}", ColourGradient[wantedLevel - 1])
                .AddField("{squadMessage}", squadMessage);
            Output.SendMessage(ally.userEntity, message);
        }
    }
}