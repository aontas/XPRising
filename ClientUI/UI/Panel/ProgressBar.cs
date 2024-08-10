using BepInEx.Logging;
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
    public const int BaseWidth = 50;
    public const int BarHeight = 22;

    private const int MinHeaderWidth = 30;

    private readonly GameObject _contentBase;
    private readonly CanvasGroup _canvasGroup;
    private readonly Outline _highlight;
    private readonly LayoutElement _layoutBackground;
    private readonly LayoutElement _layoutFilled;
    private readonly TextMeshProUGUI _tooltipText;
    private readonly TextMeshProUGUI _headerText;
    private readonly Image _barImage;
    private readonly TextMeshProUGUI _changeText;

    private readonly FrameTimer _timer = new();
    private int _alertTimeRemainingMs = 0;
    private bool _alertTransitionOff = true;
    private const int TaskIterationDelay = 15;
    
    // Timeline:
    // (flash in -> flash stay -> flash fade) x3 -> visible -> (if _alertOff) fade out
    
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
    private const int AlertAnimationLength = FlashPulseLengthMs * 3 + FlashPulseEndsMs;

    public bool IsActive => _contentBase.active;

    public event EventHandler ProgressBarMinimised;

    private ActiveState _activeState = ActiveState.Unchanged;

    public ProgressBar(GameObject panel, Color colour)
    {
        // This is the base panel for the bar
        _contentBase = UIFactory.CreateHorizontalGroup(panel, "ProgressBarBase", true, false, true, true, 0, default, Color.black);
        UIFactory.SetLayoutElement(_contentBase, minWidth: BaseWidth, minHeight: BarHeight, flexibleWidth: 0, flexibleHeight: 0, preferredHeight: BarHeight);
        _canvasGroup = _contentBase.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 1.0f;
        _highlight = _contentBase.AddComponent<Outline>();
        _highlight.effectColor = Color.black;

        // Split the base bar panel into _headerTxt, progressBarSection and _tooltipTxt
        _headerText = UIFactory.CreateLabel(_contentBase, "HeaderText", "");
        UIFactory.SetLayoutElement(_headerText.gameObject, minWidth: MinHeaderWidth, minHeight: BarHeight,
            preferredHeight: BarHeight, preferredWidth: MinHeaderWidth);

        var progressBarSection = UIFactory.CreateHorizontalGroup(_contentBase, "ProgressBarSection", false, true,
            true, true, 0, default, Colour.PanelBackground);
        UIFactory.SetLayoutElement(progressBarSection, minWidth: BaseWidth - MinHeaderWidth, minHeight: BarHeight,
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
        tooltipRect.anchorMin = Vector2.zero;
        tooltipRect.anchorMax = Vector2.one;
        
        // Add some change text. Positioning to be updated, but it should be outside the regular layout
        _changeText = UIFactory.CreateLabel(_headerText.gameObject, "ChangeText", "", alignment: TextAlignmentOptions.MidlineRight, color: Colour.Highlight);
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
            AlertIteration,
            TimeSpan.FromMilliseconds(TaskIterationDelay),
            false);
    }

    public void Reset()
    {
        _timer.Stop();
    }
    
    public void SetProgress(float progress, string header, string tooltip, ActiveState activeState, Color colour,
        string changeText, bool flash)
    {
        _layoutBackground.flexibleWidth = 1.0f - progress;
        _layoutFilled.flexibleWidth = progress;
        _headerText.text = header;
        _tooltipText.text = tooltip;
        _barImage.color = colour;
        _changeText.text = changeText;
        _changeText.color = changeText.StartsWith("-") ? Colour.NegativeChange : Colour.PositiveChange;

        switch (activeState)
        {
            case ActiveState.NotActive:
                if (_alertTimeRemainingMs > 0)
                {
                    // If we are in an alert, then either this will disappear shortly, or we can update it to disappear
                    if (!_alertTransitionOff)
                    {
                        // Use the max of the FadeOut length or time remaining, so it smoothly transitions out
                        _alertTimeRemainingMs = Math.Max(FadeOutLengthMs, _alertTimeRemainingMs);
                        _alertTransitionOff = true;
                    }
                }
                else if (_activeState == ActiveState.Active)
                {
                    // If we are active, then fade out
                    _alertTimeRemainingMs = FadeOutLengthMs;
                    _alertTransitionOff = true;
                    _timer.Start();
                }
                else if (_activeState == ActiveState.Unchanged)
                {
                    _activeState = ActiveState.NotActive;
                    _contentBase.SetActive(false);
                }
                break;
            case ActiveState.Active:
                _activeState = ActiveState.Active;
                _contentBase.SetActive(true);
                _contentBase.transform.parent.gameObject.SetActive(true);
                _canvasGroup.alpha = 1;
                _alertTransitionOff = false;
                break;
            case ActiveState.Unchanged:
                break;
        }

        if (flash)
        {
            _activeState = ActiveState.Active;
            _canvasGroup.alpha = 1;
            // Set alert time remaining to full animation length
            _alertTimeRemainingMs = AlertAnimationLength;
            _timer.Start();
        }
    }

    // See constants section for timeline
    private void AlertIteration()
    {
        // Only iterate if this is active (don't progress if the UI is disabled)
        if (!IsActive) return;
        
        switch (_alertTimeRemainingMs)
        {
            case > FlashPulseEndsMs:
                // Do flash pulse
                var flashPulseTimeMs = (_alertTimeRemainingMs - FlashPulseEndsMs) % FlashPulseLengthMs;
                switch (flashPulseTimeMs)
                {
                    case > FlashPulseInEnds:
                        // Fade in to full colour
                        _highlight.effectColor = Color.Lerp(Colour.Highlight, Color.black, Math.Max((float)(flashPulseTimeMs - FlashPulseInEnds)/FlashInLengthMs, 0));
                        break;
                    case > FlashOutLengthMs:
                        // Stay at full visibility
                        _highlight.effectColor = Colour.Highlight;
                        break;
                    case > 0:
                        // Start fading highlight out
                        _highlight.effectColor = Color.Lerp(Color.black, Colour.Highlight, Math.Max((float)flashPulseTimeMs/FlashLengthMs, 0));
                        break;
                }
                // Show change text
                _changeText.gameObject.SetActive(true);
                _changeText.color = Colour.Highlight;
                break;
            case > FadeOutLengthMs:
                // Total visible length
                _highlight.effectColor = Color.black;
                // Hide change text
                if (_changeText.gameObject.active) _changeText.gameObject.SetActive(false);
                break;
            case > 0:
                // Fade out overtime
                if (_alertTransitionOff) _canvasGroup.alpha = Math.Min((float)_alertTimeRemainingMs / FadeOutLengthMs, 1.0f);
                // If not fading out, then we are done with the animation. Skip to end.
                else _alertTimeRemainingMs = 0;
                break;
            default:
                _timer.Stop();
                if (_alertTransitionOff)
                {
                    _activeState = ActiveState.NotActive;
                    _contentBase.SetActive(false);
                    OnProgressBarMinimised();
                }
                break;
        }
        
        _alertTimeRemainingMs = Math.Max(_alertTimeRemainingMs - TaskIterationDelay, 0);
    }

    private void OnProgressBarMinimised()
    {
        ProgressBarMinimised?.Invoke(this, EventArgs.Empty);
    }
}