using System;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands;

public static class AllianceCommands
{
    private static EntityManager _entityManager = Plugin.Server.EntityManager;

    private static void CheckPlayerGroupsActive(ChatCommandContext ctx)
    {
        if (!Plugin.PlayerGroupsActive)
        {
            var message = L10N.Get(L10N.TemplateKey.SystemNotEnabled)
                .AddField("{system}", "Player group");
            throw Output.ChatError(ctx, message);
        }
    }
    
    [Command("group show", "gs", "", "Prints out info about your current group and your group preferences", adminOnly: false)]
    public static void ShowGroupInformation(ChatCommandContext ctx)
    {
        CheckPlayerGroupsActive(ctx);
        
        var playerCharacter = ctx.Event.SenderCharacterEntity;
        var steamID = ctx.User.PlatformId;

        var groupDetails = L10N.Get(L10N.TemplateKey.AllianceGroupInfoNone);
        if (Cache.AlliancePlayerToGroupId.TryGetValue(playerCharacter, out var currentGroupId))
        {
            groupDetails = Cache.AlliancePlayerGroups[currentGroupId].PrintAllies();
        }
        
        var alliancePreferences = Database.AlliancePlayerPrefs[steamID];

        var pendingInviteDetails = L10N.Get(L10N.TemplateKey.AllianceInvitesNone);
        if (Cache.AlliancePendingInvites.TryGetValue(playerCharacter, out var pendingInvites) && pendingInvites.Count > 0)
        {
            var invitesList = pendingInvites.OrderBy(x => x.InvitedAt).Select((invite, i) => $"{i}: {invite.InviterName} at {invite.InvitedAt}");
            pendingInviteDetails = L10N.Get(L10N.TemplateKey.AllianceCurrentInvites).AddField("{invites}", string.Join("\n", invitesList));
        }

        var preferences = L10N.Get(L10N.TemplateKey.AlliancePreferences)
            .AddField("{preferences}", alliancePreferences.ToString());
        Output.ChatReply(ctx, pendingInviteDetails, preferences, groupDetails);
    }

    [Command("group ignore", "gi", "", "Toggles ignoring group invites for yourself.", adminOnly: false)]
    public static void ToggleIgnoreGroups(ChatCommandContext ctx)
    {
        CheckPlayerGroupsActive(ctx);

        var steamID = ctx.User.PlatformId;

        var preferences = Database.AlliancePlayerPrefs[steamID];
        preferences.IgnoringInvites = !preferences.IgnoringInvites;
        Database.AlliancePlayerPrefs[steamID] = preferences;
        
        if (preferences.IgnoringInvites)
        {
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceGroupIgnore));
        }
        else
        {
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceGroupListen));
        }
    }

    [Command("group add", "ga", "[playerName]", "Adds a player to your group. Leave blank to add all \"close\" players to your group.", adminOnly: false)]
    public static void GroupAddOrCreate(ChatCommandContext ctx, string playerName = "")
    {
        CheckPlayerGroupsActive(ctx);

        var playerCharacter = ctx.Event.SenderCharacterEntity;

        if (Cache.AlliancePlayerToGroupId.TryGetValue(playerCharacter, out var currentGroupId) &&
            Cache.AlliancePlayerGroups.TryGetValue(currentGroupId, out var currentGroup))
        {
            if (currentGroup.Allies.Count >= Plugin.MaxPlayerGroupSize)
            {
                var message = L10N.Get(L10N.TemplateKey.AllianceMaxGroupSize)
                    .AddField("{maxGroupSize}", Plugin.MaxPlayerGroupSize.ToString());
                throw Output.ChatError(ctx, message);
            }
        }

        Alliance.PlayerGroup newPlayerGroup;
        if (playerName != "")
        {
            if (!PlayerCache.FindPlayer(playerName, true, out var targetEntity, out _))
            {
                var message = L10N.Get(L10N.TemplateKey.GeneralPlayerNotFound)
                    .AddField("{playerName}", playerName);
                throw Output.ChatError(ctx, message);
            }
            else if (targetEntity.Equals(playerCharacter))
            {
                throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceAddSelfError));
            }
            
            newPlayerGroup = new Alliance.PlayerGroup();
            newPlayerGroup.Allies.Add(targetEntity);
        }
        else
        {
            Alliance.GetLocalPlayers(playerCharacter, Plugin.LogSystem.Xp, out newPlayerGroup);

            if (newPlayerGroup.Allies.Count < 2)
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceNoNearPlayers));
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

        currentGroup = Cache.AlliancePlayerGroups[groupId];
        foreach (var newAlly in newPlayerGroup.Allies)
        {
            var allyPlayerCharacter = _entityManager.GetComponentData<PlayerCharacter>(newAlly);
            var allyName = allyPlayerCharacter.Name.ToString();
            var allySteamID = _entityManager.GetComponentData<User>(allyPlayerCharacter.UserEntity).PlatformId;
            
            if (currentGroup.Allies.Contains(newAlly))
            {
                // Player already in current group, skipping.
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceInYourGroup).AddField("{playerName}", allyName));
                continue;
            }

            var newAllyAlliancePrefs = Database.AlliancePlayerPrefs[allySteamID];
            if (newAllyAlliancePrefs.IgnoringInvites)
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceIgnoringInvites).AddField("{playerName}", allyName));
                continue;
            }
            else if (Cache.AlliancePlayerToGroupId.ContainsKey(newAlly))
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceInOtherGroup).AddField("{playerName}", allyName));
                continue;
            }
            
            var pendingInvites = Cache.AlliancePendingInvites[newAlly];
            var inviteId = pendingInvites.Count + 1;
            if (!pendingInvites.Add(new AlliancePendingInvite(groupId, ctx.Name)))
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceAlreadyInvited).AddField("{playerName}", allyName));
                continue;
            }

            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceInviteSent).AddField("{playerName}", allyName));

            var inviteString = inviteId == 1 ? "" : $" {inviteId}";

            var message = L10N.Get(L10N.TemplateKey.AllianceGroupInvited)
                .AddField("{playerName}", ctx.User.CharacterName.ToString())
                .AddField("{acceptCommand}", $".group yes{inviteString}")
                .AddField("{declineCommand}", $".group no{inviteString}");
            Output.SendMessage(allyPlayerCharacter.UserEntity, message);
        }
    }

    [Command("group yes", "gy", "[index]", "Accept the oldest invite, or the invite specified by the provided index.", adminOnly: false)]
    public static void GroupAccept(ChatCommandContext ctx, int index = -1)
    {
        CheckPlayerGroupsActive(ctx);

        var playerCharacter = ctx.Event.SenderCharacterEntity;
        if (!Cache.AlliancePendingInvites.TryGetValue(playerCharacter, out var pendingInvites))
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceInvitesNone));
        }

        if (index >= pendingInvites.Count)
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceInvite404));
        }

        var sortedInvitesList = pendingInvites.OrderBy(x => x.InvitedAt).ToList();
        var acceptingInvite = index < 0 ? sortedInvitesList[0] : sortedInvitesList[index];

        pendingInvites.Remove(acceptingInvite);

        if (!Cache.AlliancePlayerGroups.TryGetValue(acceptingInvite.GroupId, out var group))
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceInviteGroup404));
        } else if (group.Allies.Count >= Plugin.MaxPlayerGroupSize)
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceInviteMaxPlayers).AddField("{maxGroupSize}", Plugin.MaxPlayerGroupSize.ToString()));
        }
        
        Cache.AlliancePlayerToGroupId[playerCharacter] = acceptingInvite.GroupId;
        group.Allies.Add(playerCharacter);

        foreach (var ally in group.Allies)
        {
            if (ally == playerCharacter)
            {
                Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceInviteAccepted), group.PrintAllies());
            }
            else
            {
                var allyUserEntity = _entityManager.GetComponentData<PlayerCharacter>(ally).UserEntity;
                Output.SendMessage(allyUserEntity, L10N.Get(L10N.TemplateKey.AllianceGroupOtherJoined).AddField("{playerName}", ctx.Name));
            }
        }
    }

    [Command("group no", "gn", "[index]", "Reject the oldest invite, or the invite specified by the provided index.", adminOnly: false)]
    public static void GroupReject(ChatCommandContext ctx, int index = -1)
    {
        CheckPlayerGroupsActive(ctx);

        var playerCharacter = ctx.Event.SenderCharacterEntity;
        if (!Cache.AlliancePendingInvites.TryGetValue(playerCharacter, out var pendingInvites))
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceInvitesNone));
        }

        if (index >= pendingInvites.Count)
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceInvite404));
        }

        var sortedInvitesList = pendingInvites.OrderBy(x => x.InvitedAt).ToList();
        var rejectingInvite = index < 0 ? sortedInvitesList[0] : sortedInvitesList[index];

        pendingInvites.Remove(rejectingInvite);
        
        Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceInviteRejected));
    }

    [Command("group leave", "gl", "", "Leave your current group.", adminOnly: false)]
    public static void LeaveGroup(ChatCommandContext ctx)
    {
        CheckPlayerGroupsActive(ctx);

        var playerCharacter = ctx.Event.SenderCharacterEntity;
        if (!Cache.AlliancePlayerToGroupId.TryGetValue(playerCharacter, out var groupId))
        {
            throw Output.ChatError(ctx, L10N.Get(L10N.TemplateKey.AllianceGroupNull));
        }

        Cache.AlliancePlayerToGroupId.Remove(playerCharacter);
        if (!Cache.AlliancePlayerGroups.TryGetValue(groupId, out var group))
        {
            // This should never happen, but just in case we can just skip.
            return;
        }

        group.Allies.Remove(playerCharacter);
        
        foreach (var ally in group.Allies)
        {
            var allyUserEntity = _entityManager.GetComponentData<PlayerCharacter>(ally).UserEntity;
            var message =
                L10N.Get(L10N.TemplateKey.AllianceGroupOtherLeft)
                    .AddField("{playerName}", ctx.Name);
            Output.SendMessage(allyUserEntity, message);
        }
        Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceGroupLeft));
    }

    [Command("group wipe", "gw", "", "Clear out any existing groups and invites", adminOnly: false)]
    public static void WipeGroups(ChatCommandContext ctx)
    {
        CheckPlayerGroupsActive(ctx);

        Cache.AlliancePendingInvites.Clear();
        Cache.AlliancePlayerToGroupId.Clear();
        Cache.AlliancePlayerGroups.Clear();

        Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.AllianceGroupWipe));
    }
}