using System;
using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using XPRising.Systems;
using XPRising.Utils;
using XPRising.Utils.Prefabs;

namespace XPRising.Hooks;

[HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.DealDamage))]
public class DealDamageSystemDealDamagePatch
{
    private static EntityManager EntityManager => Plugin.Server.EntityManager;
    
    public static void Postfix(DealDamageSystem __instance)
    {
        if (!Plugin.WeaponMasterySystemActive) return;
        
        var entityArray = __instance._Query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            var damageEvent = EntityManager.GetComponentData<DealDamageEvent>(entity);

            var sourceEntity = damageEvent.SpellSource;
            if (EntityManager.TryGetComponentData<EntityOwner>(damageEvent.SpellSource, out var entityOwner))
            {
                sourceEntity = entityOwner.Owner;
            }

            if (EntityManager.TryGetComponentData<PlayerCharacter>(sourceEntity, out var sourcePlayerCharacter))
            {
                LogDamage(EntityManager, sourceEntity, damageEvent);
                
                var spellGuid = Helper.GetPrefabGUID(damageEvent.SpellSource);
                var masteryType = MasteryHelper.GetMasteryTypeForEffect(spellGuid.GuidHash, out var ignore, out var uncertain);
                if (ignore)
                {
                    continue;
                }
                if (uncertain)
                {
                    LogDamage(EntityManager, sourceEntity, damageEvent, "NEEDS SUPPORT: ", true);
                    if (damageEvent.MainType == MainDamageType.Spell) masteryType = GlobalMasterySystem.MasteryType.Spell;
                }

                EntityManager.TryGetComponentData<User>(sourcePlayerCharacter.UserEntity, out var sourceUser);
                var hasStats = EntityManager.TryGetComponentData<UnitStats>(damageEvent.Target, out var victimStats);
                var hasLevel = EntityManager.HasComponent<UnitLevel>(damageEvent.Target);
                var hasMovement = EntityManager.HasComponent<Movement>(damageEvent.Target);
                if (hasStats && hasLevel && hasMovement)
                {
                    var skillMultiplier = damageEvent.MainFactor > 0 ? damageEvent.MainFactor : 1f;
                    var masteryValue =
                        MathF.Max(victimStats.PhysicalPower.Value, victimStats.SpellPower.Value) * skillMultiplier;
                    WeaponMasterySystem.UpdateMastery(sourceUser.PlatformId, masteryType, masteryValue, damageEvent.Target);
                }
                else
                {
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Prefab {DebugTool.GetPrefabName(damageEvent.Target)} has [S: {hasStats}, L: {hasLevel}, M: {hasMovement}]");
                }
            }
            else if (!EntityManager.TryGetComponentData<PlayerCharacter>(sourceEntity, out var targetPlayerCharacter))
            {
                continue;
            }
        }
    }
    
    private static void LogDamage(EntityManager em, Entity source, DealDamageEvent damageEvent, string prefix = "", bool forceLog = false)
    {
        Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info,
            () =>
                $"{prefix}Source: {GetName(em, source, out _)} -> " +
                $"({DebugTool.GetPrefabName(damageEvent.SpellSource)}) -> " +
                $"{GetName(em, damageEvent.Target, out _)}" +
                $"[Multiplier: {damageEvent.MainFactor}]", forceLog);
    }

    private static string GetName(EntityManager em, Entity entity, out bool isUser)
    {
        if (em.TryGetComponentData<PlayerCharacter>(entity, out var playerCharacterSource))
        {
            isUser = true;
            return $"{playerCharacterSource.Name.Value}";
        }
        else
        {
            isUser = false;
            return $"{DebugTool.GetPrefabName(entity)}[{MobData(em, entity)}]";
        }
    }

    private static string MobData(EntityManager em, Entity entity)
    {
        var output = "";
        if (em.TryGetComponentData<UnitLevel>(entity, out var unitLevel))
        {
            output += $"{unitLevel.Level.Value},";
        }

        if (em.TryGetComponentData<EntityCategory>(entity, out var entityCategory))
        {
            output += $"{Enum.GetName(entityCategory.MainCategory)}";
        }

        return output;
    }
}