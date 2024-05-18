using BepInEx.Logging;
using XPRising.Systems;
using XPRising.Utils.Prefabs;

namespace XPRising.Utils;

public static class MasteryHelper
{
    public static WeaponMasterySystem.MasteryType GetMasteryTypeForEffect(Effects effect, out bool uncertain)
    {
        uncertain = false;
        switch (effect)
        {
            case Effects.AB_Vampire_Axe_Frenzy_Dash_Hit:
            case Effects.AB_Vampire_Axe_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Axe_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Axe_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Axe_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Axe_XStrike_Toss_Projectile01:
            case Effects.AB_Vampire_Axe_XStrike_Toss_Projectile02:
            case Effects.AB_Vampire_Axe_XStrike_Toss_Projectile03:
            case Effects.AB_Vampire_Axe_XStrike_Toss_Projectile04:
                return WeaponMasterySystem.MasteryType.Axe;
            case Effects.AB_Vampire_Crossbow_IceShard_ForEachVampire_Trigger:
            case Effects.AB_Vampire_Crossbow_IceShard_Trigger:
            case Effects.AB_Vampire_Crossbow_Primary_Mounted_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_VeilOfBlood_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_VeilOfBones_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_VeilOfChaos_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_VeilOfFrost_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_VeilOfIllusion_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_VeilOfShadow_Projectile:
            case Effects.AB_Vampire_Crossbow_Primary_VeilOfStorm_Projectile:
            case Effects.AB_Vampire_Crossbow_RainOfBolts_Trigger:
            case Effects.AB_Vampire_Crossbow_Snapshot_Projectile:
                return WeaponMasterySystem.MasteryType.Crossbow;
            case Effects.AB_Vampire_GreatSword_Mounted_Hit:
            case Effects.AB_Vampire_GreatSword_Primary_Moving_Hit01:
            case Effects.AB_Vampire_GreatSword_Primary_Moving_Hit02:
            case Effects.AB_Vampire_GreatSword_Primary_Moving_Hit03:
                return WeaponMasterySystem.MasteryType.GreatSword;
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_Mounted_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_VeilOfBlood_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_VeilOfBones_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_VeilOfChaos_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_VeilOfFrost_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_VeilOfIllusion_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_VeilOfShadow_Projectile:
            case Effects.AB_Vampire_Longbow_Primary_VeilOfStorm_Projectile:
                return WeaponMasterySystem.MasteryType.LongBow;
            case Effects.AB_Vampire_Mace_CrushingBlow_Slam_Hit:
            case Effects.AB_Vampire_Mace_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Mace_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Mace_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Mace_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Mace_Smack_Hit:
                return WeaponMasterySystem.MasteryType.Mace;
            case Effects.AB_Vampire_Reaper_HowlingReaper_Hit:
            case Effects.AB_Vampire_Reaper_HowlingReaper_Projectile:
            case Effects.AB_Vampire_Reaper_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Reaper_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Reaper_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Reaper_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Reaper_TendonSwing_Twist_Hit:
                return WeaponMasterySystem.MasteryType.Scythe;
            case Effects.AB_Vampire_Slashers_Camouflage_Secondary_Hit:
            case Effects.AB_Vampire_Slashers_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Slashers_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Slashers_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Slashers_Primary_Mounted_Hit:
                return WeaponMasterySystem.MasteryType.Slasher;
            case Effects.AB_Vampire_Spear_Harpoon_Throw_Projectile:
            case Effects.AB_Vampire_Spear_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Spear_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Spear_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Spear_Primary_Mounted_Hit:
            case Effects.AB_Spear_AThousandSpears_Stab_Hit:
            case Effects.AB_Spear_AThousandSpears_Recast_Impale_Hit:
                return WeaponMasterySystem.MasteryType.Spear;
            case Effects.AB_Vampire_Sword_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Sword_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Sword_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Sword_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Sword_Shockwave_Main_Projectile:
            case Effects.AB_Vampire_Sword_Shockwave_Recast_Hit_Trigger:
            case Effects.AB_Vampire_Sword_Shockwave_Recast_TravelEnd:
            case Effects.AB_Vampire_Sword_Shockwave_Recast_TravelToTargetFirstStrike:
            case Effects.AB_Vampire_Sword_Shockwave_Recast_TravelToTargetSecondStrike:
            case Effects.AB_Vampire_Sword_Shockwave_Recast_TravelToTargetThirdStrike:
            case Effects.AB_Vampire_Sword_Whirlwind_Spin_Hit:
            case Effects.AB_Vampire_Sword_Whirlwind_Spin_LastHit:
                return WeaponMasterySystem.MasteryType.Sword;
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Unarmed_Primary_Mounted_Hit:
                return WeaponMasterySystem.MasteryType.Unarmed;
            case Effects.AB_Vampire_VeilOfBlood_BloodNova:
            case Effects.AB_Vampire_VeilOfBones_BounceProjectile:
            case Effects.AB_Vampire_VeilOfChaos_Bomb:
            case Effects.AB_Vampire_VeilOfChaos_SpellMod_BonusDummy_Bomb:
            case Effects.AB_Vampire_VeilOfFrost_AoE:
            case Effects.AB_Vampire_VeilOfIllusion_SpellMod_RecastDetonate:
            case Effects.AB_Blood_Shadowbolt_Projectile:
                return WeaponMasterySystem.MasteryType.Spell;
            case Effects.AB_Vampire_Whip_Dash_Hit:
            case Effects.AB_Vampire_Whip_Entangle_Hit:
            case Effects.AB_Vampire_Whip_Primary_Hit01:
            case Effects.AB_Vampire_Whip_Primary_Hit03:
            case Effects.AB_Vampire_Whip_Primary_Mounted_Hit01:
                return WeaponMasterySystem.MasteryType.Whip;
            case Effects.AB_Vampire_Withered_SlowAttack_Hit:
                uncertain = true;
                return WeaponMasterySystem.MasteryType.Unarmed;
        }

        uncertain = true;
        return WeaponMasterySystem.MasteryType.Unarmed;
    }
}