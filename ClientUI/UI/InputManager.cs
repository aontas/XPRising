using UnityEngine;

namespace ClientUI.UI;

public static class InputManager
{
    public static MouseState Mouse = new MouseState();

    public static void Update()
    {
        Mouse.Position = Input.mousePosition;
        Mouse.ScrollDelta = Input.mouseScrollDelta;
        Mouse.Button0 = GetButtonState(0);
        Mouse.Button1 = GetButtonState(1);
        Mouse.Button2 = GetButtonState(2);
    }

    private static MouseState.ButtonState GetButtonState(int button)
    {
        if (Input.GetMouseButtonUp(button))
        {
            return MouseState.ButtonState.Up | MouseState.ButtonState.Released;
        }
        else if (Input.GetMouseButtonDown(button))
        {
            return MouseState.ButtonState.Down | MouseState.ButtonState.Clicked;
        }
        else if (Input.GetMouseButton(button))
        {
            return MouseState.ButtonState.Down;
        }

        return MouseState.ButtonState.Up;
    }
}