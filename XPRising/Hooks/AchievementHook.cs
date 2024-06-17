using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using XPRising.Utils;

namespace XPRising.Hooks;

[HarmonyPatch]
public class AchievementHook
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClaimAchievementSystem), nameof(ClaimAchievementSystem.HandleEvent))]
    private static void ClaimAchievementSystemPostfix(
        ClaimAchievementSystem __instance,
        EntityCommandBuffer commandBuffer,
        PrefabGUID claimAchievementGUID,
        FromCharacter fromCharacter,
        bool forceClaim)
    {
        if (claimAchievementGUID.GuidHash == -122882616) // achievement prior to GettingReadyForTheHunt
        {
            var characterEntity = fromCharacter.Character;
            var userEntity = fromCharacter.User;
            if (!Plugin.Server.EntityManager.TryGetComponentData<AchievementOwner>(userEntity, out var achievementOwner)) return;
            
            var achievementOwnerEntity = achievementOwner.Entity._Entity;
            var entityCommandBuffer = Helper.EntityCommandBufferSystem.CreateCommandBuffer();
            PrefabGUID achievementPrefabGUID = new(560247139); // Journal_GettingReadyForTheHunt
            __instance.CompleteAchievement(entityCommandBuffer, achievementPrefabGUID, userEntity, characterEntity, achievementOwnerEntity, false, true);
        }
    }
}