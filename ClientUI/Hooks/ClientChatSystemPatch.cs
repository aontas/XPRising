using ClientUI.Transport.Handlers;
using HarmonyLib;
using ProjectM.Network;
using ProjectM.UI;
using Unity.Collections;

namespace ClientUI.Hooks;

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
            if (ev.MessageType == ServerChatMessageType.System && ClientMessageActions.HandleChatMessage(ev.MessageText.ToString()))
            {
                __instance.EntityManager.DestroyEntity(entity);
            }
        }
    }
}