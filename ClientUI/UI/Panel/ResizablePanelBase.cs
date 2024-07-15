using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Panels;

namespace ClientUI.UI.Panel;

public abstract class ResizeablePanelBase : PanelBase
{
    protected ResizeablePanelBase(UIBase owner) : base(owner) { }

    protected abstract UIManager.Panels PanelType { get; }
    public override bool CanDragAndResize => true;

    private bool ApplyingSaveData { get; set; } = true;

    protected override void ConstructPanelContent()
    {
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
        Dragger.DragableArea = Rect;
        // Update resizer elements
        Dragger.OnEndResize();
    }

    protected void RemoveDefaultPanelImageAndMask()
    {
        // Remove the rect mask so that items (e.g. floating text) can appear outside the panel
        var mask2D = ContentRoot.GetComponentInParent<RectMask2D>();
        if (mask2D) GameObject.Destroy(mask2D);
        
        // Remove the panel backgrounds, to enable easier manual layouts and resizing
        var componentsInChildren = ContentRoot.transform.parent.GetComponentsInChildren<Image>();
        foreach (var component in componentsInChildren)
        {
            GameObject.Destroy(component);
        }
    }

    protected override void OnClosePanelClicked()
    {
        // Do nothing for now
    }

    public override void OnFinishDrag()
    {
        base.OnFinishDrag();
        SaveInternalData();
    }

    public override void OnFinishResize()
    {
        base.OnFinishResize();
        SaveInternalData();
    }

    public void SaveInternalData()
    {
        if (ApplyingSaveData) return;

        SetSaveDataToConfigValue();
    }

    private void SetSaveDataToConfigValue()
    {
        Plugin.Instance.Config.Bind("Panels", $"{PanelType}", "", "Serialised panel data").Value = this.ToSaveData();
    }

    private string ToSaveData()
    {
        try
        {
            return string.Join("|", new string[]
            {
                Rect.RectAnchorsToString(),
                Rect.RectPositionToString()
            });
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Warning,$"Exception generating Panel save data: {ex}");
            return "";
        }
    }

    private void ApplySaveData()
    {
        var data = Plugin.Instance.Config.Bind("Panels", $"{PanelType}", "", "Serialised panel data").Value;
        ApplySaveData(data);
    }

    private void ApplySaveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;
        string[] split = data.Split('|');

        try
        {
            Rect.SetAnchorsFromString(split[0]);
            Rect.SetPositionFromString(split[1]);
            this.EnsureValidSize();
            this.EnsureValidPosition();
        }
        catch
        {
            Plugin.Log(LogLevel.Warning, "Invalid or corrupt panel save data! Restoring to default.");
            SetDefaultSizeAndPosition();
            SetSaveDataToConfigValue();
        }
    }

    protected override void LateConstructUI()
    {
        ApplyingSaveData = true;

        base.LateConstructUI();

        // apply panel save data or revert to default
        try
        {
            ApplySaveData();
        }
        catch (Exception ex)
        {
            Plugin.Log(LogLevel.Error,$"Exception loading panel save data: {ex}");
            SetDefaultSizeAndPosition();
        }

        ApplyingSaveData = false;

        Dragger.OnEndResize();
    }
}