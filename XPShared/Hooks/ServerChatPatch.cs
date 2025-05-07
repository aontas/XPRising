#nullable enable
using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using XPShared.Services;

namespace XPShared.Hooks;

public static class ServerChatPatch
{
    private static EntityManager EntityManager => Plugin.World.EntityManager;

    private static Harmony? _harmony;

    public static void Initialize()
    {
        if (_harmony != null)
            throw new Exception("Detour already initialized. Plugin will do this for you.");

        _harmony = Harmony.CreateAndPatchAll(typeof(ServerChatPatch), MyPluginInfo.PLUGIN_GUID);
    }

    public static void Uninitialize()
    {
        if (_harmony == null)
            throw new Exception("Detour wasn't initialized. Are you trying to unload twice?");

        _harmony.UnpatchSelf();
    }

    // Make sure you intercept before CrimsonChatFilter as it is very greedy in what it matches and subsequently deletes from the queue
    [HarmonyBefore("CrimsonChatFilter")]
    [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ChatMessageSystem __instance)
    {
        if (!Plugin.IsInitialised || !Plugin.IsServer) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        NativeArray<ChatMessageEvent> chatMessageEvents = __instance.EntityQueries[0].ToComponentDataArray<ChatMessageEvent>(Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var chatMessageEvent = chatMessageEvents[i];
                
                Plugin.Log(LogLevel.Debug, chatMessageEvent.MessageText.ToString());
                
                var steamId = entity.Read<FromCharacter>().Character.GetSteamId();
                if (ChatService.DeserialiseMessage(chatMessageEvent.MessageText.ToString(), steamId))
                {
                    EntityManager.DestroyEntity(entity);
                }
            }
        }
        finally
        {
            entities.Dispose();
            chatMessageEvents.Dispose();
        }
    }
}