using BepInEx.Logging;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using XPRising.Systems;
using XPRising.Transport;
using XPRising.Utils.Prefabs;
using Faction = XPRising.Utils.Prefabs.Faction;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Utils;

public static class FactionHeat {
    public static readonly Faction[] ActiveFactions = {
        Faction.Bandits,
        Faction.Blackfangs,
        Faction.Critters,
        Faction.Gloomrot,
        Faction.Legion,
        Faction.Militia,
        Faction.Undead,
        Faction.Werewolf
    };
    
    public static readonly string[] ColourGradient = { "fef001", "ffce03", "fd9a01", "fd6104", "ff2c05", "f00505" };

    public static readonly int[] HeatLevels = { 100, 250, 500, 1000, 1500, 3000 };
    public static readonly int LastHeatIndex = HeatLevels.Length - 1;
    public static readonly int LastHeatThreshold = HeatLevels[LastHeatIndex];
    public static readonly string MaxHeatColour = ColourGradient[LastHeatIndex];
    
    // Units that generate extra heat.
    private static readonly HashSet<Units> ExtraHeatUnits = new HashSet<Units>(
        FactionUnits.farmNonHostile.Select(u => u.type)
            .Union(FactionUnits.farmFood.Select(u => u.type))
            .Union(FactionUnits.otherNonHostile.Select(u => u.type)));

    public static void GetActiveFaction(PrefabGUID guid, out Faction activeFaction)
    {
        var faction = Helper.ConvertGuidToFaction(guid);
        GetActiveFaction(faction, out activeFaction);
    }
    
    public static void GetActiveFaction(Faction faction, out Faction activeFaction) {
        switch (faction) {
            // Bandit
            case Faction.Traders_T01:
            case Faction.Bandits:
                activeFaction = Faction.Bandits;
                break;
            // Black fangs
            case Faction.Blackfangs:
            case Faction.Blackfangs_Livith:
                activeFaction = Faction.Blackfangs;
                break;
            // Human
            case Faction.Militia:
            case Faction.ChurchOfLum_SpotShapeshiftVampire:
            case Faction.ChurchOfLum:
            case Faction.Traders_T02:
            case Faction.World_Prisoners:
                activeFaction = Faction.Militia;
                break;
            // Human: gloomrot
            case Faction.Gloomrot:
                activeFaction = Faction.Gloomrot;
                break;
            // Legion
            case Faction.Legion:
                activeFaction = Faction.Legion;
                break;
            // Nature
            case Faction.Bear:
            case Faction.Critters:
            case Faction.Wolves:
                activeFaction = Faction.Critters;
                break;
            // Undead
            case Faction.Undead:
                activeFaction = Faction.Undead;
                break;
            // Werewolves
            case Faction.Werewolf:
            case Faction.WerewolfHuman:
                activeFaction = Faction.Werewolf;
                break;
            case Faction.VampireHunters:
                activeFaction = Faction.VampireHunters;
                break;
            // Do nothing
            case Faction.ChurchOfLum_Slaves:
            case Faction.ChurchOfLum_Slaves_Rioters:
            case Faction.Corrupted:
            case Faction.CorruptedBloodBuffSpawns:
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
                activeFaction = Faction.Unknown;
                break;
            default:
                Plugin.Log(LogSystem.Wanted, LogLevel.Warning, $"Faction not handled for GetActiveFaction: {Enum.GetName(faction)}");
                activeFaction = Faction.Unknown;
                break;
        }
    }
    
    public static void GetActiveFactionHeatValue(Faction faction, Units victim, bool isVBlood, out int heatValue, out Faction activeFaction)
    {
        GetActiveFaction(faction, out activeFaction);
        if (activeFaction == Faction.Unknown)
        {
            heatValue = 0;
            return;
        }

        // Default to 10 heat
        heatValue = 10;
        
        // Add in special cases for specific origin factions
        switch (faction) {
            // Bandit
            case Faction.Traders_T01:
                heatValue = 300; // Don't kill the merchants
                break;
            // Human
            case Faction.ChurchOfLum_SpotShapeshiftVampire:
                heatValue = 25; // These are looking out for vampires - slightly strong so get more heat
                break;
            case Faction.Traders_T02:
                heatValue = 300; // Don't kill the merchants
                break;
            case Faction.ChurchOfLum:
                heatValue = 15; // These are slightly stronger than other militia
                break;
            // Legion
            case Faction.Legion:
                heatValue = 10;
                break;
            // Undead
            case Faction.Undead:
                heatValue = 5; // There are generally lots of undead (also skeletons have less agency than "alive" mobs)
                break;
            // Werewolves
            case Faction.Werewolf:
            case Faction.WerewolfHuman:
                heatValue = 20; // Fairly individual + more close-knit faction
                break;
            case Faction.VampireHunters:
                heatValue = 3;
                break;
        }
        
        if (isVBlood) heatValue *= WantedSystem.VBloodMultiplier;
        else if (ExtraHeatUnits.Contains(victim)) heatValue = (int)(heatValue * 1.5);
    }

    public static string GetFactionStatus(Faction faction, int heat, ulong steamId) {
        var preferences = Database.PlayerPreferences[steamId];
        var factionName = ClientActionHandler.FactionTooltip(faction, preferences.Language);
        var starOutput = HeatLevels.Aggregate("", (current, t) => current + (heat < t ? "☆" : "★"));
        var additionalHeat = heat > HeatLevels[LastHeatIndex] ? $" (+{heat - LastHeatThreshold})" : "";

        return $"{factionName}: {starOutput}{additionalHeat}";
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
            if (Cache.PlayerHasUINotifications(ally.userComponent.PlatformId))
            {
                XPShared.Transport.Utils.ServerSendNotification(ally.userComponent, "Ambush!", squadMessage, LogLevel.Info, $"#{ColourGradient[wantedLevel - 1]}");
            }
            else
            {
                Output.SendMessage(ally.userEntity, message);
            }
        }
    }
}