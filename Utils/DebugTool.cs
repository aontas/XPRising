using BepInEx.Logging;
using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace XPRising.Utils;

public class DebugTool
{
    private static string MaybeAddSpace(string input)
    {
        return input.Length > 0 ? input.TrimEnd() + " " : input;
    }
    
    public static PrefabGUID GetAndLogPrefabGuid(Entity entity, string logPrefix = "", Plugin.LogSystem logSystem = Plugin.LogSystem.Core)
    {
        var guid = Helper.GetPrefabGUID(entity);
        LogPrefabGuid(guid, logPrefix, logSystem);
        return guid;
    }
    
    public static void LogPrefabGuid(PrefabGUID guid, string logPrefix = "", Plugin.LogSystem logSystem = Plugin.LogSystem.Core)
    {
        Plugin.Log(logSystem, LogLevel.Info, () => $"{MaybeAddSpace(logPrefix)}Prefab: {GetPrefabName(guid)} ({guid.GuidHash})");
    }

    public static void LogEntity(Entity entity, string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Core)
    {
        Plugin.Log(logSystem, LogLevel.Info, () => $"{MaybeAddSpace(logPrefix)}{entity} - {GetPrefabName(entity)}");
    }

    public static void LogDebugEntity(
        Entity entity,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Core)
    {
        Plugin.Log(logSystem, LogLevel.Info,
            () => $"{MaybeAddSpace(logPrefix)}Entity: {entity} ({Plugin.Server.EntityManager.Debug.GetEntityInfo(entity)})");
    }

    private static IEnumerable<string> StatsBufferToEnumerable(DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer, string logPrefix = "")
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var data = buffer[i];
            yield return $"{MaybeAddSpace(logPrefix)}B[{i}]: {data.StatType} {data.Value} {data.ModificationType} {data.Id.Id} {data.Priority} {data.ValueByStacks} {data.IncreaseByStacks}";
        }
    }
    
    public static void LogStatsBuffer(
        DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer,
        string logPrefix = "",
        Plugin.LogSystem logSystem = Plugin.LogSystem.Core)
    {
        Plugin.Log(logSystem, LogLevel.Info, StatsBufferToEnumerable(buffer, logPrefix));
        
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