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

## [Pre-release]

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