# XPRising

> This is a revitalisation of RPGMods. It has similar core ideas, with some reduced config complexity, looking to upgrade into the future.

# XPRising Update for 1.0

> Initial release for 1.0 current in testing. Check the releases page for recent (pre-release) versions.

# XPRising Requirements

- [BepInExPack V Rising](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/) (Server)
- [BloodStone](https://v-rising.thunderstore.io/package/deca/Bloodstone/) (Server)
- [VampireCommandFramework](https://v-rising.thunderstore.io/package/deca/VampireCommandFramework/) (Server)

# Further documentation

A full list of commands (when all the systems are configured to be on) can be found [here](Command.md)

Documentation for the individual systems can be found [here](Documentation.md). It is worth noting that while both the weapon and bloodline mastery systems\
are intended to provide bonus stats, this is a work in progress and they don't actually provide anything yet.


# Credits

The RPG mod was initially developed by [Kaltharos](https://github.com/Kaltharos).

Other contributors:
`Dimentox#1154` (Discord), `Nopey#1337` (Discord), `syllabicat#0692` (Discord), `errox#7604` (Discord), Jason Williams (`SALTYFLEA#3772`), [Trodi](https://github.com/oscarpedrero), [Dresmyr](https://github.com/Darkon47), [aontas](https://github.com/aontas)

Some code snippets have been lifted from The Random Encounters mod, developed by [adainrivers](https://github.com/adainrivers/randomencounters).

[V Rising Mod Community](https://discord.gg/vrisingmods) - the premier community for V Rising mods.

# By the community, for the community

> It was crucial for us to keep the code open source to ensure excellent support and provide other mod developers the opportunity to develop plugins for XPRising at any time. This project is free and open to everyone, created by the community for the community, and everyone is a part of the development!

# Patch notes / Changelog

- 0.1.0 Initial update for VRising 1.0
- 0.1.1
  - Fix crash when trying to determine nearby allies (for group xp/heat)
  - Added a new command to temporarily bypass the lvl 20 requirement for the "Getting ready for the Hunt" journal
  - Fixed bloodline mastery logging to only log when you have it enabled
  - Changed the mod icon
  - Linked the other documentation files to README.md