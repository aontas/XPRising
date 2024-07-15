namespace XPShared.Transport.Messages;

public interface ISerialisableChatMessage
{
    internal MessageRegistry.MessageTypes Type();
    void Serialize(BinaryWriter writer);
    void Deserialize(BinaryReader reader);
}