using System;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands;

[CommandGroup("experience","xp")]
public static class ExperienceCommands {
    private static EntityManager _entityManager = Plugin.Server.EntityManager;

    [Command("get", "g", "", "Display your current xp", adminOnly: false)]
    public static void GetXp(ChatCommandContext ctx) {
        if (!Plugin.ExperienceSystemActive)
        {
            ctx.Reply("Experience system is not enabled.");
            return;
        }
        var user = ctx.Event.User;
        var characterName = user.CharacterName.ToString();
        var steamID = user.PlatformId;
        int userXp = ExperienceSystem.GetXp(steamID);
        ExperienceSystem.GetLevelAndProgress(userXp, out int progress, out int earnedXp, out int neededXp);
        int userLevel = ExperienceSystem.ConvertXpToLevel(userXp);
        string response = $"-- <color={Output.White}>{characterName}</color> --\n";
        response += $"Level: <color={Output.White}>{userLevel:D}</color> (<color={Output.White}>{progress:D}%</color>) ";
        response += $" [ XP: <color={Output.White}>{earnedXp:D}</color> / <color={Output.White}>{neededXp:D}</color> ]";
        if (ExperienceSystem.LevelRewardsOn) response += $" You have {(Database.PlayerAbilityIncrease.ContainsKey(steamID) ? Database.PlayerAbilityIncrease[steamID] : 0)} ability points to spend.";
        ctx.Reply(response);
    }

    [Command("set", "s", "<playerName> <level>", "Sets the specified player's level to the start of the given level", adminOnly: false)]
    public static void SetLevel(ChatCommandContext ctx, string name, int level){
        if (!Plugin.ExperienceSystemActive){
            ctx.Reply("Experience system is not enabled.");
            return;
        }
        ulong steamID;

        if (PlayerCache.FindPlayer(name, true, out var targetEntity, out var targetUserEntity)){
            steamID = _entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
        }
        else
        {
            ctx.Reply($"Could not find specified player \"{name}\".");
            return;
        }
        
        ExperienceSystem.SetXp(steamID, ExperienceSystem.ConvertLevelToXp(level));
        ExperienceSystem.CheckAndApplyLevel(targetEntity, targetUserEntity, steamID);
        ctx.Reply($"Player \"{name}\" Experience is now set to be<color={Output.White}> {ExperienceSystem.GetXp(steamID)}</color>");
    }

    [Command("log", "l", "", "Toggles logging of xp gain.", adminOnly: false)]
    public static void LogExperience(ChatCommandContext ctx){
        if (!Plugin.ExperienceSystemActive){
            ctx.Reply("Experience system is not enabled.");
            return;
        }
        var steamID = ctx.User.PlatformId;
        var loggingData = Database.PlayerLogConfig[steamID];
        loggingData.LoggingExp = !loggingData.LoggingExp;
        ctx.Reply(loggingData.LoggingExp
            ? "Experience gain is now being logged."
            : $"Experience gain is no longer being logged.");
        Database.PlayerLogConfig[steamID] = loggingData;
    }

    [Command("ability", "a", "<AbilityName> <amount>", "Spend given points on given ability", adminOnly: false)]
    public static void AddClassAbility(ChatCommandContext ctx, string name, int amount){
        if (!Plugin.ExperienceSystemActive){
            ctx.Reply("Experience system is not enabled.");
            return;
        }
        if (!ExperienceSystem.LevelRewardsOn){
            ctx.Reply("Experience Class system is not enabled.");
            return;
        }
        
        ulong steamID = ctx.Event.User.PlatformId;
        var userEntity = ctx.Event.SenderUserEntity;
        var playerCharacter = ctx.Event.SenderCharacterEntity;
        
        try{
            int spendPoints = amount;
            string abilityName = name;

            if (!Database.PlayerAbilityIncrease.ContainsKey(steamID)) Database.PlayerAbilityIncrease[steamID] = 0;

            if (Database.PlayerAbilityIncrease[steamID] < spendPoints){
                ctx.Reply("Not enough points!");
                return;
            }

            if (Database.ExperienceClassStats.ContainsKey(abilityName.ToLower())){
                foreach (var buff in Database.ExperienceClassStats[abilityName.ToLower()]){
                    Database.PlayerLevelStats[steamID][buff.Key] += buff.Value * spendPoints;
                }

                Database.PlayerAbilityIncrease[steamID] -= spendPoints;
                Helper.ApplyBuff(userEntity, playerCharacter, Helper.AppliedBuff);
                ctx.Reply($"Spent {spendPoints}. You have {Database.PlayerAbilityIncrease[steamID]} points left to spend.");
                foreach (var buff in Database.PlayerLevelStats[steamID]){
                    ctx.Reply($"{buff.Key} : {buff.Value}");
                }
            }
            else {
                ctx.Reply("Type \".xp ability show\" to see current buffs.");
                ctx.Reply($"Type .xp ability <ability> to spend ability points. You have {Database.PlayerAbilityIncrease[steamID]} points left to spend.");
                ctx.Reply("You can spend ability points on:");
                ctx.Reply(string.Join(", ", Database.ExperienceClassStats.Keys.ToList()));
            }

        }
        catch (Exception ex){
            Plugin.Log(Plugin.LogSystem.Xp, LogLevel.Error, $"Could not spend point! {ex}");
            ctx.Reply($"Could not spend point! {ex.Message}");
        }
    }
    
    [Command("ability show", "as", "", "Display the buffs provided by the XP class system", adminOnly: false)]
    public static void ShowClassAbility(ChatCommandContext ctx){
        if (!Plugin.ExperienceSystemActive){
            ctx.Reply("Experience system is not enabled.");
            return;
        }
        if (!ExperienceSystem.LevelRewardsOn){
            ctx.Reply("Experience Class system is not enabled.");
            return;
        }
        
        var steamID = ctx.User.PlatformId;
        
        foreach (var buff in Database.PlayerLevelStats[steamID]){
            ctx.Reply($"{buff.Key} : {buff.Value}");
        }
    }
    
    [Command("ability reset", "ar", "", "Reset your spent ability points", adminOnly: false)]
    public static void ResetClassAbility(ChatCommandContext ctx){
        if (!Plugin.ExperienceSystemActive){
            ctx.Reply("Experience system is not enabled.");
            return;
        }
        if (!ExperienceSystem.LevelRewardsOn){
            ctx.Reply("Experience Class system is not enabled.");
            return;
        }
        
        var steamID = ctx.User.PlatformId;
        var userEntity = ctx.Event.SenderUserEntity;
        var playerCharacter = ctx.Event.SenderCharacterEntity;
        
        Database.PlayerLevelStats[steamID] = new LazyDictionary<ProjectM.UnitStatType, float>();
        Database.PlayerAbilityIncrease[steamID] = 0;
        Cache.player_level[steamID] = 0;
        ExperienceSystem.CheckAndApplyLevel(playerCharacter, userEntity, steamID);
        ctx.Reply("Ability level up points reset.");
    }
    
    [Command(name: "bump20", shortHand: "", adminOnly: false, usage: "", description: "Temporarily bumps you to lvl20 so you can skip the quest")]
    public static void BumpToLevel20(ChatCommandContext ctx)
    {
        var playerEntity = ctx.Event.SenderCharacterEntity;
        var userEntity = ctx.Event.SenderUserEntity;

        try
        {
            SetUserLevel(ctx.Event.SenderCharacterEntity, ctx.Event.SenderUserEntity, ctx.User.PlatformId, 20, 5);
            ctx.Reply($"You has been bumped to lvl 20 for 5 seconds. Equip an item and then claim the reward.");
        }
        catch (Exception e)
        {
            Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"Failed to bump20 {e.Message}", true);
            throw ctx.Error($"Failed to bump20. Check logs for more details.");
        }
    }
    
    [Command(name: "bump20", shortHand: "", adminOnly: false, usage: "<PlayerName>", description: "Temporarily bumps the player to lvl20 so they can skip the quest")]
    public static void BumpToLevel20(ChatCommandContext ctx, string playerName)
    {
        if (!PlayerCache.FindPlayer(playerName, true, out var playerEntity, out var userEntity))
        {
            throw ctx.Error("Player not found.");
        }

        var steamId = PlayerCache.GetSteamIDFromName(playerName);

        try
        {
            SetUserLevel(playerEntity, userEntity, steamId, 20, 5);
            ctx.Reply($"Player has been bumped to lvl 20 for 5 seconds.");
            Output.SendMessage(userEntity, "You have been bumped to lvl 20 for 5 seconds. Equip an item and then claim the reward.");
        }
        catch (Exception e)
        {
            Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"Failed to bump20 {e.Message}", true);
            throw ctx.Error($"Failed to bump20. Check logs for more details.");
        }
    }
    
    public static void SetUserLevel(Entity player, Entity user, ulong steamID, int level, int delaySeconds)
    {
        Equipment equipment = Plugin.Server.EntityManager.GetComponentData<Equipment>(player);
        equipment.ArmorLevel._Value = 0;
        equipment.WeaponLevel._Value = 0;
        equipment.SpellLevel._Value = level;
                
        Plugin.Server.EntityManager.SetComponentData(player, equipment);
        
        Task.Delay(delaySeconds * 1000).ContinueWith(t =>
            ExperienceSystem.ApplyLevel(player, ExperienceSystem.GetLevel(steamID)));
    }
}