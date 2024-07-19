using Bloodstone.Network;

namespace XPShared.Transport.Messages;

public class ActionSerialisedMessage : VNetworkChatMessage
{
    public string Group = "";
    public string ID = "";
    public string Label = "";
    public string Colour = "#808080";
    public bool Enabled = true;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Group);
        writer.Write(ID);
        writer.Write(Label);
        writer.Write(Colour);
        writer.Write(Enabled);
    }

    public void Deserialize(BinaryReader reader)
    {
        Group = reader.ReadString();
        ID = reader.ReadString();
        Label = reader.ReadString();
        Colour = reader.ReadString();
        Enabled = reader.ReadBoolean();
    }
}