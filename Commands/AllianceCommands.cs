﻿using System;
using System.Linq;
using ProjectM;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Utils;

namespace XPRising.Commands;

public static class AllianceCommands
{
    private static EntityManager _entityManager = Plugin.Server.EntityManager;
    
    [Command("group show", "gs", "", "Prints out info about your current group and your group preferences", adminOnly: false)]
    public static void ShowGroupInformation(ChatCommandContext ctx)
    {
        if (!Plugin.PlayerGroupsActive)
        {
            throw ctx.Error("Player groups not allowed.");
        }
        var playerCharacter = ctx.Event.SenderCharacterEntity;

        var groupDetails = "You are not currently in a group.";
        if (Cache.AlliancePlayerToGroupId.TryGetValue(playerCharacter, out var currentGroupId))
        {
            groupDetails = Cache.AlliancePlayerGroups[currentGroupId].PrintAllies();
        }
        
        var alliancePreferences = Database.AlliancePlayerPrefs[playerCharacter];

        var pendingInviteDetails = "You currently have no pending group invites.";
        if (Cache.AlliancePendingInvites.TryGetValue(playerCharacter, out var pendingInvites) && pendingInvites.Count > 0)
        {
            var invitesList = pendingInvites.OrderBy(x => x.InvitedAt).Select((invite, i) => $"{i}: {invite.InviterName} at {invite.InvitedAt}");
            pendingInviteDetails = $"Current invites:\n{string.Join("\n", invitesList)}";
        }
        
        ctx.Reply($"{pendingInviteDetails}\n\nPreferences:\n{alliancePreferences.ToString()}\n\n{groupDetails}");
    }

    [Command("group ignore", "gi", "", "Toggles ignoring group invites for yourself.", adminOnly: false)]
    public static void ToggleIgnoreGroups(ChatCommandContext ctx)
    {
        if (!Plugin.PlayerGroupsActive)
        {
            throw ctx.Error("Player groups not allowed.");
        }
        var playerCharacter = ctx.Event.SenderCharacterEntity;

        var prefs = Database.AlliancePlayerPrefs[playerCharacter];
        prefs.IgnoringInvites = !prefs.IgnoringInvites;
        Database.AlliancePlayerPrefs[playerCharacter] = prefs;
        
        if (prefs.IgnoringInvites)
        {
            ctx.Reply("You are now ignoring all group invites.");
        }
        else
        {
            ctx.Reply("You are now listening for all group invites.");
        }
    }

    [Command("group add", "ga", "[playerName]", "Adds a player to your group. Leave blank to add all \"close\" players to your group.", adminOnly: false)]
    public static void GroupAddOrCreate(ChatCommandContext ctx, string playerName = "")
    {
        if (!Plugin.PlayerGroupsActive)
        {
            throw ctx.Error("Player groups not allowed.");
        }
        var playerCharacter = ctx.Event.SenderCharacterEntity;

        Alliance.PlayerGroup newPlayerGroup;
        if (playerName != "")
        {
            if (!Helper.FindPlayer(playerName, true, out var targetEntity, out var targetUserEntity))
            {
                throw ctx.Error($"Could not find specified player \"{playerName}\".");
            }
            else if (targetEntity.Equals(playerCharacter))
            {
                throw ctx.Error($"You cannot add yourself to your group");
            }
            
            newPlayerGroup = new Alliance.PlayerGroup();
            newPlayerGroup.Allies.Add(targetEntity);
        }
        else
        {
            Alliance.GetLocalPlayers(playerCharacter, Plugin.LogSystem.Xp, out newPlayerGroup);

            if (newPlayerGroup.Allies.Count < 2)
            {
                ctx.Reply($"No nearby players detected to make a group with.");
                return;
            }
        }

        if (!Cache.AlliancePlayerToGroupId.TryGetValue(playerCharacter, out var groupId))
        {
            groupId = Guid.NewGuid();
            
            // Create a new group and ensure that we are in it.
            Cache.AlliancePlayerToGroupId[playerCharacter] = groupId;
            Cache.AlliancePlayerGroups[groupId] = new Alliance.PlayerGroup() { Allies = { playerCharacter } };
        }

        var currentGroup = Cache.AlliancePlayerGroups[groupId];
        foreach (var newAlly in newPlayerGroup.Allies)
        {
            var allyPlayerCharacter = _entityManager.GetComponentData<PlayerCharacter>(newAlly);
            var allyName = allyPlayerCharacter.Name.ToString();
            
            if (currentGroup.Allies.Contains(newAlly))
            {
                // Player already in current group, skipping.
                ctx.Reply($"{allyName} is already in your group.");
                continue;
            }

            var newAllyAlliancePrefs = Database.AlliancePlayerPrefs[newAlly];
            if (newAllyAlliancePrefs.IgnoringInvites)
            {
                ctx.Reply($"{allyName} is currently ignoring group invites. Ask them to change this setting before attempting to make a group.");
                continue;
            }
            else if (Cache.AlliancePlayerToGroupId.ContainsKey(newAlly))
            {
                ctx.Reply($"{allyName} is already in a group. They should leave their current one first.");
                continue;
            }
            
            var pendingInvites = Cache.AlliancePendingInvites[newAlly];
            var inviteId = pendingInvites.Count + 1;
            if (!pendingInvites.Add(new AlliancePendingInvite(groupId, ctx.Name)))
            {
                ctx.Reply($"{allyName} already has a pending invite to this group.");
                continue;
            }

            ctx.Reply($"{allyName} was sent an invite to this group.");

            var inviteString = inviteId == 1 ?
                $"Type \".group yes\" to accept or \".group no\" to reject." :
                $"Type \".group yes {inviteId}\" to accept or \".group no {inviteId}\" to reject.";
            
            Output.SendLore(allyPlayerCharacter.UserEntity, $"{ctx.User.CharacterName} has invited you to join their group! {inviteString} No further messages will be sent about this invite.");
        }
    }

    [Command("group yes", "gy", "[index]", "Accept the oldest invite, or the invite specified by the provided index.", adminOnly: false)]
    public static void GroupAccept(ChatCommandContext ctx, int index = -1)
    {
        if (!Plugin.PlayerGroupsActive)
        {
            throw ctx.Error("Player groups not allowed.");
        }
        var playerCharacter = ctx.Event.SenderCharacterEntity;
        if (!Cache.AlliancePendingInvites.TryGetValue(playerCharacter, out var pendingInvites))
        {
            throw ctx.Error("You have no pending invites to accept.");
        }

        if (index >= pendingInvites.Count)
        {
            throw ctx.Error("Could not find invite. If you have removed other invites, the invite list index may have changed.");
        }

        var sortedInvitesList = pendingInvites.OrderBy(x => x.InvitedAt).ToList();
        var acceptingInvite = index < 0 ? sortedInvitesList[0] : sortedInvitesList[index];

        pendingInvites.Remove(acceptingInvite);

        if (!Cache.AlliancePlayerGroups.TryGetValue(acceptingInvite.GroupId, out var group))
        {
            throw ctx.Error("The group you are trying to join no longer exists.");
        }
        
        Cache.AlliancePlayerToGroupId[playerCharacter] = acceptingInvite.GroupId;
        group.Allies.Add(playerCharacter);

        foreach (var ally in group.Allies)
        {
            if (ally == playerCharacter)
            {
                ctx.Reply($"You have successfully joined the group!\n{group.PrintAllies()}");
            }
            else
            {
                var allyUserEntity = _entityManager.GetComponentData<PlayerCharacter>(ally).UserEntity;
                Output.SendLore(allyUserEntity, $"{ctx.Name} has joined your group.");
            }
        }
    }

    [Command("group no", "gn", "[index]", "Reject the oldest invite, or the invite specified by the provided index.", adminOnly: false)]
    public static void GroupReject(ChatCommandContext ctx, int index = -1)
    {
        if (!Plugin.PlayerGroupsActive)
        {
            throw ctx.Error("Player groups not allowed.");
        }
        var playerCharacter = ctx.Event.SenderCharacterEntity;
        if (!Cache.AlliancePendingInvites.TryGetValue(playerCharacter, out var pendingInvites))
        {
            throw ctx.Error("You have no pending invites to reject.");
        }

        if (index >= pendingInvites.Count)
        {
            throw ctx.Error("Could not find invite. If you have removed other invites, the invite list index may have changed.");
        }

        var sortedInvitesList = pendingInvites.OrderBy(x => x.InvitedAt).ToList();
        var rejectingInvite = index < 0 ? sortedInvitesList[0] : sortedInvitesList[index];

        pendingInvites.Remove(rejectingInvite);
        
        ctx.Reply("You have rejected the invite.");
    }

    [Command("group leave", "gl", "", "Leave your current group.", adminOnly: false)]
    public static void LeaveGroup(ChatCommandContext ctx)
    {
        if (!Plugin.PlayerGroupsActive)
        {
            throw ctx.Error("Player groups not allowed.");
        }
        var playerCharacter = ctx.Event.SenderCharacterEntity;
        if (!Cache.AlliancePlayerToGroupId.TryGetValue(playerCharacter, out var groupId))
        {
            throw ctx.Error("You are not currently in a group.");
        }

        Cache.AlliancePlayerToGroupId.Remove(playerCharacter);
        if (!Cache.AlliancePlayerGroups.TryGetValue(groupId, out var group))
        {
            // This should never happen, but just in case we can just skip.
            throw ctx.Error("Your group has been removed.");
        }

        group.Allies.Remove(playerCharacter);
        
        foreach (var ally in group.Allies)
        {
            var allyUserEntity = _entityManager.GetComponentData<PlayerCharacter>(ally).UserEntity;
            Output.SendLore(allyUserEntity, $"{ctx.Name} has left your group.");
        }
        ctx.Reply($"You have left the group.");
    }

    [Command("group wipe", "gw", "", "Clear out any existing groups and invites", adminOnly: true)]
    public static void WipeGroups(ChatCommandContext ctx)
    {
        if (!Plugin.PlayerGroupsActive)
        {
            throw ctx.Error("Player groups not allowed.");
        }
        Cache.AlliancePendingInvites.Clear();
        Cache.AlliancePlayerToGroupId.Clear();
        Cache.AlliancePlayerGroups.Clear();

        ctx.Reply("Groups have been wiped.");
    }
}