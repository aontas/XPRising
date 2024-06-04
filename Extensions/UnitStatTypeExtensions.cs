using ProjectM;

namespace XPRising.Extensions;

public static class UnitStatTypeExtensions
{
    private enum Category
    {
        Offensive,
        Defensive,
        Resource,
        Other
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
            case UnitStatType.AttackSpeed:
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
            case UnitStatType.ShieldAbsorb:
                return Category.Defensive;
            case UnitStatType.MovementSpeed:
            case UnitStatType.EnergyGain:
            case UnitStatType.MaxEnergy:
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
            default:
                return Category.Other;
        }
    }

    public static bool IsOffensiveStat(this UnitStatType unitStatType)
    {
        return unitStatType.StatCategory() == Category.Offensive;
    }

    public static bool IsDefensiveStat(this UnitStatType unitStatType)
    {
        return unitStatType.StatCategory() == Category.Defensive;
    }

    public static bool IsResourceStat(this UnitStatType unitStatType)
    {
        return unitStatType.StatCategory() == Category.Resource;
    }
}