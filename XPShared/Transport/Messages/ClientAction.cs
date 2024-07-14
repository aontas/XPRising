using Bloodstone.API;
using Stunlock.Network;
using Unity.Collections;

namespace XPShared.Transport.Messages;

public class ClientAction : VNetworkMessage
{
    public enum ActionType
    {
        Connect,
        Disconnect,
        ButtonClick,
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

    // Read your contents from the reader.
    public void Deserialize(NetBufferIn reader)
    {
        Action = Enum.Parse<ActionType>(reader.ReadString(Allocator.Temp));
        Value = reader.ReadString(Allocator.Temp);
        reader.ReadString(Allocator.Temp);
        reader.ReadString(Allocator.Temp);
    }

    // Write your contents to the writer.
    public void Serialize(ref NetBufferOut writer)
    {
        writer.Write(Enum.GetName(Action));
        writer.Write(Value);
        writer.Write("test");
        writer.Write("test");
    }
}