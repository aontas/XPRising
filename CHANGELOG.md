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