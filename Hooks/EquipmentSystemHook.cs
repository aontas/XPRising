using HarmonyLib;
using ProjectM.Gameplay.Systems;
using Unity.Entities;
using Unity.Collections;
using ProjectM;
using BepInEx.Logging;
using XPRising.Utils;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Hooks;

[HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
public class WeaponLevelSystemSpawnPatch
{
     private static void Prefix(WeaponLevelSystem_Spawn __instance)
     {
         if (Plugin.ExperienceSystemActive)
         {
             Plugin.Log(LogSystem.Buff, LogLevel.Info, "WeaponLevelSystem spawn");
             var entityManager = __instance.EntityManager;
             var entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
             foreach (var entity in entities){
                 // Remove any weapon level, as we set this manually to generate the player level.
                 if (entityManager.TryGetComponentData<WeaponLevel>(entity, out var weaponLevel))
                 {
                     weaponLevel.Level = 0;
                     entityManager.SetComponentData(entity, weaponLevel);
                 }
             }
         }
     }
}

[HarmonyPatch(typeof(BuffByItemCategoryCountSystem), nameof(BuffByItemCategoryCountSystem.OnUpdate))]
public class BuffByItemCategoryCountSystemPatch
{
    private static void Postfix(BuffByItemCategoryCountSystem __instance)
    {
        if (Plugin.ExperienceSystemActive)
        {
            Plugin.Log(Plugin.LogSystem.Buff, LogLevel.Info, "Post BuffByItemCatCount");
            var entityManager = Plugin.Server.EntityManager;
            var entities = __instance.__query_342315204_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var inventoryChangedEvent = entityManager.GetComponentData<InventoryChangedEvent>(entity);
                // We only care about events that "remove" items from inventory (and move them to the user's equip)
                // This only seems to work for armour/magic/cloaks. Weapons still stay in the inventory location when
                // equipped, so we also need the WeaponLevelSystem_Spawn patch above to handle them.
                if (inventoryChangedEvent.ChangeType != InventoryChangedEventType.Removed) continue;
                DebugTool.LogPrefabGuid(inventoryChangedEvent.Item);
                
                DumpItemLevels(__instance.EntityManager, inventoryChangedEvent.ItemEntity);
            }
        }
    }

    private static void DumpItemLevels(EntityManager entityManager, Entity entity)
    {
        entityManager.TryGetComponentData<Equippable>(entity, out var equippable);
        if (equippable.EquipBuff.Equals(Entity.Null)) return;

        if (entityManager.TryGetComponentData<ArmorLevel>(equippable.EquipBuff, out var armorLevel))
        {
            armorLevel.Level = 0;
            entityManager.SetComponentData(equippable.EquipBuff, armorLevel);
        }
        if (entityManager.TryGetComponentData<WeaponLevel>(equippable.EquipBuff, out var weaponLevel))
        {
            weaponLevel.Level = 0;
            entityManager.SetComponentData(equippable.EquipBuff, weaponLevel);
        }
        if (entityManager.TryGetComponentData<SpellLevel>(equippable.EquipBuff, out var spellLevel))
        {
            spellLevel.Level = 0;
            entityManager.SetComponentData(equippable.EquipBuff, spellLevel);
        }
    }
}