using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ClientUI.UI.Panel;

public class ProgressPanelBase : ResizeablePanelBase
{
    public ProgressPanelBase(UIBase owner) : base(owner) { }

    public static ProgressPanelBase Instance { get; private set; }

    public override string Name => "XPRising.Progress";
    public override int MinWidth => 600;
    public override int MinHeight => Math.Max(bars.Count * 24, 1); // Bar height is 20 + 4 spacing
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 1f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 1f);
    public override Vector2 DefaultPosition => new Vector2(238, 0);
    public static float CurrentPanelWidth => Instance.Rect.rect.width;
    public static float CurrentPanelHeight => Instance.Rect.rect.height;
    public override UIManager.Panels PanelType => UIManager.Panels.Progress;
    public override bool CanDragAndResize => true;

    private static Dictionary<string, ProgressBar> bars = new();

    protected override void ConstructPanelContent()
    {
        Instance = this;
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false, true, true, 4);
        // this.Rect.
        // Display._mainDisplay.renderingWidth;
            
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
        Dragger.DragableArea = this.Rect;
    }

    public void ChangeProgress(string label, int level, float progress, string tooltip)
    {
        if (!bars.TryGetValue(label, out var progressBar))
        {
            progressBar = Instance.AddBar(label);
        }
            
        progressBar.SetProgress(progress, $"{level:D2}", $"{tooltip} ({progress:P})");
    }

    private ProgressBar AddBar(string label)
    {
        var progressBar = new ProgressBar(ContentRoot.gameObject, new Color(0.5f, 0.8f, 0.1f));
        bars.Add(label, progressBar);
        Instance.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bars.Count * 24);
        return progressBar;
    }

    private class ProgressBar
    {
        private readonly LayoutElement _layoutBackground;
        private readonly LayoutElement _layoutFilled;
        private readonly Text _tooltipTxt;
        private readonly Text _levelTxt;

        public ProgressBar(GameObject panel, Color colour)
        {
            // This is the base panel for the bar
            var contentBase = UIFactory.CreateHorizontalGroup(panel, "ProgressBarBase", true, true, true, true, 0, default, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(contentBase, minWidth: 400, minHeight: 20, flexibleWidth: 0, flexibleHeight: 0, preferredHeight: 20);
        
            // Split the base bar panel into LevelTxt, progressBar and TooltipText
            _levelTxt = UIFactory.CreateLabel(contentBase, "levelText", $"00", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(_levelTxt.gameObject, minWidth: 30, minHeight: 20, preferredHeight: 20, preferredWidth: 30);
            
            var progressBarSection = UIFactory.CreateHorizontalGroup(contentBase, "ProgressBarSection", false, true, true, true, 0, default, Color.black);
            UIFactory.SetLayoutElement(progressBarSection, minWidth: 300, minHeight: 20, flexibleWidth: 10000);
                
            var progressFilled = UIFactory.CreateUIObject("ProgressFilled", progressBarSection);
            var filledImage = progressFilled.AddComponent<Image>();
            filledImage.color = colour;
            _layoutFilled = UIFactory.SetLayoutElement(progressFilled, minWidth: 0, flexibleWidth: 1);
            var progressBackground = UIFactory.CreateUIObject("ProgressBackground", progressBarSection);
            var backgroundImage = progressBackground.AddComponent<Image>();
            backgroundImage.color = Color.black;
            _layoutBackground = UIFactory.SetLayoutElement(progressBackground, minWidth: 0, flexibleWidth: 1);
            
            _tooltipTxt = UIFactory.CreateLabel(progressBarSection, "tooltipText", $"", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(_tooltipTxt.gameObject, ignoreLayout: true);
            var tooltipRect = _tooltipTxt.gameObject.GetComponent<RectTransform>();
            tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
            tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 60);
            var outline = _tooltipTxt.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
        }
            
        public void SetProgress(float progress, string level, string tooltip)
        {
            _layoutBackground.flexibleWidth = 1.0f - progress;
            _layoutFilled.flexibleWidth = progress;
            _levelTxt.text = level;
            _tooltipTxt.text = tooltip;
        }
    }
}