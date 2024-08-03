using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.Network;
using XPShared.Transport;
using XPShared.Transport.Messages;

namespace XPShared;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.Bloodstone")]
public class Plugin : BasePlugin
{ 
    private static ManualLogSource _logger;
    
    public static bool IsDebug { get; private set; } = false;
    
    public override void Load()
    {
        // Ensure the logger is accessible in static contexts.
        _logger = base.Log;

        MessageHandler.RegisterClientAction();
        
        var assemblyConfigurationAttribute = typeof(Plugin).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        var buildConfigurationName = assemblyConfigurationAttribute?.Configuration;
        IsDebug = buildConfigurationName == "Debug";
        MessageUtils.OnClientConnectionEvent += character =>
        {
            MessageHandler.ServerReceiveFromClient(character, new ClientAction(ClientAction.ActionType.Connect, ""));
        };
        Log(LogLevel.Info, $"Plugin is loaded [version: {MyPluginInfo.PLUGIN_VERSION}]");
    }
    
    public override bool Unload()
    {
        MessageHandler.UnregisterClientAction();
        
        return true;
    }
    
    public new static void Log(LogLevel level, string message)
    {
        if (!IsDebug && level > LogLevel.Info) return;
        _logger.Log(level, $"{DateTime.Now:u}: [XPShared] {message}");
    }
}