#nullable enable
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;
using XPShared.Services;

namespace XPShared.Hooks;

public static class ClientChatPatch
{
    private static Harmony? _harmony;
    
    private static Entity _localCharacter = Entity.Null;
    private static Entity _localUser = Entity.Null;
    public static Entity LocalCharacter =>
        _localCharacter != Entity.Null
            ? _localCharacter
            : (ConsoleShared.TryGetLocalCharacterInCurrentWorld(out _localCharacter, Plugin.World)
                ? _localCharacter
                : Entity.Null);
    public static Entity LocalUser =>
        _localUser != Entity.Null
            ? _localUser
            : (ConsoleShared.TryGetLocalUserInCurrentWorld(out _localUser, Plugin.World)
                ? _localUser
                : Entity.Null);
    public static ulong LocalSteamId => _localUser != Entity.Null ? _localUser.GetSteamId() : 0;

    public static void Initialize()
    {
        if (_harmony != null)
            throw new Exception("Detour already initialized. Plugin will do this for you.");

        _harmony = Harmony.CreateAndPatchAll(typeof(ClientChatPatch), MyPluginInfo.PLUGIN_GUID);
    }

    public static void Uninitialize()
    {
        if (_harmony == null)
            throw new Exception("Detour wasn't initialized. Are you trying to unload twice?");

        _harmony.UnpatchSelf();
    }

    /// <summary>
    /// Monitor the chat system, collecting and deserialising supported messages.
    /// Once a message is successfully deserialised, destroy the event as the message is not something that will be
    /// useful for users to see in their chat logs.
    /// Make sure you patch to be run before CrimsonChatFilter as it is very greedy in what it matches and removes
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyBefore("CrimsonChatFilter")]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    private static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (!Plugin.IsClient) return;
        
        var entities = __instance._ReceiveChatMessagesQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var ev = __instance.EntityManager.GetComponentData<ChatMessageServerEvent>(entity);
            if (ev.MessageType == ServerChatMessageType.System && ChatService.DeserialiseMessage(ev.MessageText.ToString(), LocalSteamId))
            {
                // Remove this as it is an internal message that the user is unlikely wanting to see in their chat
                __instance.EntityManager.DestroyEntity(entity);
            }
        }
    }

    /// <summary>
    /// Resets the local User/Character properties when connecting to a server.
    /// This is useful to call between server connections as leaving a server can invalidate the user entities.
    /// </summary>
    public static void ResetUser()
    {
        _localUser = Entity.Null;
        _localCharacter = Entity.Null;
    }
}