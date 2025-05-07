using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ProjectM.Scripting;
using Unity.Entities;
using UnityEngine;
using XPShared.Hooks;
using XPShared.Services;

namespace XPShared;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private static SystemService _systemService;
    private static ManualLogSource _logger;
    private static Harmony _harmonyBootPatch;
    
    public static World World;
    public static SystemService SystemService => _systemService ??= new(World);
    public static ClientGameManager ClientGameManager => SystemService.ClientScriptMapper._ClientGameManager;
    
    public static bool IsDebug { get; private set; } = false;
    
    public static bool IsServer => Application.productName == "VRisingServer";

    public static bool IsClient => Application.productName == "VRising";

    public static bool IsInitialised { get; private set; } = false;

    public override void Load()
    {
        // Ensure the logger is accessible in static contexts.
        _logger = base.Log;
        
        if (IsClient)
        {
            ClientChatPatch.Initialize();
        }
        else if (IsServer)
        {
            ServerChatPatch.Initialize();
            ChatService.ListenForClientRegister();
        }
        _harmonyBootPatch = Harmony.CreateAndPatchAll(typeof(GameManangerPatch));
        
        var assemblyConfigurationAttribute = typeof(Plugin).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        var buildConfigurationName = assemblyConfigurationAttribute?.Configuration;
        IsDebug = buildConfigurationName == "Debug";
        
        GameFrame.Initialize(this);
        
        Log(LogLevel.Info, $"Plugin is loaded [version: {MyPluginInfo.PLUGIN_VERSION}]");
    }
    
    public override bool Unload()
    {
        if (IsClient)
        {
            ClientChatPatch.Uninitialize();
        }
        else if (IsServer)
        {
            ServerChatPatch.Uninitialize();
        }
        
        GameFrame.Uninitialize();
        
        return true;
    }
    
    public static void GameDataOnInitialize(World world)
    {
        World = world;
        IsInitialised = true;
    }
    
    public new static void Log(LogLevel level, string message)
    {
        if (!IsDebug && level > LogLevel.Info) return;
        _logger.Log(level, $"{DateTime.Now:u}: [XPShared] {message}");
    }
}