using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClientUI.Transport.Messages;

internal class ProgressSerialisedMessage : ISerialisableChatMessage
{
    public string Label = "";
    public int Level = 0;
    public float ProgressPercentage = 0f;
    public string Tooltip = "";

    public MessageRegistry.MessageTypes Type()
    {
        return MessageRegistry.MessageTypes.ProgressSerialisedMessage;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public void Deserialize(string input)
    {
        
    }
}