﻿using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using ClientUI.Client;
using ClientUI.Client.UI;
using HarmonyLib;
using ProjectM.Network;
using System;
using ClientUI.Hooks;
using ClientUI.Transport;
using ClientUI.Transport.Handlers;
using ClientUI.Transport.Messages;
using Unity.Entities;

namespace ClientUI
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.VampireCommandFramework", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("gg.deca.Bloodstone")]
    public class Plugin : BasePlugin
    {
        private static ManualLogSource _logger;
        internal static Plugin Instance { get; private set; }
        internal static readonly string ClientNonce = $"{Random.Shared.Next()}"; 
        public static Harmony _harmony;
        
        private static TimerClient _timer;

        public override void Load()
        {
            Instance = this;

            // Ensure the logger is accessible in static contexts.
            _logger = base.Log;
            
            MessageRegistry.RegisterMessage();
            _harmony = Harmony.CreateAndPatchAll(typeof(ClientChatSystemPatch));
            Harmony.CreateAndPatchAll(typeof(GameManangerPatch));
            // GameData.OnInitialize += GameDataOnInitialize;
            // GameData.OnDestroy += GameDataOnDestroy;

            if (VWorld.IsClient)
            {
                UIManager.Initialize();
            }

            if (VWorld.IsServer)
            {
                //CommandRegistry.RegisterCommandType(typeof(TestCommand));
            }


            // Plugin startup logic
            Plugin.Log(LogLevel.Info, $"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static void ServerSetBarData(User playerCharacter, string bar, int level, float progressPercentage, string tooltip)
        {
            var msg = new ProgressSerialisedMessage();
            msg.Label = bar;
            msg.ProgressPercentage = progressPercentage;
            msg.Level = level;
            msg.Tooltip = tooltip;
            ServerMessageActions.Send(playerCharacter, msg);
        }

        public override bool Unload()
        {

            if (VWorld.IsServer)
            {
                // Events.OnAddExp -= ServerAddXP;
            }
            else
            {
                
            }
            MessageRegistry.UnregisterMessages();
            _harmony.UnpatchSelf();

            // GameData.OnDestroy -= GameDataOnDestroy;
            // GameData.OnInitialize -= GameDataOnInitialize;
            return true;
        }

        public static void GameDataOnInitialize(World world)
        {
            if (VWorld.IsClient)
            {
                _timer = new TimerClient();

                _timer.Start(
                world =>
                {
                    Plugin.Log(LogLevel.Info, "Starting UI...");
                    ClientMessageActions.Send(new ClientAction(ClientAction.ActionType.Connect, $"{ClientNonce}"));
                    _timer.Stop();
                },
                input =>
                {
                    if (input is not int secondAutoUIr)
                    {
                        Plugin.Log(LogLevel.Error, "Starting UI timer delay function parameter is not a valid integer");
                        return TimeSpan.MaxValue;
                    }

                    var seconds = 5;
                    Plugin.Log(LogLevel.Info, $"Next Starting UI will start in {seconds} seconds.");
                    return TimeSpan.FromSeconds(seconds);
                });
            }

            if (VWorld.IsServer)
            {
                // Events.OnAddExp += ServerAddXP;
            }
        }

        private static void GameDataOnDestroy()
        {
            //Logger.LogInfo("GameDataOnDestroy");
        }
        
        public new static void Log(LogLevel level, string message)
        {
            _logger.Log(level, $"{DateTime.Now:u}: {message}");
        }
    }
}