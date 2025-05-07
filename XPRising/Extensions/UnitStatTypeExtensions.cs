using System;
using ProjectM;

namespace XPRising.Extensions;

public static class UnitStatTypeExtensions
{
    [Flags]
    public enum Category
    {
        None = 0,
        Offensive = 0b0001,
        Defensive = 0b0010,
        Resource = 0b0100,
        Other = 0b1000,
        Any = 0b1111,
    }

    private static Category StatCategory(this UnitStatType unitStatType)
    {
        switch (unitStatType)
        {
            case UnitStatType.PhysicalPower:
            case UnitStatType.SiegePower:
            case UnitStatType.CooldownRecoveryRate:
            case UnitStatType.SpellPower:
            case UnitStatType.PhysicalLifeLeech:
            case UnitStatType.SpellLifeLeech:
            case UnitStatType.PhysicalCriticalStrikeChance:
            case UnitStatType.PhysicalCriticalStrikeDamage:
            case UnitStatType.SpellCriticalStrikeChance:
            case UnitStatType.SpellCriticalStrikeDamage:
            case UnitStatType.DamageVsUndeads:
            case UnitStatType.DamageVsHumans:
            case UnitStatType.DamageVsDemons:
            case UnitStatType.DamageVsMechanical:
            case UnitStatType.DamageVsBeasts:
            case UnitStatType.DamageVsCastleObjects:
            case UnitStatType.DamageVsVampires:
            case UnitStatType.DamageVsLightArmor:
            case UnitStatType.DamageVsVBloods:
            case UnitStatType.DamageVsMagic:
            case UnitStatType.PrimaryAttackSpeed:
            case UnitStatType.PrimaryLifeLeech:
            case UnitStatType.PrimaryCooldownModifier:
            case UnitStatType.BonusPhysicalPower:
            case UnitStatType.BonusSpellPower:
            case UnitStatType.SpellCooldownRecoveryRate:
            case UnitStatType.WeaponCooldownRecoveryRate:
            case UnitStatType.UltimateCooldownRecoveryRate:
            case UnitStatType.MinionDamage:
                return Category.Offensive;
            case UnitStatType.MaxHealth:
            case UnitStatType.PhysicalResistance:
            case UnitStatType.FireResistance:
            case UnitStatType.HolyResistance:
            case UnitStatType.SilverResistance:
            case UnitStatType.SunChargeTime:
            case UnitStatType.SunResistance:
            case UnitStatType.GarlicResistance:
            case UnitStatType.SpellResistance:
            case UnitStatType.Radial_SpellResistance:
            case UnitStatType.PassiveHealthRegen:
            case UnitStatType.ResistVsUndeads:
            case UnitStatType.ResistVsHumans:
            case UnitStatType.ResistVsDemons:
            case UnitStatType.ResistVsMechanical:
            case UnitStatType.ResistVsBeasts:
            case UnitStatType.ResistVsCastleObjects:
            case UnitStatType.ResistVsVampires:
            case UnitStatType.ImmuneToHazards:
            case UnitStatType.HealthRecovery:
            case UnitStatType.PvPResilience:
            case UnitStatType.CCReduction:
            case UnitStatType.DamageReduction:
            case UnitStatType.HealingReceived:
            case UnitStatType.SilverCoinResistance:
                return Category.Defensive;
            case UnitStatType.MovementSpeed:
            case UnitStatType.Vision:
            case UnitStatType.ReducedResourceDurabilityLoss:
            case UnitStatType.FallGravity:
            case UnitStatType.BloodDrain:
            case UnitStatType.BloodEfficiency:
            case UnitStatType.InventorySlots:
                return Category.Other;
            case UnitStatType.ResourcePower:
            case UnitStatType.ResourceYield:
            case UnitStatType.DamageVsWood:
            case UnitStatType.DamageVsMineral:
            case UnitStatType.DamageVsVegetation:
                return Category.Resource;
            // TODO these need to be properly categorised
            case UnitStatType.AbilityAttackSpeed:
            case UnitStatType.IncreasedShieldEfficiency:
            case UnitStatType.TravelCooldownRecoveryRate:
            case UnitStatType.ReducedBloodDrain:
            case UnitStatType.BonusMaxHealth:
            case UnitStatType.BonusMovementSpeed:
            case UnitStatType.BonusShapeshiftMovementSpeed:
            case UnitStatType.BonusMountMovementSpeed:
            case UnitStatType.UltimateEfficiency:
            case UnitStatType.SpellFreeCast:
            case UnitStatType.WeaponFreeCast:
            case UnitStatType.WeaponSkillPower:
            case UnitStatType.FeedCooldownRecoveryRate:
            case UnitStatType.BloodMendHealEfficiency:
            case UnitStatType.DemountProtection:
            case UnitStatType.BloodDrainMultiplier:
            case UnitStatType.CorruptionDamageReduction:
            default:
                return Category.Other;
        }
    }

    public static bool IsOffensiveStat(this UnitStatType unitStatType)
    {
        return (unitStatType.StatCategory() & Category.Offensive) != Category.None;
    }

    public static bool IsDefensiveStat(this UnitStatType unitStatType)
    {
        return (unitStatType.StatCategory() & Category.Defensive) != Category.None;
    }

    public static bool IsResourceStat(this UnitStatType unitStatType)
    {
        return (unitStatType.StatCategory() & Category.Resource) != Category.None;
    }
}