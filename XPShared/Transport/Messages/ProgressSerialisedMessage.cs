using System.Text.Json;
using System.Text.Json.Serialization;

namespace XPShared.Transport.Messages;

public class ProgressSerialisedMessage : ISerialisableChatMessage
{
    // JsonPropertyName is used to reduce the data serialised, without resorting to including another library
    [JsonPropertyName("0")]
    public string Label = "";
    [JsonPropertyName("1")]
    public int Level = 0;
    [JsonPropertyName("2")]
    public float ProgressPercentage = 0f;
    [JsonPropertyName("3")]
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