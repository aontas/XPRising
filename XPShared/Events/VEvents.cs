using BepInEx.Logging;
using Unity.Entities;

namespace XPShared.Events;

// Concepts taking from @mfoltz updates for Bloodstone. Preparing standardisation support.
public class VEvents
{
#nullable enable
    public interface IGameEvent { } 
    public abstract class DynamicGameEvent : EventArgs, IGameEvent
    {
        public Entity Source { get; set; }
        public Entity? Target { get; set; }

        readonly Dictionary<Type, object> _components = [];
        public void AddComponent<T>(T component) where T : struct => _components[typeof(T)] = component;
        public bool TryGetComponent<T>(out T component) where T : struct
        {
            if (_components.TryGetValue(typeof(T), out var boxed) && boxed is T cast)
            {
                component = cast;
                return true;
            }

            component = default;
            return false;
        }
    }
    public abstract class GameEvent<T> where T : IGameEvent, new()
    {
        public delegate void EventModuleHandler(T args);
        public event EventModuleHandler? EventHandler;
        public bool HasSubscribers => EventHandler != null;
        protected void Raise(T args)
        {
            EventHandler?.Invoke(args);
        }
        public void Subscribe(EventModuleHandler handler) => EventHandler += handler;

        public void Unsubscribe(EventModuleHandler handler) => EventHandler -= handler;
        public abstract void Initialize();
        public abstract void Uninitialize();
    }
    public static class ModuleRegistry
    {
        static readonly Dictionary<Type, object> _modules = [];
        public static void Register<T>(GameEvent<T> module) where T : IGameEvent, new()
        {
            module.Initialize();
            _modules[typeof(T)] = module;
        }
        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent, new()
        {
            if (_modules.TryGetValue(typeof(T), out var module))
            {
                ((GameEvent<T>)module).Subscribe(handler.Invoke);
            }
            else
            {
                Plugin.Log(LogLevel.Warning, $"[Subscribe] No registered module for event type! ({typeof(T).Name})");
            }
        }
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent, new()
        {
            if (_modules.TryGetValue(typeof(T), out var module))
            {
                ((GameEvent<T>)module).Unsubscribe(handler.Invoke);
            }
            else
            {
                Plugin.Log(LogLevel.Warning, $"[Unsubscribe] No registered module for event type! ({typeof(T).Name})");
            }
        }
        public static bool TryGet<T>(out GameEvent<T>? module) where T : IGameEvent, new()
        {
            if (_modules.TryGetValue(typeof(T), out var result))
            {
                Plugin.Log(LogLevel.Warning, $"try get counts: {_modules.Count})");
                module = (GameEvent<T>)result;
                return true;
            }

            module = default;
            return false;
        }
    }

    static bool _initialized = false;
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
    }
#nullable restore
}