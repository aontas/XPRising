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
    public static void Postfix(DealDamageSystem __instance)
    {
        var entityArray = __instance._Query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            var damageEvent = __instance.EntityManager.GetComponentData<DealDamageEvent>(entity);

            var sourceEntity = damageEvent.SpellSource;
            if (__instance.EntityManager.TryGetComponentData<EntityOwner>(damageEvent.SpellSource, out var entityOwner))
            {
                sourceEntity = entityOwner.Owner;
            }

            if (__instance.EntityManager.TryGetComponentData<PlayerCharacter>(sourceEntity, out var sourcePlayerCharacter))
            {
                var spellGuid = Helper.GetPrefabGUID(damageEvent.SpellSource);
                var masteryType = MasteryHelper.GetMasteryTypeForEffect(spellGuid.GuidHash, out var ignore, out var uncertain);
                if (ignore)
                {
                    continue;
                }
                if (uncertain)
                {
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info,
                        () =>
                            $"NEEDS SUPPORT: Source: {GetName(__instance.EntityManager, sourceEntity, out _)} -> " +
                            $"({DebugTool.GetPrefabName(damageEvent.SpellSource)}) [{Enum.GetName(damageEvent.MainType)}] -> " +
                            $"{GetName(__instance.EntityManager, damageEvent.Target, out _)}", true);
                    if (damageEvent.MainType == MainDamageType.Spell) masteryType = WeaponMasterySystem.MasteryType.Spell;
                }

                __instance.EntityManager.TryGetComponentData<User>(sourcePlayerCharacter.UserEntity, out var sourceUser);
                var hasStats = __instance.EntityManager.TryGetComponentData<UnitStats>(damageEvent.Target, out var victimStats);
                if (hasStats)
                {
                    var masteryValue = damageEvent.MainType == MainDamageType.Physical
                        ? victimStats.PhysicalPower.Value
                        : victimStats.SpellPower.Value;
                    WeaponMasterySystem.UpdateMastery(sourceUser.PlatformId, masteryType, masteryValue,
                        Helper.IsVBlood(sourceEntity));
                }
                else
                {
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info,
                        $"Prefab {DebugTool.GetPrefabName(damageEvent.Target)} has no stats", true);
                }
            }
            else if (!__instance.EntityManager.TryGetComponentData<PlayerCharacter>(sourceEntity, out var targetPlayerCharacter))
            {
                continue;
            }

            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info,
                () =>
                    $"Source: {GetName(__instance.EntityManager, sourceEntity, out _)} -> " +
                    $"({DebugTool.GetPrefabName(damageEvent.SpellSource)}) -> " +
                    $"{GetName(__instance.EntityManager, damageEvent.Target, out _)}");
        }
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