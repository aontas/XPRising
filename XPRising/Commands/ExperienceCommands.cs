using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
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
        var steamID = user.PlatformId;
        var userXp = ExperienceSystem.GetXp(steamID);
        ExperienceSystem.GetLevelAndProgress(userXp, out var level, out var progress, out var earnedXp, out var neededXp);
        var message = L10N.Get(L10N.TemplateKey.XpLevel)
            .AddField("{level}", level.ToString())
            .AddField("{progress}", $"{(progress * 100):N1}")
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
        var loggingData = Database.PlayerPreferences[steamID];
        loggingData.LoggingExp = !loggingData.LoggingExp;
        var message = loggingData.LoggingExp
            ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
            : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
        Output.ChatReply(ctx, message.AddField("{system}", "XP"));
        Database.PlayerPreferences[steamID] = loggingData;
    }
    
    [Command(name: "questSkip", shortHand: "qs", adminOnly: false, usage: "", description: "Skips the level requirement quest. Quest should be auto-skipped, but just in case you need it.")]
    public static void SkipLevel20Quest(ChatCommandContext ctx)
    {
        var playerEntity = ctx.Event.SenderCharacterEntity;
        var userEntity = ctx.Event.SenderUserEntity;

        if (!Plugin.Server.EntityManager.TryGetComponentData<AchievementOwner>(userEntity, out var achievementOwner)) return;
            
        var achievementOwnerEntity = achievementOwner.Entity._Entity;
        var entityCommandBuffer = Helper.EntityCommandBufferSystem.CreateCommandBuffer();
        PrefabGUID achievementPrefabGuid = new(560247139); // Journal_GettingReadyForTheHunt
        Helper.ClaimAchievementSystem.CompleteAchievement(entityCommandBuffer, achievementPrefabGuid, userEntity, playerEntity, achievementOwnerEntity, false, true);
    }
}