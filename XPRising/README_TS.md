# XPRising

## About

This mod provides a mechanism for players to have their level set by gaining XP in the world, primarily by killing enemies.

There is an optional (but recommended) [companion UI](https://thunderstore.io/c/v-rising/p/XPRising/ClientUI/) that supports displaying XP bars and notifications for players.

## Features

### XP system
- Players gain experience by killing enemies
- Admin configurable setting for giving players stat bonuses for each level

### Mastery system
- Allows players to accrue "mastery" towards weapons and bloodlines
- Mastery systems allow stat bonuses to be applied to players

### Wanted system
- A system that tracks player kills against different factions in the game and causes factions to ambush players with enemies as their "heat" level increases.

### Installation

- Install [BepInEx](https://thunderstore.io/c/v-rising/p/BepInEx/BepInExPack_V_Rising/).
- Install [VCF](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/).
- Install [XPShared](https://thunderstore.io/c/v-rising/p/XPRising/XPShared/).
- Extract `XPRising.dll` into `(VRising folder)/BepInEx/plugins`.

### Configuration

The base configuration and data files will be generated after the game launches the first time.
You can use the chat commands listed in [Commands.md](https://github.com/aontas/XPRising/blob/main/Command.md) for some admin tasks and for users to display more data/logging.   

#### Main config: `(VRising folder)/BepInEx/config/XPRising.cfg`
This file can be used to enable/disable the systems in this mod. See the config file for more documentation.

#### Secondary config folder: `(VRising folder)/BepInEx/config/XPRising_(Server name)/`
This folder will contain the configuration files for the enabled systems. Any save with the same server name will load the same config files.

#### Data folder: `(VRising folder)/BepInEx/config/XPRising_(Server name)/Data/`
This folder contains data files used by this mod.

### Language folder: `(VRising folder)/BepInEx/config/XPRising_(Server name)/Languages/`
This folder contains localisation files that can be used to add new localisations.

#### Security config
For group/dedicated servers, it is recommended that an admin grants themselves higher permission early on (run the `.paa` command as admin).
This will allow that user to grant higher privilege levels to other users and configure privilege levels of commands.
See [Commands.md](https://github.com/aontas/XPRising/blob/main/Command.md) for the default command/privilege list.

### Documentation

Documentation can be found [here](https://github.com/aontas/XPRising/blob/main/Documentation.md).

### Support

Join the [modding community](https://vrisingmods.com/discord) and add a post in the technical-support channel.

### Changelog

Found [here](https://github.com/aontas/XPRising/blob/main/CHANGELOG.md)

### Donations

If you would like to make a donation, you can do so through [Kofi](https://ko-fi.com/aontas)