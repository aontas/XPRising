using ProjectM;
using ProjectM.Network;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Text.RegularExpressions;
using ProjectM.Scripting;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VampireCommandFramework;
using ProjectM.CastleBuilding;
using Stunlock.Core;
using Unity.Transforms;
using XPRising.Hooks;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils.Prefabs;

namespace XPRising.Utils
{
    public static class Helper
    {
        private static Entity empty_entity = new Entity();
        private static System.Random rand = new System.Random();
        
        private static IsSystemInitialised<ServerGameManager> _serverGameManager = default;

        public static PrefabGUID SeverePunishmentDebuff = new PrefabGUID((int)Buffs.Buff_General_Garlic_Fever);          //-- Using this for PvP Punishment debuff
        public static PrefabGUID MinorPunishmentDebuff = new PrefabGUID((int)Buffs.Buff_General_Garlic_Area_Inside);

        //-- LevelUp Buff
        public static PrefabGUID LevelUp_Buff = new PrefabGUID((int)Effects.AB_ChurchOfLight_Priest_HealBomb_Buff);
        public static PrefabGUID HostileMark_Buff = new PrefabGUID((int)Buffs.Buff_Cultist_BloodFrenzy_Buff);

        //-- Fun
        public static PrefabGUID HolyNuke = new PrefabGUID((int)Effects.AB_Paladin_HolyNuke_Buff);
        public static PrefabGUID Pig_Transform_Debuff = new PrefabGUID((int)Remainders.Witch_PigTransformation_Buff);
        
        public static PrefabGUID AB_BloodBuff_VBlood_0 = new PrefabGUID((int)Effects.AB_BloodBuff_VBlood_0);
        public static PrefabGUID AB_BloodBuff_Base = new PrefabGUID((int)Effects.AB_BloodBuff_Base);
        
        public static int buffGUID = (int)Effects.AB_BloodBuff_VBlood_0;
        public static int ForbiddenBuffGuid = (int)SetBonus.SetBonus_MaxHealth_Minor_Buff_01;
        public static PrefabGUID AppliedBuff = AB_BloodBuff_VBlood_0;

        public static Regex rxName = new Regex(@"(?<=\])[^\[].*");
        
        public static bool GetServerGameManager(out ServerGameManager serverGameManager)
        {
            serverGameManager = _serverGameManager.system;
            if (!_serverGameManager.isInitialised)
            {
                var ssm = Plugin.Server.GetExistingSystemManaged<ServerScriptMapper>();
                if (ssm == null) return false;
                _serverGameManager.system = ssm._ServerGameManager;
                _serverGameManager.isInitialised = true;
                serverGameManager = _serverGameManager.system;
            }
            return true;
        }

        public static ModifyUnitStatBuff_DOTS MakeBuff(UnitStatType type, double strength) {
            ModifyUnitStatBuff_DOTS buff;

            var modType = ModificationType.Add;
            if (Helper.multiplierStats.Contains(type)) {
                modType = ModificationType.Multiply;
            }
            buff = (new ModifyUnitStatBuff_DOTS() {
                StatType = type,
                Value = (float)strength,
                ModificationType = modType,
                Modifier = 1,
                Id = ModificationId.NewId(0)
            });
            return buff;
        }
        
        public static double CalcBuffValue(double strength, double effectiveness, double rate, UnitStatType type)
        {
            effectiveness = Math.Max(effectiveness, 1);
            return strength * rate * effectiveness;
        }

        public static FixedString64Bytes GetTrueName(string name)
        {
            MatchCollection match = rxName.Matches(name);
            if (match.Count > 0)
            {
                name = match[^1].ToString();
            }
            return name;
        }

        public static void ApplyBuff(Entity User, Entity Char, PrefabGUID GUID)
        {
            var des = Plugin.Server.GetExistingSystemManaged<DebugEventsSystem>();
            var fromCharacter = new FromCharacter()
            {
                User = User,
                Character = Char
            };
            var buffEvent = new ApplyBuffDebugEvent()
            {
                BuffPrefabGUID = GUID
            };
            des.ApplyBuff(fromCharacter, buffEvent);
        }

        public static void RemoveBuff(Entity Char, PrefabGUID GUID)
        {
            if (BuffUtility.HasBuff(Plugin.Server.EntityManager, Char, GUID) &&
                BuffUtility.TryGetBuff(Plugin.Server.EntityManager, Char, GUID, out var buffEntity))
            {
                Plugin.Server.EntityManager.AddComponent<DestroyTag>(buffEntity);
            }
        }

        public static void AddItemToInventory(ChatCommandContext ctx, PrefabGUID guid, int amount)
        {
            var gameData = Plugin.Server.GetExistingSystemManaged<GameDataSystem>();
            var itemSettings = AddItemSettings.Create(Plugin.Server.EntityManager, gameData.ItemHashLookupMap);
            var inventoryResponse = InventoryUtilitiesServer.TryAddItem(itemSettings, ctx.Event.SenderCharacterEntity, guid, amount);
        }

        private struct FakeNull
        {
            public int value;
            public bool has_value;
        }
        public static bool TryGiveItem(Entity characterEntity, PrefabGUID itemGuid, int amount, out Entity itemEntity)
        {
            itemEntity = Entity.Null;
            
            var gameData = Plugin.Server.GetExistingSystemManaged<GameDataSystem>();
            var itemSettings = AddItemSettings.Create(Plugin.Server.EntityManager, gameData.ItemHashLookupMap);
            
            unsafe
            {
                var bytes = stackalloc byte[Marshal.SizeOf<FakeNull>()];
                var bytePtr = new IntPtr(bytes);
                Marshal.StructureToPtr(new FakeNull { value = 0, has_value = true }, bytePtr, false);
                var boxedBytePtr = IntPtr.Subtract(bytePtr, 0x10);
                var hack = new Il2CppSystem.Nullable<int>(boxedBytePtr);
                var inventoryResponse = InventoryUtilitiesServer.TryAddItem(
                    itemSettings,
                    characterEntity,
                    itemGuid,
                    amount);
                if (inventoryResponse.Success)
                {
                    itemEntity = inventoryResponse.NewEntity;
                    return true;
                }

                return false;
            }
        }

        public static void DropItemNearby(Entity characterEntity, PrefabGUID itemGuid, int amount)
        {
            InventoryUtilitiesServer.CreateDropItem(Plugin.Server.EntityManager, characterEntity, itemGuid, amount, new Entity());
        }

        public static bool HasBuff(Entity player, PrefabGUID BuffGUID)
        {
            return BuffUtility.HasBuff(Plugin.Server.EntityManager, player, BuffGUID);
        }

        public static bool SpawnNPCIdentify(out float identifier, string name, float3 position, float minRange = 1, float maxRange = 2, float duration = -1)
        {
            identifier = 0f;
            float default_duration = 5.0f;
            float duration_final;
            var isFound = Enum.TryParse(name, true, out Prefabs.Units unit);
            if (!isFound) return false;

            float UniqueID = (float)rand.NextDouble();
            if (UniqueID == 0.0) UniqueID += 0.00001f;
            else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
            duration_final = default_duration + UniqueID;

            while (Cache.spawnNPC_Listen.ContainsKey(duration))
            {
                UniqueID = (float)rand.NextDouble();
                if (UniqueID == 0.0) UniqueID += 0.00001f;
                else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
                duration_final = default_duration + UniqueID;
            }

            UnitSpawnerReactSystemPatch.listen = true;
            identifier = duration_final;
            var Data = new SpawnNpcListen(duration, default, default, default, false);
            Cache.spawnNPC_Listen.Add(duration_final, Data);

            Plugin.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, new PrefabGUID((int)unit), position, 1, minRange, maxRange, duration_final);
            return true;
        }

        public static bool SpawnAtPosition(Entity user, Prefabs.Units unit, int count, float3 position, float minRange = 1, float maxRange = 2, float duration = -1) {
            var guid = new PrefabGUID((int)unit);

            try
            {
                Plugin.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, guid, position, count, minRange, maxRange, duration);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static PrefabGUID GetPrefabGUID(Entity entity)
        {
            var entityManager = Plugin.Server.EntityManager;
            if (entity == Entity.Null || !entityManager.TryGetComponentData<PrefabGUID>(entity, out var prefabGuid))
            {
                prefabGuid = new PrefabGUID(0);
            }

            return prefabGuid;
        }
        
        public static Prefabs.Faction ConvertGuidToFaction(PrefabGUID guid) {
            if (Enum.IsDefined(typeof(Prefabs.Faction), guid.GetHashCode())) return (Prefabs.Faction)guid.GetHashCode();
            return Prefabs.Faction.Unknown;
        }
        
        public static Prefabs.Units ConvertGuidToUnit(PrefabGUID guid) {
            if (Enum.IsDefined(typeof(Prefabs.Units), guid.GetHashCode())) return (Prefabs.Units)guid.GetHashCode();
            return Prefabs.Units.Unknown;
        }

        public static void TeleportTo(ChatCommandContext ctx, float3 position)
        {
            var entity = Plugin.Server.EntityManager.CreateEntity(
                    ComponentType.ReadWrite<FromCharacter>(),
                    ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
                );

            Plugin.Server.EntityManager.SetComponentData<FromCharacter>(entity, new()
            {
                User = ctx.Event.SenderUserEntity,
                Character = ctx.Event.SenderCharacterEntity
            });

            Plugin.Server.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new()
            {
                Position = new float3(position.x, position.y, position.z),
                Target = PlayerTeleportDebugEvent.TeleportTarget.Self
            });
        }
        
        public static bool IsInCastle(Entity user)
        {
            var userLocalToWorld = Plugin.Server.EntityManager.GetComponentData<LocalToWorld>(user);
            var userPosition = userLocalToWorld.Position;
            var query = Plugin.Server.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PrefabGUID>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<UserOwner>(),
                ComponentType.ReadOnly<CastleFloor>());
            
            foreach (var entityModel in query.ToEntityArray(Allocator.Temp))
            {
                if (!Plugin.Server.EntityManager.TryGetComponentData<LocalToWorld>(entityModel, out var localToWorld))
                {
                    continue;
                }
                var position = localToWorld.Position;
                if (Math.Abs(userPosition.x - position.x) < 3 && Math.Abs(userPosition.z - position.z) < 3)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsVBlood(Entity entity)
        {
            return Plugin.Server.EntityManager.TryGetComponentData(entity, out BloodConsumeSource victimBlood) && IsVBlood(victimBlood);
        }

        public static bool IsVBlood(BloodConsumeSource bloodSource)
        {
            var guidHash = bloodSource.UnitBloodType._Value.GuidHash;
            return guidHash == (int)Remainders.BloodType_VBlood ||
                   guidHash == (int)Remainders.BloodType_GateBoss ||
                   guidHash == (int)Remainders.BloodType_DraculaTheImmortal;
        }
        
        public static bool IsItemEquipBuff(PrefabGUID prefabGuid)
        {
            switch ((Items)prefabGuid.GuidHash)
            {
                case Items.Item_EquipBuff_Armor_Base:
                case Items.Item_EquipBuff_Base:
                case Items.Item_EquipBuff_Clothes_Base:
                case Items.Item_EquipBuff_MagicSource_Base:
                case Items.Item_EquipBuff_MagicSource_BloodKey_T01:
                case Items.Item_EquipBuff_MagicSource_General:
                case Items.Item_EquipBuff_MagicSource_NoAbility_Base:
                case Items.Item_EquipBuff_MagicSource_Soulshard:
                case Items.Item_EquipBuff_MagicSource_Soulshard_Dracula:
                case Items.Item_EquipBuff_MagicSource_Soulshard_Manticore:
                case Items.Item_EquipBuff_MagicSource_Soulshard_Solarus:
                case Items.Item_EquipBuff_MagicSource_Soulshard_TheMonster:
                case Items.Item_EquipBuff_MagicSource_T06_Blood:
                case Items.Item_EquipBuff_MagicSource_T06_Chaos:
                case Items.Item_EquipBuff_MagicSource_T06_Frost:
                case Items.Item_EquipBuff_MagicSource_T06_Illusion:
                case Items.Item_EquipBuff_MagicSource_T06_Storm:
                case Items.Item_EquipBuff_MagicSource_T06_Unholy:
                case Items.Item_EquipBuff_MagicSource_T08_Blood:
                case Items.Item_EquipBuff_MagicSource_T08_Chaos:
                case Items.Item_EquipBuff_MagicSource_T08_Frost:
                case Items.Item_EquipBuff_MagicSource_T08_Illusion:
                case Items.Item_EquipBuff_MagicSource_T08_Storm:
                case Items.Item_EquipBuff_MagicSource_T08_Unholy:
                case Items.Item_EquipBuff_MagicSource_TriggerBuffOnPrimaryHit:
                case Items.Item_EquipBuff_MagicSource_TriggerCastOnPrimaryHit:
                case Items.Item_EquipBuff_MagicSource_Utility_Base:
                case Items.Item_EquipBuff_Shared_General:
                case Items.Item_EquipBuff_Weapon_Base:
                    return true;
                default:
                    if (Enum.IsDefined((EquipBuffs)prefabGuid.GuidHash))
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        public static LazyDictionary<UnitStatType, float> GetAllStatBonuses(ulong steamID, Entity owner)
        {
            LazyDictionary<UnitStatType, float> statusBonus = new();
            
            if (Plugin.WeaponMasterySystemActive) WeaponMasterySystem.BuffReceiver(ref statusBonus, owner, steamID);
            // if (Plugin.BloodlineSystemActive) BloodlineSystem.BuffReceiver(ref statusBonus, owner, steamID);
            if (ExperienceSystem.LevelRewardsOn && Plugin.ExperienceSystemActive) ExperienceSystem.BuffReceiver(ref statusBonus, steamID);
            return statusBonus;
        }
        
        public static HashSet<UnitStatType> percentageStats = new()
            {
                UnitStatType.PhysicalCriticalStrikeChance,
                UnitStatType.SpellCriticalStrikeChance,
                UnitStatType.PhysicalCriticalStrikeDamage,
                UnitStatType.SpellCriticalStrikeDamage,
                UnitStatType.PhysicalLifeLeech,
                UnitStatType.PrimaryLifeLeech,
                UnitStatType.SpellLifeLeech,
                UnitStatType.AttackSpeed,
                UnitStatType.PrimaryAttackSpeed,
                UnitStatType.PassiveHealthRegen,
                UnitStatType.ResourceYield
            };

        //This should be a dictionary lookup for the stats to what mod type they should use
        public static HashSet<UnitStatType> multiplierStats = new()
            {
                UnitStatType.PrimaryCooldownModifier,
                UnitStatType.WeaponCooldownRecoveryRate,
                UnitStatType.SpellCooldownRecoveryRate,
                UnitStatType.UltimateCooldownRecoveryRate, /*
                {UnitStatType.PhysicalResistance },
                {UnitStatType.SpellResistance },
                {UnitStatType.ResistVsBeasts },
                {UnitStatType.ResistVsCastleObjects },
                {UnitStatType.ResistVsDemons },
                {UnitStatType.ResistVsHumans },
                {UnitStatType.ResistVsMechanical },
                {UnitStatType.ResistVsPlayerVampires },
                {UnitStatType.ResistVsUndeads },
                {UnitStatType.ReducedResourceDurabilityLoss },
                {UnitStatType.BloodDrain },*/
                UnitStatType.ResourceYield
            };

        public static HashSet<UnitStatType> baseStatsSet = new()
            {
                UnitStatType.PhysicalPower,
                UnitStatType.ResourcePower,
                UnitStatType.SiegePower,
                UnitStatType.AttackSpeed,
                UnitStatType.FireResistance,
                UnitStatType.GarlicResistance,
                UnitStatType.SilverResistance,
                UnitStatType.HolyResistance,
                UnitStatType.SunResistance,
                UnitStatType.SpellResistance,
                UnitStatType.PhysicalResistance,
                UnitStatType.SpellCriticalStrikeDamage,
                UnitStatType.SpellCriticalStrikeChance,
                UnitStatType.PhysicalCriticalStrikeDamage,
                UnitStatType.PhysicalCriticalStrikeChance,
                UnitStatType.PassiveHealthRegen,
                UnitStatType.ResourceYield,
                UnitStatType.PvPResilience,
                UnitStatType.ReducedResourceDurabilityLoss
            };
        
        public static string CamelCaseToSpaces(UnitStatType type) {
            var name = Enum.GetName(type);
            // Split words by camel case
            // ie, PhysicalPower => "Physical Power"
            return Regex.Replace(name, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }

        private struct IsSystemInitialised<T>()
        {
            public bool isInitialised = false;
            public T system = default;
        }
    }
}
