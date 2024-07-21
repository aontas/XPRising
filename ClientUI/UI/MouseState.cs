using UnityEngine;

namespace ClientUI.UI;

public struct MouseState
{
    [Flags]
    public enum ButtonState
    {
        Up = 0,
        Down = 1,
        Clicked = 2,
        Released = 4,
    }
    
    public Vector3 Position = Vector3.zero;
    public Vector2 ScrollDelta = Vector2.zero;
    public ButtonState Button0 = ButtonState.Up;
    public ButtonState Button1 = ButtonState.Up;
    public ButtonState Button2 = ButtonState.Up;

    public MouseState()
    {
    }
}