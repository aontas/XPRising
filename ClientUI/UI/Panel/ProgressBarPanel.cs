using ClientUI.UI.Util;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using XPShared.Transport.Messages;

namespace ClientUI.UI.Panel;

public class ProgressBarPanel : ResizeablePanelBase
{
    public ProgressBarPanel(UIBase owner) : base(owner) { }

    public static ProgressBarPanel Instance { get; private set; }

    private const int BaseWidth = 400;
    private const int BasePadding = 4;
    private const int Spacing = 4;
    private const int VPadding = 2;
    private const int HPadding = 2;
    private const int Border = 2;
    private readonly Vector4 _paddingVector = new Vector4(VPadding, VPadding, HPadding, HPadding);
    
    public override string Name => "XPRising.Progress";
    public override int MinWidth => BaseWidth + HPadding * 2 + Border + BasePadding;
    public override int MinHeight => ExpectedHeight(_bars.Count(p => p.Value.IsActive)) + BasePadding;
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 1f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 1f);
    public override Vector2 DefaultPosition => new Vector2(850, -225);
    protected override UIManager.Panels PanelType => UIManager.Panels.Progress;
    public override bool CanDragAndResize => true;

    private readonly Dictionary<string, ProgressBar> _bars = new();
    private readonly Dictionary<string, GameObject> _groups = new();
    
    private int ExpectedHeight(int activeBars)
    {
        // Bar height is [20 + spacing + border]. total height is [bar height * count - spacing] as spacing should only be between bars. 
        return activeBars * (ProgressBar.BarHeight + Spacing + Border) - Spacing + VPadding;
    }
    
    protected override void ConstructPanelContent()
    {
        Instance = this;
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false, true, true, 0);
        
        // Remove the rect mask so that items (e.g. floating text) can appear outside the panel
        RemoveDefaultPanelImageAndMask();
            
        // Disable the title bar
        TitleBar.SetActive(false);
        // Enable the draggable box area (this now being set to the whole panel)
        UpdatePanelSize();
    }

    private bool _inUpdatePanelSize = false;

    internal void UpdatePanelSize()
    {
        _inUpdatePanelSize = true;
        var groupHeightSum = (float)BasePadding;
        float UpdateGroupPanelSize(GameObject gameObject)
        {
            var groupRect = gameObject.GetComponent<RectTransform>();
            var groupHeight = ExpectedHeight(groupRect.GetAllChildren().Count(transform => transform.gameObject.active));
            groupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, groupHeight);
            return groupHeight;
        }

        foreach (var (key, groupGameObject) in _groups)
        {
            var height = UpdateGroupPanelSize(groupGameObject);
            groupHeightSum += height;
        }
        
        // Update main size
        Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, groupHeightSum);
        Dragger.DragableArea = Rect;
        Dragger.OnEndResize();
        _inUpdatePanelSize = false;
    }

    public override void OnFinishResize()
    {
        if (!_inUpdatePanelSize)
        {
            // As we can't disable vertical resizing, just reset the height if the user drags. 
            UpdatePanelSize();
            base.OnFinishResize();
        }
    }
    
    public void ChangeProgress(ProgressSerialisedMessage data)
    {
        if (!_bars.TryGetValue(data.Label, out var progressBar))
        {
            progressBar = Instance.AddBar(data.Group, data.Label);
        }

        
        var validatedProgress = Math.Clamp(data.ProgressPercentage, 0f, 1f);
        var colour = Colour.ParseColour(data.Colour, validatedProgress);
        progressBar.SetProgress(validatedProgress, $"{data.Level:D2}", $"{data.Tooltip} ({validatedProgress:P})", data.Active, colour, data.Change);

        if (data.Change != "")
        {
            FloatingText.SpawnFloatingText(progressBar._contentBase, data.Change, Colour.HighlightColour);
        }
        
        // Update panel size
        UpdatePanelSize();
    }

    private ProgressBar AddBar(string group, string label)
    {
        if (!_groups.TryGetValue(group, out var groupPanel))
        {
            groupPanel = UIFactory.CreateVerticalGroup(ContentRoot, group, true, false, true, true, Spacing, padding: _paddingVector);
            _groups.Add(group, groupPanel);
        }
        var progressBar = new ProgressBar(groupPanel, Colour.DefaultBarColour);
        _bars.Add(label, progressBar);
        
        return progressBar;
    }
}