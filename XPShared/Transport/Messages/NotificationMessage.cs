using BepInEx.Logging;

namespace XPShared.Transport.Messages;

public class NotificationMessage : IChatMessage
{
    public string ID = "";
    public string Message = "";
    public LogLevel Severity = LogLevel.Message;
    public string Colour = "";
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(ID);
        writer.Write(Message);
        writer.Write((int)Severity);
        writer.Write(Colour);
    }

    public void Deserialize(BinaryReader reader)
    {
        ID = reader.ReadString();
        Message = reader.ReadString();
        Severity = (LogLevel)reader.ReadInt32();
        Colour = reader.ReadString();
    }
}