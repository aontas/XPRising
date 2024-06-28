using System;
using BepInEx.Logging;
using Faction = XPRising.Utils.Prefabs.Faction;
using Units = XPRising.Utils.Prefabs.Units;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Utils;

using Faction = Prefabs.Faction;
using Units = Prefabs.Units;

public static class FactionUnits
{
    public struct Unit
    {
        public Units type { get; }
        public int level { get; }
        public int value { get; }

        public Unit(Units type, int level, int value)
        {
            this.type = type;
            this.level = level;
            this.value = value;
        }
    }

    private static Unit[] bandit_units =
    {
        new(Units.CHAR_Bandit_Rascal, 10, 1),
        new(Units.CHAR_Bandit_Scout, 10, 1),
        new(Units.CHAR_Bandit_Wolf, 14, 1),
        // The worker bandits are fairly lame
        // new(Units.CHAR_Bandit_Worker_Gatherer, 14, 1),
        // new(Units.CHAR_Bandit_Worker_Miner, 14, 1),
        // new(Units.CHAR_Bandit_Worker_Woodcutter, 14, 1),
        new(Units.CHAR_Bandit_Hunter, 16, 2),
        new(Units.CHAR_Bandit_Thug, 16, 2),
        new(Units.CHAR_Bandit_Thief, 18, 3),
        new(Units.CHAR_Bandit_Mugger, 20, 3),
        new(Units.CHAR_Bandit_Trapper, 20, 3),
        new(Units.CHAR_Bandit_Deadeye, 26, 4),
        new(Units.CHAR_Bandit_Stalker, 30, 4),
        new(Units.CHAR_Bandit_Bomber, 32, 4),
        // Units.CHAR_Bandit_Deadeye_Frostarrow_VBlood, // 20
        // Units.CHAR_Bandit_Foreman_VBlood, // 20
        // Units.CHAR_Bandit_StoneBreaker_VBlood, // 20
        // Units.CHAR_Bandit_Stalker_VBlood, // 27
        // Units.CHAR_Bandit_Bomber_VBlood, // 30
        // Units.CHAR_Bandit_Deadeye_Chaosarrow_VBlood, // 30
        // Units.CHAR_Bandit_Tourok_VBlood, // 37
    };

    private static Unit[] church =
    {
        new(Units.CHAR_ChurchOfLight_Miner_Standard, 42, 1),
        new(Units.CHAR_ChurchOfLight_Archer, 56, 1),
        new(Units.CHAR_ChurchOfLight_SlaveRuffian, 60, 1),
        new(Units.CHAR_ChurchOfLight_Cleric, 62, 2),
        new(Units.CHAR_ChurchOfLight_Footman, 62, 2),
        new(Units.CHAR_ChurchOfLight_Rifleman, 62, 2),
        new(Units.CHAR_ChurchOfLight_SlaveMaster_Enforcer, 64, 3),
        new(Units.CHAR_ChurchOfLight_SlaveMaster_Sentry, 64, 3),
        new(Units.CHAR_ChurchOfLight_Knight_2H, 68, 3),
        new(Units.CHAR_ChurchOfLight_Knight_Shield, 68, 3),
        new(Units.CHAR_ChurchOfLight_CardinalAide, 70, 4),
        new(Units.CHAR_ChurchOfLight_Lightweaver, 72, 4),
        new(Units.CHAR_ChurchOfLight_Paladin, 74, 4),
        new(Units.CHAR_ChurchOfLight_Priest, 74, 4),
    };

    private static Unit[] church_elite =
    {
        new(Units.CHAR_Paladin_DivineAngel, 80, 5),
        // Units.CHAR_ChurchOfLight_Sommelier_VBlood, // 70
        // Units.CHAR_ChurchOfLight_Cardinal_VBlood, // 74
        // Units.CHAR_ChurchOfLight_Overseer_VBlood, // 66
        // Units.CHAR_ChurchOfLight_Paladin_VBlood, // 80
    };

    private static Unit[] church_extra =
    {
        new(Units.CHAR_Militia_EyeOfGod, 0, 1) // Spell effect?
    };

    private static Unit[] cultist_units =
    {
        new(Units.CHAR_Scarecrow, 54, 1),
        new(Units.CHAR_Cultist_Pyromancer, 60, 2),
        new(Units.CHAR_Cultist_Slicer, 60, 2),
    };

    private static Unit[] cursed_units =
    {
        new(Units.CHAR_Cursed_MonsterToad, 61, 1),
        new(Units.CHAR_Cursed_ToadSpitter, 61, 1),
        new(Units.CHAR_Cursed_Witch_Exploding_Mosquito, 61, 1),
        new(Units.CHAR_Cursed_MonsterToad_Minion, 62, 1),
        new(Units.CHAR_Cursed_Mosquito, 62, 1),
        new(Units.CHAR_Cursed_Wolf, 62, 1),
        new(Units.CHAR_Cursed_WormTerror, 62, 2),
        new(Units.CHAR_Cursed_Bear_Standard, 64, 2),
        new(Units.CHAR_Cursed_Nightlurker, 64, 2),
        new(Units.CHAR_Cursed_Witch, 72, 3),
        new(Units.CHAR_Cursed_Bear_Spirit, 80, 3),
        new(Units.CHAR_Cursed_Wolf_Spirit, 80, 3),
        new(Units.CHAR_Cursed_MountainBeast_SpiritDouble, 83, 5),
        // Units.CHAR_Cursed_MountainBeast_VBlood, // 83
        // Units.CHAR_Cursed_ToadKing_VBlood, // 64
        // Units.CHAR_Cursed_Witch_VBlood, // 77
    };

    private static Unit[] farmlands =
    {
        new(Units.CHAR_Farmlands_HostileVillager_Female_FryingPan, 28, 1),
        new(Units.CHAR_Farmlands_HostileVillager_Female_Pitchfork, 28, 1),
        new(Units.CHAR_Farmlands_HostileVillager_Male_Club, 28, 1),
        new(Units.CHAR_Farmlands_HostileVillager_Male_Shovel, 28, 1),
        new(Units.CHAR_Farmlands_HostileVillager_Male_Torch, 28, 1),
        new(Units.CHAR_Farmlands_HostileVillager_Male_Unarmed, 28, 1),
        new(Units.CHAR_Farmlands_Woodcutter_Standard, 34, 1),
        new(Units.CHAR_Farmland_Wolf, 40, 1),
    };

    public static Unit[] farmNonHostile =
    {
        new(Units.CHAR_Farmlands_Villager_Female_Sister, 20, 1),
        new(Units.CHAR_Farmlands_Villager_Female, 26, 1),
        new(Units.CHAR_Farmlands_Villager_Male, 26, 1),
        new(Units.CHAR_Farmlands_Farmer, 34, 1),
    };

    public static Unit[] farmFood =
    {
        new(Units.CHAR_Farmlands_SheepOld, 10, 1),
        new(Units.CHAR_Farmlands_SmallPig, 20, 1),
        new(Units.CHAR_Farmlands_Pig, 24, 1),
        new(Units.CHAR_Farmlands_Cow, 30, 1),
        new(Units.CHAR_Farmlands_Sheep, 36, 1),
        new(Units.CHAR_Farmlands_Ram, 38, 1),
    };

    // These levels are inaccurate
    public static Unit[] otherNonHostile =
    {
        new(Units.CHAR_ChurchOfLight_Villager_Female, 1, 1),
        new(Units.CHAR_ChurchOfLight_Villager_Male, 1, 1),
        new(Units.CHAR_Gloomrot_Villager_Female, 1, 1),
        new(Units.CHAR_Gloomrot_Villager_Male, 1, 1),
    };

    private static Unit[] forest =
    {
        new(Units.CHAR_Forest_Wolf, 10, 1),
        new(Units.CHAR_Forest_AngryMoose, 16, 2),
        new(Units.CHAR_Forest_Bear_Standard, 18, 2),
        new(Units.CHAR_Forest_Crow, 14, 1),
        new(Units.CHAR_Ocean_Blowfish, 26, 1),
        new(Units.CHAR_Ocean_Piranha, 26, 1),
        new(Units.CHAR_SeaSerpent, 27, 1),
        new(Units.CHAR_SeaSerpent_Hard, 28, 2),
        // Units.CHAR_Forest_Wolf_VBlood, // 16
        // Units.CHAR_Forest_Bear_Dire_Vblood, // 35
    };

    private static Unit[] gloomrot =
    {
        new(Units.CHAR_Gloomrot_Pyro, 56, 1),
        new(Units.CHAR_Gloomrot_Batoon, 58, 1),
        new(Units.CHAR_Gloomrot_Railgunner, 58, 1),
        new(Units.CHAR_Gloomrot_Tazer, 58, 1),
        new(Units.CHAR_Gloomrot_Technician, 58, 1),
        new(Units.CHAR_Gloomrot_Technician_Labworker, 58, 1),
        new(Units.CHAR_Gloomrot_TractorBeamer, 58, 1),
        new(Units.CHAR_Gloomrot_SentryOfficer, 60, 2),
        new(Units.CHAR_Gloomrot_SentryTurret, 60, 1),
        new(Units.CHAR_Gloomrot_SpiderTank_Driller, 60, 2),
        new(Units.CHAR_Gloomrot_AceIncinerator, 74, 2),
        new(Units.CHAR_Gloomrot_SpiderTank_LightningRod, 74, 4),
        new(Units.CHAR_Gloomrot_SpiderTank_Gattler, 77, 4),
        new(Units.CHAR_Gloomrot_SpiderTank_Zapper, 77, 4),
        // Units.CHAR_Gloomrot_Iva_VBlood, // 60
        // Units.CHAR_Gloomrot_Purifier_VBlood, // 60
        // Units.CHAR_Gloomrot_Voltage_VBlood, // 60
        // Units.CHAR_Gloomrot_TheProfessor_VBlood, // 74
        // Units.CHAR_Gloomrot_RailgunSergeant_VBlood, // 77
        // Units.CHAR_Gloomrot_Monster_VBlood, // 83
    };

    private static Unit[] harpy =
    {
        new(Units.CHAR_Harpy_Dasher, 66, 1),
        new(Units.CHAR_Harpy_FeatherDuster, 66, 1),
        new(Units.CHAR_Harpy_Sorceress, 68, 1),
        new(Units.CHAR_Harpy_Scratcher, 70, 1),
        // Units.CHAR_Harpy_Matriarch_VBlood, // 68
    };

    private static Unit[] militia_units =
    {
        // new(Units.CHAR_Militia_BellRinger, 36, 1),
        new(Units.CHAR_Militia_Hound, 36, 1),
        new(Units.CHAR_Militia_Light, 36, 1),
        new(Units.CHAR_Militia_Torchbearer, 36, 2),
        new(Units.CHAR_Militia_InkCrawler, 38, 2),
        new(Units.CHAR_Militia_Guard, 40, 1),
        new(Units.CHAR_Militia_Bomber, 47, 1),
        new(Units.CHAR_Militia_Longbowman, 42, 3),
        new(Units.CHAR_Militia_Nun, 42, 3),
        new(Units.CHAR_Militia_Miner_Standard, 50, 1),
        new(Units.CHAR_Militia_Heavy, 54, 3),
        new(Units.CHAR_Militia_Devoted, 56, 2),
        new(Units.CHAR_Militia_Crossbow, 70, 2),
        // Units.CHAR_Militia_BishopOfDunley_VBlood, // 57
        // Units.CHAR_Militia_Glassblower_VBlood, // 47
        // Units.CHAR_Militia_Guard_VBlood, // 44
        // Units.CHAR_Militia_Hound_VBlood, // 48
        // Units.CHAR_Militia_HoundMaster_VBlood, // 48
        // Units.CHAR_Militia_Leader_VBlood, // 57
        // Units.CHAR_Militia_Longbowman_LightArrow_Vblood, // 40
        // Units.CHAR_Militia_Nun_VBlood, // 44
        // Units.CHAR_Militia_Scribe_VBlood, // 47
    };

    private static Unit[] vhunter =
    {
        new(Units.CHAR_VHunter_Leader_VBlood, 44, 5),
        new(Units.CHAR_VHunter_Jade_VBlood, 57, 5),
        new(Units.CHAR_VHunter_CastleMan, 65, 5),
    };

    private static Unit[] wtf =
    {
        new(Units.CHAR_ChurchOfLight_Sommelier_BarrelMinion, 50, 1),
        // Units.CHAR_Poloma_VBlood, // 35 - geomancer
        // Units.CHAR_Villager_CursedWanderer_VBlood, // 62
        // Units.CHAR_Villager_Tailor_VBlood, // 40
    };

    private static Unit[] spiders =
    {
        new(Units.CHAR_Spider_Forestling, 20, 1),
        new(Units.CHAR_Spider_Forest, 26, 1),
        new(Units.CHAR_Spider_Baneling, 56, 1),
        new(Units.CHAR_Spider_Spiderling, 56, 1),
        new(Units.CHAR_Spider_Melee, 58, 2),
        new(Units.CHAR_Spider_Range, 58, 2),
        new(Units.CHAR_Spider_Broodmother, 60, 4),
        // Units.CHAR_Spider_Queen_VBlood, // 58
    };

    private static Unit[] golems =
    {
        // Nature?
        new(Units.CHAR_IronGolem, 36, 1),
        new(Units.CHAR_StoneGolem, 36, 1),
        new(Units.CHAR_CopperGolem, 42, 1),
        new(Units.CHAR_RockElemental, 50, 1),
        new(Units.CHAR_Treant, 57, 1),
        new(Units.CHAR_IceElemental, 60, 1),
    };

    private static Unit[] mutants =
    {
        new(Units.CHAR_Mutant_RatHorror, 58, 1),
        new(Units.CHAR_Mutant_FleshGolem, 60, 2),
        new(Units.CHAR_Mutant_Wolf, 64, 1),
        new(Units.CHAR_Mutant_Spitter, 70, 2),
        new(Units.CHAR_Mutant_Bear_Standard, 74, 2),
    };

    private static Unit[] undead_minions =
    {
        new(Units.CHAR_Undead_SkeletonSoldier_TombSummon, 1, 1),
        new(Units.CHAR_Undead_SkeletonSoldier_Withered, 1, 1),
        new(Units.CHAR_Undead_SkeletonCrossbow_Graveyard, 2, 1),
        new(Units.CHAR_Undead_RottingGhoul, 4, 1),
        new(Units.CHAR_Undead_ArmoredSkeletonCrossbow_Farbane, 18, 1),
        new(Units.CHAR_Undead_SkeletonCrossbow_GolemMinion, 18, 1),
        new(Units.CHAR_Undead_SkeletonCrossbow_Farbane_OLD, 20, 1),
        new(Units.CHAR_Undead_SkeletonSoldier_Armored_Farbane, 20, 1),
        new(Units.CHAR_Undead_SkeletonSoldier_GolemMinion, 20, 1),
        new(Units.CHAR_Undead_SkeletonApprentice, 22, 1),
        new(Units.CHAR_Undead_UndyingGhoul, 25, 2),
        new(Units.CHAR_Graveyard_Crow, 27, 1),
        new(Units.CHAR_Undead_Priest, 27, 3),
        new(Units.CHAR_Undead_Ghoul_TombSummon, 30, 1),
        new(Units.CHAR_Undead_FlyingSkull, 32, 2),
        new(Units.CHAR_Undead_Assassin, 35, 3),
        new(Units.CHAR_Undead_ArmoredSkeletonCrossbow_Dunley, 38, 1),
        new(Units.CHAR_Undead_SkeletonGolem, 38, 3),
        new(Units.CHAR_Undead_Ghoul_Armored_Farmlands, 40, 2),
        new(Units.CHAR_Undead_SkeletonSoldier_Armored_Dunley, 40, 2),
        new(Units.CHAR_Undead_SkeletonSoldier_Infiltrator, 40, 1),
        new(Units.CHAR_Undead_Guardian, 42, 2),
        new(Units.CHAR_Undead_Necromancer, 46, 3),
        new(Units.CHAR_Undead_Necromancer_TombSummon, 46, 3),
        new(Units.CHAR_Undead_SkeletonMage, 44, 3),
        new(Units.CHAR_Vampire_Withered, 52, 1),
        new(Units.CHAR_Undead_CursedSmith_FloatingWeapon_Axe, 60, 3),
        new(Units.CHAR_Undead_CursedSmith_FloatingWeapon_Base, 60, 3),
        new(Units.CHAR_Undead_CursedSmith_FloatingWeapon_Mace, 60, 3),
        new(Units.CHAR_Undead_CursedSmith_FloatingWeapon_Slashers, 60, 3),
        new(Units.CHAR_Undead_CursedSmith_FloatingWeapon_Spear, 60, 3),
        new(Units.CHAR_Undead_CursedSmith_FloatingWeapon_Sword, 60, 3),
        new(Units.CHAR_Undead_ShadowSoldier, 60, 2), // hit and disappear
        new(Units.CHAR_Undead_SkeletonSoldier_Base, 60, 1),
        new(Units.CHAR_Undead_GhostMilitia_Crossbow, 63, 2),
        new(Units.CHAR_Undead_GhostMilitia_Light, 63, 2),
        new(Units.CHAR_Undead_ZealousCultist_Ghost, 64, 1),
        new(Units.CHAR_Undead_GhostAssassin, 65, 3),
        new(Units.CHAR_Undead_GhostBanshee, 65, 3),
        new(Units.CHAR_Undead_GhostBanshee_TombSummon, 65, 3),
        new(Units.CHAR_Undead_GhostGuardian, 65, 3),
        // Units.CHAR_Undead_BishopOfDeath_VBlood, // 27
        // Units.CHAR_Undead_BishopOfShadows_VBlood, // 47
        // Units.CHAR_Undead_CursedSmith_VBlood, // 65
        // Units.CHAR_Undead_Infiltrator_VBlood, // 47
        // Units.CHAR_Undead_Leader_Vblood, // 44
        // Units.CHAR_Undead_Priest_VBlood, // 35
        // Units.CHAR_Undead_ZealousCultist_VBlood,
    };

    private static Unit[] werewolves =
    {
        new(Units.CHAR_Farmlands_HostileVillager_Werewolf, 65, 1),
        // new(Units.CHAR_WerewolfChieftain_VBlood, 64, 2),
    };

    private static Unit[] winter =
    {
        new(Units.CHAR_Winter_Wolf, 50, 1),
        new(Units.CHAR_Winter_Moose, 52, 2),
        new(Units.CHAR_Noctem_Crow, 54, 1),
        new(Units.CHAR_Winter_Bear_Standard, 54, 2),
        // Units.CHAR_Wendigo_VBlood, // 57
        // Units.CHAR_Winter_Yeti_VBlood // 74
    };

    // Units friendly to players
    private static Unit[] servants =
    {
        new(Units.CHAR_Bandit_Bomber_Servant, 32, 1),
        new(Units.CHAR_Bandit_Deadeye_Servant, 26, 1),
        new(Units.CHAR_Bandit_Hunter_Servant, 16, 1),
        new(Units.CHAR_Bandit_Miner_Standard_Servant, 14, 1),
        new(Units.CHAR_Bandit_Mugger_Servant, 20, 1),
        new(Units.CHAR_Bandit_Stalker_Servant, 30, 1),
        new(Units.CHAR_Bandit_Thief_Servant, 18, 1),
        new(Units.CHAR_Bandit_Thug_Servant, 16, 1),
        new(Units.CHAR_Bandit_Trapper_Servant, 20, 1),
        new(Units.CHAR_Bandit_Woodcutter_Standard_Servant, 14, 1),
        new(Units.CHAR_Bandit_Worker_Gatherer_Servant, 14, 1),
        new(Units.CHAR_ChurchOfLight_Archer_Servant, 56, 1),
        new(Units.CHAR_ChurchOfLight_Cleric_Servant, 62, 1),
        new(Units.CHAR_ChurchOfLight_Footman_Servant, 62, 1),
        new(Units.CHAR_ChurchOfLight_Knight_2H_Servant, 68, 1),
        new(Units.CHAR_ChurchOfLight_Knight_Shield_Servant, 68, 1),
        new(Units.CHAR_ChurchOfLight_Lightweaver_Servant, 72, 1),
        new(Units.CHAR_ChurchOfLight_Miner_Standard_Servant, 42, 1),
        new(Units.CHAR_ChurchOfLight_Paladin_Servant, 74, 1),
        new(Units.CHAR_ChurchOfLight_Priest_Servant, 74, 1),
        new(Units.CHAR_ChurchOfLight_Rifleman_Servant, 62, 1),
        new(Units.CHAR_ChurchOfLight_SlaveMaster_Enforcer_Servant, 64, 1),
        new(Units.CHAR_ChurchOfLight_SlaveMaster_Sentry_Servant, 64, 1),
        new(Units.CHAR_ChurchOfLight_SlaveRuffian_Servant, 60, 1),
        new(Units.CHAR_ChurchOfLight_Villager_Female_Servant, 50, 1),
        new(Units.CHAR_ChurchOfLight_Villager_Male_Servant, 50, 1),
        new(Units.CHAR_Farmlands_Farmer_Servant, 34, 1),
        new(Units.CHAR_Farmlands_Nun_Servant, 46, 1),
        new(Units.CHAR_Farmlands_Villager_Female_Servant, 26, 1),
        new(Units.CHAR_Farmlands_Villager_Female_Sister_Servant, 20, 1),
        new(Units.CHAR_Farmlands_Villager_Male_Servant, 26, 1),
        new(Units.CHAR_Farmlands_Woodcutter_Standard_Servant, 34, 1),
        new(Units.CHAR_Legion_Assassin_Servant, 83, 1),
        new(Units.CHAR_Legion_NightMaiden_Lesser_Servant, 57, 1),
        new(Units.CHAR_Legion_NightMaiden_Servant, 83, 1),
        new(Units.CHAR_Militia_BellRinger_Servant, 36, 1),
        new(Units.CHAR_Militia_Bomber_Servant, 47, 1),
        new(Units.CHAR_Militia_Crossbow_Servant, 36, 1),
        new(Units.CHAR_Militia_Devoted_Servant, 56, 1),
        new(Units.CHAR_Militia_Guard_Servant, 40, 1),
        new(Units.CHAR_Militia_Heavy_Servant, 54, 1),
        new(Units.CHAR_Militia_Light_Servant, 36, 1),
        new(Units.CHAR_Militia_Torchbearer_Servant, 36, 1),
        new(Units.CHAR_Militia_Longbowman_Servant, 42, 1),
        new(Units.CHAR_Militia_Miner_Standard_Servant, 40, 1),
        new(Units.CHAR_Gloomrot_AceIncinerator_Servant, 72, 1),
        new(Units.CHAR_Gloomrot_Batoon_Servant, 58, 1),
        new(Units.CHAR_Gloomrot_Pyro_Servant, 56, 1),
        new(Units.CHAR_Gloomrot_Railgunner_Servant, 58, 1),
        new(Units.CHAR_Gloomrot_SentryOfficer_Servant, 60, 1),
        new(Units.CHAR_Gloomrot_Tazer_Servant, 58, 1),
        new(Units.CHAR_Gloomrot_Technician_Labworker_Servant, 58, 1),
        new(Units.CHAR_Gloomrot_Technician_Servant, 58, 1),
        new(Units.CHAR_Gloomrot_TractorBeamer_Servant, 58, 1),
        new(Units.CHAR_Gloomrot_Villager_Female_Servant, 50, 1),
        new(Units.CHAR_Gloomrot_Villager_Male_Servant, 50, 1),
        new(Units.CHAR_Paladin_FallenAngel, 80, 1),
        new(Units.CHAR_Spectral_Guardian, 1, 1),
        new(Units.CHAR_Spectral_SpellSlinger, 60, 1),
        new(Units.CHAR_Unholy_DeathKnight, 60, 1),
        new(Units.CHAR_Unholy_FallenAngel, 0, 1), // this "naturally" spawns after CHAR_Paladin_DivineAngel dies
        new(Units.CHAR_Vampire_Cultist_Female_Servant, 50, 1),
        new(Units.CHAR_Vampire_Cultist_Male_Servant, 50, 1),
    };

    private static Unit[] legion =
    {
        new(Units.CHAR_Vampire_Cultist_BloodCrystal, 50, 1),
        new(Units.CHAR_Vampire_Cultist_BloodCrystal_Female, 50, 1),
        new(Units.CHAR_Legion_BatSwarm, 83, 1),
        new(Units.CHAR_Legion_Shadowkin, 83, 1),
        new(Units.CHAR_Legion_Gargoyle_Styx, 83, 2),
        new(Units.CHAR_Legion_Assassin, 83, 3),
        new(Units.CHAR_Legion_BloodProphet, 84, 2),
        new(Units.CHAR_Legion_Dreadhorn, 84, 3),
        new(Units.CHAR_Legion_NightMaiden, 84, 2),
        new(Units.CHAR_Legion_Nightmare, 84, 1),
        new(Units.CHAR_Legion_Vargulf, 84, 1),
        new(Units.CHAR_Vampire_CrimsonIronMaiden, 84, 2),
        new(Units.CHAR_Legion_Gargoyle, 85, 3),
        new(Units.CHAR_Legion_Guardian_DraculaMinion, 85, 3),
    };

    // Unknown levels
    private static Unit[] nether_demons =
    {
        new(Units.CHAR_Harpy_Sorceress_NetherDemon, 60, 1),
        new(Units.CHAR_Treant_NetherDemon, 60, 1),
        new(Units.CHAR_Cursed_Witch_NetherDemon, 80, 1),
        new(Units.CHAR_IronGolem_NetherDemon, 80, 1),
        new(Units.CHAR_RockElemental_NetherDemon, 80, 1),
        new(Units.CHAR_Undead_SkeletonGolem_NetherDemon, 80, 1),
    };

    private static ArraySegment<Unit> GetUnitsForLevel(Unit[] units, int playerLevel)
    {
        var maxUnitLevel = playerLevel + 10;
        // Assuming that any unit array has at least 1 element...
        var i = 1;
        // This assumes that the units are in level order...
        for (; i < units.Length && units[i].level < maxUnitLevel; i++)
        {
        }

        return new ArraySegment<Unit>(units, 0, i);
    }

    public static readonly Faction[] SupportedFactions = {
        Faction.Bandits,
        Faction.ChurchOfLum_Slaves,
        Faction.ChurchOfLum_SpotShapeshiftVampire,
        Faction.Critters,
        Faction.Cursed,
        Faction.Gloomrot,
        Faction.Elementals,
        Faction.Harpy,
        Faction.Legion,
        Faction.Militia,
        Faction.Mutants,
        Faction.Spiders,
        Faction.Undead,
        Faction.Werewolf
    };
    // This should only really handle the "active" factions
    public static ArraySegment<Unit> GetFactionUnits(Faction faction, int playerLevel, int wantedLevel)
    {
        switch (faction)
        {
            case Faction.Bandits:
                return GetUnitsForLevel(bandit_units, playerLevel);
            case Faction.ChurchOfLum_Slaves:
                return GetUnitsForLevel(cultist_units, playerLevel);
            case Faction.ChurchOfLum_SpotShapeshiftVampire:
                return GetUnitsForLevel(church, playerLevel);
            case Faction.Critters:
                return GetUnitsForLevel(playerLevel < 50 ? forest : winter, playerLevel);
            case Faction.Cursed:
                return GetUnitsForLevel(cursed_units, playerLevel);
            case Faction.Gloomrot:
                return GetUnitsForLevel(gloomrot, playerLevel);
            case Faction.Elementals:
                return GetUnitsForLevel(golems, playerLevel);
            case Faction.Harpy:
                return GetUnitsForLevel(harpy, playerLevel);
            case Faction.Militia:
                if (wantedLevel > 3)
                {
                    return GetUnitsForLevel(church, playerLevel);
                }

                if (wantedLevel > 1)
                {
                    return GetUnitsForLevel(militia_units, playerLevel);
                }

                return GetUnitsForLevel(farmlands, playerLevel);
            case Faction.Mutants:
                return GetUnitsForLevel(mutants, playerLevel);
            case Faction.Spiders:
                return GetUnitsForLevel(spiders, playerLevel);
            case Faction.Undead:
                return GetUnitsForLevel(undead_minions, playerLevel);
            case Faction.Werewolf:
                return GetUnitsForLevel(werewolves, playerLevel);
            case Faction.Legion:
                return GetUnitsForLevel(legion, playerLevel);
            case Faction.Bear:
            case Faction.ChurchOfLum:
            case Faction.ChurchOfLum_Slaves_Rioters:
            case Faction.Ignored:
            case Faction.NatureSpirit:
            case Faction.Plants:
            case Faction.Players:
            case Faction.Players_Castle_Prisoners:
            case Faction.Players_Mutant:
            case Faction.Players_Shapeshift_Human:
            case Faction.Spiders_Shapeshifted:
            case Faction.Traders_T01:
            case Faction.Traders_T02:
            case Faction.Unknown:
            case Faction.VampireHunters:
            case Faction.WerewolfHuman:
            case Faction.Wendigo:
            case Faction.Wolves:
            case Faction.World_Prisoners:
            default:
                Plugin.Log(Plugin.LogSystem.Wanted, LogLevel.Warning, $"{Enum.GetName(faction)} units not yet suppported");
                return GetUnitsForLevel(bandit_units, playerLevel);
        }
    }
}