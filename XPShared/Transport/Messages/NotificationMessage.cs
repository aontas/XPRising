using BepInEx.Logging;
using Bloodstone.Network;

namespace XPShared.Transport.Messages;

public class NotificationMessage : VNetworkChatMessage
{
    public string ID = "";
    public string Message = "";
    public LogLevel Severity = LogLevel.Message; 
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(ID);
        writer.Write(Message);
        writer.Write((int)Severity);
    }

    public void Deserialize(BinaryReader reader)
    {
        ID = reader.ReadString();
        Message = reader.ReadString();
        Severity = (LogLevel)reader.ReadInt32();
    }
}