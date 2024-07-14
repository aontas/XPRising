using System.Text.Json;
using System.Text.Json.Serialization;

namespace XPShared.Transport.Messages;

public class ProgressSerialisedMessage : ISerialisableChatMessage
{
    public enum ActiveState
    {
        NotActive,
        Active,
        Burst,
    }
    
    // JsonPropertyName is used to reduce the data serialised, without resorting to including another library
    [JsonPropertyName("0")] public string Group = "";
    [JsonPropertyName("1")] public string Label = "";
    [JsonPropertyName("2")] public int Level = 0;
    [JsonPropertyName("3")] public float ProgressPercentage = 0f;
    [JsonPropertyName("4")] public string Tooltip = "";
    [JsonPropertyName("5")] public ActiveState Active = ActiveState.NotActive;
    [JsonPropertyName("6")] public string Change = "";
    [JsonPropertyName("7")] public string Colour = "";

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