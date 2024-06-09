using System;

namespace XPRising.Models;

public struct AlliancePendingInvite
{
    public Guid GroupId;
    public string InviterName;
    public DateTime InvitedAt;

    public AlliancePendingInvite(Guid groupId, string inviterName)
    {
        GroupId = groupId;
        InviterName = inviterName;
        InvitedAt = DateTime.Now;
    }

    public bool Equals(AlliancePendingInvite other)
    {
        return GroupId.Equals(other.GroupId);
    }

    public override bool Equals(object obj)
    {
        return obj is AlliancePendingInvite other && Equals(other);
    }

    public override int GetHashCode()
    {
        return GroupId.GetHashCode();
    }
}