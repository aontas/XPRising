using ProjectM;
using ProjectM.Network;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Text.RegularExpressions;
using ProjectM.Scripting;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using VampireCommandFramework;
using ProjectM.CastleBuilding;
using Stunlock.Core;
using Unity.Transforms;
using XPRising.Hooks;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils.Prefabs;
using XPShared;

namespace XPRising.Utils
{
    public static class Helper
    {
        private static Entity empty_entity = new Entity();
        private static System.Random rand = new System.Random();
        
        public static ServerScriptMapper ServerScriptMapper { get; internal set; }
        public static ServerGameManager ServerGameManager { get; internal set; }
        public static EntityCommandBufferSystem EntityCommandBufferSystem { get; internal set; }
        public static ClaimAchievementSystem ClaimAchievementSystem { get; internal set; }
        public static EndSimulationEntityCommandBufferSystem EndSimECBSystem { get; internal set; }

        public static Regex rxName = new Regex(@"(?<=\])[^\[].*");

        public static void Initialise()
        {
            ServerScriptMapper = Plugin.Server.GetExistingSystemManaged<ServerScriptMapper>();
            ServerGameManager = ServerScriptMapper._ServerGameManager;
            ClaimAchievementSystem = Plugin.Server.GetExistingSystemManaged<ClaimAchievementSystem>();
            EntityCommandBufferSystem = Plugin.Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
            EndSimECBSystem = Plugin.Server.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        public static FixedString64Bytes GetTrueName(string name)
        {
            MatchCollection match = Helper.rxName.Matches(name);
            if (match.Count > 0)
            {
                name = match[^1].ToString();
            }
            return name;
        }
        
        // This refers to the localisation string for "{value} Experience"
        static readonly AssetGuid XPAssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb");
        static readonly float3 XPTextColour = new float3(1, 1, 0);
        public static void CreateXpText(float3 location, float value, Entity character, Entity userEntity)
        {
            var commandBuffer = EndSimECBSystem.CreateCommandBuffer();
            ScrollingCombatTextMessage.Create(Plugin.Server.EntityManager, commandBuffer, XPAssetGuid, location, XPTextColour, character, value);
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

        public static LazyDictionary<UnitStatType, float> GetAllStatBonuses(ulong steamID, Entity owner)
        {
            LazyDictionary<UnitStatType, float> statusBonus = new();
            
            if (Plugin.WeaponMasterySystemActive || Plugin.BloodlineSystemActive || Plugin.ExperienceSystemActive) GlobalMasterySystem.BuffReceiver(ref statusBonus, owner, steamID);
            return statusBonus;
        }
        
        // TODO check this list
        public static HashSet<UnitStatType> percentageStats = new()
            {
                UnitStatType.PhysicalCriticalStrikeChance,
                UnitStatType.SpellCriticalStrikeChance,
                UnitStatType.PhysicalCriticalStrikeDamage,
                UnitStatType.SpellCriticalStrikeDamage,
                UnitStatType.PhysicalLifeLeech,
                UnitStatType.PrimaryLifeLeech,
                UnitStatType.SpellLifeLeech,
                UnitStatType.PrimaryAttackSpeed,
                UnitStatType.PassiveHealthRegen,
                UnitStatType.ResourceYield
            };

        // TODO check this list
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
        
        public static ModifyUnitStatBuff_DOTS MakeModifyUnitStatBuff_DOTS(UnitStatType type, float value,
            ModificationType modType)
        {
            return new ModifyUnitStatBuff_DOTS
            {
                StatType = type,
                Value = value,
                ModificationType = modType,
                Modifier = 1,
                Id = ModificationId.NewId(0)
            };
        }

        public static void ApplyBuffs(Entity userEntity, Entity playerEntity, ulong steamId)
        {

            Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Applying XPRising Buffs");
            if (!userEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
            {
                Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Error, "entity did not have buffer");
                return;
            }

            // Clear the buffer before applying more stats as it is persistent
            buffer.Clear();

            // Should this be stored rather than calculated each time?
            var statusBonus = GetAllStatBonuses(steamId, playerEntity);
            foreach (var bonus in statusBonus)
            {
                buffer.Add(MakeModifyUnitStatBuff_DOTS(bonus.Key, bonus.Value, ModificationType.Add));
            }

            Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Done Adding, Buffer length: " + buffer.Length);
        }
    }
}
