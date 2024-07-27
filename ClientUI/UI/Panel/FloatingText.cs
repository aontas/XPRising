using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XPShared;
using Random = System.Random;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;
using UniversalUI = ClientUI.UniverseLib.UI.UniversalUI;

namespace ClientUI.UI.Panel;

public class FloatingText : IEquatable<FloatingText>
{
    private GameObject gameObject { get; set; }
    private TextMeshProUGUI _changeText;
    private readonly FrameTimer _timer;
    private readonly Vector3 _moveDirection;
    private const int TickRate = 10;
    private int _lifetime = 500;

    private FloatingText(GameObject parent, string text, Color colour)
    {
        gameObject = UIFactory.CreateUIObject($"FloatingText", parent, new Vector2(100, 20));
        UIFactory.SetLayoutElement(gameObject, ignoreLayout: true);
        gameObject.AddComponent<Outline>();
        
        _changeText = gameObject.AddComponent<TextMeshProUGUI>();
        _changeText.color = colour;
        _changeText.text = text;
        _changeText.fontSize = 24;
        _changeText.font = UIFactory.Font;
        _changeText.alignment = TextAlignmentOptions.Center;
        _changeText.overflowMode = TextOverflowModes.Overflow;
        try
        {
            _changeText.outlineWidth = 0.15f;
            _changeText.outlineColor = Color.black;
        }
        catch (Exception)
        {
            // This can throw if the mod is attempting to run this when exiting the application.
        }
        
        gameObject.transform.position = Input.mousePosition + Vector3.up*(Random.Shared.NextSingle() * 20 + 20) + Vector3.left*((Random.Shared.NextSingle() - 0.5f) * 40);
        
        // Get a mostly vertical move direction
        _moveDirection = Vector3.up;
        _moveDirection.x = (Random.Shared.NextSingle() - 0.5f) * 0.75f;
        
        // Increase velocity
        _moveDirection *= (Random.Shared.NextSingle() * 0.5f + 0.5f);
        
        _timer = new FrameTimer();
        _timer.Initialise(FloatText, TimeSpan.FromMilliseconds(TickRate), false);
        _timer.Start();
    }

    private void FloatText()
    {
        gameObject.transform.Translate(_moveDirection);
        _lifetime -= TickRate;

        if (_lifetime < 0)
        {
            _timer.Stop();
            TextObjects.Remove(this);
            GameObject.Destroy(gameObject);
        }
    }

    private static readonly List<FloatingText> TextObjects = new List<FloatingText>();
    public static void SpawnFloatingText(GameObject parent, string text, Color colour)
    {
        TextObjects.Add(new FloatingText(parent, text, colour));
    }

    public bool Equals(FloatingText other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(gameObject, other.gameObject);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FloatingText)obj);
    }

    public override int GetHashCode()
    {
        return (gameObject != null ? gameObject.GetHashCode() : 0);
    }
}