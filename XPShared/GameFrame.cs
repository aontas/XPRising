#nullable enable
using BepInEx.Logging;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace XPShared;

public delegate void GameFrameUpdateEventHandler();

/// <summary>
/// This class provides hooks for the Update and LateUpdate frame
/// functions invoked by Unity. Original code comes from [Bloodstone](https://thunderstore.io/c/v-rising/p/deca/Bloodstone/)
/// </summary>
public class GameFrame : MonoBehaviour
{
    private static GameFrame? _instance;

    /// <summary>
    /// This event will be emitted on every Update call. It may be
    /// more performant to inject your own MonoBehavior if you do not
    /// need to be invoked every frame.
    /// </summary>
    public static event GameFrameUpdateEventHandler? OnUpdate;

    /// <summary>
    /// This event will be emitted on every LateUpdate call. The same
    /// considerations as with the OnUpdate event apply. 
    /// </summary>
    public static event GameFrameUpdateEventHandler? OnLateUpdate;

    void Update()
    {
        try
        {
            OnUpdate?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Error, "Error dispatching OnUpdate event:");
            Plugin.Log(LogLevel.Error, ex.ToString());
        }
    }

    void LateUpdate()
    {
        try
        {
            OnLateUpdate?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Error, "Error dispatching OnLateUpdate event:");
            Plugin.Log(LogLevel.Error, ex.ToString());
        }
    }

    public static void Initialize(Plugin plugin)
    {
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<GameFrame>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<GameFrame>();
        }

        _instance = plugin.AddComponent<GameFrame>();
    }

    public static void Uninitialize()
    {
        OnUpdate = null;
        OnLateUpdate = null;
        Destroy(_instance);
        _instance = null;
    }
}