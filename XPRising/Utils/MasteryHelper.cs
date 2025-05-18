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
            case Effects.AB_Vampire_Claws_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Claws_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Claws_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Claws_Primary_MeleeAttack_Unholy_Hit01:
            case Effects.AB_Vampire_Claws_Primary_MeleeAttack_Unholy_Hit02:
            case Effects.AB_Vampire_Claws_Primary_MeleeAttack_Unholy_Hit03:
            case Effects.AB_Vampire_Claws_Primary_Mounted_Hit:
            case Effects.AB_Vampire_Claws_Puncture_Area:
            case Effects.AB_Vampire_Claws_SkeweringLeap_HitBox_Buff:
            case Effects.AB_Vampire_Claws_SkeweringLeap_Unholy_HitBox_Buff:
            case Effects.AB_Vampire_Claws_VaultSlash_Hit:
            case Effects.AB_Vampire_Claws_VaultSlash_Unholy_Hit:
                return GlobalMasterySystem.MasteryType.WeaponClaws;
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
            case Effects.AB_Vampire_Daggers_Primary_Mounted_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_VeilOfBlood_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_VeilOfBones_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_VeilOfChaos_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_VeilOfFrost_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_VeilOfIllusion_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_VeilOfShadow_Projectile:
            case Effects.AB_Vampire_Daggers_Primary_VeilOfStorm_Projectile:
            case Effects.AB_Vampire_Daggers_RainOfDaggers_Throw:
            case Effects.AB_Vampire_Daggers_Shared_GroundDagger_Homing:
            case Effects.AB_Vampire_Daggers_Shared_GroundDagger_Primary:
            case Effects.AB_Vampire_Daggers_Shared_GroundDagger_RainOfDaggers:
                return GlobalMasterySystem.MasteryType.WeaponDaggers;
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
            case Effects.AB_Pistols_Primary_Attack_Projectile_Mounted_02:
            case Effects.AB_Pistols_FanTheHammer_Projectile:
            case Effects.AB_Pistols_ExplosiveShot_Shot_Projectile:
            case Effects.AB_Pistols_ExplosiveShot_Shot_ExplosiveImpact:
            case Effects.AB_Pistols_Primary_Attack_VeilOfBlood_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfBlood_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfBones_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfBones_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfChaos_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfChaos_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfFrost_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfFrost_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfIllusion_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfIllusion_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfShadow_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfShadow_Projectile_02:
            case Effects.AB_Pistols_Primary_Attack_VeilOfStorm_Projectile_01:
            case Effects.AB_Pistols_Primary_Attack_VeilOfStorm_Projectile_02:
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
            case Effects.AB_Vampire_TwinBlades_Javelin_Projectile:
            case Effects.AB_Vampire_TwinBlades_Javelin_Throw:
            case Effects.AB_Vampire_TwinBlades_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_TwinBlades_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_TwinBlades_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_TwinBlades_Primary_Mounted_Hit:
            case Effects.AB_Vampire_TwinBlades_SweepingStrike_Hit:
            case Effects.AB_Vampire_TwinBlades_SweepingStrike_Melee:
                return GlobalMasterySystem.MasteryType.WeaponTwinblades;
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit01:
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit02:
            case Effects.AB_Vampire_Unarmed_Primary_MeleeAttack_Hit03:
            case Effects.AB_Vampire_Unarmed_Primary_Mounted_Hit:
                return GlobalMasterySystem.MasteryType.WeaponClaws;
            case Effects.AB_Vampire_Whip_Dash_Hit:
            case Effects.AB_Vampire_Whip_Entangle_Hit:
            case Effects.AB_Vampire_Whip_Primary_Hit01:
            case Effects.AB_Vampire_Whip_Primary_Hit03:
            case Effects.AB_Vampire_Whip_Primary_Mounted_Hit01:
                return GlobalMasterySystem.MasteryType.WeaponWhip;
            // Spell schools
            // Veil
            case Effects.AB_Vampire_VeilOfBlood_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfBlood_BloodNova:
            case Effects.AB_Vampire_VeilOfBones_BounceProjectile:
            case Effects.AB_Vampire_VeilOfBones_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfChaos_Bomb:
            case Effects.AB_Vampire_VeilOfChaos_SpellMod_BonusDummy_Bomb:
            case Effects.AB_Vampire_VeilOfChaos_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfFrost_AoE:
            case Effects.AB_Vampire_VeilOfFrost_SpellMod_IllusionFrostBlast:
            case Effects.AB_Vampire_VeilOfFrost_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfIllusion_SpellMod_RecastDetonate:
            case Effects.AB_Vampire_VeilOfIllusion_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfShadow_TriggerBonusEffects:
            case Effects.AB_Vampire_VeilOfStorm_TriggerBonusEffects:
            // Blood
            case Effects.AB_Blood_BloodRite_AreaTrigger:
            case Effects.AB_Blood_BloodFountain_Ground_Impact:
            case Effects.AB_Blood_SanguineCoil_Projectile:
            case Effects.AB_Blood_BloodStorm_Projectile:
            case Effects.AB_Blood_BloodStorm_PostBuffAttack:
            case Effects.AB_Blood_VampiricCurse_Buff:
            case Effects.AB_Blood_Shadowbolt_Projectile:
            case Effects.AB_Blood_Shadowbolt_VampiricCurse_Buff:
            case Effects.AB_Blood_BloodRite_SpellMod_DamageOnAttackBuff:
            case Effects.AB_Blood_CrimsonBeam_Channel:
            case Effects.AB_Blood_BloodFountain_Spellmod_Recast_Ground_Impact:
            case Effects.AB_Blood_BloodRite_Projectile:
            case Effects.AB_Blood_Shadowbolt_SpellMod_Area:
            case Effects.AB_Blood_Shadowbolt_SpellMod_ForkProjectile:
            case Effects.AB_Blood_VampiricCurse_Buff_Lesser:
            case Effects.AB_Blood_VampiricCurse_Projectile:
            case Effects.AB_Blood_VampiricCurse_SpellMod_Area:
            // Chaos
            case Effects.AB_Chaos_Aftershock_AreaThrow:
            case Effects.AB_Chaos_Aftershock_GreatSword_Projectile:
            case Effects.AB_Chaos_Aftershock_Projectile:
            case Effects.AB_Chaos_Aftershock_SpellMod_KnockbackArea:
            case Effects.AB_Chaos_Barrier_Charges:
            case Effects.AB_Chaos_Barrier_Recast_AreaImpact:
            case Effects.AB_Chaos_Barrier_Recast_Projectile:
            case Effects.AB_Chaos_ChaosBarrage_Area:
            case Effects.AB_Chaos_ChaosBarrage_Channel:
            case Effects.AB_Chaos_ChaosBarrage_Projectile:
            case Effects.AB_Chaos_MercilessCharge_EndImpact:
            case Effects.AB_Chaos_MercilessCharge_Phase:
            case Effects.AB_Chaos_RainOfChaos_SpellMod_BurnDebuff:
            case Effects.AB_Chaos_RainOfChaos_SpellMod_MegaMeteor:
            case Effects.AB_Chaos_RainOfChaos_Throw_Meteor:
            case Effects.AB_Chaos_RainOfChaos_Throw_Meteor_Center:
            case Effects.AB_Chaos_Void_SpellMod_BurnArea:
            case Effects.AB_Chaos_Void_SpellMod_ClusterBomb:
            case Effects.AB_Chaos_Void_Throw:
            case Effects.AB_Chaos_Voidquake_End:
            case Effects.AB_Chaos_Volley_Projectile_First:
            case Effects.AB_Chaos_Volley_Projectile_Second:
            // Frost
            case Effects.AB_Frost_ArcticLeap_FrostSpikes_01:
            case Effects.AB_Frost_ArcticLeap_FrostSpikes_02:
            case Effects.AB_Frost_ArcticLeap_FrostSpikes_03:
            case Effects.AB_Frost_ArcticLeap_FrostSpikes_04:
            case Effects.AB_Frost_ColdSnap_Area:
            case Effects.AB_Frost_CrystalLance_Projectile_SpellMod_Pierce:
            case Effects.AB_Frost_CrystalLance_Projectile:
            case Effects.AB_Frost_FrostBat_AoE:
            case Effects.AB_Frost_FrostBat_Projectile:
            case Effects.AB_Frost_IceBlockVortex_Buff_Chill:
            case Effects.AB_Frost_IceBlockVortex_Delay:
            case Effects.AB_Frost_IceNova_RingArea:
            case Effects.AB_Frost_IceNova_SpellMod_Recast_Throw:
            case Effects.AB_Frost_IceNova_Throw:
            case Effects.AB_Frost_Shared_SpellMod_FrostWeapon_Buff:
            case Effects.AB_FrostBarrier_Pulse:
            case Effects.AB_FrostBarrier_Recast_Cone:
            // Illusion
            case Effects.AB_Illusion_Mosquito_Area_Explosion:
            case Effects.AB_Illusion_Mosquito_Summon:
            case Effects.AB_Illusion_PhantomAegis_SpellMod_Explode:
            case Effects.AB_Illusion_SpectralGuardian_Summon_Cast:
            case Effects.AB_Illusion_SpectralWolf_Projectile_Bouncing:
            case Effects.AB_Illusion_SpectralWolf_Projectile_First:
            case Effects.AB_Illusion_WispDance_Buff01:
            case Effects.AB_Illusion_WispDance_Buff02:
            case Effects.AB_Illusion_WispDance_Buff03:
            case Effects.AB_Illusion_WispDance_Recast_Channel:
            case Effects.AB_Illusion_WispDance_Recast_Projectile:
            case Effects.AB_Illusion_WraithSpear_Projectile:
            // Storm
            case Effects.AB_Storm_BallLightning_AreaImpact:
            case Effects.AB_Storm_BallLightning_Projectile:
            case Effects.AB_Storm_Cyclone_Projectile:
            case Effects.AB_Storm_Discharge_Projectile:
            case Effects.AB_Storm_Discharge_Spellmod_Recast_AreaImpact:
            case Effects.AB_Storm_Discharge_StormShield_Buff_01:
            case Effects.AB_Storm_Discharge_StormShield_Buff_01_MagicSourceVariation:
            case Effects.AB_Storm_Discharge_StormShield_Buff_02:
            case Effects.AB_Storm_Discharge_StormShield_Buff_02_MagicSourceVariation:
            case Effects.AB_Storm_Discharge_StormShield_Buff_03:
            case Effects.AB_Storm_Discharge_StormShield_HitBuff:
            case Effects.AB_Storm_EyeOfTheStorm_Cast:
            case Effects.AB_Storm_EyeOfTheStorm_Throw:
            case Effects.AB_Storm_LightningTendrils_Projectile:
            case Effects.AB_Storm_LightningTyphoon_Hit:
            case Effects.AB_Storm_LightningTyphoon_Projectile:
            case Effects.AB_Storm_LightningWall_Cast:
            case Effects.AB_Storm_LightningWall_Object:
            case Effects.AB_Storm_LightningWall_SpellMod_Shield_HitBuff:
            case Effects.AB_Storm_LightningWall_SpellMod_Snare_HitBuff:
            case Effects.AB_Storm_LightningWall_SpellMod_Static_HitBuff:
            case Effects.AB_Storm_PolarityShift_Projectile:
            case Effects.AB_Storm_PolarityShift_SpellMod_AreaImpactDestination:
            case Effects.AB_Storm_RagingTempest_Area_Hit:
            case Effects.AB_Storm_RagingTempest_LightningStrike:
            // Unholy
            case Effects.AB_Unholy_ArmyOfTheDead_Cast:
            case Effects.AB_Unholy_ChainsOfDeath_Cast:
            case Effects.AB_Unholy_ChainsOfDeath_Channeling_Target_Debuff:
            case Effects.AB_Unholy_ChainsOfDeath_Projectile:
            case Effects.AB_Unholy_ChainsOfDeath_Slow_01:
            case Effects.AB_Unholy_ChainsOfDeath_Slow_02:
            case Effects.AB_Unholy_ChainsOfDeath_Slow_03:
            case Effects.AB_Unholy_ChainsOfDeath_Slow_04:
            case Effects.AB_Unholy_ChainsOfDeath_Spellmod_Area:
            case Effects.AB_Unholy_ChainsOfDeath_Spellmod_BoneSpirit:
            case Effects.AB_Unholy_ChainsOfDeath_SpellMod_SkullNova_HitBuff:
            case Effects.AB_Unholy_ChainsOfDeath_SpellMod_SkullNova_Projectile:
            case Effects.AB_Unholy_CorpseExplosion_SpellMod_DoubleImpact:
            case Effects.AB_Unholy_CorpseExplosion_SpellMod_SkullNova_HitBuff:
            case Effects.AB_Unholy_CorpseExplosion_SpellMod_SkullNova_Projectile:
            case Effects.AB_Unholy_CorpseExplosion_Throw:
            case Effects.AB_Unholy_CorruptedSkull_Projectile:
            case Effects.AB_Unholy_CorruptedSkull_Projectile_Wave01:
            case Effects.AB_Unholy_CorruptedSkull_Projectile_Wave02:
            case Effects.AB_Unholy_CorruptedSkull_SpellMod_BoneSpirit:
            case Effects.AB_Unholy_CorruptedSkull_SpellMod_LesserProjectile:
            case Effects.AB_Unholy_DeathKnight_Summon:
            case Effects.AB_Unholy_Shared_SpellMod_SkeletonBomb_Impact:
            case Effects.AB_Unholy_Soulburn_Area:
            case Effects.AB_Unholy_SummonFallenAngel_Cast:
            case Effects.AB_Unholy_UnstableArarchnid_Explode_Hit:
            case Effects.AB_Unholy_UnstableArarchnid_Explode_Small_Hit:
            case Effects.AB_Unholy_WardOfTheDamned_Recast_Cone:
            case Effects.AB_Unholy_WardOfTheDamned_Buff:
                return GlobalMasterySystem.MasteryType.Spell;
            case Effects.AB_Shapeshift_Bear_MeleeAttack_Hit: // Should this give unarmed mastery?
            case Effects.AB_Bear_Shapeshift_AreaAttack_Hit: // Should this give unarmed mastery?
                return GlobalMasterySystem.MasteryType.WeaponClaws;
            // Effects that shouldn't do anything to mastery.
            case Effects.AB_FeedBoss_03_Complete_AreaDamage: // Boss death explosion
            case Effects.AB_ChurchOfLight_Priest_HealBomb_Buff: // Used as the lvl up animation
            case Effects.AB_Charm_Projectile: // Charming a unit 
            case Effects.AB_Charm_Channeling_Target_Debuff: // Charming a unit 
            case Effects.AB_Chaos_Void_SpellMod_BurnDebuff: // Too many ticks 
            case Effects.AB_Blood_HeartStrike_Debuff: // Too many ticks
            case Effects.AB_Storm_RagingTempest_Other_Self_Buff: // Too many ticks
            case Effects.AB_HighLordSword_SelfStun_Projectile: // Hitting boss with own sword
            case Effects.AB_Lucie_PlayerAbility_ClarityPotion_Throw_Throw: // Throwing potion back to boss
            case Effects.AB_Lucie_PlayerAbility_LiquidFirePotion_Throw_Throw: // Throwing potion back to boss
            case Effects.AB_Lucie_PlayerAbility_LiquidLuckPotion_Throw_Throw: // Throwing potion back to boss
            case Effects.AB_Lucie_PlayerAbility_PolymorphPotion_Throw_Throw: // Throwing potion back to boss
            case Effects.AB_Lucie_PlayerAbility_Potion_Base_Throw_Throw: // Throwing potion back to boss
            case Effects.AB_Lucie_PlayerAbility_WondrousHealingPotion_Throw_Throw: // Throwing potion back to boss
            // ignore weapon coatings
            case Effects.AB_Vampire_Coating_Blood_Area:
            case Effects.AB_Vampire_Coating_Chaos_Area:
            case Effects.AB_Vampire_Coating_Frost_Area:
            case Effects.AB_Vampire_Coating_Frost_Stagger_Buff:
            case Effects.AB_Vampire_Coating_Illusion_Buff:
            case Effects.AB_Vampire_Coating_Storm_Buff:
            case Effects.AB_Vampire_Coating_Unholy_BoneSpirit_HitBuff:
            case Effects.AB_Vampire_Coating_Unholy_Buff:
                ignore = true;
                return GlobalMasterySystem.MasteryType.None;
            case Effects.AB_Vampire_Withered_SlowAttack_Hit:
                uncertain = true;
                return GlobalMasterySystem.MasteryType.None;
        }

        switch ((Remainders)effect)
        {
            // Spell schools
            // Blood
            // Chaos
            case Remainders.Chaos_Vampire_Buff_Ignite:
            case Remainders.Chaos_Vampire_Ignite_AreaImpact:
            case Remainders.Chaos_Vampire_Buff_AgonizingFlames:
            case Remainders.Chaos_Vampire_Combust_AreaImpact:
            case Remainders.Chaos_Vampire_Ignite_AreaImpact_Soulshard:
            // Frost
            case Remainders.Frost_Vampire_Buff_NoFreeze_Shared_DamageTrigger:
            case Remainders.Frost_Vampire_Splinter_Projectile:
            // Storm
            case Remainders.Storm_Vampire_Buff_Static:
            case Remainders.Storm_Vampire_Static_ChainLightning:
            case Remainders.Storm_Vampire_Static_ChainLightning_Target_01:
            case Remainders.Storm_Vampire_Static_ChainLightning_Target_02:
            case Remainders.Storm_Vampire_Static_ChainLightning_Target_03:
            case Remainders.Storm_Vampire_Buff_Static_WeaponCharge:
                return GlobalMasterySystem.MasteryType.Spell;
        }
        
        switch ((Buffs)effect)
        {
            case Buffs.Buff_NoBlood_Debuff:
            case Buffs.Buff_General_CurseOfTheForest_Area:
            case Buffs.Buff_General_Silver_Sickness_Burn_Debuff:
            case Buffs.Buff_General_Garlic_Area_Inside:
            case Buffs.Buff_General_Garlic_Fever:
            case Buffs.Buff_General_Sludge_Poison:
            case Buffs.Buff_General_Holy_Area_T01: // Holy aura damage
            case Buffs.Buff_General_Holy_Area_T02: // Holy aura damage
                ignore = true;
                return GlobalMasterySystem.MasteryType.None;
            case Buffs.Buff_General_Corruption_Area_Debuff_T01:
            case Buffs.Buff_General_Corruption_Area_Debuff_T01_Healing:
            case Buffs.Buff_General_Corruption_Area_T01:
            case Buffs.Buff_General_Corruption_Area_T01_Healing:
                Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Warning, $"{effect} has been through mastery type as being ignored - check this");
                ignore = true;
                return GlobalMasterySystem.MasteryType.None;
            // As these are DoTs, discard them
            case Buffs.Buff_General_IgniteLesser: // [Fire] Ignite?
            case Buffs.Buff_General_Ignite:
                ignore = true;
                return GlobalMasterySystem.MasteryType.Spell;
        }

        // Effects to validate/check
        switch (effect)
        {
            // Should this spell just contribute to spell damage?
            case 123399875: // Spell_Corruption_Tier3_Snare_Throw (TODO: put this in a file)
            // Not sure why units are appearing in this list. They always seem to be from user -> CHAR_Militia_Fabian_VBlood?
            case (int)Units.CHAR_Gloomrot_SpiderTank_Zapper:
            case (int)Units.CHAR_Unholy_SkeletonWarrior_Summon:
            case (int)Units.CHAR_Gloomrot_TractorBeamer:
                ignore = true;
                return GlobalMasterySystem.MasteryType.None;
        }

        uncertain = true;
        return GlobalMasterySystem.MasteryType.None;
    }
}