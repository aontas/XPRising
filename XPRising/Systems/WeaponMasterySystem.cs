using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Unity.Entities;
using XPRising.Models;
using XPRising.Utils;
using MasteryType = XPRising.Systems.GlobalMasterySystem.MasteryType;

namespace XPRising.Systems
{
    public static class WeaponMasterySystem
    {
        private static EntityManager _em = Plugin.Server.EntityManager;

        public static double MasteryGainMultiplier = 0.1;
        public static double VBloodMultiplier = 15;

        public static void UpdateMastery(ulong steamID, MasteryType masteryType, double victimPower, Entity victimEntity)
        {
            var isVBlood = Helper.IsVBlood(victimEntity);
            double masteryValue = victimPower;
            
            var vBloodMultiplier = isVBlood ? VBloodMultiplier : 1;
            var changeInMastery = masteryValue * vBloodMultiplier * MasteryGainMultiplier * 0.001;
            
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Banking weapon mastery for {steamID}: {Enum.GetName(masteryType)}: [{masteryValue},{changeInMastery}]");
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
                    return MasteryType.WeaponUnarmed;
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
                default:
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Error, $"Cannot convert new weapon to mastery: {Enum.GetName(weapon)}. Defaulting to Spell.");
                    return MasteryType.Spell;
            }
        }
    }
}
