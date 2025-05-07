using System;
using BepInEx.Logging;
using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Stunlock.Localization;
using Unity.Entities;

namespace XPRising.Utils;

public static class DebugTool
{
    private static string MaybeAddSpace(string input)
    {
        return input.Length > 0 ? input.TrimEnd() + " " : input;
    }

    private static string DebugEntity(Entity entity)
    {
        return Plugin.Server.EntityManager.Debug.GetEntityInfo(entity);
    }

    private static string DumpEntity(Entity entity, bool fullDump = true)
    {
        var sb = new Il2CppSystem.Text.StringBuilder();
        ProjectM.EntityDebuggingUtility.DumpEntity(Plugin.Server, entity, fullDump, sb);
        return sb.ToString();
    }
    
    public static PrefabGUID GetAndLogPrefabGuid(Entity entity, string logPrefix = "", Plugin.LogSystem logSystem = Plugin.LogSystem.Debug, bool forceLog = false)
    {
        var guid = Helper.GetPrefabGUID(entity);
        LogPrefabGuid(guid, logPrefix, logSystem, forceLog);
        return guid;
    }
    
    public static void LogPrefabGuid(PrefabGUID guid, string logPrefix = "", Plugin.LogSystem logSystem = Plugin.LogSystem.Debug, bool forceLog = false)
    {
        Plugin.Log(logSystem, LogLevel.Info, () => $"{MaybeAddSpace(logPrefix)}Prefab: {GetPrefabName(guid)} ({guid.GuidHash})", forceLog);
    }

    public static void LogEntity(
        Entity entity,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Debug,
        bool forceLog = false)
    {
        Plugin.Log(logSystem, LogLevel.Info, () => $"{MaybeAddSpace(logPrefix)}{entity} - {GetPrefabName(entity)}", forceLog);
    }

    public static void LogDebugEntity(
        Entity entity,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Debug,
        bool forceLog = false)
    {
        Plugin.Log(logSystem, LogLevel.Info,
            () => $"{MaybeAddSpace(logPrefix)}Entity: {entity} ({DebugEntity(entity)})", forceLog);
    }

    public static void LogFullEntityDebugInfo(Entity entity, string logPrefix = "", bool forceLog = false)
    {
        Plugin.Log(Plugin.LogSystem.Debug, LogLevel.Info, () => $"{MaybeAddSpace(logPrefix)}Debug entity: {entity}\n{DumpEntity(entity)}", forceLog);
    }

    private static IEnumerable<string> BufferToEnumerable<T>(DynamicBuffer<T> buffer, Func<T, string> valueToString, string logPrefix = "")
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var data = buffer[i];
            yield return $"{MaybeAddSpace(logPrefix)}B[{i}]: {valueToString(data)}";
        }
    }
    
    public static void LogStatsBuffer(
        DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Debug,
        bool forceLog = false)
    {
        Func<ModifyUnitStatBuff_DOTS, string> printStats = (data) =>
            $"{data.StatType} {data.Value} {data.ModificationType} {data.Id.Id} {data.Priority} {data.ValueByStacks} {data.IncreaseByStacks}"; 
        Plugin.Log(logSystem, LogLevel.Info, BufferToEnumerable(buffer, printStats, logPrefix), forceLog);
    }

    public static void LogBuffBuffer(
        DynamicBuffer<BuffBuffer> buffer,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Debug,
        bool forceLog = false)
    {
        Func<BuffBuffer, string> printStats = (data) =>
            $"Prefab: {GetPrefabName(data.PrefabGuid)}\nDebug BuffBuffer:{DumpEntity(data.Entity, false)}"; 
        Plugin.Log(logSystem, LogLevel.Info, BufferToEnumerable(buffer, printStats, logPrefix), forceLog);
    }
    
    public static string GetPrefabName(PrefabGUID hashCode)
    {
        var s = Plugin.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        string name = "Nonexistent";
        if (hashCode.GuidHash == 0)
        {
            return name;
        }
        try
        {
            name = s._PrefabLookupMap.GetName(hashCode);
        }
        catch
        {
            name = "NoPrefabName";
        }
        return name;
    }

    public static string GetPrefabName(Entity entity)
    {
        // TODO This can fail. Check and may update? 
        // var EM = WorldUtility.FindClientWorld().EntityManager;
        // if (EM.HasComponent<CharacterHUD>(entity))
        // {
        //     var hud = EM.GetComponentData<CharacterHUD>(entity);
        //     if (Localization.HasKey(hud.LocalizedName))
        //     {
        //         return Localization.Get(hud.LocalizedName);
        //     }
        // }
        return GetPrefabName(Helper.GetPrefabGUID(entity));
    }
}