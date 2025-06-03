# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

- `Added` for new features.
- `Changed` for changes in existing functionality.
- `Deprecated` for soon-to-be removed features.
- `Removed` for now removed features.
- `Fixed` for any bug fixes.
- `Security` in case of vulnerabilities.

## [0.4.10] - 2025-06-04

### Added

- Added support for allowing all mastery gain to reduce as mastery increase (separate from prestige mechanics). The `Mastery Gain Reduction` configuration option in `GlobalMasteryConfig.cfg` can be used to allow a linear mastery gain (across 0-100%) to having the mastery gain reduced to 0 as the mastery reaches 100%.

### Fixed

- Fixed XP gain on mobs that should get 0 XP
- Updated support for ambush mobs so that Bloodcraft will treat them as normal mobs and give an appropriate amount of XP

## [0.4.9] - 2025-05-30

### Changed

- Updated documentation for bloodline mastery to make it match current behaviour and describe it clearer

### Fixed

- Fixed issue that would double apply growth reduction for bloodline mastery when feeding on V Bloods
- Fixed configuration folder normalisation to ensure folder paths are more consistent/usable

## [0.4.8] - 2025-05-30

### Changed

- Updated UnitStats.md to fix some stat inconsistencies

### Fixed

- Ensured that if there is an error during initialisation that we print out the message, rather than just the stack trace

## [0.4.7] - 2025-05-27

### Changed

- Change XP scaling outside a valid "level range". Default level range is 30, which gives plenty of scope for normal play but effectively restricts players from cheesing high level mobs when low level to massively boost their level.

### Fixed

- Fixed support for keeping level bonuses configured in `globalMasteryConfig.json` when mastery config preset is set to something other than `custom`
- Boss feeding event explosions no longer trigger any mastery changes
- Added handling for potential edge case when accessing allied player lookup

## [0.4.6] - 2025-05-24

### Changed

- Changed values used to generate weapon mastery gain. This should be a smoother mastery gain experience now.

### Fixed

- Fixed crash that could occur when feeding on enemies
- Added support for more abilities for mastery gain.
- All CHAR_* damage events should now correctly be caught. These would only occur when damaging the summonable steed of Sir Fabian, but they should all be handled appropriately now.

## [0.4.5] - 2025-05-21

### Added

- Added support for "corruption" blood mastery to be used in mastery API commands

### Fixed

- Updated support for more player attacks
- Fixed support for detecting blood type changes
- Fixed ability to disable weapon mastery system

## [0.4.4] - 2025-05-19

### Changed

- Updated to latest VCF and unhollowed dependencies

### Fixed

- Improved support for tracking mastery against more skills
- Fixed output message parameterisation when attempting to set player preferences with an incorrect preference name

## [0.4.3] - 2025-05-18

### Added

- Added support for setting a preference for progress bar colours for ClientUI. This can be done via the `xpc` command. For example, `.xpc colours red,#00ff00,#00f` will set the XP bar to red, mastery bars to green and blood mastery bars to blue. You can use an empty settings value to reset this.

### Fixed

- Fixed support for setting buff values for mastery/level bonuses.

## [0.4.2] - 2025-05-16

### Added

- Retry connection and Hide UI buttons to ClientUI when it fails to connect to the server

### Fixed

- Fixed changing endgame necklaces from changing player level
- Further improved support for detecting claw attacks

## [0.4.1] - 2025-05-13

### Added

- Exposed group XP buff config within the `ExperienceConfig.cfg` to allow users to change this value.

### Fixed

- Fixed mastery for new weapons so that they actually put mastery into the correct group.

## [0.4.0] - 2025-05-10

### Added

- Add base support for weapons and blood types new to v1.1 (claws, daggers, twin blades, corrupted blood)

### Changed

- Client <-> Server communication now achieved via chat (similar to Bloodcraft. Thanks [zfolmt](https://github.com/mfoltz))
- A number of systems have moved between different system patches as the old systems either don't work or don't work in the same way
- Removed dependency on [Bloodstone](https://thunderstore.io/c/v-rising/p/deca/Bloodstone/). This is due to not being needed as the Client -> System data communication is no longer being used. Extracted the GameFrame from Bloodstone as I'd rather have fewer dependencies getting up to speed for v1.1.
- Changed player level being stored in WeaponLevel instead of SpellLevel. This is due to the spell slot doing weird stuff and not yet being able to find an appropriate hook to detect equipped slot changes consistently. This seems to make the level experience more consistent 

### Fixed

- Fixed up mod for v1.1 of V Rising

## [0.3.10] - 2025-03-31

### Added

- Added support for a text panel

### Fixed

- Dragging now doesn't require multiple clicks when dragging a second panel
- Fixed some minor inconsistencies when displaying resize cursor hints

## [0.3.9] - 2024-11-10

### Added

- Added support for resetting a single mastery at a time. Users can now use `.m r spell` to reset spell mastery.
- Added support for translations when showing mastery types in chat responses. This might require resetting the language files as `Spell mastery` will be changed to `Spell`.

### Changed

- Removed some unnecessary includes in the project files.
- Removed unneeded section of `release.yml`

### Removed

- Removed the `bloodline` command group. This has been merged with `mastery` command group.

## [0.3.8] - 2024-10-13

### Fixed

- Ensure units spawned with a lifetime of a multiple of 10 (and no decimal value) do not trigger attempting to spawn them at level 0. Internal support for spawning wanted units at level 0 has been dropped to help make sure this is the case (and spawning units at level 0 would rarely ever be useful in practise).

## [0.3.7] - 2024-10-03

### Fixed

- Fixed display of wanted level in UI. It now shows the percentage through a heat level correctly.

## [0.3.6] - 2024-10-02

### Added

- Added localisation support for strings visible in the experience bars
- Added support for disabling the buff on level up (better fix should be coming soon)

### Fixed

- Fixed legion heat generation so that legion ambush kills now correctly lower heat instead of raising it
- Fixed UI initialisation so that initial panel locations load correctly regardless of screen scale

## [0.3.5] - 2024-09-29

### Added

- XP floating text now appears when the player kills something. This may have strange behaviour for group XP, but seems OK so far. Shout out to [@zfolmt](https://github.com/mfoltz)
- Added support for disabling drag/resize anchor in the UI. This can be accessed via the expanded action buttons.

### Changed

- Reduced Legion heat generation as it was too high. Should be about 33% reduced.
- Increased value of some Legion units so they don't spawn in too large numbers at higher wanted levels

### Fixed

- Fixed Legion wanted spawning faction to spawn as the legion faction (instead of Vampire Hunter)
- Fixed error log reported at high heat levels (6*)
- Fixed applying mastery buffs when the XP buff is removed from the global config

## [0.3.4] - 2024-09-18

### Fixed

- Fixed/updated the mod to support V Rising v1.0.9.0

## [0.3.3] - 2024-09-17

### Changed

- Improved the consistency of some minor log messages

### Fixed

- Fixed initialisation and connection of the UI client to the server

## [0.3.2] - 2024-09-08

### Added

- Wanted levels are now saved between server restarts
- Added setting to allow heat to be retained when player is killed

### Changed

- Improved squads spawn in for the legion faction across a greater cross-section of player levels
- Legion faction now does not spawn in as VampireHunters. This means they will no longer attack other legion members, but will attack other factions as per their normal behaviour.
- Multiple changes to "Merciless Bloodlines" option:
  - When toggled off, players will now receive bloodline mastery for all kills, rather than just feeding kills
  - When toggled on, players will now receive bloodline mastery for all feeding kills (even when bloodlines don't match).
- Messages shown in the UI notifications should generally no longer also be shown in the chat log

### Fixed

- UI progress bar now updates when drinking a potion for a different type of blood

## [0.3.1] - 2024-08-16

### Changed

- Wanted levels now show a star value in the UI, instead of just the number
- Mastery gain now takes into account weapon/skill specific attributes to smooth out mastery gain between weapons and spells (for example, the spear skill "A Thousand Spears" will give less mastery per hit than a basic hit from a mace)

### Fixed

- Wanted levels are now shown correctly in the UI (percentage and bar filled)
- UI messages now no longer continue their timers when not visible
- Wanted levels now correct update in the UI on player death
- Fixed display of wanted level colours in the UI

## [0.3.0] - 2024-08-08

### Added

- Added ClientUI and XPShared mods to support showing XP/Mastery/Wanted progress bars in the Client UI.\
This is driven by the XPRising server mod to ensure that there is a single source of data and the UI can just focus on doing UI things.

### Changed

- Wanted level now decreases consistently over time

### Fixed

- Mastery sometimes would not be counted when a mob was killed in a single blow. This should now correctly track those Mastery increases as well.

## [0.2.4] - 2024-06-29

### Added

- Legion faction now supported by wanted system
- Added support for users setting text size personal preference. This has moved all user preferences to the same database config section, which will cause all user settings to be reset (eg, logging/language preferences, NOT xp/mastery levels).
- Added support for a `range` buff type for mastery buffs
- Added support for admins to set up the buffs provided for gaining levels. This is currently set in the `Data\globalMasteryConfig.json`, but this file name at least will change in the future.

### Fixed

- Militia faction will no longer spawn CHAR_Paladin_DivineAngel. This has been reworked a bit to instead spawn in other strong church units.
- More non-hostile villagers have been added to the list to not give XP and give extra wanted heat
- Global Mastery Config
  - `ratio` type buffs with `requiredMastery` > 0 now provide the buff starting at 0 once the player reaches the required mastery level (eg, required mastery of 50% -> buff is 0 until 50%, 0.5 at 75%, 1.0 at 100%)
  - `fixed` type buffs on `activeBonus` now provide a flat multiplier on the weapon stat, instead of a flat addition. This is to keep it more consistent with the `ratio` type.
  - Mastery effectiveness is now applied correctly on both `baseBonus` and `activeBonus`
  
## [0.2.3] - 2024-06-26

### Added

- Dracula blood type added to mastery list

### Changed

- All text output by the mod has better support for being resized. Soon to be a user preference instead of a server preference.
- Localisation changes:
  - MasteryGainOnKill => removed string substitution sections from template (this is done elsewhere)
- Blood specific logging has been merged with mastery logging
- Now has a setting to get V Blood to improve X random blood types, instead of just the current blood. This can be set from 1 random type to all types. Setting this config to 0 will make the V Blood only apply to the current blood type.

### Fixed

- Fixed mastery (weapon and blood) not being applied due to not being logged
- Fixed percentage stats displayed by `.pb`
- Improved effectiveness/growth display for mastery
- Feeding on V Blood now correctly improves blood mastery

## [0.2.2] - 2024-06-24

### Added

- New UnitStats documentation for giving some basic example of stat type bonuses

### Changed

- Greatly reduced many of the default mastery buff stats, as they were way too high.

### Fixed

- Fixed mastery gain not being applied unless mastery logging is turned on
- Fixed error message in log file if weapon/blood mastery systems are not enabled
- Changing blood types via blood potions now re-applies the correct blood mastery
- Level 0 can now correctly gain more than 1 xp per kill
- Mastery buffs no longer attempt to be applied if the corresponding system is turned off (weapon/blood mastery)
- Fixed localisation loading to correctly load the data

## [0.2.1] - 2024-06-20

### Changed

- Mastery gains at full mastery now report that it is full
- Localisation keys changed:
  - Removed: BloodAdjusted, BloodlineDecay, BloodlineHeader, BloodlineMasteryGain, BloodReset, BloodSet, MasteryGain
  - Added: MasteryGainOnKill, MasteryFull, PlayerInfo

### Fixed

- Fixed loading language files when deprecated/unknown localisation keys are present
- Fixed ensuring mastery buffs are being correctly applied on weapon switching
- Fixed support for displaying long messages in the chat box
- Fixed requiredMastery buffs to apply at the correct required mastery

## [0.2.0] - 2024-06-18

### Changed

- Significant overhaul to mastery/bloodline system. These are now merged into a singular "mastery" system.
- Merged/removed a large number of commands to support mastery system change
- Replaced `.xp bump20` command with `.xp questSkip`. This will only be needed for users currently stuck on that quest. All future users should "magically" just skip past it. (Thanks @mfoltz for ideas)

### Added

- Added support for buffs when gaining mastery. There is a myriad of config available to set buffs up. Check out `GlobalMasteryConfig.cfg` and `Data\globalMasteryConfig.json`. It can support a similar config to what was previously available in RPGMod, but it also supports providing "fixed" buffs at specific levels, using the equipped weapon as a base for buffs, with more options coming soon. Thanks @nerzhei for ideas/support!

## [0.1.15] - 2024-06-11

### Added

- Added a `DestroyWhenDisabled` flag to ambush units so they disappear if not engaged

### Changed

- Close allies now returns closest `MaxGroupSize` players, which can be a mix of clan and group players

### Fixed

- Fixed ambush squad faction to correctly be Vampire Hunters (instead of shape-shifted vampires)
- Fixed display of `.playerinfo`. Also added `.playerbuffs` to just show buff info.
- Added a check when sending messages to players to ensure they exist correctly

## [0.1.14] - 2024-06-10

### Fixed

- Ambush squads will now have a much closer level to players (especially if the player is high level)
- Groups will no longer double up with clans. This is to better enforce max group sizes.
- Fixed ambush colour text replacement example in default localisations
- Improved consistency of setting faction and level for ambush squads

### Changed

- Ambushers are now scared of V Blood bosses: they will only spawn 100m away from a boss (configurable)
- XP group range default value is now 40m (this is more in line with unit draw distance)
- Group XP calculation now uses avg group level (or player level if they are higher) to calculate XP. This provides a better levelling experience in a group as it no longer double penalises any level disparity.
- XP calculation now caps any negative level difference so the user can get always get some useful amount of XP (even if small)

## [0.1.13] - 2024-05-31

### Fixed

- Improved config initialisation and prevented initialisation failures to overwrite existing mod config/data

### Added

- Users can now create/update their own custom language localisations by copying/editing the `XPRising/Languages/example_localisation_template.json` file.
  Adding more language files will enable users to optionally select those languages as their displayed language for in-game messages output by this mod.
  Known issue: Wanted system ambush flavour text currently has no support for localisation.

### Changed

- Death XP loss is now calculated as % of current level XP, instead of % of total XP.

## [0.1.12] - 2024-05-29

### Added

- Initial implementation of localisation for XP gain 

## [0.1.11] - 2024-05-28

### Fixed

- Fixed allowing authed admins to have max permission. This allows them to correctly use `.paa`.
- Fixed Thunderstore deployment (or it will be soon!)

### Added

- All admin commands are logged. This can be changed by setting the `Command log privilege level = 100` in the XPRising.cfg file.

### Changed

- Auto-saving is no longer chatty.

### Removed

- No longer offer the option of human-readable percentage stats. They are now always in the human-readable format. Not that there are any at the moment.

## [0.1.10] - 2024-05-28

### Changed

- Removed admin permission requirement for all permissions. Added a new command (`.paa`) as the only admin command to set the privilege level of the current user to the highest value.
  This allows the configuration of permissions to happen solely within the game.

## [0.1.9] - 2024-05-26

### Fixed

- Fix `.xp bump20` command and the infrequent but similar crash when changing magic sources

## [0.1.8] - 2024-05-25

### Fixed

- Second attempt at fixing player level, this time including support for items breaking and then repairing them while equipped.

## [0.1.7] - 2024-05-25

### Added

- Added support for capping the XP gain for a kill. Admins can now set a max percentage-of-level that users cannot gain more than.
- Added a maximum to player group sizes. This can be configured in the base settings. Note that this is for custom groups, not clans.
- Improved support for more triggers for gaining mastery on hits
- Now support having different config folders per world, so you can have multiple local worlds. This does currently require that the world names be unique.

### Fixed

- Fixed gear levels from interfering with level granted from experience. This is done in a way that will allow the system to be turned off if needed.
- Fixed support for being able to disable all individual systems included in mod. This was mostly fixed for the Experience system, but small changes improved for other systems as well.
- When players level up, the XP buff is now updated to provide the correct buffs for that level. This is still just HP at this stage.

### Changed

- Group XP calculation uses the highest level of the players in proximity, rather than using an average level. This ensures a more consistent play experience.
- Bloodline mastery can now only be gained by feeding. Completing a feed will give more bloodline mastery than killing part-way through.
- Updated BepInEx dependencies
- Updated documentation

## [0.1.6] - 2024-05-22

### Added

- `.playerinfo` now also displays the buffs of the given player

### Fixed

- Fix brute blood buff > 30% strength from messing with player levels. It will still add a single level to maintain the bloodline power, but won't change the level more than that.
- DB initialisation/loading will now correctly initialise the data for hardcoded defaults

## [0.1.5] - 2024-05-21

### Fixed

- Fix re-setting of XP to the end of the previous level when joining a server

## [0.1.4] - 2024-05-19

### Added

- Added support for more spell types when checking for weapon mastery on hit

### Fixed

- Fixed auto-save frequency. This is now also logged on server start.
- Min/Max XP and level calculations have been improved and some edge cases for these have been fixed

### Changed

- Updated command detection to better match VCF (allowing commands with same name but different required args to co-exist)

### Security

- Split playerinfo command into personal and other player queries to allow higher privilege requirements to look at other player data. Users can no longer use this info to track down other players.

## [0.1.3] - 2024-05-19

### Added

- Weapon mastery is now primarily added on hit, instead of in-combat/on death

### Fixed

- Fixed not being able to be allocated an odd level (only even ones)
- Fixed player level flipping between values

### Changed

- Updated dependency versions

## [0.1.2] - 2024-05-18

### Added

- Added support to load starting XP for player characters directly from the server configuration options.
  Server admins can now set lowest level via this setting.
- Improved auto-save config to aid admin configuration

### Fixed

- `group add` command can now be successfully run
- Fixed bug with attempting to read Weapon mastery data from internal database
- Bloodine mastery logging now correctly only happens if the player has enabled it
- Fixed auto-saving to correctly only log that it is saving when it is saving
- Stopped start-up logs from complaining about debug functions not provided to players
- Fixed white text colouring in messages to players
- Fixed saving alliance/custom group user preferences

### Changed

- Added this ChangeLog
- Updated documentation for clarity

## [0.1.1] - 2024-05-17

### Added

- Added a new command to temporarily bypass the lvl 20 requirement for the "Getting ready for the Hunt" journal

### Fixed

- Fix crash when trying to determine nearby allies (for group xp/heat)
- Fixed bloodline mastery logging to only log when you have it enabled

### Changed

- Changed the mod icon
- Linked the other documentation files to README.md

## [0.1.0] - 2024-05-16

### Changed

- Changed name to XPRising
- 0.1.0 Initial update for VRising 1.0