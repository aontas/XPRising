using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;

namespace XPShared.Events;

public static class ServerEvents
{
#nullable enable
    public static class BuffEvents
    {
        public class BuffGained : VEvents.IGameEvent
        {
            public PrefabGUID GUID { get; set; }
        }

        public class BuffLost : VEvents.IGameEvent
        {
            public PrefabGUID GUID { get; set; }
        }
    }

    public static class CombatEvents
    {
        public class CombatStart : VEvents.IGameEvent
        {
            public ulong SteamId { get; set; }
        }

        public class CombatEnd : VEvents.IGameEvent
        {
            public ulong SteamId { get; set; }
        }

        public class PlayerKillMob : VEvents.DynamicGameEvent
        {
        }
        
        public class PlayerKillModule : VEvents.GameEvent<PlayerKillMob>
        {
            private static PlayerKillModule? _instance;
            static Harmony? _harmony;
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            public PlayerKillModule()
            {
                _instance = this;
                VEvents.ModuleRegistry.Register(_instance);
            }
            public class Patch {
                [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
                public static void Postfix(DeathEventListenerSystem __instance)
                {
                    // If we have no subscribers don't worry about running a query
                    if (_instance == null || !_instance.HasSubscribers) return;
                    
                    NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);
                    foreach (DeathEvent ev in deathEvents) {
                        // DebugTool.LogEntity(ev.Died, "Death Event occured for:", LogSystem.Death);
                        // TODO check the following for Bloodcraft minor XP
                        //if (entity.TryGetComponent(out IsMinion isMinion) && isMinion.Value)

                        var killer = ev.Killer;
                        
                        // For this to count as a player kill, it must:
                        // - not be a minion
                        // - have a level
                        // - have a movement object
                        var ignoreAsKill = __instance.EntityManager.HasComponent<Minion>(ev.Died) || !__instance.EntityManager.HasComponent<UnitLevel>(ev.Died) || !__instance.EntityManager.HasComponent<Movement>(ev.Died);
                        
                        // If the killer is the victim, then we can skip trying to raise this event.
                        if (!ignoreAsKill && !killer.Equals(ev.Died))
                        {
                            // If the entity killing is a minion, switch the killer to the owner of the minion.
                            if (__instance.EntityManager.HasComponent<Minion>(killer))
                            {
                                if (__instance.EntityManager.TryGetComponentData<EntityOwner>(killer, out var entityOwner))
                                {
                                    killer = entityOwner.Owner;
                                }
                            }
                            
                            if (__instance.EntityManager.HasComponent<PlayerCharacter>(killer))
                            {
                                _instance?.Raise(new PlayerKillMob {Source = killer, Target = ev.Died});
                            }
                        }
                    }
                }
            }
        }
    }
#nullable restore
}