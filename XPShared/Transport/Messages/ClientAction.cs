namespace XPShared.Transport.Messages;

public class ClientAction : IChatMessage
{
    public enum ActionType
    {
        Connect,
        Disconnect,
        ButtonClick,
        Register,
    }
        
    public ActionType Action { get; private set; }
    public string Value { get; private set; }

    // You need to implement an empty constructor for when your message is received but not yet serialized.
    public ClientAction()
    {
        Value = "";
    }

    public ClientAction(ActionType actionType, string value)
    {
        Action = actionType;
        Value = value;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Enum.GetName(Action));
        writer.Write(Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        Action = Enum.Parse<ActionType>(reader.ReadString());
        Value = reader.ReadString();
    }
}