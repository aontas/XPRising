using Bloodstone.API;
using ProjectM.Network;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    public static class PlayerInfoCommands
    {
        private static EntityManager _entityManager = VWorld.Server.EntityManager;

        [Command(name: "playerinfo", shortHand: "pi", adminOnly: false, usage: "[PlayerName]", description: "Display the player information details.")]
        public static void PlayerInfoCommand(ChatCommandContext ctx, string playerName = null)
        {
            var user = ctx.Event.User;
            var playerEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ctx.Event.SenderUserEntity;

            if (playerName != null)
            {
                if (!Helper.FindPlayer(playerName, false, out playerEntity, out userEntity))
                {
                    throw ctx.Error("Player not found.");
                }

                user = _entityManager.GetComponentData<User>(userEntity);
            }
            
            var steamID = user.PlatformId;
            var name = user.CharacterName.ToString();
            var playerEntityString = $"{playerEntity.Index.ToString()}:{playerEntity.Version.ToString()}";
            var userEntityString = $"{userEntity.Index.ToString()}:{userEntity.Version.ToString()}";
            var ping = _entityManager.GetComponentData<Latency>(playerEntity).Value;
            var position = _entityManager.GetComponentData<LocalTransform>(playerEntity).Position;

            ctx.Reply($"Name: <color={Output.White}>{name}</color>");
            ctx.Reply($"SteamID: <color={Output.White}>{steamID:D}</color>");
            ctx.Reply($"Latency: <color={Output.White}>{ping:F3}</color>s");
            ctx.Reply($"Admin: <color={Output.White}>{user.IsAdmin.ToString()}</color>s");
            ctx.Reply("-- Position --");
            ctx.Reply($"X: <color={Output.White}>{position.x:F2}</color> " +
                      $"Y: <color={Output.White}>{position.y:F2}</color> " +
                      $"Z: <color={Output.White}>{position.z:F2}</color>");
            ctx.Reply($"-- <color={Output.White}>Entities</color> --");
            ctx.Reply($"Char Entity: <color={Output.White}>{playerEntityString}");
            ctx.Reply($"User Entity: <color={Output.White}>{userEntityString}");
            if (Plugin.ExperienceSystemActive)
            {
                var currentXp = ExperienceSystem.GetXp(steamID);
                var currentLevel = ExperienceSystem.ConvertXpToLevel(currentXp);
                ExperienceSystem.GetLevelAndProgress(currentXp, out _, out var xpEarned, out var xpNeeded);
                ctx.Reply($"-- <color={Output.White}>Experience --");
                ctx.Reply($"Level: <color={Output.White}>{currentLevel.ToString()}</color> [<color={Output.White}>{xpEarned.ToString()}</color>/<color={Output.White}>{xpNeeded.ToString()}</color>]");
            }
        }
    }
}
