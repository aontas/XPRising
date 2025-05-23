using ProjectM;
using ProjectM.Network;
using BepInEx.Logging;
using Stunlock.Core;
using Unity.Entities;
using XPRising.Utils;
using XPShared;
using MasteryType = XPRising.Systems.GlobalMasterySystem.MasteryType;

namespace XPRising.Systems
{
    public static class WeaponMasterySystem
    {
        private static EntityManager _em = Plugin.Server.EntityManager;

        public static double MasteryGainMultiplier = 0.1;
        public static double VBloodMultiplier = 5;

        /// <summary>
        /// Calculates and banks any mastery increases for the damage event
        /// </summary>
        /// <param name="sourceEntity">The ability that is dealing damage to the target</param>
        /// <param name="targetEntity">The target that is receiving the damage</param>
        /// <param name="change">The HP change due to this event. For damage events, this is a negative number.</param>
        public static void HandleDamageEvent(Entity sourceEntity, Entity targetEntity, float change)
        {
            if (sourceEntity.TryGetComponent<EntityOwner>(out var damageOwner) &&
                damageOwner.Owner.TryGetComponent<PlayerCharacter>(out var sourcePlayerCharacter) &&
                damageOwner.Owner.TryGetComponent<UnitStats>(out var stats))
            {
                var abilityGuid = Helper.GetPrefabGUID(sourceEntity);
                var masteryType = MasteryHelper.GetMasteryTypeForEffect(abilityGuid.GuidHash, out var ignore, out var uncertain);
                
                float divisor = masteryType == MasteryType.Spell ? stats.SpellPower : stats.PhysicalPower;
                
                LogDamage(damageOwner, targetEntity, abilityGuid, -change, divisor);
                if (ignore)
                {
                    return;
                }
                if (uncertain)
                {
                    LogDamage(damageOwner, targetEntity, abilityGuid, change, divisor, "NEEDS SUPPORT: ", true);
                    return;
                }
            
                sourcePlayerCharacter.UserEntity.TryGetComponent<User>(out var sourceUser);
                var hasLevel = targetEntity.TryGetComponent<UnitLevel>(out var targetLevel);
                var hasMovement = targetEntity.Has<Movement>();
                if (hasLevel && hasMovement)
                {
                    var currentMastery = Math.Max(Database.PlayerMastery[sourceUser.PlatformId][masteryType].Mastery, 0.1);
                    var levelMultiplier = Math.Clamp(targetLevel.Level / currentMastery, 0.1f, 1.3f);
                    var masteryValue = -change / divisor;
                    WeaponMasterySystem.UpdateMastery(sourceUser.PlatformId, masteryType, masteryValue * levelMultiplier, targetEntity);
                }
                else
                {
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Prefab {DebugTool.GetPrefabName(targetEntity)} has [L: {hasLevel}, M: {hasMovement}]");
                }
            }
        }

        public static void UpdateMastery(ulong steamID, MasteryType masteryType, double masteryValue, Entity victimEntity)
        {
            var isVBlood = Helper.IsVBlood(victimEntity);
            
            var vBloodMultiplier = isVBlood ? VBloodMultiplier : 1;
            var changeInMastery = masteryValue * vBloodMultiplier * MasteryGainMultiplier * 0.02;
            
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Banking weapon mastery for {steamID}: {Enum.GetName(masteryType)}: [{masteryValue:F4},{changeInMastery:F4}]");
            GlobalMasterySystem.BankMastery(steamID, victimEntity, masteryType, changeInMastery);
        }

        public static WeaponType GetWeaponType(Entity player, out Entity weaponEntity)
        {
            weaponEntity = _em.GetComponentData<Equipment>(player).WeaponSlot.SlotEntity._Entity;
            var weaponType = WeaponType.None;
            if (_em.HasComponent<EquippableData>(weaponEntity))
            {
                var weaponData = _em.GetComponentData<EquippableData>(weaponEntity);
                weaponType = weaponData.WeaponType;
            }
            return weaponType;
        }
        
        public static MasteryType WeaponToMasteryType(WeaponType weapon)
        {
            // Note: we are not just simply casting the int value of weapon to a MasteryType to help ensure forwards compatibility.
            switch (weapon)
            {
                case WeaponType.None:
                    return MasteryType.Spell;
                case WeaponType.Spear:
                    return MasteryType.WeaponSpear;
                case WeaponType.Sword:
                    return MasteryType.WeaponSword;
                case WeaponType.Scythe:
                    return MasteryType.WeaponScythe;
                case WeaponType.Crossbow:
                    return MasteryType.WeaponCrossbow;
                case WeaponType.Mace:
                    return MasteryType.WeaponMace;
                case WeaponType.Slashers:
                    return MasteryType.WeaponSlasher;
                case WeaponType.Axes:
                    return MasteryType.WeaponAxe;
                case WeaponType.FishingPole:
                    return MasteryType.WeaponFishingPole;
                case WeaponType.Rapier:
                    return MasteryType.WeaponRapier;
                case WeaponType.Pistols:
                    return MasteryType.WeaponPistol;
                case WeaponType.GreatSword:
                    return MasteryType.WeaponGreatSword;
                case WeaponType.Longbow:
                    return MasteryType.WeaponLongBow;
                case WeaponType.Whip:
                    return MasteryType.WeaponWhip;
                case WeaponType.Daggers:
                    return MasteryType.WeaponDaggers;
                case WeaponType.Claws:
                    return MasteryType.WeaponClaws;
                case WeaponType.Twinblades:
                    return MasteryType.WeaponTwinblades;
                default:
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Error, $"Cannot convert new weapon to mastery: {Enum.GetName(weapon)}. Defaulting to Spell.", true);
                    return MasteryType.Spell;
            }
        }
        
        private static void LogDamage(Entity source, Entity target, PrefabGUID abilityPrefab, float change, float divisor, string prefix = "", bool forceLog = false)
        {
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info,
                () =>
                    $"{prefix}{GetName(source, out _)} -> " +
                    $"({DebugTool.GetPrefabName(abilityPrefab)}) -> " +
                    $"{GetName(target, out _)}" +
                    $"[diff: {change}, div: {divisor}, val: {change/divisor}]", forceLog);
        }

        private static string GetName(Entity entity, out bool isUser)
        {
            if (entity.TryGetComponent<PlayerCharacter>(out var playerCharacterSource))
            {
                isUser = true;
                return $"{playerCharacterSource.Name.Value}";
            }
            else
            {
                isUser = false;
                return $"{DebugTool.GetPrefabName(entity)}[{MobData(entity)}]";
            }
        }

        private static string MobData(Entity entity)
        {
            var output = "";
            if (entity.TryGetComponent<UnitLevel>(out var unitLevel))
            {
                output += $"{unitLevel.Level.Value},";
            }

            if (entity.TryGetComponent<EntityCategory>(out var entityCategory))
            {
                output += $"{Enum.GetName(entityCategory.MainCategory)}";
            }

            return output;
        }
    }
}
