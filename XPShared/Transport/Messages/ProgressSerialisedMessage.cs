﻿using Bloodstone.Network;

namespace XPShared.Transport.Messages;

public class ProgressSerialisedMessage : VNetworkChatMessage
{
    public enum ActiveState
    {
        Unchanged,
        NotActive,
        Active,
    }
    
    public string Group = "";
    public string Label = "";
    public int Level = 0;
    public float ProgressPercentage = 0f;
    public string Tooltip = "";
    public ActiveState Active = ActiveState.Unchanged;
    public string Change = "";
    public string Colour = "";
    public bool Flash = false;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Group);
        writer.Write(Label);
        writer.Write(Level);
        writer.Write(ProgressPercentage);
        writer.Write(Tooltip);
        writer.Write((int)Active);
        writer.Write(Change);
        writer.Write(Colour);
        writer.Write(Flash);
    }

    public void Deserialize(BinaryReader reader)
    {
        Group = reader.ReadString();
        Label = reader.ReadString();
        Level = reader.ReadInt32();
        ProgressPercentage = reader.ReadSingle();
        Tooltip = reader.ReadString();
        Active = (ActiveState)reader.ReadInt32();
        Change = reader.ReadString();
        Colour = reader.ReadString();
        Flash = reader.ReadBoolean();
    }
}