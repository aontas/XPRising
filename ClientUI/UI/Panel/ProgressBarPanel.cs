using BepInEx.Logging;
using ClientUI.UI.Util;
using UnityEngine;
using UnityEngine.UI;
using XPShared.Transport.Messages;
using UIBase = ClientUI.UniverseLib.UI.UIBase;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class ProgressBarPanel : ResizeablePanelBase
{
    public ProgressBarPanel(UIBase owner) : base(owner) { }

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
    private readonly Dictionary<string, Group> _groups = new();

    private struct Group
    {
        public GameObject GameObject;
        public RectTransform RectTransform;

        public Group(RectTransform rectTransform, GameObject gameObject)
        {
            RectTransform = rectTransform;
            GameObject = gameObject;
        }
    }
    
    private int ExpectedHeight(int activeBars)
    {
        // Bar height is [20 + spacing + border]. total height is [bar height * count - spacing] as spacing should only be between bars. 
        return activeBars * (ProgressBar.BarHeight + Spacing + Border) - Spacing + VPadding;
    }
    
    protected override void ConstructPanelContent()
    {
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
        float UpdateGroupPanelSize(RectTransform rectTransform)
        {
            var groupHeight = ExpectedHeight(rectTransform.GetAllChildren().Count(transform => transform.gameObject.active));
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, groupHeight);
            return groupHeight;
        }

        foreach (var (_, group) in _groups)
        {
            var height = UpdateGroupPanelSize(group.RectTransform);
            groupHeightSum += height;
        }
        
        // Update main size
        Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, groupHeightSum);
        Dragger.DraggableArea = Rect;
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
            progressBar = AddBar(data.Group, data.Label);
        }

        
        var validatedProgress = Math.Clamp(data.ProgressPercentage, 0f, 1f);
        var colour = Colour.ParseColour(data.Colour, validatedProgress);
        progressBar.SetProgress(validatedProgress, $"{data.Level:D2}", $"{data.Tooltip} ({validatedProgress:P})", data.Active, colour, data.Change);

        if (data.Change != "")
        {
            FloatingText.SpawnFloatingText(ContentRoot, data.Change, Colour.HighlightColour);
        }
        
        // Update panel size
        UpdatePanelSize();
    }

    internal override void Reset()
    {
        foreach (var (_, group) in _groups)
        {
            GameObject.Destroy(group.GameObject);
        }
        _groups.Clear();
        _bars.Clear();
        UpdatePanelSize();
    }

    private ProgressBar AddBar(string groupName, string label)
    {
        if (!_groups.TryGetValue(groupName, out var group))
        {
            var groupGameObject = UIFactory.CreateVerticalGroup(ContentRoot, groupName, true, false, true, true, Spacing, padding: _paddingVector);
            group.GameObject = groupGameObject;
            group.RectTransform = groupGameObject.GetComponent<RectTransform>();
            _groups.Add(groupName, group);
        }
        var progressBar = new ProgressBar(group.GameObject, Colour.DefaultBarColour);
        _bars.Add(label, progressBar);
        progressBar.ProgressBarMinimised += (_, _) => { UpdatePanelSize(); }; 
        
        return progressBar;
    }
}