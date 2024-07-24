using ClientUI.UI.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XPShared;
using static XPShared.Transport.Messages.ProgressSerialisedMessage;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class ProgressBar
{
    public const int BaseWidth = 400;
    public const int BarHeight = 22;

    private const int MinLevelWidth = 30;

    private readonly GameObject _contentBase;
    private readonly CanvasGroup _canvasGroup;
    private readonly Outline _highlight;
    private readonly LayoutElement _layoutBackground;
    private readonly LayoutElement _layoutFilled;
    private readonly TextMeshProUGUI _tooltipText;
    private readonly TextMeshProUGUI _levelText;
    private readonly Image _barImage;
    private readonly TextMeshProUGUI _changeText;

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
    private const int VisibleLengthMs = 500;
    private const int FadeOutLengthMs = 500;

    private const int FlashPulseInEnds = FlashLengthMs + FlashOutLengthMs;
    private const int FlashPulseLengthMs = FlashInLengthMs + FlashLengthMs + FlashOutLengthMs;
    
    // Time remaining constants
    private const int FlashPulseEndsMs = VisibleLengthMs + FadeOutLengthMs;
    private const int BurstAnimationLength = FlashPulseLengthMs * 3 + FlashPulseEndsMs;

    public bool IsActive => _contentBase.active;

    public event EventHandler ProgressBarMinimised;

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
        _levelText = UIFactory.CreateLabel(_contentBase, "levelText", "");
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

        // Add the tooltip text after the bars so that it appears on top
        _tooltipText = UIFactory.CreateLabel(progressBarSection, "tooltipText", "");
        UIFactory.SetLayoutElement(_tooltipText.gameObject, ignoreLayout: true);
        // Outline the text so it can be seen regardless of the colour or bar fill.
        _tooltipText.gameObject.AddComponent<Outline>();
        var tooltipRect = _tooltipText.gameObject.GetComponent<RectTransform>();
        // Does this work?
        tooltipRect.anchorMin = Vector2.zero;
        tooltipRect.anchorMax = Vector2.one;
        
        // Add some change text. Positioning to be updated, but it should be outside the regular layout
        _changeText = UIFactory.CreateLabel(_levelText.gameObject, "ChangeText", "", alignment: TextAlignmentOptions.MidlineRight, color: Colour.HighlightColour);
        UIFactory.SetLayoutElement(_changeText.gameObject, ignoreLayout: true);
        _changeText.gameObject.AddComponent<Outline>();
        _changeText.overflowMode = TextOverflowModes.Overflow;
        var floatingTextRect = _changeText.gameObject.GetComponent<RectTransform>();
        floatingTextRect.anchorMin = Vector2.zero;
        floatingTextRect.anchorMax = Vector2.up;
        floatingTextRect.pivot = new Vector2(1, 0.5f);
        floatingTextRect.localPosition = Vector3.left * 10;
        floatingTextRect.sizeDelta = new Vector2(50, 25);
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
        _changeText.color = changeText.StartsWith("-") ? Colour.LowLightColour : Colour.HighlightColour;

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
                _burstOff = false;
                break;
            case ActiveState.Burst:
                // If we are inactive, then burst on -> off. If we are active and not already bursting to off, burst on -> on (gives small flash animation)
                _burstOff = _burstOff || !_contentBase.active;
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
                        _highlight.effectColor = Color.Lerp(Colour.HighlightColour, Color.black, Math.Max((float)(flashPulseTimeMs - FlashPulseInEnds)/FlashInLengthMs, 0));
                        break;
                    case > FlashOutLengthMs:
                        // Stay at full visibility
                        _highlight.effectColor = Colour.HighlightColour;
                        break;
                    case > 0:
                        // Start fading highlight out
                        _highlight.effectColor = Color.Lerp(Color.black, Colour.HighlightColour, Math.Max((float)flashPulseTimeMs/FlashLengthMs, 0));
                        break;
                }
                // Show change text
                _changeText.gameObject.SetActive(true);
                _changeText.color = Colour.HighlightColour;
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
                _timer.Stop();
                if (_burstOff)
                {
                    _contentBase.SetActive(false);
                    OnProgressBarMinimised();
                }
                break;
        }
        
        _burstTimeRemainingMs = Math.Max(_burstTimeRemainingMs - TaskIterationDelay, 0);
    }

    private void OnProgressBarMinimised()
    {
        ProgressBarMinimised?.Invoke(this, EventArgs.Empty);
    }
}