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