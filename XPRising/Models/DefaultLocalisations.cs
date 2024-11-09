using System.Collections.Generic;
using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Models;

public static class DefaultLocalisations
{
    public static readonly L10N.LanguageData LocalisationAU = new L10N.LanguageData()
    {
        language = "en_AU",
        overrideDefaultLanguage = true,
        localisations = new()
        {
            {
                L10N.TemplateKey.AllianceAddSelfError,
                $"You cannot add yourself to your group"
            },
            {
                L10N.TemplateKey.AllianceAlreadyInvited,
                $"{{playerName}} already has a pending invite to this group."
            },
            {
                L10N.TemplateKey.AllianceCurrentInvites,
                $"Current invites:\n{{invites}}"
            },
            {
                L10N.TemplateKey.AllianceGroupEmpty,
                $"Group has no members"
            },
            {
                L10N.TemplateKey.AllianceGroupMembers,
                $"Group members:\n{{members}}"
            },
            {
                L10N.TemplateKey.AllianceGroupIgnore,
                $"You are now ignoring all group invites."
            },
            {
                L10N.TemplateKey.AllianceGroupInfoNone,
                $"You are not currently in a group."
            },
            {
                L10N.TemplateKey.AllianceGroupInvited,
                $"{{playerName}} has invited you to join their group! Type \"{{acceptCommand}}\" to accept or \"{{declineCommand}}\" to reject. No further messages will be sent about this invite."
            },
            {
                L10N.TemplateKey.AllianceGroupLeft,
                $"You have left the group."
            },
            {
                L10N.TemplateKey.AllianceGroupListen,
                $"You are now listening for all group invites."
            },
            {
                L10N.TemplateKey.AllianceGroupLoggedOut,
                $"{{playerName}} has logged out and left your group."
            },
            {
                L10N.TemplateKey.AllianceGroupNull,
                $"You are not currently in a group."
            },
            {
                L10N.TemplateKey.AllianceGroupOtherJoined,
                $"{{playerName}} has joined your group."
            },
            {
                L10N.TemplateKey.AllianceGroupOtherLeft,
                $"{{playerName}} has left your group."
            },
            {
                L10N.TemplateKey.AllianceGroupWipe,
                $"Groups have been wiped."
            },
            {
                L10N.TemplateKey.AllianceIgnoringInvites,
                $"{{playerName}} is currently ignoring group invites. Ask them to change this setting before attempting to make a group."
            },
            {
                L10N.TemplateKey.AllianceInOtherGroup,
                $"{{playerName}} is already in a group. They should leave their current one first."
            },
            {
                L10N.TemplateKey.AllianceInvite404,
                $"Could not find invite. If you have removed other invites, the invite list ID may have changed."
            },
            {
                L10N.TemplateKey.AllianceInviteAccepted,
                $"You have successfully joined the group!"
            },
            {
                L10N.TemplateKey.AllianceInviteGroup404,
                $"The group you are trying to join no longer exists."
            },
            {
                L10N.TemplateKey.AllianceInviteMaxPlayers,
                $"The group has already reached the maximum vampire limit ({{maxGroupSize}})."
            },
            {
                L10N.TemplateKey.AllianceInviteRejected,
                $"You have rejected the invite."
            },
            {
                L10N.TemplateKey.AllianceInviteSent,
                $"{{playerName}} was sent an invite to this group."
            },
            {
                L10N.TemplateKey.AllianceInvitesNone,
                $"You currently have no pending group invites."
            },
            {
                L10N.TemplateKey.AllianceInYourGroup,
                $"{{playerName}} is already in your group."
            },
            {
                L10N.TemplateKey.AllianceMaxGroupSize,
                $"Your group has already reached the maximum vampire limit ({{maxGroupSize}})."
            },
            {
                L10N.TemplateKey.AllianceNoNearPlayers,
                $"No nearby players detected to make a group with."
            },
            {
                L10N.TemplateKey.AlliancePreferences,
                $"Preferences:\n{{preferences}}"
            },
            {
                L10N.TemplateKey.BloodlineMercilessErrorBlood,
                $"<color={Output.DarkRed}>You have no bloodline to get mastery...</color>"
            },
            {
                L10N.TemplateKey.BloodlineMercilessErrorWeak,
                $"<color={Output.DarkRed}>Bloodline is too weak to increase mastery...</color>"
            },
            {
                L10N.TemplateKey.BloodlineMercilessUnmatchedBlood,
                $"<color={Output.DarkRed}>Bloodline is not compatible with yours...</color>"
            },
            {
                L10N.TemplateKey.BloodNoValue,
                $"You haven't developed any bloodline..."
            },
            {
                L10N.TemplateKey.BloodType404,
                $"{{bloodType}} Bloodline not found! Did you typo?"
            },
            {
                L10N.TemplateKey.BloodUnknown,
                $"Unknown user blood type: {{bloodType}}."
            },
            {
                L10N.TemplateKey.DBLoad,
                $"Loading data..."
            },
            {
                L10N.TemplateKey.DBLoadComplete,
                $"Data load complete"
            },
            {
                L10N.TemplateKey.DBLoadError,
                $"Error loading data. Data that failed to load was not overwritten in currently loaded data. See server BepInEx log for details."
            },
            {
                L10N.TemplateKey.DBSave,
                $"Saving data..."
            },
            {
                L10N.TemplateKey.DBSaveComplete,
                $"Data save complete"
            },
            {
                L10N.TemplateKey.DBSaveError,
                $"Error saving data. See server BepInEx log for details."
            },
            {
                L10N.TemplateKey.DBWipe,
                $"Wiping data..."
            },
            {
                L10N.TemplateKey.DBWipeComplete,
                $"Data wipe complete"
            },
            {
                L10N.TemplateKey.DBWipeError,
                $"Error wiping data. See server BepInEx log for details."
            },
            {
                L10N.TemplateKey.GeneralPlayerNotFound,
                $"Could not find specified player \"{{playerName}}\"."
            },
            {
                L10N.TemplateKey.GeneralUnknown,
                $"No default display string for {{key}}"
            },
            {
                L10N.TemplateKey.LocalisationsAvailable,
                $"Available languages: {{languages}}"
            },
            {
                L10N.TemplateKey.LocalisationSet,
                $"Localisation language set to {{language}}"
            },
            {
                L10N.TemplateKey.MasteryAdjusted,
                $"{{masteryType}} Mastery for \"{{playerName}}\" adjusted by <color={Output.White}>{{value}}%</color>"
            },
            {
                L10N.TemplateKey.MasteryDecay,
                $"You've been offline for {{duration}} minute(s). Your mastery has decayed by <color={Output.DarkRed}>{{decay}}%</color>"
            },
            {
                L10N.TemplateKey.MasteryFull,
                $"<color={Output.DarkYellow}>Mastery is at MAX! [ {{masteryType}}: {{currentMastery}}% ]</color>"
            },
            {
                L10N.TemplateKey.MasteryGainOnKill,
                $"<color={Output.DarkYellow}>Mastery has changed after kill:</color>"
            },
            {
                L10N.TemplateKey.MasteryHeader,
                $"-- <color={Output.White}>Weapon Mastery</color> --"
            },
            {
                L10N.TemplateKey.MasteryNoValue,
                $"You haven't even tried to master anything..."
            },
            {
                L10N.TemplateKey.MasteryReset,
                $"Resetting {{masteryType}} Mastery"
            },
            {
                L10N.TemplateKey.MasteryResetFail,
                $"Could not reset {{masteryType}} Mastery. Mastery needs to be above {{value}}% to reset."
            },
            {
                L10N.TemplateKey.MasterySet,
                $"{{masteryType}} Mastery for \"{{playerName}}\" set to <color={Output.White}>{{value}}%</color>"
            },
            {
                L10N.TemplateKey.MasteryType404,
                $"Mastery type not found! did you typo?"
            },
            {
                L10N.TemplateKey.PermissionCommandSet,
                $"Command ({{command}}) required privilege is now set to <color={Output.White}>{{value}}</color>."
            },
            {
                L10N.TemplateKey.PermissionCommandUnknown,
                $"Command ({{command}}) is not recognised as a valid command."
            },
            {
                L10N.TemplateKey.PermissionModifyHigherError,
                $"You cannot set a privilege higher than your own"
            },
            {
                L10N.TemplateKey.PermissionModifySelfError,
                $"You cannot modify your own privilege level."
            },
            {
                L10N.TemplateKey.PermissionNoCommands,
                $"<color={Output.White}>No commands</color>"
            },
            {
                L10N.TemplateKey.PermissionNoUsers,
                $"<color={Output.White}>No permissions</color>"
            },
            {
                L10N.TemplateKey.PermissionPlayerSet,
                $"\"{{playerName}}\" permission is now set to <color={Output.White}>{{value}}</color>."
            },
            {
                L10N.TemplateKey.PlayerInfo,
                $"-- Player Info --"
            },
            {
                L10N.TemplateKey.PlayerInfoAdmin,
                $"Admin: <color={Output.White}>{{admin}}</color>"
            },
            {
                L10N.TemplateKey.PlayerInfoBuffs,
                $"-- <color={Output.White}>Stat buffs</color> --"
            },
            {
                L10N.TemplateKey.PlayerInfoLatency,
                $"Latency: <color={Output.White}>{{value}}</color>s"
            },
            {
                L10N.TemplateKey.PlayerInfoName,
                $"Name: <color={Output.White}>{{playerName}}</color>"
            },
            {
                L10N.TemplateKey.PlayerInfoNoBuffs,
                $"None"
            },
            {
                L10N.TemplateKey.PlayerInfoOffline,
                $"<color={Color.Red}>Offline</color>"
            },
            {
                L10N.TemplateKey.PlayerInfoPosition,
                $"-- Position --"
            },
            {
                L10N.TemplateKey.PlayerInfoSteamID,
                $"SteamID: <color={Output.White}>{{steamID}}</color>"
            },
            {
                L10N.TemplateKey.PowerPointsAvailable,
                $"You have {{value}} power points available"
            },
            {
                L10N.TemplateKey.PowerPointsNotEnough,
                $"You don't have enough power points to redeem"
            },
            {
                L10N.TemplateKey.PowerPointsReset,
                $"Reset all spent power points"
            },
            {
                L10N.TemplateKey.PowerPointsSpendError,
                $"Error spending power points"
            },
            {
                L10N.TemplateKey.PowerPointsSpent,
                $"You spent {{value}} power points ({{remaining}} points remaining)"
            },
            {
                L10N.TemplateKey.PreferenceNotExist,
                $"Preference not recognised ({{preference}})"
            },
            {
                L10N.TemplateKey.PreferenceTextSize,
                $"Text size: {{textSize}}"
            },
            {
                L10N.TemplateKey.PreferenceTitle,
                $"-- Player preferences --"
            },
            {
                L10N.TemplateKey.SystemEffectivenessDisabled,
                $"Effectiveness Subsystem disabled, not resetting {{system}}."
            },
            {
                L10N.TemplateKey.SystemLogDisabled,
                $"{{system}} is no longer being logged."
            },
            {
                L10N.TemplateKey.SystemLogEnabled,
                $"{{system}} is now being logged."
            },
            {
                L10N.TemplateKey.SystemNotEnabled,
                $"{{system}} system is not enabled."
            },
            {
                L10N.TemplateKey.WantedFactionHeatStatus,
                $"<color=#{{colour}}>{{squadMessage}}</color>"
            },
            {
                L10N.TemplateKey.WantedFactionUnsupported,
                $"Faction not yet supported. Supported factions: {{supportedFactions}}"
            },
            {
                L10N.TemplateKey.WantedHeatDataEmpty,
                $"All heat levels 0"
            },
            {
                L10N.TemplateKey.WantedHeatDecrease,
                $"Wanted level decreased ({{factionStatus}})"
            },
            {
                L10N.TemplateKey.WantedHeatIncrease,
                $"Wanted level increased ({{factionStatus}})"
            },
            {
                L10N.TemplateKey.WantedLevelSet,
                $"Player \"{{playerName}}\" wanted value changed"
            },
            {
                L10N.TemplateKey.WantedLevelsNone,
                $"No active wanted levels"
            },
            {
                L10N.TemplateKey.WantedTriggerAmbush,
                $"Successfully triggered ambush check for \"{{playerName}}\""
            },
            {
                L10N.TemplateKey.WantedMinionRemoveError,
                $"Finished with errors (check logs). Removed {{value}} units."
            },
            {
                L10N.TemplateKey.WantedMinionRemoveSuccess,
                $"Finished successfully. Removed {{value}} units."
            },
            {
                L10N.TemplateKey.XpGain,
                $"<color={Output.LightYellow}>You gain {{xpGained}} XP by slaying a Lv.{{mobLevel}} enemy.</color> [ XP: <color={Output.White}>{{earned}}</color>/<color={Output.White}>{{needed}}</color> ]"
            },
            {
                L10N.TemplateKey.XpLevel,
                $"-- <color={Output.White}>Experience</color> --\nLevel: <color={Output.White}>{{level}}</color> (<color={Output.White}>{{progress}}%</color>) [ XP: <color={Output.White}>{{earned}}</color> / <color={Output.White}>{{needed}}</color> ]"
            },
            {
                L10N.TemplateKey.XpLevelUp,
                $"<color={Output.LightYellow}>Level up! You're now level</color> <color={Output.White}>{{level}}</color><color={Output.LightYellow}>!</color>"
            },
            {
                L10N.TemplateKey.XpLost,
                $"You've been defeated, <color={Output.White}>{{xpLost}}</color> XP is lost. [ XP: <color={Output.White}>{{earned}}</color>/<color={Output.White}>{{needed}}</color> ]"
            },
            {
                L10N.TemplateKey.XpSet,
                $"Player \"{{playerName}}\" has their level set to <color={Output.White}>{{level}}</color>"
            },
            {
                L10N.TemplateKey.BarXp,
                $"XP: {{earned}}/{{needed}}"
            },
            {
                L10N.TemplateKey.BarWeaponUnarmed,
                $"Unarmed"
            },
            {
                L10N.TemplateKey.BarWeaponSpear,
                $"Spear"
            },
            {
                L10N.TemplateKey.BarWeaponSword,
                $"1H Sword"
            },
            {
                L10N.TemplateKey.BarWeaponScythe,
                $"Scythe"
            },
            {
                L10N.TemplateKey.BarWeaponCrossbow,
                $"Crossbow"
            },
            {
                L10N.TemplateKey.BarWeaponMace,
                $"Mace"
            },
            {
                L10N.TemplateKey.BarWeaponSlasher,
                $"Slasher"
            },
            {
                L10N.TemplateKey.BarWeaponAxe,
                $"Axe"
            },
            {
                L10N.TemplateKey.BarWeaponFishingPole,
                $"Fishing pole"
            },
            {
                L10N.TemplateKey.BarWeaponRapier,
                $"Rapier"
            },
            {
                L10N.TemplateKey.BarWeaponPistol,
                $"Pistol"
            },
            {
                L10N.TemplateKey.BarWeaponGreatSword,
                $"2H Sword"
            },
            {
                L10N.TemplateKey.BarWeaponLongBow,
                $"Longbow"
            },
            {
                L10N.TemplateKey.BarWeaponWhip,
                $"Whip"
            },
            {
                L10N.TemplateKey.BarSpell,
                $"Spell"
            },
            {
                L10N.TemplateKey.BarBloodNone,
                $"Frail blood"
            },
            {
                L10N.TemplateKey.BarBloodBrute,
                $"Brute blood"
            },
            {
                L10N.TemplateKey.BarBloodCreature,
                $"Creature blood"
            },
            {
                L10N.TemplateKey.BarBloodDracula,
                $"Dracula blood"
            },
            {
                L10N.TemplateKey.BarBloodDraculin,
                $"Draculin blood"
            },
            {
                L10N.TemplateKey.BarBloodMutant,
                $"Mutant blood"
            },
            {
                L10N.TemplateKey.BarBloodRogue,
                $"Rogue blood"
            },
            {
                L10N.TemplateKey.BarBloodScholar,
                $"Scholar blood"
            },
            {
                L10N.TemplateKey.BarBloodWarrior,
                $"Warrior blood"
            },
            {
                L10N.TemplateKey.BarBloodWorker,
                $"Worker blood"
            },
            {
                L10N.TemplateKey.BarFactionBandits,
                $"Bandits"
            },
            {
                L10N.TemplateKey.BarFactionCritters,
                $"Nature"
            },
            {
                L10N.TemplateKey.BarFactionGloomrot,
                $"Gloomrot"
            },
            {
                L10N.TemplateKey.BarFactionLegion,
                $"Legion of Blood"
            },
            {
                L10N.TemplateKey.BarFactionMilitia,
                $"Church Militia"
            },
            {
                L10N.TemplateKey.BarFactionUndead,
                $"The Undead"
            },
            {
                L10N.TemplateKey.BarFactionWerewolf,
                $"Werewolves"
            },
        }
    };

    public static readonly L10N.LanguageData LocalisationPirate = new L10N.LanguageData()
    {
        language = "en_PIRATE",
        overrideDefaultLanguage = false,
        localisations = new()
        {
            {
                L10N.TemplateKey.XpGain,
                $"<color={Output.LightYellow}>YAAARRR! Ye gained {{xpGained}} XP by murderin' a Lv.{{mobLevel}} swashbuckler!</color> [ XP: <color={Output.White}>{{earned}}</color>/<color={Output.White}>{{needed}}</color> ]"
            },
            {
                L10N.TemplateKey.XpLevelUp,
                $"<color={Output.LightYellow}>ME HEARTY! Ye level be more! Yer level is now </color> <color={Output.White}>{{level}}</color><color={Output.LightYellow}>!</color>"
            },
            {
                L10N.TemplateKey.XpLost,
                $"Ye walked the plank! Yer XP be lost. [ XP: <color={Output.White}>{{earned}}</color>/<color={Output.White}>{{needed}}</color> ]"
            },
        }
    };
    
    public static readonly List<L10N.LanguageData> AllDefaultLocalisations =
        new() { LocalisationAU, LocalisationPirate };
}