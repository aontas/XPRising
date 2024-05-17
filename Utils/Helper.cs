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
using BepInEx.Logging;
using VampireCommandFramework;
using Bloodstone.API;
using ProjectM.CastleBuilding;
using Stunlock.Core;
using Unity.Transforms;
using XPRising.Hooks;
using XPRising.Models;
using XPRising.Utils.Prefabs;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Utils
{
    // TODO test wanted level display in game
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
        
        // TODO are either of these a better applied buff/forbidden buff?
        public static PrefabGUID AB_BloodBuff_VBlood_0 = new PrefabGUID((int)Effects.AB_BloodBuff_VBlood_0);
        public static PrefabGUID AB_BloodBuff_Base = new PrefabGUID((int)Effects.AB_BloodBuff_Base);
        
        //public static int buffGUID = (int)SetBonus.SetBonus_Damage_Minor_Buff_01;
        //public static PrefabGUID AppliedBuff = new PrefabGUID(buffGUID);
        public static int buffGUID = (int)Effects.AB_BloodBuff_VBlood_0;
        public static int ForbiddenBuffGuid = (int)SetBonus.SetBonus_MaxHealth_Minor_Buff_01;
        public static PrefabGUID AppliedBuff = AB_BloodBuff_VBlood_0;

        public static Regex rxName = new Regex(@"(?<=\])[^\[].*");
        
        public static bool humanReadablePercentageStats = false;
        public static bool inverseMultipersDisplayReduction = true;
        
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
            if (Helper.inverseMultiplierStats.Contains(type)) {
                var value = strength * effectiveness;
                return 1 - value / (value + rate);
            }
            return strength * rate * effectiveness;
        }

        public static FixedString64Bytes GetTrueName(string name)
        {
            MatchCollection match = rxName.Matches(name);
            if (match.Count > 0)
            {
                name = match[match.Count - 1].ToString();
            }
            return name;
        }

        public static void CreatePlayerCache() {

            Cache.NamePlayerCache.Clear();
            Cache.SteamPlayerCache.Clear();
            EntityQuery query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc() {
                All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<User>()
                    },
                Options = EntityQueryOptions.IncludeDisabled
            });
            var userEntities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in userEntities) {
                var userData = Plugin.Server.EntityManager.GetComponentData<User>(entity);
                PlayerData playerData = new PlayerData(
                    userData.CharacterName,
                    userData.PlatformId,
                    userData.IsConnected,
                    userData.IsAdmin,
                    entity,
                    userData.LocalCharacter._Entity);

                Cache.NamePlayerCache.TryAdd(GetTrueName(userData.CharacterName.ToString().ToLower()), playerData);
                Cache.SteamPlayerCache.TryAdd(userData.PlatformId, playerData);

            }

            Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, "Player Cache Created.");
        }
        
        public static void UpdatePlayerCache(Entity userEntity, User userData, bool forceOffline = false)
        {
            PlayerData playerData = new PlayerData(
                userData.CharacterName,
                userData.PlatformId,
                !forceOffline && userData.IsConnected,
                userData.IsAdmin,
                userEntity,
                userData.LocalCharacter._Entity);

            Cache.NamePlayerCache[GetTrueName(userData.CharacterName.ToString().ToLower())] = playerData;
            Cache.SteamPlayerCache[userData.PlatformId] = playerData;
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

        public static string GetNameFromSteamID(ulong SteamID)
        {
            if (Cache.SteamPlayerCache.TryGetValue(SteamID, out var data))
            {
                return data.CharacterName.ToString();
            }
            else
            {
                return null;
            }
        }

        public static ulong GetSteamIDFromName(string name)
        {
            if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out var data))
            {
                return data.SteamID;
            }
            else
            {
                return 0;
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

        public static bool FindPlayer(string name, bool mustOnline, out Entity playerEntity, out Entity userEntity)
        {
            EntityManager entityManager = Plugin.Server.EntityManager;

            //-- Way of the Cache
            if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out var data))
            {
                playerEntity = data.CharEntity;
                userEntity = data.UserEntity;
                if (mustOnline)
                {
                    var userComponent = entityManager.GetComponentData<User>(userEntity);
                    if (!userComponent.IsConnected)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                playerEntity = empty_entity;
                userEntity = empty_entity;
                return false;
            }
        }
        
        public static bool FindPlayer(ulong steamid, bool mustOnline, out Entity playerEntity, out Entity userEntity)
        {
            EntityManager entityManager = Plugin.Server.EntityManager;

            //-- Way of the Cache
            if (Cache.SteamPlayerCache.TryGetValue(steamid, out var data))
            {
                playerEntity = data.CharEntity;
                userEntity = data.UserEntity;
                if (mustOnline)
                {
                    var userComponent = entityManager.GetComponentData<User>(userEntity);
                    if (!userComponent.IsConnected)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                playerEntity = empty_entity;
                userEntity = empty_entity;
                return false;
            }
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
            var entity = VWorld.Server.EntityManager.CreateEntity(
                    ComponentType.ReadWrite<FromCharacter>(),
                    ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
                );

            VWorld.Server.EntityManager.SetComponentData<FromCharacter>(entity, new()
            {
                User = ctx.Event.SenderUserEntity,
                Character = ctx.Event.SenderCharacterEntity
            });

            VWorld.Server.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new()
            {
                Position = new float3(position.x, position.y, position.z),
                Target = PlayerTeleportDebugEvent.TeleportTarget.Self
            });
        }
        
        public static bool IsInCastle(Entity user)
        {
            // TODO check if these can better check if someone is in a castle:
            // Helper.ismounted || helper.isInCastle 
            // ProjectM.Mounter
            // ProjectM.Resident
            // ProjectM.Residency
            
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
                    // Item was equipped, set level.
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

        // For stats that reduce as a multiplier of 1 - their value, so that a value of 0.5 halves the stat, and 0.75 quarters it.
        // I do this so that we can compute linear increases to a formula of X/(X+Y) where Y is the amount for +100% effectivness and X is the stat value
        public static HashSet<UnitStatType> inverseMultiplierStats = new()
            {
                // TODO check these, but the latest patch notes suggest that there are no longer any inverse stats
                // UnitStatType.PrimaryCooldownModifier,
                // UnitStatType.WeaponCooldownRecoveryRate,
                // UnitStatType.SpellCooldownRecoveryRate,
                // UnitStatType.UltimateCooldownRecoveryRate
                /*,
                UnitStatType.PhysicalResistance,
                UnitStatType.SpellResistance,
                UnitStatType.ResistVsBeasts,
                UnitStatType.ResistVsCastleObjects,
                UnitStatType.ResistVsDemons,
                UnitStatType.ResistVsHumans,
                UnitStatType.ResistVsMechanical,
                UnitStatType.ResistVsPlayerVampires,
                UnitStatType.ResistVsUndeads,
                UnitStatType.BloodDrain,
                UnitStatType.ReducedResourceDurabilityLoss
                */
            };

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
