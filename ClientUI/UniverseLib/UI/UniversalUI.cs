using Bloodstone.Hooks;
using ClientUI.UI;
using ClientUI.UniverseLib.UI.Panels;
using UnityEngine;
using InputFieldRef = ClientUI.UniverseLib.UI.Models.InputFieldRef;
using UIBehaviourModel = ClientUI.UniverseLib.UI.Models.UIBehaviourModel;

namespace ClientUI.UniverseLib.UI;

/// <summary>Handles all <see cref="UIBase"/> UIs on the UniverseLib UI canvas.</summary>
public static class UniversalUI
{
    internal static readonly Dictionary<string, UIBase> registeredUIs = new();
    internal static readonly List<UIBase> uiBases = new();

    /// <summary>Returns true if UniverseLib is currently initializing it's UI.</summary>
    public static bool Initializing { get; internal set; } = true;

    /// <summary>Returns true if any <see cref="UIBase"/> is being displayed.</summary>
    public static bool AnyUIShowing => registeredUIs.Any(it => it.Value.Enabled);

    /// <summary>The UniverseLib global Canvas root.</summary>
    public static GameObject CanvasRoot { get; private set; }

    /// <summary>The GameObject used to hold returned <see cref="IPooledObject"/> objects.</summary>
    public static GameObject PoolHolder { get; private set; }

    /// <summary>The default font asset.</summary>
    public static Font DefaultFont { get; private set; }

    /// <summary>A safe value for the maximum amount of characters allowed in an InputField.</summary>
    public const int MAX_INPUTFIELD_CHARS = 16000;

    public static Vector2 CanvasDimensions = new Vector2(1920, 1080);

    /// <summary>
    /// Create and register a <see cref="UIBase"/> with the provided ID, and optional update method.
    /// </summary>
    /// <param name="id">A unique ID for your UI.</param>
    /// <param name="updateMethod">An optional method to receive Update calls with, invoked when your UI is displayed.</param>
    /// <returns>Your newly created <see cref="UIBase"/>, if successful.</returns>
    public static UIBase RegisterUI(string id, Action updateMethod)
    {
        return new(id, updateMethod);
    }

    /// <summary>
    /// Create and register a <typeparamref name="T"/> with the provided ID, and optional update method.<br />
    /// You can use this to register a custom <see cref="UIBase"/> type instead of the default type.
    /// </summary>
    /// <param name="id">A unique ID for your UI.</param>
    /// <param name="updateMethod">An optional method to receive Update calls with, invoked when your UI is displayed.</param>
    /// <returns>Your newly created <typeparamref name="T"/>, if successful.</returns>
    public static T RegisterUI<T>(string id, Action updateMethod) where T : UIBase
    {
        return (T)Activator.CreateInstance(typeof(T), new object[] { id, updateMethod });
    }

    /// <summary>
    /// Sets the <see cref="UIBase"/> with the corresponding <paramref name="id"/> to be active or disabled.
    /// </summary>
    public static void SetUIActive(string id, bool active)
    {
        if (registeredUIs.TryGetValue(id, out UIBase uiBase))
        {
            uiBase.RootObject.SetActive(active);
            if (active)
                uiBase.SetOnTop();
            else if (uiBase != PanelManager.resizeCursorUIBase)
                PanelManager.ForceEndResize();

            return;
        }
        throw new ArgumentException($"There is no UI registered with the id '{id}'");
    }

    // Initialization

    internal static void Init()
    {
        CreateRootCanvas();

        // Global UI Pool Holder
        PoolHolder = new GameObject("PoolHolder");
        PoolHolder.transform.parent = CanvasRoot.transform;
        PoolHolder.SetActive(false);

        GameFrame.OnUpdate += Update;
        
        Initializing = false;
    }

    // Main UI Update loop

    internal static void Update()
    {
        if (Initializing)
            return;

        CoroutineUtility.TickRoutines();

        if (!AnyUIShowing || !CanvasRoot)
            return;

        InputManager.Update();

        InputFieldRef.UpdateInstances();
        UIBehaviourModel.UpdateInstances();

        // Update registered UIs
        PanelManager.focusHandledThisFrame = false;
        PanelManager.draggerHandledThisFrame = false;

        for (int i = 0; i < uiBases.Count; i++)
        {
            UIBase ui = uiBases[i];
            if (ui.Enabled) ui.Update();
        }
    }

    // UI Construction

    private static void CreateRootCanvas()
    {
        CanvasRoot = new GameObject("UniverseLibCanvas");
        UnityEngine.Object.DontDestroyOnLoad(CanvasRoot);
        CanvasRoot.hideFlags |= HideFlags.HideAndDontSave;
        CanvasRoot.layer = 5;
        CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

        CanvasRoot.SetActive(false);

        CanvasRoot.SetActive(true);
    }
}