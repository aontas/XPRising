namespace XPShared.Transport.Messages;

public class DisplayTextMessage : IChatMessage
{
    public string Group = "";
    public string ID = "";
    public string Title = "";
    public string Text = "";
    public bool Reset = true;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Group);
        writer.Write(ID);
        writer.Write(Title);
        writer.Write(Text);
        writer.Write(Reset);
    }

    public void Deserialize(BinaryReader reader)
    {
        Group = reader.ReadString();
        ID = reader.ReadString();
        Title = reader.ReadString();
        Text = reader.ReadString();
        Reset = reader.ReadBoolean();
    }
}