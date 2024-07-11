using HarmonyLib;
using ProjectM.Network;
using ProjectM.UI;
using Unity.Collections;
using XPShared.Transport;

namespace XPShared.Hooks;

public class ClientChatSystemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    private static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        var entities = __instance.__query_172511197_1.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var ev = __instance.EntityManager.GetComponentData<ChatMessageServerEvent>(entity);
            if (ev.MessageType == ServerChatMessageType.System &&
                MessageRegistry.ReadMessageHeader(ev.MessageText.ToString(), out var userNonce, out var type, out var serialisedMessage))
            {
                // This is a valid message and the message matches our nonce
                if (userNonce == Plugin.ClientNonce) MessageHandler.ClientReceiveFromServer(type, serialisedMessage);
                
                // Regardless of whether it matches our nonce, we should remove this as it is an internal message that
                // the user is unlikely wanting to see in their chat
                __instance.EntityManager.DestroyEntity(entity);
            }
        }
    }
}