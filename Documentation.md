## Experience System

Disable the V Rising Gear Level system and replace it with a traditional RPG experience system,
complete with exp sharing between clan members or other players designated as allies.

By default, player HP will increase by a minor amount each level.
To configure the player level bonus:
- Set `Mastery Config Preset` to `custom` in `BepInEx\config\XPRising_XXXXX\Data\GlobalMasteryConfig.cfg`
- Edit the `xpBuffConfig` section in the generated config in `BepInEx\config\XPRising_XXXXX\Data\globalMasteryConfig.json`
  - Note that this config is only generated after running the server once
  - See [UnitStats](UnitStats.md) for more configuration documentation.
- The maximum level difference a mob can be compared to the player so that they can receive appropriate XP is governed by `LevelRange`. Players will receive less XP from mobs that are lower level, dropping to a minimum value at the specified range. Players will receive more XP from mobs that are higher level, until a peak is reached, which will then drop back down to the standard value before dropping further.
  - At 0 level difference, this is the standard XP
  - At +range level difference, the player receives minimal XP
  - At -range level difference, the player receives the same XP as at 0 level difference. Between 0 difference and -range difference, XP gain increases then decreases in a single sawtooth pattern. This is intended as a protection against abusing unbound level difference XP gains. 

## Mastery System
The mastery system allows players to get extra buffs as they master weapons/bloodlines/spells.
Increasing mastery of any type can now progressively give extra bonuses to the character's stats.
<details>

### Weapon Mastery
Weapon/spell mastery will increase when the weapon/spell is used to damage a creature. This mastery will be granted when that creature is killed. If the player leaves combat before the creature is killed, this mastery is lost.

### Blood Mastery
Feeding on enemies will progress the mastery of that bloodline. If the feeding is cancelled, to kill your victim, a smaller amount of mastery is granted.
V Bloods will give increase mastery improvements.

#### Configuration

| Option                        | Value | Documentation                                                                                                   |
|-------------------------------|-------|-----------------------------------------------------------------------------------------------------------------|
| Merciless Bloodlines          | true  | Victim blood quality needs to be of a higher value than blood mastery to gain mastery                           |
|                               | false | Mastery always improves when the level is less than 100%                                                        |
| V Blood improves X bloodlines | 0     | Player's current blood type is used to determine what blood mastery to increase when feeding on a V Blood       |
|                               | 10    | All blood types gain mastery when feeding on a V Blood                                                          |
|                               | X     | X randomly chosen blood types gain mastery when feeding on a V Blood (for who knows what the V Blood contains?) |
| Mastery Gain Multiplier       | X     | Mastery gain is multiplied by this value. Can be used to increase/decrease mastery gain.                        |
| VBlood Mastery Multiplier     | X     | Bonus V Blood mastery multiplier (this applies to weapon mastery as well)                                       |

### Mastery buff configuration
The buffs provided by the mastery system can be configured two ways: there are some preset options for quick configuration, or there is the custom configuration which allows great flexibility.

Current preset options can be found in `GlobalMasteryConfig.cfg`

Note that any configuration other than `custom` will result in the `BepInEx\config\XPRising_XXXXX\Data\globalMasteryConfig.json` file being overwritten on launch. On first launch, you can set the preset, then change it to `custom` after to allow edits to the base config.

See [UnitStats](UnitStats.md) for more configuration documentation.

### Mastery Decay
When the vampire goes offline, all their mastery will continuously decay until they come back online. This can be disabled.

### Effectiveness System
Effectiveness acts as a multiplier for the mastery. The initial effectiveness starts at 100%.
When mastery is reset using ".mastery reset <type>", the current mastery level is added to effectiveness and then is set to 0%.
As the vampire then increases in mastery, the effective mastery is `mastery * effectiveness`.

Effectiveness is specific for each mastery.

### Growth System
The growth system is used to determine how fast mastery can be gained at higher levels of effectiveness.
This means that higher effectiveness will slow to mastery gain (at 1, 200% effectiveness gives a mastery growth rate of 50%).
Config supports modifying the rate at which this growth slows. Set growth per effectiveness to 0 to have no change in growth. Higher numbers make the growth drop off slower.
Negative values have the same effect as positive (ie, -1 == 1 for the growth per effectiveness setting).

This is only relevant if the effectiveness system is turned on.

</details>

## Wanted System
<details>
A system where every NPC you kill contributes to a wanted level system. As you kill more NPCs from a faction,
your wanted level will rise higher and higher.

As your wanted level increases, more difficult squads of ambushers will be sent by that faction to kill you.
Wanted levels for will eventually cooldown the longer you go without killing NPCs from a faction, so space out
your kills to ensure you don't get hunted by an extremely elite group of assassins.

Another way of lowering your wanted level is to kill Vampire Hunters.

Otherwise, if you are dead for any reason at all, your wanted level will reset back to 0. This behaviour can be modified by editing the "Heat percentage lost on death" option in the `BepInEx\config\XPRising_XXXXX\WantedConfig.cfg` file.
```
Note:
- Ambush may only occur when the player is in combat.
- All mobs spawned by this system is assigned to Faction_VampireHunters, except for the legion
```
</details>

## Clans and Groups and XP sharing
Killing with other vampires can share XP and wanted heat levels within the group.

A vampire is considered in your group if they are in your clan or if you use the `group` commands to create a group with
them. A group will only share XP if the members are close enough to each other, governed by the `Ally Max Distance` configuration.
There is a configurable maximum number of players that can be added using the `group` commands.

<details>
<summary>Experience</summary>
Group XP is awarded based on the ratio of the average group level to the sum of the group level. It is then multiplied
by a bonus value `( 1.2^(group size - 1) )`, up to a maximum of `1.5`.
</details>

<details>
<summary>Heat</summary>
Increases in heat levels are applied uniformly for every member in the group.
</details>

## Localisation
There is support for users to create their own localisation files for this mod.\
The example template can be found here: `BepInEx\config\XPRising_XXXXX\Languages\example_localisation_template.json`

This file can be copied and modified to include additional languages. To set the new language as the default language selected for all users, set the `overrideDefaultLanguage` setting to `true`. The example file can be deleted/renamed to have it regenerated.

Multiple files can be added to this directory to support multiple languages.\
Additionally, if users are only using a portion of the mod, the unused sections of the language file can be safely removed.

## Command Permission
Commands are configured to require a minimum level of privilege for the user to be able to use them.\
Command privileges should be automatically created when the plugin starts (each time). Default required privilege is 100 for\
commands marked as "isAdmin" or 0 for those not marked.

Privilege levels range from 0 to 100, with 0 as the default privilege for users (lowest), and 100 as the highest privilege (admin).