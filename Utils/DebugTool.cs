using System;
using BepInEx.Logging;
using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace XPRising.Utils;

public static class DebugTool
{
    private static string MaybeAddSpace(string input)
    {
        return input.Length > 0 ? input.TrimEnd() + " " : input;
    }
    
    public static PrefabGUID GetAndLogPrefabGuid(Entity entity, string logPrefix = "", Plugin.LogSystem logSystem = Plugin.LogSystem.Core, bool forceLog = false)
    {
        var guid = Helper.GetPrefabGUID(entity);
        LogPrefabGuid(guid, logPrefix, logSystem, forceLog);
        return guid;
    }
    
    public static void LogPrefabGuid(PrefabGUID guid, string logPrefix = "", Plugin.LogSystem logSystem = Plugin.LogSystem.Core, bool forceLog = false)
    {
        Plugin.Log(logSystem, LogLevel.Info, () => $"{MaybeAddSpace(logPrefix)}Prefab: {GetPrefabName(guid)} ({guid.GuidHash})", forceLog);
    }

    public static void LogEntity(
        Entity entity,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Core,
        bool forceLog = false)
    {
        Plugin.Log(logSystem, LogLevel.Info, () => $"{MaybeAddSpace(logPrefix)}{entity} - {GetPrefabName(entity)}", forceLog);
    }

    public static void LogDebugEntity(
        Entity entity,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Core,
        bool forceLog = false)
    {
        Plugin.Log(logSystem, LogLevel.Info,
            () => $"{MaybeAddSpace(logPrefix)}Entity: {entity} ({Plugin.Server.EntityManager.Debug.GetEntityInfo(entity)})", forceLog);
    }

    public static void LogFullEntityDebugInfo(Entity entity, bool forceLog = false)
    {
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, () =>
        {
            var sb = new Il2CppSystem.Text.StringBuilder();
            ProjectM.EntityDebuggingUtility.DumpEntity(Plugin.Server, entity, true, sb);
            return $"Debug entity: {sb.ToString()}";
        }, forceLog);
    }

    private static IEnumerable<string> StatsBufferToEnumerable<T>(DynamicBuffer<T> buffer, Func<T, string> valueToString, string logPrefix = "")
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
        Plugin.LogSystem logSystem = Plugin.LogSystem.Core,
        bool forceLog = false)
    {
        Func<ModifyUnitStatBuff_DOTS, string> printStats = (data) =>
            $"{data.StatType} {data.Value} {data.ModificationType} {data.Id.Id} {data.Priority} {data.ValueByStacks} {data.IncreaseByStacks}"; 
        Plugin.Log(logSystem, LogLevel.Info, StatsBufferToEnumerable(buffer, printStats, logPrefix), forceLog);
    }

    public static void LogBuffBuffer(
        DynamicBuffer<BuffBuffer> buffer,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Core,
        bool forceLog = false)
    {
        // for (int i = 0; i < buffer.Length; i++)
        // {
        //     var data = buffer[i];
        //     DebugTool.LogPrefabGuid(data.PrefabGuid, "Item not equipped by PC:", logSystem);
        //     DebugTool.LogDebugEntity(data.Entity, $"Debug BuffBuffer[{i}]:", logSystem);
        // }
        
        Func<BuffBuffer, string> printStats = (data) =>
            $"Prefab: {GetPrefabName(data.PrefabGuid)}\nDebug BuffBuffer:{Plugin.Server.EntityManager.Debug.GetEntityInfo(data.Entity)}"; 
        Plugin.Log(logSystem, LogLevel.Info, StatsBufferToEnumerable(buffer, printStats, logPrefix), forceLog);
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
            name = s.PrefabGuidToNameDictionary[hashCode];
        }
        catch
        {
            name = "NoPrefabName";
        }
        return name;
    }

    public static string GetPrefabName(Entity entity)
    {
        return GetPrefabName(Helper.GetPrefabGUID(entity));
    }
}