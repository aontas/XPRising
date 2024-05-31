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
    
    private static void CheckXPSystemActive(ChatCommandContext ctx)
    {
        if (!Plugin.ExperienceSystemActive)
        {
            var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                .AddField("{system}", "XP");
            throw Output.ChatError(ctx, message);
        }
    }

    [Command("get", "g", "", "Display your current xp", adminOnly: false)]
    public static void GetXp(ChatCommandContext ctx)
    {
        CheckXPSystemActive(ctx);
        var user = ctx.Event.User;
        var characterName = user.CharacterName.ToString();
        var steamID = user.PlatformId;
        int userXp = ExperienceSystem.GetXp(steamID);
        ExperienceSystem.GetLevelAndProgress(userXp, out int progress, out int earnedXp, out int neededXp);
        int userLevel = ExperienceSystem.ConvertXpToLevel(userXp);
        var message = L10N.Get(L10N.TemplateKey.XpLevel)
            .AddField("{playerName}", characterName)
            .AddField("{level}", userLevel.ToString())
            .AddField("{progress}", progress.ToString())
            .AddField("{earned}", earnedXp.ToString())
            .AddField("{needed}", neededXp.ToString());
        Output.ChatReply(ctx, message);
    }

    [Command("set", "s", "<playerName> <level>", "Sets the specified player's level to the start of the given level", adminOnly: false)]
    public static void SetLevel(ChatCommandContext ctx, string name, int level)
    {
        CheckXPSystemActive(ctx);
        ulong steamID;

        if (PlayerCache.FindPlayer(name, true, out var targetEntity, out var targetUserEntity)){
            steamID = _entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
        }
        else
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound)
                .AddField("{playerName}", name));
        }
        
        ExperienceSystem.SetXp(steamID, ExperienceSystem.ConvertLevelToXp(level));
        ExperienceSystem.CheckAndApplyLevel(targetEntity, targetUserEntity, steamID);
        
        var message = L10N.Get(L10N.TemplateKey.XpSet)
            .AddField("{playerName}", name)
            .AddField("{level}", ExperienceSystem.GetLevel(steamID).ToString());
        Output.ChatReply(ctx, message);
    }

    [Command("log", "l", "", "Toggles logging of xp gain.", adminOnly: false)]
    public static void LogExperience(ChatCommandContext ctx)
    {
        CheckXPSystemActive(ctx);
        
        var steamID = ctx.User.PlatformId;
        var loggingData = Database.PlayerLogConfig[steamID];
        loggingData.LoggingExp = !loggingData.LoggingExp;
        var message = loggingData.LoggingExp
            ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
            : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
        Output.ChatReply(ctx, message.AddField("{system}", "XP"));
        Database.PlayerLogConfig[steamID] = loggingData;
    }
    
    [Command(name: "bump20", shortHand: "", adminOnly: false, usage: "", description: "Temporarily bumps you to lvl20 so you can skip the quest")]
    public static void BumpToLevel20(ChatCommandContext ctx)
    {
        var playerEntity = ctx.Event.SenderCharacterEntity;
        var userEntity = ctx.Event.SenderUserEntity;

        try
        {
            SetUserLevel(ctx.Event.SenderCharacterEntity, ctx.Event.SenderUserEntity, ctx.User.PlatformId, 20, 5);
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.XpBump));
        }
        catch (Exception e)
        {
            Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"Failed to bump20 {e.Message}", true);
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.XpBumpError));
        }
    }
    
    [Command(name: "bump20", shortHand: "", adminOnly: false, usage: "<PlayerName>", description: "Temporarily bumps the player to lvl20 so they can skip the quest")]
    public static void BumpToLevel20(ChatCommandContext ctx, string playerName)
    {
        if (!PlayerCache.FindPlayer(playerName, true, out var playerEntity, out var userEntity))
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound)
                .AddField("{playerName}", playerName));
        }

        var steamId = PlayerCache.GetSteamIDFromName(playerName);

        try
        {
            SetUserLevel(playerEntity, userEntity, steamId, 20, 5);
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.XpAdminBump));
            Output.SendMessage(userEntity, L10N.Get(L10N.TemplateKey.XpBump));
        }
        catch (Exception e)
        {
            Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"Failed to bump20 {e.Message}", true);
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.XpBumpError));
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