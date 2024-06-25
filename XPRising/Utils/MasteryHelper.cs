using BepInEx.Logging;
using XPRising.Systems;
using XPRising.Utils.Prefabs;

namespace XPRising.Utils;

public static class MasteryHelper
{
    public static GlobalMasterySystem.MasteryType GetMasteryTypeForEffect(int effect, out bool ignore, out bool uncertain)
    {
        ignore = false;
        uncertain = false;
        switch ((Effects)effect)
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
                return GlobalMasterySystem.MasteryType.WeaponAxe;
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
            case Effects.AB_Vampire_Crossbow_RainOfBolts_Throw_Center:
            case Effects.AB_Vampire_Crossbow_RainOfBolts_Throw:
            case Effects.AB_Vampire_Crossbow_Snapshot_Projectile:
            case Effects.AB_Vampire_Crossbow_Snapshot_Projectile_Fork:
                return GlobalMasterySystem.MasteryType.WeaponCrossbow;
            case Effects.AB_Vampire_GreatSword_Mounted_Hit:
            case Effects.AB_Vampire_GreatSword_Primary_Moving_Hit01:
            case Effects.AB_Vampire_GreatSword_Primary_Moving_Hit02:
            case Effects.AB_Vampire_GreatSword_Primary_Moving_Hit03:
            case Effects.AB_GreatSword_GreatCleaver_Hit_01:
            case Effects.AB_GreatSword_LeapAttack_Hit:
                return GlobalMasterySystem.MasteryType.WeaponGreatSword;
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
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile_Focus01:
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile_Focus02:
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile_Focus03:
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile_Return_Focus01:
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile_Return_Focus02:
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile_Return_Focus03:
            case Effects.AB_Vampire_Longbow_GuidedArrow_Projectile_Return:
            case Effects.AB_Longbow_MultiShot_HitBuff:
            case Effects.AB_Longbow_MultiShot_HitBuff_Focus01:
            case Effects.AB_Longbow_MultiShot_HitBuff_Focus02:
            case Effects.AB_Longbow_MultiShot_HitBuff_Focus03:
            case Effects.AB_Longbow_MultiShot_Projectile:
            case Effects.AB_Longbow_MultiShot_Projectile_Focus01:
            case Effects.AB_Longbow_MultiShot_Projectile_Focus02:
            case Effects.AB_Longbow_MultiShot_Projectile_Focus03:
                return GlobalMasterySystem.MasteryType.WeaponLongBow;
            case Effects.AB_Vampire_Mace_CrushingBlow_Slam_Hit:
            case Effects.AB_Vampire_Mace_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Mace_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Mace_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Mace_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Mace_Smack_Hit:
                return GlobalMasterySystem.MasteryType.WeaponMace;
            case Effects.AB_Pistols_Primary_Attack_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_Projectile_Mounted_01:
            case Effects.AB_Pistols_FanTheHammer_Projectile:
            case Effects.AB_Pistols_ExplosiveShot_Shot_Projectile:
            case Effects.AB_Pistols_ExplosiveShot_Shot_ExplosiveImpact:
            case Effects.AB_Pistols_Primary_Attack_VeilOfShadow_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfShadow_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfChaos_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfChaos_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfIllusion_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfBones_Projectile_01:
                return GlobalMasterySystem.MasteryType.WeaponPistol;
            case Effects.AB_Vampire_Reaper_HowlingReaper_Hit:
            case Effects.AB_Vampire_Reaper_HowlingReaper_Projectile:
            case Effects.AB_Vampire_Reaper_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Reaper_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Reaper_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Reaper_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Reaper_TendonSwing_Twist_Hit:
                return GlobalMasterySystem.MasteryType.WeaponScythe;
            case Effects.AB_Vampire_Slashers_Camouflage_Secondary_Hit:
            case Effects.AB_Vampire_Slashers_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Slashers_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Slashers_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Slashers_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Slashers_ElusiveStrike_Dash_PhaseIn:
            case Effects.AB_Vampire_Slashers_ElusiveStrike_Dash_PhaseOut:
            case Effects.AB_Vampire_Slashers_ElusiveStrike_Dash_PhaseOut_TripleDash:
            case Effects.AB_Blood_VampiricCurse_SlashersLegendary_Buff:
                return GlobalMasterySystem.MasteryType.WeaponSlasher;
            case Effects.AB_Vampire_Spear_Harpoon_Throw_Projectile:
            case Effects.AB_Vampire_Spear_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Spear_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Spear_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Spear_Primary_Mounted_Hit:
            case Effects.AB_Spear_AThousandSpears_Stab_Hit:
            case Effects.AB_Spear_AThousandSpears_Recast_Impale_Hit:
                return GlobalMasterySystem.MasteryType.WeaponSpear;
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
                return GlobalMasterySystem.MasteryType.WeaponSword;
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Unarmed_Primary_Mounted_Hit:
                return GlobalMasterySystem.MasteryType.WeaponUnarmed;
            case Effects.AB_Vampire_VeilOfBlood_BloodNova:
            case Effects.AB_Vampire_VeilOfBones_BounceProjectile:
            case Effects.AB_Vampire_VeilOfChaos_Bomb:
            case Effects.AB_Vampire_VeilOfChaos_SpellMod_BonusDummy_Bomb:
            case Effects.AB_Vampire_VeilOfFrost_AoE:
            case Effects.AB_Vampire_VeilOfIllusion_SpellMod_RecastDetonate:
            case Effects.AB_Blood_Shadowbolt_Projectile:
                return GlobalMasterySystem.MasteryType.Spell;
            case Effects.AB_Vampire_Whip_Dash_Hit:
            case Effects.AB_Vampire_Whip_Entangle_Hit:
            case Effects.AB_Vampire_Whip_Primary_Hit01:
            case Effects.AB_Vampire_Whip_Primary_Hit03:
            case Effects.AB_Vampire_Whip_Primary_Mounted_Hit01:
                return GlobalMasterySystem.MasteryType.WeaponWhip;
            // Spell schools
            // Veil
            case Effects.AB_Vampire_VeilOfShadow_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfBlood_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfChaos_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfBones_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfIllusion_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfFrost_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfStorm_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfFrost_SpellMod_IllusionFrostBlast:
            // Blood
            case Effects.AB_Blood_BloodRite_AreaTrigger:
            case Effects.AB_Blood_BloodFountain_Ground_Impact:
            case Effects.AB_Blood_SanguineCoil_Projectile:
            case Effects.AB_Blood_BloodStorm_Projectile:
            case Effects.AB_Blood_BloodStorm_PostBuffAttack:
            case Effects.AB_Blood_VampiricCurse_Buff:
            case Effects.AB_Blood_BloodRite_SpellMod_DamageOnAttackBuff:
            // Chaos
            case Effects.AB_Chaos_Volley_Projectile_First:
            case Effects.AB_Chaos_Volley_Projectile_Second:
            case Effects.AB_Chaos_Aftershock_AreaThrow:
            case Effects.AB_Chaos_Void_Throw:
            case Effects.AB_Chaos_ChaosBarrage_Projectile:
            case Effects.AB_Chaos_ChaosBarrage_Area:
            case Effects.AB_Chaos_Barrier_Recast_Projectile:
            case Effects.AB_Chaos_Barrier_Charges:
            case Effects.AB_Chaos_MercilessCharge_Phase:
            case Effects.AB_Chaos_MercilessCharge_EndImpact:
            case Effects.AB_Chaos_Voidquake_End:
            // Frost
            case Effects.AB_Frost_FrostBat_Projectile:
            case Effects.AB_FrostBarrier_Pulse:
            case Effects.AB_FrostBarrier_Recast_Cone:
            case Effects.AB_Frost_CrystalLance_Projectile:
            case Effects.AB_Frost_IceNova_Throw:
            case Effects.AB_Frost_ColdSnap_Area:
            case Effects.AB_Frost_IceBlockVortex_Delay:
            case Effects.AB_Frost_IceBlockVortex_Buff_Chill:
            case Effects.AB_Frost_Shared_SpellMod_FrostWeapon_Buff:
            // Illusion
            case Effects.AB_Illusion_WraithSpear_Projectile:
            case Effects.AB_Illusion_Mosquito_Area_Explosion:
            case Effects.AB_Illusion_SpectralWolf_Projectile_First:
            case Effects.AB_Illusion_SpectralWolf_Projectile_Bouncing:
            case Effects.AB_Illusion_WispDance_Buff01:
            case Effects.AB_Illusion_WispDance_Buff02:
            case Effects.AB_Illusion_WispDance_Buff03:
            case Effects.AB_Illusion_WispDance_Recast_Projectile:
            // Storm
            case Effects.AB_Storm_EyeOfTheStorm_Throw:
            case Effects.AB_Storm_Discharge_StormShield_Buff_03:
            case Effects.AB_Storm_Discharge_Spellmod_Recast_AreaImpact:
            case Effects.AB_Storm_BallLightning_Projectile:
            case Effects.AB_Storm_BallLightning_AreaImpact:
            case Effects.AB_Storm_PolarityShift_Projectile:
            case Effects.AB_Storm_LightningWall_Object:
            case Effects.AB_Storm_Cyclone_Projectile:
            case Effects.AB_Storm_Discharge_StormShield_Buff_01:
            case Effects.AB_Storm_Discharge_StormShield_Buff_02:
            case Effects.AB_Storm_LightningTyphoon_Hit:
            case Effects.AB_Storm_LightningTyphoon_Projectile:
            case Effects.AB_Storm_RagingTempest_Area_Hit:
            // Unholy
            case Effects.AB_Unholy_CorruptedSkull_Projectile:
            case Effects.AB_Unholy_CorruptedSkull_Projectile_Wave01:
            case Effects.AB_Unholy_CorruptedSkull_Projectile_Wave02:
            case Effects.AB_Unholy_CorruptedSkull_SpellMod_BoneSpirit:
            case Effects.AB_Unholy_CorpseExplosion_Throw:
            case Effects.AB_Unholy_CorpseExplosion_SpellMod_SkullNova_Projectile:
            case Effects.AB_Unholy_WardOfTheDamned_Recast_Cone:
            case Effects.AB_Unholy_Soulburn_Area:
                return GlobalMasterySystem.MasteryType.Spell;
            case Effects.AB_Shapeshift_Bear_MeleeAttack_Hit: // Should this give unarmed mastery?
            case Effects.AB_Bear_Shapeshift_AreaAttack_Hit: // Should this give unarmed mastery?
                return GlobalMasterySystem.MasteryType.WeaponUnarmed;
            // Effects that shouldn't do anything to mastery.
            case Effects.AB_FeedBoss_03_Complete_AreaDamage: // Boss death explosion
            case Effects.AB_ChurchOfLight_Priest_HealBomb_Buff: // Used as the lvl up animation
            case Effects.AB_Charm_Projectile: // Charming a unit 
            case Effects.AB_Charm_Channeling_Target_Debuff: // Charming a unit 
            case Effects.AB_Chaos_Void_SpellMod_BurnDebuff: // Too many ticks 
            case Effects.AB_Blood_HeartStrike_Debuff: // Too many ticks
            case Effects.AB_Storm_RagingTempest_Other_Self_Buff: // Too many ticks
                ignore = true;
                return GlobalMasterySystem.MasteryType.WeaponUnarmed;
            case Effects.AB_Vampire_Withered_SlowAttack_Hit:
                uncertain = true;
                return GlobalMasterySystem.MasteryType.WeaponUnarmed;
        }

        switch ((Remainders)effect)
        {
            // Spell schools
            // Blood
            // Chaos
            case Remainders.Chaos_Vampire_Buff_Ignite:
            case Remainders.Chaos_Vampire_Ignite_AreaImpact:
            case Remainders.Chaos_Vampire_Buff_AgonizingFlames:
            // Frost
            case Remainders.Frost_Vampire_Buff_NoFreeze_Shared_DamageTrigger:
            // Storm
            case Remainders.Storm_Vampire_Buff_Static:
            case Remainders.Storm_Vampire_Static_ChainLightning_Target_01:
            case Remainders.Storm_Vampire_Static_ChainLightning_Target_02:
            case Remainders.Storm_Vampire_Buff_Static_WeaponCharge:
                return GlobalMasterySystem.MasteryType.Spell;
        }
        
        switch ((Buffs)effect)
        {
            case Buffs.Buff_NoBlood_Debuff:
            case Buffs.Buff_General_CurseOfTheForest_Area:
            case Buffs.Buff_General_Silver_Sickness_Burn_Debuff:
            case Buffs.Buff_General_Garlic_Area_Base:
            case Buffs.Buff_General_Garlic_Area_Inside:
            case Buffs.Buff_General_Garlic_Fever:
            case Buffs.Buff_General_Sludge_Poison:
            case Buffs.Buff_General_Holy_Area_T01: // Holy aura damage
            case Buffs.Buff_General_Holy_Area_T02: // Holy aura damage
                ignore = true;
                return GlobalMasterySystem.MasteryType.WeaponUnarmed;
            case Buffs.Buff_General_IgniteLesser: // [Fire] Ignite?
                return GlobalMasterySystem.MasteryType.Spell;
        }

        uncertain = true;
        return GlobalMasterySystem.MasteryType.WeaponUnarmed;
    }
}