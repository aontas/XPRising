using System.Text.Json;
using System.Text.Json.Serialization;

namespace XPShared.Transport.Messages;

public class ActionSerialisedMessage : ISerialisableChatMessage
{
    // JsonPropertyName is used to reduce the data serialised, without resorting to including another library
    [JsonPropertyName("0")] public string Group = "";
    [JsonPropertyName("1")] public string Label = "";
    [JsonPropertyName("2")] public string Action = "";

    public MessageRegistry.MessageTypes Type()
    {
        return MessageRegistry.MessageTypes.ActionSerialisedMessage;
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public void Deserialize(string input)
    {
        
    }
}