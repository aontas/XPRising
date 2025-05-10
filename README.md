# XPRising

This is a revitalisation of RPGMods. It has similar core ideas, with some reduced config complexity, looking to upgrade into the future.

## About

This mod is now comprised of 3 components: XPRising (Server), ClientUI (Client) and XPShared (Server & Client).
The client portion of this mod is entirely optional, but is recommended as it provides good feedback to the user.

#### XPRising
This mod provides the following features:
- Switching from using gear level scheme to and XP based level scheme
- Support for gaining mastery in weapons and bloodlines
- Faction "wanted" system to spawn ambushers
- Support for sending required data to ClientUI

#### ClientUI
This mod provides the following framework features:
- Displaying progress bars (such as for XP or mastery levels)
- Displaying "action" buttons to extend user interaction
- Displaying notification messages (instead of relying on the chat log)

Note: these features are powered by a server-side mod. The server mod needs to support sending the appropriate info to the client to display appropriate UI elements.

#### XPShared
This is a basic plugin mod that contains some shared configuration and logic to support sending the appropriate messages between the server and client.
This will be required on both the server and client.

### XPRising Requirements

- [BepInExPack V Rising](https://thunderstore.io/c/v-rising/p/BepInEx/BepInExPack_V_Rising/) (Server/Client)
- [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/) (Server)

## Documentation

- [ChangeLog](CHANGELOG.md)
- [Command list](Command.md): A full list of commands (when all the systems are configured to be on) can be found here.
- [System documentation](Documentation.md): Each of the systems has some further documentation on this link.
- [Unit stat documentation](UnitStats.md): A list of stats and their effects that can be used for global mastery configuration.

## Contributors

- [Kaltharos](https://github.com/Kaltharos)
- [Dresmyr](https://github.com/Darkon47)
- [Trodi](https://github.com/oscarpedrero)
- [deca](https://github.com/decaprime)
- [aontas](https://github.com/aontas)
- Jason Williams (`SALTYFLEA#3772`)
- [Maicol González](https://github.com/nerzhei)

#### Other thanks

- `Dimentox#1154` (Discord)
- `Nopey#1337` (Discord)
- `syllabicat#0692` (Discord)
- `errox#7604` (Discord)
- Jason Williams (`SALTYFLEA#3772`)
- [adainrivers](https://github.com/adainrivers)
- [Odjit](https://github.com/Odjit)
- [zfolmt](https://github.com/mfoltz)
- `Bromelda` and the [BloodCraft](https://discord.gg/aDh98KtEWZ) server
- `Vex` and the [Vexor World](https://discord.gg/dnVXnHbS) server

#### [V Rising Mod Community](https://discord.gg/vrisingmods) - the premier community for V Rising mods

## By the community, for the community

> It was crucial for us to keep the code open source to ensure excellent support and provide other mod developers the opportunity to develop plugins for XPRising at any time. This project is free and open to everyone, created by the community for the community, and everyone is a part of the development!