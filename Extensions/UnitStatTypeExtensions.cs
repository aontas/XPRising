using ProjectM;

namespace XPRising.Extensions;

public static class UnitStatTypeExtensions
{
    public static bool IsOffensiveStat(this UnitStatType unitStatType)
    {
        switch (unitStatType)
        {
            case UnitStatType.PhysicalPower:
            case UnitStatType.SpellPower:
            case UnitStatType.AttackSpeed:
            case UnitStatType.MinionDamage:
            case UnitStatType.DamageVsBeasts:
            case UnitStatType.DamageVsDemons:
            case UnitStatType.DamageVsHumans:
            case UnitStatType.DamageVsMagic:
            case UnitStatType.DamageVsMechanical:
            case UnitStatType.DamageVsUndeads:
            case UnitStatType.DamageVsVampires:
            case UnitStatType.PhysicalLifeLeech:
            case UnitStatType.PrimaryLifeLeech:
            case UnitStatType.SpellCooldownRecoveryRate:
            case UnitStatType.SpellLifeLeech:
            case UnitStatType.WeaponCooldownRecoveryRate:
            case UnitStatType.UltimateCooldownRecoveryRate:
            case UnitStatType.SpellCriticalStrikeChance:
            case UnitStatType.SpellCriticalStrikeDamage:
            case UnitStatType.PhysicalCriticalStrikeChance:
            case UnitStatType.PhysicalCriticalStrikeDamage:
            case UnitStatType.DamageVsVBloods:
            case UnitStatType.DamageVsLightArmor:
            case UnitStatType.DamageVsCastleObjects:
            case UnitStatType.DamageVsWood:
            case UnitStatType.ResourcePower:
                return true;
            default:
                return false;
        }
    }
}