### Setting up globalMasteryConfig.json

#### Mastery types
The following types can be used to configure mastery buff stats:

| Weapons           | Bloodlines     |
|-------------------|----------------|
| weaponUnarmed     | bloodNone      |
| weaponSpear       | bloodBrute     |
| weaponSword       | bloodCreature  |
| weaponScythe      | bloodDracula   |
| weaponCrossbow    | bloodDraculin  |
| weaponMace        | bloodMutant    |
| weaponSlasher     | bloodRogue     |
| weaponAxe         | bloodScholar   |
| weaponFishingPole | bloodWarrior   |
| weaponRapier      | bloodWorker    |
| weaponPistol      | bloodCorrupted |
| weaponGreatSword  |                |
| weaponLongBow     |                |
| weaponWhip        |                |
| weaponClaws       |                |
| weaponDaggers     |                |
| weaponTwinBlades  |                |
| spell             |                |
Note: Spell is currently counted as a weapon mastery for the purposes of mastery disabling/mastery reset.

#### File JSON format

This is the type definitions in Typescript (in case you are familiar with that)\
Note:
- all floats default to 0
- all lists default to null (effectively empty lists)
- all maps default to null (effectively empty maps)
- properties marked with `?` are not required (for those not familiar with typescript)

```typescript
type BonusType = "fixed" | "range" | "ratio"
type MasteryType = string // see above table
type UnitStatType = string // see below table
type StatCategory = string // see below table

interface BonusData
{
    bonusType: BonusType, // defaults to "fixed"
    statType: UnitStateType, // defaults to "physicalPower"
    value?: float,
    range?: float[],
    requiredMastery?: float,
    inactiveMultiplier?: float,
}

interface ActiveBonusData
{
    bonusType: BonusType, // defaults to "fixed"
    statCategory: StatCategory, // defaults to "none"
    value: float,
    requiredMastery?: float,
}

interface MasteryConfig {
    templates?: string[],
    baseBonus?: BaseBonus[],
    activeBonus?: ActiveBonusData[],
    maxEffectiveness?: float,
    decayValue?: float,
    growthPerEffectiveness?: float,
}

interface FileFormat // This is the base that is used as the JSON file
{
    mastery?: { [key: MasteryType]: MasteryConfig },
    masteryTemplates?: { [key: string]: MasteryConfig },
    defaultWeaponMasteryConfig?: MasteryConfig,
    defaultBloodMasteryConfig?: MasteryConfig,
    xpBuffConfig?: MasteryConfig, // This controls the buffs provided at each XP level (rather than mastery)
}
```

#### BonusData
This object describes the bonus that should be applied for a mastery. This only applies to a character when they have the appropriate mastery equipped (ie, weapon or bloodline type matches) unless the `inactiveMultiplier` property is used. Not all properties are required, this is shown above with the `?` suffix.

Bonus values can be applied in the following forms:
- `fixed`: adds the specified value to the buff
- `ratio`: applies a ratio of the current mastery to the value and then adds to the buff (100% mastery adds the value as shown)
  - `10`: At 0% mastery, this will give 0, 50% will give 5 and 100% will give 10.
- `range`: this works similar to `ratio` but allows a range of values to be applied based on mastery.
  - `[0, 0, 10, 20]`: At 0% -> 33% mastery, this will apply 0. 33% -> 66% will have a `ratio` of 0 -> 10. 66% -> 100% will have a `ratio` of 10 -> 20.
  - This supports any number of values

`requiredMastery` property gates when any specific bonus is applied. The player must have a higher mastery to get the bonus.\
`inactiveMultiplier` property ensure that the bonus gets applied (multiplied by this) if the player does not have this mastery active (either via weapon not equipped or bloodline is different)

#### ActiveBonus
Active bonus applies the value to the category of stats (offensive, resource, defensive, any) that the active weapon is providing. Can be used to buff (or debuff) the equipped weapon. The bonuses are applied over all the stats matching the `statCategory` of the bonus (ie, you can buff all defensive stats on their own).

Currently, there are no "active" buff supports for bloodlines. This is coming.

Any and all of these systems can be configured and turned on/off.

See the table below for more information on stat categories and which stats can be used for the above config.


### Units stats and their effects in V Rising
The following effects will be applied at 100% mastery. This is some initial investigation, any additional results that you find and can provide will be added to the table.

Note that different weapons will have different damage coefficients. All the damage bonus stats in the table are not normalised, but do seem to work to add some additional damage. Further investigation is required.

| Stat                          | Category  | Value | Effect                                                                     |
|-------------------------------|-----------|:-----:|----------------------------------------------------------------------------|
| AbilityAttackSpeed            | offensive |       |                                                                            |
| BloodDrain                    | other     |   1   | 1 minute and 40 sec drain on 10 liters of blood                            |
| BloodDrainMultiplier          | other     |       |                                                                            |
| BloodEfficiency               | other     |  25   | 500% added (scholar blood 100%)                                            |
| BloodMendHealEfficiency       | other     |       |                                                                            |
| BonusMaxHealth                | defensive |       |                                                                            |
| BonusMountMovementSpeed       | other     |       |                                                                            |
| BonusMovementSpeed            | other     |       |                                                                            |
| BonusPhysicalPower            | offensive |   1   | 1 physical power                                                           |
| BonusShapeshiftMovementSpeed  | other     |  0.2  | baseSpeed * (1 + value) => 6.5*1.2 => 7.8                                  |
| BonusSpellPower               | offensive |   1   | 1 spell power                                                              |
| CCReduction                   | defensive |  50   | half the CC amount (2 sec stun -> 1 sec)                                   |
| CooldownRecoveryRate          | offensive | 0.15  | minus 1 sec                                                                |
| CorruptionDamageReduction     | defensive |       |                                                                            |
| DamageReduction               | defensive |  10   | 6 dmg reduction                                                            |
| DamageVsBeasts                | offensive |   1   | 45 dmg                                                                     |
| DamageVsCastleObjects         | offensive |   1   | 100% extra (to walls, building, golem before someones in it)               |
| DamageVsDemons                | offensive |   1   | 21 dmg                                                                     |
| DamageVsHumans                | offensive |   1   | 22 dmg                                                                     |
| DamageVsLightArmor            | offensive |       | no data                                                                    |
| DamageVsMagic                 | offensive |   1   | 29 dmg                                                                     |
| DamageVsMechanical            | offensive |   1   | 30 dmg                                                                     |
| DamageVsMineral               | resource  |       | no data                                                                    |
| DamageVsUndeads               | offensive |   1   | 22 dmg                                                                     |
| DamageVsVampires              | offensive |   1   | 14 dmg                                                                     |
| DamageVsVBloods               | offensive |   1   | 27 dmg                                                                     |
| DamageVsVegetation            | resource  |       | no data                                                                    |
| DamageVsWood                  | resource  |   1   | increases damage when attacking trees                                      |
| DemountProtection             | defensive |       |                                                                            |
| FallGravity                   | other     |       | no data[^1]                                                                |
| FeedCooldownRecoveryRate      | other     |       |                                                                            |
| FireResistance                | defensive |  10   | 23 dmg reduction per tick                                                  |
| GarlicResistance              | defensive |  10   | 10 resistance attribute                                                    |
| HealingReceived               | defensive |  10   | 700 hp per tick (blood rose potion)                                        |
| HealthRecovery                | defensive |  10   | 2-3 hp per hit (primary attack)                                            |
| HolyResistance                | defensive |  10   | 2 reduction from holy zones, 5 dmg reduction from mobs                     |
| ImmuneToHazards               | defensive |  25   | 20 dmg reduction (fire)                                                    |
| IncreasedShieldEfficiency     | defensive |       |                                                                            |
| InventorySlots                | other     |       | no data[^1]                                                                |
| MaxHealth                     | defensive |   1   | 1 hp                                                                       |
| MinionDamage                  | offensive |   1   | 21 dmg (deathknight), 11 dmg (skellies)                                    |
| MovementSpeed                 | other     |   1   | 1                                                                          |
| PassiveHealthRegen            | defensive |  0.5  | 583 health/sec (no data on different health pools)                         |
| PhysicalCriticalStrikeChance  | offensive |  0.1  | 10% increase                                                               |
| PhysicalCriticalStrikeDamage  | offensive |  0.1  | 10% increase                                                               |
| PhysicalLifeLeech             | offensive |   1   | 4 health (primary attack) (skills give same health per damage 22dmg=22 hp) |
| PhysicalPower                 | offensive |   1   | 1 physical power                                                           |
| PhysicalResistance            | defensive |  0.5  | 21 dmg reduction                                                           |
| PrimaryAttackSpeed            | offensive |  0.1  | 10% increase                                                               |
| PrimaryCooldownModifier       | offensive |  30   | immediate attack after combo is over                                       |
| PrimaryLifeLeech              | offensive |   1   | 210 hp on main attack                                                      |
| PvPResilience                 | defensive |       | no data                                                                    |
| Radial_SpellResistance        | defensive |       | no data                                                                    |
| ReducedBloodDrain             | other     |       |                                                                            |
| ReducedResourceDurabilityLoss | other     |       | no data[^2]                                                                |
| ResistVsBeasts                | defensive |       | no data                                                                    |
| ResistVsCastleObjects         | defensive |       | no data                                                                    |
| ResistVsDemons                | defensive |       | no data                                                                    |
| ResistVsHumans                | defensive |       | no data                                                                    |
| ResistVsMechanical            | defensive |       | no data                                                                    |
| ResistVsUndeads               | defensive |       | no data                                                                    |
| ResistVsVampires              | defensive |  0.5  | 9 dmg reduction (melee), 34 reduction (spell)                              |
| ResourcePower                 | resource  |   1   | 1 harvesting power                                                         |
| ResourceYield                 | resource  |  0.1  | 10% extra yield                                                            |
| ShieldAbsorb                  | defensive |   1   | 84 extra shield                                                            |
| SiegePower                    | offensive |       | no data                                                                    |
| SilverCoinResistance          | defensive |       | no data                                                                    |
| SilverResistance              | defensive |  10   | 10 resistance attribute (no data on actual coin/ore value)                 |
| SpellCooldownRecoveryRate     | offensive | 0.15  | minus 1 sec                                                                |
| SpellCriticalStrikeChance     | offensive |  0.1  | 10% increase                                                               |
| SpellCriticalStrikeDamage     | offensive |  0.1  | 10% increase                                                               |
| SpellFreeCast                 | offensive |       |                                                                            |
| SpellLifeLeech                | offensive |  0.8  | 66 (chaos volley) [^3]                                                     |
| SpellPower                    | offensive |   1   | 1 spell power                                                              |
| SpellResistance               | defensive |  0.5  | 34 dmg reduction (shadowbolt)                                              |
| SunChargeTime                 | defensive |   2   | 2 sec                                                                      |
| SunResistance                 | defensive |  10   | 10 resistance attribute (20 dmg reduction)                                 |
| TravelCooldownRecoveryRate    | other     |       |                                                                            |
| UltimateCooldownRecoveryRate  | offensive |   1   | minus 60 sec                                                               |
| UltimateEfficiency            | offensive |       |                                                                            |
| Vision                        | other     |       | no data[^1]                                                                |
| WeaponCooldownRecoveryRate    | offensive | 0.15  | minus 1 sec                                                                |
| WeaponFreeCast                | offensive |       |                                                                            |
| WeaponSkillPower              | offensive |       |                                                                            |

[^1]: These stats likely do not work at all
[^2]: This has direct support in server settings, untested
[^3]: Different spells do different amounts (have not tested hp/damage)