using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientUI.UniverseLib.UI.Models;

/// <summary>
/// A simple helper class to handle a button's OnClick more effectively, along with some helpers.
/// </summary>
public class ButtonRef
{
    /// <summary>
    /// Invoked when the Button is clicked.
    /// </summary>
    public Action OnClick;

    /// <summary>
    /// The actual Button component this object is a reference to.
    /// </summary>
    public Button Component { get; }

    /// <summary>
    /// The Text component on the button.
    /// </summary>
    public TextMeshProUGUI ButtonText { get; }

    /// <summary>
    /// The GameObject this Button is attached to.
    /// </summary>
    public GameObject GameObject => Component.gameObject;

    /// <summary>
    /// The RectTransform for this Button.
    /// </summary>
    public RectTransform Transform { get; }

    /// <summary>
    /// Helper for <c>Button.enabled</c>.
    /// </summary>
    public bool Enabled
    {
        get => Component.enabled;
        set => Component.enabled = value;
    }

    public ButtonRef(Button button)
    {
        this.Component = button;
        this.ButtonText = button.GetComponentInChildren<TextMeshProUGUI>();
        this.Transform = button.GetComponent<RectTransform>();

        button.onClick.AddListener(new Action(() => { OnClick?.Invoke(); }));
    }
}