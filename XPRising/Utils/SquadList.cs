using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Unity.Mathematics;
using XPRising.Systems;
using Random = System.Random;

namespace XPRising.Utils
{
    using Faction = Prefabs.Faction;
    using Units = Prefabs.Units;

    public static class SquadList {

        private static Random generate = new();

        private struct UnitDetails {
            public Units type;
            public int count;
            public int level;
            public int range;

            public UnitDetails(Prefabs.Units type, int count, int level, int range) {
                this.type = type;
                this.count = count;
                this.level = level;
                this.range = range;
            }
        }

        private struct Squad {
            public string message;
            public List<UnitDetails> units;

            public Squad(string message, List<UnitDetails> units) {
                this.message = message;
                this.units = units;
            }
        }

        private static string GenericAmbushMessage(Faction faction, int wantedLevel) {
            switch (wantedLevel) {
                case 0:
                    return "";
                case 1:
                    return $"A small squad of {faction} are ambushing!";
                case 2:
                    return $"A squad of {faction} are ambushing!";
                case 3:
                    return $"A large squad of {faction} are ambushing!";
                default:
                    return $"The {faction} are attacking with as many as they can spare!";
            }
        }

        private static int GetLevel(int unitLevel, int playerLevel, int modifier = 0) {
            // Set the unit level to playerLevel, or to the expected level of the unit (if that is lower).
            // This scales the squad down to the PC level (or retains their own level) to help ensure the PC has a
            // chance to kill them.
            var level = Math.Clamp(unitLevel, playerLevel - 8, playerLevel + 2);

            return Math.Max(level + modifier, 1);
        }

        private static List<UnitDetails>
            GenerateSquadUnits(ArraySegment<FactionUnits.Unit> units, int wantedLevel, int playerLevel) {
            var squadUnits = new List<UnitDetails>();

            var remainingSquadValue = wantedLevel * 5;
            Plugin.Log(Plugin.LogSystem.SquadSpawn, LogLevel.Info, $"Generate squad (spawn value: {remainingSquadValue})");

            while (remainingSquadValue > 0) {
                var nextUnitIndex = generate.Next(0, units.Count);

                var unit = units[nextUnitIndex];
                var unitValue = unit.value * Math.Max(unit.level - playerLevel, 1);
                var possibleSpawnLimit = Math.Clamp(remainingSquadValue / unitValue, 1, 5);
                var unitSpawn = generate.Next(1, possibleSpawnLimit);

                var level = GetLevel(unit.level, playerLevel);

                squadUnits.Add(new UnitDetails(unit.type, unitSpawn, level, 1));

                remainingSquadValue -= unitSpawn * unitValue;
            }

            return squadUnits;
        }

        private static Squad GetSquad(Faction faction, int playerLevel, int wantedLevel) {
            var chance = generate.Next(100);

            Plugin.Log(Plugin.LogSystem.SquadSpawn, LogLevel.Info, $"GetSquad for {faction} (RNG: {chance})");

            // Very small chance unique squads
            switch (chance) {
                // Just creating a bunch of farm animals here because this would be hilarious.
                case 0:
                    return new Squad("Oh no! The farm animals are coming!",
                        GenerateSquadUnits(new ArraySegment<FactionUnits.Unit>(FactionUnits.farmFood), wantedLevel, playerLevel));
                case 1:
                    return new Squad("Jack is getting hungry...",
                        new List<UnitDetails>() { new(Units.CHAR_Scarecrow, 1, playerLevel, 1) });
            }

            switch (faction) {
                case Faction.Bandits:
                    if (chance > 90) {
                        // Bunch of archers
                        return new Squad($"The {faction} sent their best archers to take you out!",
                            new List<UnitDetails>() {
                                new(Units.CHAR_Bandit_Deadeye, 2 * wantedLevel, Math.Max(playerLevel - 1, 1), 8)
                            });
                    }

                    if (chance > 80) {
                        // Bunch of wolves
                        return new Squad($"The {faction} have sent the wolves to track you down!",
                            new List<UnitDetails>() {
                                new(Units.CHAR_Bandit_Wolf, 3 * wantedLevel, Math.Max(playerLevel - 1, 1), 4),
                                new(Units.CHAR_Bandit_Hunter, 2, playerLevel + 2, 4)
                            });
                    }

                    break;
                case Faction.Militia:
                    if (wantedLevel > 3 && chance > 93) {
                        return new Squad($"The {faction} have called upon their church to burn you!",
                            new List<UnitDetails>() {
                                new(Units.CHAR_Militia_EyeOfGod, 3 * wantedLevel, Math.Max(playerLevel - 1, 1), 5)
                            });
                    }

                    if (chance > 50) {
                        var unitTypes = FactionUnits.GetFactionUnits(faction, playerLevel, wantedLevel);
                        var squadUnits = GenerateSquadUnits(unitTypes, wantedLevel, Math.Max(playerLevel - 2, 1));

                        var wantedMessage = "The church is leading a squad to kill you!";
                        switch (wantedLevel) {
                            case 1:
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Knight_2H, 1, Math.Max(playerLevel - 1, 1), 5));
                                break;
                            case 2:
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Knight_Shield, 1, Math.Max(playerLevel - 2, 1), 5));
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Priest, 1, Math.Max(playerLevel - 1, 1), 5));
                                break;
                            case 3:
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Knight_Shield, 1, Math.Max(playerLevel - 1, 1), 5));
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Lightweaver, 1, Math.Max(playerLevel - 1, 1), 5));
                                break;
                            case 4:
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Knight_2H, 1, Math.Max(playerLevel - 1, 1), 5));
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Paladin, 1, Math.Max(playerLevel - 1, 1), 5));
                                break;
                            case 5:
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Lightweaver, 1, Math.Max(playerLevel - 1, 1), 5));
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Paladin, 1, Math.Max(playerLevel - 1, 1), 5));
                                break;
                            default: // and above!
                                wantedMessage = $"The {faction} have sent an elite church team to kill you!";
                                squadUnits.Add(new(Units.CHAR_ChurchOfLight_Lightweaver, 2, Math.Max(playerLevel - 1, 1), 8));
                                squadUnits.Add(new UnitDetails(Units.CHAR_ChurchOfLight_Priest, 1, Math.Max(playerLevel - 1, 1), 5));
                                squadUnits.Add(new(Units.CHAR_ChurchOfLight_Paladin, 1, Math.Max(playerLevel - 1, 1), 8));
                                break;
                        } 

                        return new Squad(wantedMessage, squadUnits);
                    }

                    break;
                case Faction.Undead:
                    if (chance > 70) {
                        // A horde of undead are coming!
                        Units mainUnit;
                        if (playerLevel > 65) {
                            mainUnit = Units.CHAR_Undead_SkeletonSoldier_Base;
                        }
                        else if (playerLevel > 45) {
                            mainUnit = Units.CHAR_Undead_SkeletonSoldier_Armored_Dunley;
                        }
                        else if (playerLevel > 25) {
                            mainUnit = Units.CHAR_Undead_SkeletonSoldier_Armored_Farbane;
                        }
                        else {
                            mainUnit = Units.CHAR_Undead_SkeletonSoldier_Withered;
                        }

                        return new Squad($"The {faction} have summoned a horde to kill you!",
                            new List<UnitDetails>() {
                                new(mainUnit, 6 * wantedLevel, Math.Max(playerLevel - 1, 1), 5),
                            });
                    }

                    break;
                case Faction.Gloomrot:
                    if (wantedLevel > 3 && chance > 93) {
                        return new Squad($"The {faction} lab technicians have turned they technology upon you!",
                            new List<UnitDetails>() {
                                new(Units.CHAR_Monster_LightningPillar, 3 * wantedLevel, Math.Max(playerLevel - 1, 1), 5)
                            });
                    }
                    if (chance < 5) {
                        // TANKS!
                        Units mainUnit;
                        switch (generate.Next(2)) {
                            case 0:
                                mainUnit = Units.CHAR_Gloomrot_Railgunner;
                                break;
                            case 1:
                                mainUnit = Units.CHAR_Gloomrot_Pyro;
                                break;
                            case 2:
                            default:
                                mainUnit = Units.CHAR_Gloomrot_AceIncinerator;
                                break;
                        }

                        return new Squad($"The {faction} want to TANK YOU VERY MUCH!",
                            new List<UnitDetails>() {
                                new(mainUnit, 1 * (int)Math.Ceiling(wantedLevel * 0.5), playerLevel, 5),
                                new(Units.CHAR_Gloomrot_Technician, 3 * wantedLevel, Math.Max(playerLevel - 1, 1), 5)
                            });
                    }
                    if (chance < 15) {
                        // TURRETS!
                        return new Squad($"The {faction} turrets have come out to play!",
                            new List<UnitDetails>() {
                                new(Units.CHAR_Gloomrot_SentryTurret, 4 * wantedLevel, Math.Max(playerLevel - 1, 1), 5)
                            });
                    }
                    break;
                case Faction.Critters:
                    // No specific squads yet!
                    break;
                case Faction.Werewolf:
                    return new Squad($"A {faction} squad is ambushing you!",
                        new List<UnitDetails>() {
                            new(Units.CHAR_Farmlands_HostileVillager_Werewolf, 3 * wantedLevel, Math.Max(playerLevel - 1, 1), 5)
                        });
                default:
                    Plugin.Log(Plugin.LogSystem.Core, LogLevel.Warning, $"No specific squad generation handling has been added for {faction}");
                    break;
            }

            var units = FactionUnits.GetFactionUnits(faction, playerLevel, wantedLevel);
            return new Squad(GenericAmbushMessage(faction, wantedLevel),
                GenerateSquadUnits(units, wantedLevel, playerLevel));
        }

        public static string SpawnSquad(int playerLevel, float3 position, Faction faction, int wantedLevel) {
            var squad = GetSquad(faction, playerLevel, wantedLevel);
            
            foreach (var unit in squad.units)
            {
                // wanted level/level difference
                // 1/-2, 2/1, 3/2, 4/5, 5/6
                var unitLevel =
                    wantedLevel > 3 ? playerLevel + wantedLevel + 1 :
                    wantedLevel > 1 ? playerLevel + wantedLevel - 1:
                    playerLevel - 2;
                var spawnFaction = faction == Faction.Legion ? SpawnUnit.SpawnFaction.WantedUnit : SpawnUnit.SpawnFaction.VampireHunters;
                var lifetime = SpawnUnit.EncodeLifetime((int)WantedSystem.ambush_despawn_timer, unitLevel, spawnFaction);
                SpawnUnit.Spawn(unit.type, position, unit.count, unit.range, unit.range + 4f, lifetime);
                Plugin.Log(Plugin.LogSystem.SquadSpawn, LogLevel.Info, $"Spawning: {unit.count}*{unit.type}");
            }

            Plugin.Log(Plugin.LogSystem.SquadSpawn, LogLevel.Info, $"Spawn finished");

            return squad.message;
        }
    }
}
