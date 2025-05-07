namespace XPShared.Transport.Messages;

public class ClientRegisterMessage : IChatMessage
{
    public int ClientNonce { get; private set; }
    public ulong SteamId { get; set; }

    // You need to implement an empty constructor for when your message is received but not yet serialized.
    public ClientRegisterMessage()
    {
        ClientNonce = 0;
        SteamId = 0;
    }

    public ClientRegisterMessage(int clientNonce, ulong steamId)
    {
        ClientNonce = clientNonce;
        SteamId = steamId;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(ClientNonce);
        writer.Write(SteamId);
    }

    public void Deserialize(BinaryReader reader)
    {
        ClientNonce = reader.ReadInt32();
        SteamId = reader.ReadUInt64();
    }
}