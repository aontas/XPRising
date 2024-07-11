using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using HarmonyLib;
using XPShared.Hooks;
using XPShared.Transport;

namespace XPShared;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.Bloodstone")]
public class Plugin : BasePlugin
{
    internal static readonly string ClientNonce = $"{Random.Shared.Next()}"; 
    private static ManualLogSource _logger;
    private static Harmony _harmonyChatPatch;
    
    public override void Load()
    {
        // Ensure the logger is accessible in static contexts.
        _logger = base.Log;

        Log(LogLevel.Info, "Initialising XPShared");
        MessageRegistry.RegisterMessage();
        if (VWorld.IsClient)
        {
            Log(LogLevel.Debug, "XPShared is client");
            _harmonyChatPatch = Harmony.CreateAndPatchAll(typeof(ClientChatSystemPatch));
        }
    }
    
    public override bool Unload()
    {
        MessageRegistry.UnregisterMessages();
        if (VWorld.IsClient) _harmonyChatPatch.UnpatchSelf();
        
        return true;
    }
    
    public new static void Log(LogLevel level, string message)
    {
        _logger.Log(level, $"{DateTime.Now:u}: [XPShared] {message}");
    }
}