using System;
using System.Linq;
using BepInEx.Logging;
using ProjectM;
using ProjectM.Shared;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Configuration;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;
using Faction = XPRising.Utils.Prefabs.Faction;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Commands;

[CommandGroup("wanted", "w")]
public static class WantedCommands {
    private static void CheckWantedSystemActive(ChatCommandContext ctx)
    {
        if (!Plugin.WantedSystemActive)
        {
            var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                .AddField("{system}", "Wanted");
            throw Output.ChatError(ctx, message);
        }
    }
    
    private static void SendFactionWantedMessage(PlayerHeatData heatData, Entity userEntity, bool userIsAdmin) {
        bool isWanted = false;
        foreach (Faction faction in FactionHeat.ActiveFactions) {
            var heat = heatData.heat[faction];
            // Don't display this faction's heat level if the wanted level is 0.
            if (heat.level < FactionHeat.HeatLevels[0] && !userIsAdmin) continue;
            isWanted = true;
            
            Output.SendMessage(userEntity, new L10N.LocalisableString(FactionHeat.GetFactionStatus(faction, heat.level)));
            
            if (userIsAdmin && DebugLoggingConfig.IsLogging(LogSystem.Wanted))
            {
                var sinceAmbush = DateTime.Now - heat.lastAmbushed;
                var nextAmbush = Math.Max((int)(WantedSystem.ambush_interval - sinceAmbush.TotalSeconds), 0);
                Output.DebugMessage(
                    userEntity,
                    $"Level: <color={Output.White}>{heat.level:D}</color> " +
                    $"Possible ambush in <color={Color.White}>{nextAmbush:D}</color>s " +
                    $"Chance: <color={Color.White}>{WantedSystem.ambush_chance:D}</color>%");
            }
        }

        if (!isWanted) {
            Output.SendMessage(userEntity, L10N.Get(L10N.TemplateKey.WantedLevelsNone));
        }
    }
    
    [Command("get","g", "", "Shows your current wanted level", adminOnly: false)]
    public static void GetWanted(ChatCommandContext ctx)
    {
        CheckWantedSystemActive(ctx);
        var userEntity = ctx.Event.SenderUserEntity;
        
        var heatData = WantedSystem.GetPlayerHeat(userEntity);
        SendFactionWantedMessage(heatData, userEntity, ctx.IsAdmin);
    }

    [Command("set","s", "<name> <faction> <value>", "Sets the current wanted level", adminOnly: false)]
    public static void SetWanted(ChatCommandContext ctx, string name, string faction, int value) {
        CheckWantedSystemActive(ctx);
        var contextUserEntity = ctx.Event.SenderUserEntity;
            
        if (!PlayerCache.FindPlayer(name, true, out _, out var targetUserEntity))
        {
            var message = L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound).AddField("{playerName}", name);
            Output.ChatReply(ctx, message);
            return;
        }

        if (!Enum.TryParse(faction, true, out Faction heatFaction) || !FactionHeat.ActiveFactions.Contains(heatFaction)) {
            var supportedFactions = String.Join(", ", FactionHeat.ActiveFactions);
            var message = L10N.Get(L10N.TemplateKey.WantedFactionUnsupported).AddField("{supportedFactions}", supportedFactions);
            Output.ChatReply(ctx, message);
            return;
        }

        var heatLevel = value == 0
            ? 0
            : FactionHeat.HeatLevels[Math.Clamp(value - 1, 0, FactionHeat.HeatLevels.Length - 1)] + 10;

        // Set wanted level and reset last ambushed so the user can be ambushed from now (ie, greater than ambush_interval seconds ago) 
        var updatedHeatData = WantedSystem.SetPlayerHeat(
            targetUserEntity,
            heatFaction,
            heatLevel,
            DateTime.Now - TimeSpan.FromSeconds(WantedSystem.ambush_interval + 1));
        
        Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.WantedLevelSet).AddField("{playerName}", name));
        SendFactionWantedMessage(updatedHeatData, contextUserEntity, ctx.IsAdmin);
        if (!targetUserEntity.Equals(contextUserEntity)) {
            SendFactionWantedMessage(updatedHeatData, targetUserEntity, false);
        }
    }
    
    [Command("log", "l", "", "Toggle logging of heat data.", adminOnly: false)]
    public static void LogWanted(ChatCommandContext ctx){
        CheckWantedSystemActive(ctx);

        var steamID = ctx.User.PlatformId;
        var loggingData = Database.PlayerLogConfig[steamID];
        loggingData.LoggingWanted = !loggingData.LoggingWanted;
        var message = loggingData.LoggingWanted
            ? L10N.Get(L10N.TemplateKey.SystemLogEnabled)
            : L10N.Get(L10N.TemplateKey.SystemLogDisabled);
        Output.ChatReply(ctx, message.AddField("{system}", "Wanted heat"));
        Database.PlayerLogConfig[steamID] = loggingData;
    }

    [Command("trigger","t", "<name>", "Triggers the ambush check for the given user", adminOnly: false)]
    public static void TriggerAmbush(ChatCommandContext ctx, string name) {
        CheckWantedSystemActive(ctx);
        if (!PlayerCache.FindPlayer(name, true, out var playerEntity, out _))
        {
            var message = L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound).AddField("{playerName}", name);
            Output.ChatReply(ctx, message);
            return;
        }
        
        WantedSystem.CheckForAmbush(playerEntity);
        Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.WantedTriggerAmbush).AddField("{playerName}", name));
    }

    [Command("fixminions", "fm", "", "Remove broken gloomrot technician units", adminOnly: false)]
    public static void FixGloomrotMinions(ChatCommandContext ctx) {
        CheckWantedSystemActive(ctx);

        var hasErrors = false;
        var removedCount = 0;

        var query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Minion>()
            },
            Options = EntityQueryOptions.IncludeDisabled
        });
        foreach (var entity in query.ToEntityArray(Allocator.Temp)) {
            try {
                var unitName = DebugTool.GetPrefabName(Helper.GetPrefabGUID(entity));
                // Note that the "broken" units differ from "working" units only by the broken ones missing the
                // "PathRequestSolveDebugBuffer [B]" component. Ideally, we would only destroy minions missing the
                // component, but we can't test for this case by any means other than checking the string generated
                // by Plugin.Server.EntityManager.Debug.GetEntityInfo(entity). We can't test for this case as the
                // GetBuffer or HasBuffer commands fail with an AOT code exception.
                Plugin.Log(LogSystem.Wanted, LogLevel.Info, $"destroying minion {unitName}");
                
                DestroyUtility.CreateDestroyEvent(Plugin.Server.EntityManager, entity, DestroyReason.Default, DestroyDebugReason.None);
                DestroyUtility.Destroy(Plugin.Server.EntityManager, entity);
                removedCount++;
            }
            catch (Exception e) {
                Plugin.Log(LogSystem.Wanted, LogLevel.Info, "error doing test other: " + e.Message);
                hasErrors = true;
            }
        }

        var message = hasErrors
            ? L10N.Get(L10N.TemplateKey.WantedMinionRemoveError).AddField("{value}", removedCount.ToString())
            : L10N.Get(L10N.TemplateKey.WantedMinionRemoveSuccess).AddField("{value}", removedCount.ToString());
        Output.ChatReply(ctx, message);
    }
}
