using BepInEx.Logging;
using ProjectM.Network;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Utils;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Commands
{
    public static class PowerUpCommands {
        [Command("powerup", "pu", "<player_name> <max hp> <p.atk> <s.atk> <p.def> <s.def>", "Buff player with the given values.", adminOnly:true)]
        public static void PowerUpCommand(ChatCommandContext ctx, string name, string flag, float MaxHP = 0, float PATK = 0, float SATK = 0, float PDEF = 0, float SDEF = 0){

            Plugin.Log(Plugin.LogSystem.PowerUp, LogLevel.Info, "Beginning PowerUp Command");
            Plugin.Log(Plugin.LogSystem.PowerUp, LogLevel.Info, $"Arguments are as follows: {name}, {flag}, {MaxHP}, {PATK}, {SATK}, {PDEF}, {SDEF}");

            if (!PlayerCache.FindPlayer(name, false, out var playerEntity, out var userEntity))
            {
                throw ctx.Error("Specified player not found.");
            }
            ulong steamID;
            Plugin.Log(Plugin.LogSystem.PowerUp, LogLevel.Info, "Trying to get steam ID");
            if (Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user))
            {
                steamID = user.PlatformId;
            }
            else
            {
                throw ctx.Error($"Steam ID for {name} could not be found!");
            }

            var powerUpData = new PowerUpData(){
                Name = name,
                MaxHP = MaxHP,
                PATK = PATK,
                PDEF = PDEF,
                SATK = SATK,
                SDEF = SDEF
            };

            Database.PowerUpList[steamID] = powerUpData;
            Helper.ApplyBuff(userEntity, playerEntity, Helper.AppliedBuff);
            ctx.Reply($"PowerUp added to {name}.");
        }
        
        [Command("powerdown", "pd", "<playerName>", "Remove power up buff from the player.", adminOnly:true)]
        public static void PowerDownCommand(ChatCommandContext ctx, string name)
        {
            if (!PlayerCache.FindPlayer(name, false, out var playerEntity, out var userEntity))
            {
                throw ctx.Error("Specified player not found.");
            }
            ulong steamID;
            Plugin.Log(Plugin.LogSystem.PowerUp, LogLevel.Info, "Trying to get steam ID");
            if (Plugin.Server.EntityManager.TryGetComponentData<User>(userEntity, out var user))
            {
                steamID = user.PlatformId;
            }
            else
            {
                throw ctx.Error($"Steam ID for {name} could not be found!");
            }

            Database.PowerUpList.Remove(steamID);
            Helper.ApplyBuff(userEntity, playerEntity, Helper.AppliedBuff);
            ctx.Reply($"PowerUp removed from {name}.");
        }
    }
}
