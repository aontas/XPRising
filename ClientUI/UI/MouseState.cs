using UnityEngine;

namespace ClientUI.UI;

public struct MouseState
{
    [Flags]
    public enum ButtonState
    {
        Unknown = 0,
        Up = 1,
        Down = 2,
        Clicked = 4,
        Released = 8,
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