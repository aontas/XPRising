using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using XPShared;
using XPShared.Transport.Messages;
using static XPShared.Transport.Messages.ProgressSerialisedMessage;

namespace ClientUI.UI.Panel;

public class ProgressBarPanel : ResizeablePanelBase
{
    public ProgressBarPanel(UIBase owner) : base(owner) { }

    public static ProgressBarPanel Instance { get; private set; }

    private const int BaseWidth = 400;
    private const int MinLevelWidth = 30;
    private const int BarHeight = 22;
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
    public override UIManager.Panels PanelType => UIManager.Panels.Progress;
    public override bool CanDragAndResize => true;

    private readonly Dictionary<string, ProgressBar> _bars = new();
    private readonly Dictionary<string, GameObject> _groups = new();

    private static readonly Color DefaultBarColour = new Color(0.5f, 0.8f, 0.1f);
    private static readonly Color HighlightColour = Color.yellow;
    private static readonly Color LowLightColour = Color.red;
    
    private int ExpectedHeight(int activeBars)
    {
        // Bar height is [20 + spacing + border]. total height is [bar height * count - spacing] as spacing should only be between bars. 
        return activeBars * (BarHeight + Spacing + Border) - Spacing + VPadding;
    }
    
    protected override void ConstructPanelContent()
    {
        Instance = this;
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false, true, true, 0);
        
        // Remove the rect mask so that items (e.g. floating text) can appear outside the panel
        var mask2D = ContentRoot.GetComponentInParent<RectMask2D>();
        if (mask2D) GameObject.Destroy(mask2D);
            
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
        UpdatePanelSize();
    }

    private void UpdatePanelSize()
    {
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
    }

    public void ChangeProgress(ProgressSerialisedMessage data)
    {
        if (!_bars.TryGetValue(data.Label, out var progressBar))
        {
            progressBar = Instance.AddBar(data.Group, data.Label);
        }

        
        var validatedProgress = Math.Clamp(data.ProgressPercentage, 0f, 1f);
        var colour = GetColour(data.Colour, validatedProgress);
        progressBar.SetProgress(validatedProgress, $"{data.Level:D2}", $"{data.Tooltip} ({validatedProgress:P})", data.Active, colour, data.Change);
        
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
        var progressBar = new ProgressBar(groupPanel, DefaultBarColour);
        _bars.Add(label, progressBar);
        
        return progressBar;
    }

    private class ProgressBar
    {
        private readonly GameObject _contentBase;
        private readonly CanvasGroup _canvasGroup;
        private readonly Outline _highlight;
        private readonly LayoutElement _layoutBackground;
        private readonly LayoutElement _layoutFilled;
        private readonly Text _tooltipText;
        private readonly Text _levelText;
        private readonly Image _barImage;
        private readonly Text _changeText;

        private readonly FrameTimer _timer = new();
        private int _burstTimeRemainingMs = 0;
        private bool _burstOff = true;
        private const int TaskIterationDelay = 15;
        
        // Timeline:
        // (flash in -> flash stay -> flash fade) x3 -> visible -> (if _burstOff) fade out
        
        // In animation order:
        private const int FlashInLengthMs = 150;
        private const int FlashLengthMs = 150;
        private const int FlashOutLengthMs = 150;
        private const int VisibleLengthMs = 1000;
        private const int FadeOutLengthMs = 500;

        private const int FlashPulseInEnds = FlashLengthMs + FlashOutLengthMs;
        private const int FlashPulseLengthMs = FlashInLengthMs + FlashLengthMs + FlashOutLengthMs;
        
        // Time remaining constants
        private const int FlashPulseEndsMs = VisibleLengthMs + FadeOutLengthMs;
        private const int BurstAnimationLength = FlashPulseLengthMs * 3 + FlashPulseEndsMs;

        public bool IsActive => _contentBase.active;

        public ProgressBar(GameObject panel, Color colour)
        {
            // This is the base panel for the bar
            _contentBase = UIFactory.CreateHorizontalGroup(panel, "ProgressBarBase", true, false, true, true, 0, default, Color.black);
            UIFactory.SetLayoutElement(_contentBase, minWidth: BaseWidth, minHeight: BarHeight, flexibleWidth: 0, flexibleHeight: 0, preferredHeight: BarHeight);
            _canvasGroup = _contentBase.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1.0f;
            _highlight = _contentBase.AddComponent<Outline>();
            _highlight.effectColor = Color.black;

            // Split the base bar panel into _levelTxt, progressBarSection and _tooltipTxt
            _levelText = UIFactory.CreateLabel(_contentBase, "levelText", "", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(_levelText.gameObject, minWidth: MinLevelWidth, minHeight: BarHeight,
                preferredHeight: BarHeight, preferredWidth: MinLevelWidth);

            var progressBarSection = UIFactory.CreateHorizontalGroup(_contentBase, "ProgressBarSection", false, true,
                true, true, 0, default, Color.black);
            UIFactory.SetLayoutElement(progressBarSection, minWidth: BaseWidth - MinLevelWidth, minHeight: BarHeight,
                flexibleWidth: 10000);

            var progressFilled = UIFactory.CreateUIObject("ProgressFilled", progressBarSection);
            _barImage = progressFilled.AddComponent<Image>();
            _barImage.color = colour;
            _layoutFilled = UIFactory.SetLayoutElement(progressFilled, minWidth: 0, flexibleWidth: 1);
            var progressBackground = UIFactory.CreateUIObject("ProgressBackground", progressBarSection);
            var backgroundImage = progressBackground.AddComponent<Image>();
            backgroundImage.color = Color.black;
            _layoutBackground = UIFactory.SetLayoutElement(progressBackground, minWidth: 0, flexibleWidth: 1);

            // Add the tooltip text after tha bars so that it appears on top
            _tooltipText = UIFactory.CreateLabel(progressBarSection, "tooltipText", "", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(_tooltipText.gameObject, ignoreLayout: true);
            // Outline the text so it can be seen regardless of the colour or bar fill.
            _tooltipText.gameObject.AddComponent<Outline>();
            var tooltipRect = _tooltipText.gameObject.GetComponent<RectTransform>();
            // Does this work?
            tooltipRect.anchorMin = Vector2.zero;
            tooltipRect.anchorMax = Vector2.one;
            
            // Add some change text. Positioning to be updated, but it should be outside the regular layout
            _changeText = UIFactory.CreateLabel(_levelText.gameObject, "ChangeText", "", alignment: TextAnchor.MiddleRight, color: HighlightColour);
            UIFactory.SetLayoutElement(_changeText.gameObject, ignoreLayout: true);
            _changeText.gameObject.AddComponent<Outline>();
            _changeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var floatingTextRect = _changeText.gameObject.GetComponent<RectTransform>();
            floatingTextRect.anchorMin = Vector2.zero;
            floatingTextRect.anchorMax = Vector2.up;
            floatingTextRect.pivot = new Vector2(1, 0.5f);
            floatingTextRect.localPosition = Vector3.left * 10;
            // Initialise it inactive
            _changeText.gameObject.SetActive(false);

            // Initialise the timer, so we can start/stop it as necessary
            _timer.Initialise(
                BurstIteration,
                TimeSpan.FromMilliseconds(TaskIterationDelay),
                false);
        }

        public void SetProgress(float progress, string level, string tooltip, ActiveState activeState, Color colour, string changeText)
        {
            _layoutBackground.flexibleWidth = 1.0f - progress;
            _layoutFilled.flexibleWidth = progress;
            _levelText.text = level;
            _tooltipText.text = tooltip;
            _barImage.color = colour;
            _changeText.text = changeText;
            _changeText.color = changeText.StartsWith("-") ? LowLightColour : HighlightColour;

            switch (activeState)
            {
                case ActiveState.NotActive:
                    if (_burstTimeRemainingMs > 0)
                    {
                        // If we are in a burst, then either this will disappear shortly, or we can update it to do so
                        if (!_burstOff)
                        {
                            // Use the max of the FadeOut length or time remaining, so it smoothly transitions out
                            _burstTimeRemainingMs = Math.Max(FadeOutLengthMs, _burstTimeRemainingMs);
                            _burstOff = true;
                        }
                    }
                    else if (_contentBase.active)
                    {
                        // If we are active, then fade out
                        _burstTimeRemainingMs = FadeOutLengthMs;
                        _burstOff = true;
                        _timer.Start();
                    }

                    break;
                case ActiveState.Active:
                    _contentBase.SetActive(true);
                    _canvasGroup.alpha = 1;
                    if (_burstTimeRemainingMs > 0)
                    {
                        _burstOff = false;
                    }
                    break;
                case ActiveState.Burst:
                    // If we are inactive, then burst on -> off. If we are active, burst on -> on (gives small flash animation)
                    _burstOff = !_contentBase.active;
                    _contentBase.SetActive(true);
                    _canvasGroup.alpha = 1;
                    // Set burst time remaining to full animation length
                    _burstTimeRemainingMs = BurstAnimationLength;
                    _timer.Start();
                    break;
            }
        }

        // See constants section for timeline
        private void BurstIteration()
        {
            switch (_burstTimeRemainingMs)
            {
                case > FlashPulseEndsMs:
                    // Do flash pulse
                    var flashPulseTimeMs = (_burstTimeRemainingMs - FlashPulseEndsMs) % FlashPulseLengthMs;
                    switch (flashPulseTimeMs)
                    {
                        case > FlashPulseInEnds:
                            // Fade in to full colour
                            _highlight.effectColor = Color.Lerp(HighlightColour, Color.black, Math.Max((float)(flashPulseTimeMs - FlashPulseInEnds)/FlashInLengthMs, 0));
                            break;
                        case > FlashOutLengthMs:
                            // Stay at full visibility
                            _highlight.effectColor = HighlightColour;
                            break;
                        case > 0:
                            // Start fading highlight out
                            _highlight.effectColor = Color.Lerp(Color.black, HighlightColour, Math.Max((float)flashPulseTimeMs/FlashLengthMs, 0));
                            break;
                    }
                    // Show change text
                    _changeText.gameObject.SetActive(true);
                    _changeText.color = HighlightColour;
                    break;
                case > FadeOutLengthMs:
                    // Total visible length
                    _highlight.effectColor = Color.black;
                    // Hide change text
                    if (_changeText.gameObject.active) _changeText.gameObject.SetActive(false);
                    break;
                case > 0:
                    // Fade out overtime
                    if (_burstOff) _canvasGroup.alpha = Math.Min((float)_burstTimeRemainingMs / FadeOutLengthMs, 1.0f);
                    // If not fading out, then we are done with the animation. Skip to end.
                    else _burstTimeRemainingMs = 0;
                    break;
                default:
                    _timer.Dispose();
                    if (_burstOff)
                    {
                        _contentBase.SetActive(false);
                        ProgressBarPanel.Instance.UpdatePanelSize();
                    }
                    break;
            }
            
            _burstTimeRemainingMs = Math.Max(_burstTimeRemainingMs - TaskIterationDelay, 0);
        }
    }

    private Color GetColour(string colour, float progressPercentage)
    {
        if (string.IsNullOrEmpty(colour)) return DefaultBarColour;
        if (colour.StartsWith("@"))
        {
            var colourStrings = colour.Split("@", StringSplitOptions.RemoveEmptyEntries);
            if (colourStrings.Length == 0) return DefaultBarColour;
            if (colourStrings.Length == 1)
            {
                if (!ColorUtility.TryParseHtmlString(colourStrings[0], out var onlyColor)) onlyColor = DefaultBarColour;
                return onlyColor;
            }
            
            var internalRange = progressPercentage * (colourStrings.Length - 1);
            var index = (int)Math.Floor(internalRange);
            internalRange -= index;
            if (!ColorUtility.TryParseHtmlString(colourStrings[index], out var colour1)) colour1 = DefaultBarColour;
            if (!ColorUtility.TryParseHtmlString(colourStrings[index + 1], out var colour2)) colour2 = DefaultBarColour;
            return Color.Lerp(colour1, colour2, internalRange);
        }

        return !ColorUtility.TryParseHtmlString(colour, out var color) ? DefaultBarColour : color;
    }
}