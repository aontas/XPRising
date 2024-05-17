## Experience System

Disable the VRising Gear Level system and replace it with a traditional RPG experience system,
complete with exp sharing between clan members or other players designated as allies.

Currently, your HP will increase by a minor amount each level.

## Weapon Mastery System
<details>

### Weapon Mastery
Mastering a weapon will now progressively give extra bonuses to the character's stats.
Weapon mastery will increase when the weapon is used to kill a creature, and while in combat to a maximum of 60 seconds at 0.001%/Sec.
Spell mastery can only increase and take effect when no weapon is equipped, unless changed in the configuration options.

### Mastery Decay
When the vampire goes offline, all their weapon mastery will continuously decay until they come back online.

### Effectiveness System
Effectiveness acts as a multiplier for the weapon mastery. The initial effectiveness starts at 100%.
When weapon mastery is reset using ".mastery reset <type>", the current mastery level is added to effectiveness and then is set to 0%.
As the vampire then increases in weapon mastery, the effective weapon mastery is `mastery * effectiveness`.

Effectiveness is specific for each weapon.

### Growth System
The growth system is used to determine how fast mastery can be gained at higher levels of effectiveness.
This means that higher effectiveness will slow to mastery gain (at 1, 200% effectiveness gives a mastery growth rate of 50%).
Config supports modifying the rate at which this growth slows. Set growth per effectiveness to 0 to have no change in growth. Higher numbers make the growth drop off slower.
Negative values have the same effect as positive (ie, -1 == 1 for the growth per effectiveness setting).

This is only relevant if the effectiveness system is turned on.

</details>

## Bloodline Mastery System
<details>

### Bloodlines
Killing enemies while you have a blood type will progress the mastery of your bloodline.
As your bloodline grows in mastery it will provide scaling benefits to your stats.
`Merciless bloodlines` are enabled by default, which means to progress your bloodline's mastery
you need to kill a target with same blood type AND it needs to be blood of higher quality than your bloodline's mastery.
V Blood always provides progress even in merciless mode.

Bloodline mastery for blood types that don't match your current blood will still be applied at a greatly reduced amount.
The command is .bloodline or .bl

### Mastery Decay
When the vampire goes offline, all their bloodline mastery will continuously decay until they come back online.

### Effectiveness System
Effectiveness acts as a multiplier for the bloodline mastery. The initial effectiveness starts at 100%.
When bloodline mastery is reset using ".bloodline reset <type>", the current mastery level is added to effectiveness and then is set to 0%.
As the vampire then increases in bloodline mastery, the effective bloodline mastery is `mastery * effectiveness`.

Effectiveness is specific for each bloodline.

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

Otherwise, if you are dead for any reason at all, your wanted level will reset back to 0.
```
Note:
- Ambush may only occur when the player is in combat.
- All mobs spawned by this system is assigned to Faction_VampireHunters
```
</details>

## Clans and Groups and XP sharing
Killing with other vampires can share XP and wanted heat levels within the group.

A vampire is considered in your group if they are in your clan or if you use the `group` commands to create a group with
them. A group will only share XP if the members are close enough to each other, governed by the `Ally Max Distance` configuration.

<details>
<summary>Experience</summary>
Group XP is awarded based on the ratio of the average group level to the sum of the group level. It is then multiplied
by a bonus value `( 1.2^(group size - 1) )`, up to a maximum of `1.5`.
</details>

<details>
<summary>Heat</summary>
Increases in heat levels are applied uniformly for every member in the group.
</details>

## Command Permission
Commands are configured to require a minimum level of privilege for the user to be able to use them.\
Command privileges should be automatically created when the plugin starts (each time). Default required privilege is 100 for\
commands marked as "isAdmin" or 0 for those not marked.

Privilege levels range from 0 to 100, with 0 as the default privilege for users (lowest), and 100 as the highest privilege (admin).