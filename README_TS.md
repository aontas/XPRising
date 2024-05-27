XPRising is a server mod that replaces the gear level system with a more traditional levelling system, where you gain XP for killing mobs.

It also includes some systems for gaining mastery over weapons and bloodlines, as well a "Wanted" system that tracks how negative a faction perceives the player.

### Installation

- Install [BepInEx](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/).
- Install [VCF](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/).
- Extract `XPRising.dll` into `(VRising folder)/BepInEx/plugins`.

### Configuration

The base configuration and data files will be generated after the game launches the first time.

#### Main config: `(VRising folder)/BepInEx/config/XPRising.cfg`
This file can be used to enable/disable the systems in this mod. See the config file for more documentation.

#### Secondary config folder: `(VRising folder)/BepInEx/config/XPRising_(Server name)/`
This folder will contain the configuration files for the enabled systems. Any save with the same name will load the same config files.

#### Data folder: `(VRising folder)/BepInEx/config/XPRising_(Server name)/Data/`
This folder contains data files used by this mod.

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