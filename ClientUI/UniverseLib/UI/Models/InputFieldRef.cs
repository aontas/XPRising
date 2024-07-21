using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;

namespace ClientUI.UniverseLib.UI.Models;

/// <summary>
/// A simple wrapper class for working with InputFields, with some helpers and performance improvements.
/// </summary>
public class InputFieldRef : UIModel
{
    // Static

    internal static readonly HashSet<InputFieldRef> inputsPendingUpdate = new();

    internal static void UpdateInstances()
    {
        while (inputsPendingUpdate.Any())
        {
            var inputField = inputsPendingUpdate.First();
            LayoutRebuilder.MarkLayoutForRebuild(inputField.Transform);
            inputField.OnValueChanged?.Invoke(inputField.Component.text);

            inputsPendingUpdate.Remove(inputField);
        }
    }

    // Instance

    /// <summary>
    /// Invoked at most once per frame, if the input was changed in the previous frame.
    /// </summary>
    public event Action<string> OnValueChanged;

    /// <summary>
    /// The actual InputField component which this object is a reference to.
    /// </summary>
    public TMP_InputField Component { get; }

    /// <summary>
    /// The placeholder Text component.
    /// </summary>
    public TextMeshProUGUI PlaceholderText { get; }

    /// <summary>
    /// The GameObject which the InputField is attached to.
    /// </summary>
    public override GameObject UIRoot => Component.gameObject;

    /// <summary>
    /// The GameObject which the InputField is attached to.
    /// </summary>
    public GameObject GameObject => Component.gameObject;

    /// <summary>
    /// The RectTransform for this InputField.
    /// </summary>
    public RectTransform Transform { get; }

    /// <summary>
    /// The Text set to the InputField.
    /// </summary>
    public string Text
    {
        get => Component.text;
        set => Component.text = value;
    }

    public InputFieldRef(TMP_InputField component)
    {
        this.Component = component;
        Transform = component.GetComponent<RectTransform>();
        PlaceholderText = component.placeholder.TryCast<TextMeshProUGUI>();
        component.onValueChanged.AddListener(OnInputChanged);
    }

    private void OnInputChanged(string value)
    {
        if (!inputsPendingUpdate.Contains(this))
            inputsPendingUpdate.Add(this);
    }

    /// <summary>
    /// Not implemented.
    /// </summary>
    public override void ConstructUI(GameObject parent) => throw new NotImplementedException();
}