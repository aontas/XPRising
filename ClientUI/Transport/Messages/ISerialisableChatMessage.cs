namespace ClientUI.Transport.Messages;

public interface ISerialisableChatMessage
{
    internal MessageRegistry.MessageTypes Type();
    string Serialize();
    void Deserialize(string input);
}