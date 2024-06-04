using ProjectM;
using Stunlock.Core;
using XPRising.Utils.Prefabs;

namespace XPRising.Extensions;

public static class PrefabGuidExtensions
{
    public static WeaponType? ToWeaponType(this PrefabGUID prefabGuid)
    {
        var equipBuff = (EquipBuffs)prefabGuid.GuidHash;
        switch (equipBuff)
        {
            case EquipBuffs.EquipBuff_Weapon_Axe_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Axe_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Axe_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Axe_Base:
                return WeaponType.Axes;
            case EquipBuffs.EquipBuff_Weapon_Crossbow_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Crossbow_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Crossbow_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Crossbow_Base:
                return WeaponType.Crossbow;
            case EquipBuffs.EquipBuff_Weapon_FishingPole_Base:
            case EquipBuffs.EquipBuff_Weapon_FishingPole_Debug:
                return WeaponType.FishingPole;
            case EquipBuffs.EquipBuff_Weapon_GreatSword_Ability01:
            case EquipBuffs.EquipBuff_Weapon_GreatSword_Ability02:
            case EquipBuffs.EquipBuff_Weapon_GreatSword_Ability03:
            case EquipBuffs.EquipBuff_Weapon_GreatSword_Base:
                return WeaponType.GreatSword;
            case EquipBuffs.EquipBuff_Weapon_Longbow_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Longbow_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Longbow_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Longbow_Base:
                return WeaponType.Longbow;
            case EquipBuffs.EquipBuff_Weapon_Mace_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Mace_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Mace_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Mace_Base:
                return WeaponType.Mace;
            case EquipBuffs.EquipBuff_Weapon_Pistols_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Pistols_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Pistols_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Pistols_Base:
                return WeaponType.Pistols;
            case EquipBuffs.EquipBuff_Weapon_Reaper_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Reaper_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Reaper_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Reaper_Base:
                return WeaponType.Scythe;
            case EquipBuffs.EquipBuff_Weapon_Slashers_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Slashers_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Slashers_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Slashers_Base:
                return WeaponType.Slashers;
            case EquipBuffs.EquipBuff_Weapon_Spear_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Spear_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Spear_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Spear_Base:
                return WeaponType.Spear;
            case EquipBuffs.EquipBuff_Weapon_Sword_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Sword_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Sword_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Sword_Base:
                return WeaponType.Sword;
            case EquipBuffs.EquipBuff_Weapon_Unarmed_Base:
            case EquipBuffs.EquipBuff_Weapon_Unarmed_Start01:
                return WeaponType.None;
            case EquipBuffs.EquipBuff_Weapon_Whip_Ability01:
            case EquipBuffs.EquipBuff_Weapon_Whip_Ability02:
            case EquipBuffs.EquipBuff_Weapon_Whip_Ability03:
            case EquipBuffs.EquipBuff_Weapon_Whip_Base:
                return WeaponType.Whip;
            default:
                return null;
        }
    }
}