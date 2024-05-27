using System;
using System.Linq;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Utils;

namespace XPRising.Commands {
    [CommandGroup("waypoint", "wp")]
    public static class WaypointCommands {
        public static int WaypointLimit = 3;

        [Command("go", "g", "<waypoint name>", "Teleports you to the specified waypoint", adminOnly: false)]
        public static void GoToWaypoint(ChatCommandContext ctx, string waypoint) {
            var steamID = ctx.Event.User.PlatformId;
            if (Cache.PlayerInCombat(steamID) && !ctx.IsAdmin) {
                ctx.Reply("Unable to use waypoint! You're in combat!");
                return;
            }

            if (Database.Waypoints.TryGetValue(waypoint + "_" + steamID, out var wpData)) {
                if (WaypointLimit <= 0 && !ctx.IsAdmin) {
                    ctx.Reply("Personal Waypoints are forbidden to you.");
                    if(Database.Waypoints.Remove(waypoint + "_" + steamID)) {
                        ctx.Reply("The forbidden waypoint has been destroyed.");
                    }
                    return;
                }
                Helper.TeleportTo(ctx, wpData.ToFloat3());
                return;
            }

            if (Database.Waypoints.TryGetValue(waypoint, out wpData)) {
                Helper.TeleportTo(ctx, wpData.ToFloat3());
                return;
            }
            ctx.Reply("Waypoint not found.");
        }


        [Command("set", "s", "<waypoint name>", "Creates the specified personal waypoint", adminOnly: false)]
        public static void SetWaypoint(ChatCommandContext ctx, string name) {
            if(WaypointLimit <= 0 && !ctx.IsAdmin) {
                throw ctx.Error("You may not create waypoints.");
            }
            var steamID = ctx.Event.User.PlatformId;

            var waypointCount = Database.Waypoints.Keys.Count(wpName => wpName.EndsWith($"{steamID}"));
            if (waypointCount >= WaypointLimit && !ctx.IsAdmin) {
                throw ctx.Error("You already have reached your total waypoint limit.");
            }
            var waypointName = name + "_" + steamID;
            if (Database.Waypoints.TryGetValue(name, out _)) {
                ctx.Reply($"You already have a waypoint with the same name.");
                return;
            }
            var location = Plugin.Server.EntityManager.GetComponentData<LocalToWorld>(ctx.Event.SenderCharacterEntity).Position;
            Database.Waypoints[waypointName] = new WaypointData(location);
            ctx.Reply("Successfully added Waypoint.");
        }

        [Command("set global", "sg", "<waypoint name>", "Creates the specified global waypoint", adminOnly: false)]
        public static void SetGlobalWaypoint(ChatCommandContext ctx, string name) {
            var location = Plugin.Server.EntityManager.GetComponentData<LocalToWorld>(ctx.Event.SenderCharacterEntity).Position;
            Database.Waypoints[name] = new WaypointData(location);
            ctx.Reply("Successfully added Waypoint.");
        }

        [Command("remove global", "rg", "<waypoint name>", "Removes the specified global waypoint", adminOnly: false)]
        public static void RemoveGlobalWaypoint(ChatCommandContext ctx, string name) {
            Database.Waypoints.Remove(name);
            ctx.Reply("Successfully removed Waypoint.");
        }

        [Command("remove", "r", "<waypoint name>", "Removes the specified personal waypoint", adminOnly: false)]
        public static void RemoveWaypoint(ChatCommandContext ctx, string name) {
            var steamID = ctx.Event.User.PlatformId;
            var waypointName = name + "_" + steamID;
            if (!Database.Waypoints.TryGetValue(waypointName, out _)) {
                ctx.Reply($"You do not have any waypoint with this name.");
                return;
            }

            Database.Waypoints.Remove(name);
            ctx.Reply("Successfully removed Waypoint.");
        }

        [Command("list", "l", "", "lists waypoints available to you", adminOnly: false)]
        public static void ListWaypoints(ChatCommandContext ctx) {
            var steamID = ctx.Event.User.PlatformId;
            int totalWaypoints = 0;
            int count = 0;
            int wpPerMsg = 5;
            string reply = "";
            foreach (var wp in Database.Waypoints) {
                if(!wp.Key.Contains("_")) {
                    if (count < wpPerMsg) {
                        reply += $" - <color={Output.LightYellow}>{wp.Key}</color> [<color={Output.Green}>Global</color>]";
                        count++;
                    } else {
                        ctx.Reply(reply);
                        reply = "";
                        count = 0;
                    }
                    totalWaypoints++;
                }

                if (wp.Key.Contains(steamID.ToString())) {
                    if (count < wpPerMsg) {
                        string easyName = wp.Key.Substring(0, wp.Key.IndexOf("_", StringComparison.Ordinal));
                        reply += $" - <color={Output.LightYellow}>{easyName}</color>";
                        count++;
                    } else {
                        ctx.Reply(reply);
                        reply = "";
                        count = 0;
                    }
                    totalWaypoints++;
                }
            }
            if (count > 0) {
                ctx.Reply(reply);
            }
            if (totalWaypoints == 0) ctx.Reply("No waypoint available.");

        }
    }
}
