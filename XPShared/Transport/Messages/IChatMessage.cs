using BepInEx.Logging;

namespace XPShared.Transport.Messages;

public interface IChatMessage
{
    internal const int CHAT_NETWORK_EVENT_ID = 0x0D00DDAD;
    internal static void WriteHeader(BinaryWriter writer, string type, int clientNonce)
    {
        writer.Write(CHAT_NETWORK_EVENT_ID);
        writer.Write(clientNonce);
        writer.Write(type);
    }

    internal static bool ReadHeader(BinaryReader reader, out int clientNonce, out string type)
    {
        type = "";
        clientNonce = 0;

        try
        {
            var eventId = reader.ReadInt32();
            clientNonce = reader.ReadInt32();
            type = reader.ReadString();

            return eventId == CHAT_NETWORK_EVENT_ID;
        }
        catch (Exception e)
        {
            Plugin.Log(LogLevel.Debug, $"Failed to read chat message header: {e.Message}");

            return false;
        }
    }

    public void Serialize(BinaryWriter writer);

    public void Deserialize(BinaryReader reader);
}