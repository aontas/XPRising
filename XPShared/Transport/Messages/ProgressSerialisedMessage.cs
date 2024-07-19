using Bloodstone.Network;

namespace XPShared.Transport.Messages;

public class ProgressSerialisedMessage : VNetworkChatMessage
{
    public enum ActiveState
    {
        NotActive,
        Active,
        Burst,
    }
    
    public string Group = "";
    public string Label = "";
    public int Level = 0;
    public float ProgressPercentage = 0f;
    public string Tooltip = "";
    public ActiveState Active = ActiveState.NotActive;
    public string Change = "";
    public string Colour = "";

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
    }
}