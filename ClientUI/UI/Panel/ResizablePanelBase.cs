using BepInEx.Logging;
using UniverseLib.UI;

namespace ClientUI.UI.Panel;

public abstract class ResizeablePanelBase : UniverseLib.UI.Panels.PanelBase
{
    public ResizeablePanelBase(UIBase owner) : base(owner)
    {
    }
    
    public abstract UIManager.Panels PanelType { get; }
    public virtual bool ShouldSaveActiveState => true;
    public override bool CanDragAndResize => true;
    
    public bool ApplyingSaveData { get; set; }

    protected override void ConstructPanelContent()
    {
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
        Dragger.DragableArea = this.Rect;
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
        if (ApplyingSaveData)
            return;

        SetSaveDataToConfigValue();
    }

    private void SetSaveDataToConfigValue()
    {
        Plugin.Instance.Config.Bind("Panels", $"{PanelType}", "", "Serialised panel data").Value = this.ToSaveData();
    }
    
    // internal static readonly Dictionary<UIManager.Panels, ConfigElement<string>> PanelSaveData = new();
    //
    // internal static ConfigElement<string> GetPanelSaveData(UIManager.Panels panel)
    // {
    //     if (!PanelSaveData.ContainsKey(panel))
    //         PanelSaveData.Add(panel, new ConfigElement<string>(panel.ToString(), string.Empty, string.Empty, true));
    //     return PanelSaveData[panel];
    // }
    public virtual string ToSaveData()
    {
        try
        {
            return string.Join("|", new string[]
            {
                $"{ShouldSaveActiveState && Enabled}",
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

    public virtual void ApplySaveData()
    {
        // string data = ConfigManager.GetPanelSaveData(this.PanelType).Value;
        var data = Plugin.Instance.Config.Bind("Panels", $"{PanelType}", "", "Serialised panel data").Value;
        ApplySaveData(data);
    }

    protected virtual void ApplySaveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;

        string[] split = data.Split('|');

        try
        {
            Rect.SetAnchorsFromString(split[1]);
            Rect.SetPositionFromString(split[2]);
            this.EnsureValidSize();
            this.EnsureValidPosition();
            this.SetActive(bool.Parse(split[0]));
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

        // simple listener for saving enabled state
        this.OnToggleEnabled += (bool val) =>
        {
            SaveInternalData();
        };

        ApplyingSaveData = false;

        Dragger.OnEndResize();
    }
}