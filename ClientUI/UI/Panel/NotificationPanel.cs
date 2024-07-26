using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XPShared;
using XPShared.Transport.Messages;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class NotificationPanel
{
    private readonly GameObject _contentRoot;
    private GameObject _containerGameObject;
    public NotificationPanel(GameObject root)
    {
        _contentRoot = root;
        MakeContainer();
    }

    private void MakeContainer()
    {
        _containerGameObject = UIFactory.CreateUIObject("notifications", _contentRoot);
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_containerGameObject, false, false, true, true, 5, childAlignment: TextAnchor.LowerCenter);
        UIFactory.SetLayoutElement(_containerGameObject, minHeight: 25, minWidth: 30, flexibleHeight: 9999, flexibleWidth: 9999);
        var rect = _containerGameObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0);
        
        // Queue up 3 available notifications
        var notification = new Notification(_containerGameObject);
        notification.NotificationOver += (_, _) => { NotificationEnd(); };
        _availableNotifications.Enqueue(notification);
        notification = new Notification(_containerGameObject);
        notification.NotificationOver += (_, _) => { NotificationEnd(); };
        _availableNotifications.Enqueue(notification);
        notification = new Notification(_containerGameObject);
        notification.NotificationOver += (_, _) => { NotificationEnd(); };
        _availableNotifications.Enqueue(notification);
    }
    
    private readonly Queue<Tuple<string, Color>> _pendingNotifications = new();
    private readonly Queue<Notification> _availableNotifications = new();
    private readonly Queue<Notification> _notifications = new();

    public bool Active
    {
        get => _contentRoot.active;
        set => _contentRoot.SetActive(value);
    }

    public void AddNotification(NotificationMessage data)
    {
        // TODO set better colours.
        Color colour;
        switch (data.Severity)
        {
            case LogLevel.None:
            case LogLevel.Fatal:
            case LogLevel.Error:
                colour = Color.red;
                break;
            case LogLevel.Warning:
                colour = Color.Lerp(Color.red, Color.yellow, 0.5f);
                break;
            case LogLevel.Message:
            case LogLevel.Info:
                colour = Color.cyan;
                break;
            case LogLevel.Debug:
            case LogLevel.All:
            default:
                colour = Color.green;
                break;
        }
        
        _pendingNotifications.Enqueue(new Tuple<string, Color>(data.Message, colour));
        RequestNotification();
    }

    internal void Reset()
    {
        GameObject.Destroy(_containerGameObject);
        _notifications.Clear();
        _availableNotifications.Clear();
        _pendingNotifications.Clear();
        MakeContainer();
    }

    private void RequestNotification()
    {
        if (_pendingNotifications.Count == 0 || _availableNotifications.Count == 0) return;

        var (message, colour) = _pendingNotifications.Dequeue();
        var notification = _availableNotifications.Dequeue();
        _notifications.Enqueue(notification);
        notification.SetNotification(message, colour);
    }

    private void NotificationEnd()
    {
        var notification = _notifications.Dequeue();
        _availableNotifications.Enqueue(notification);

        RequestNotification();
    }
    
    private class Notification
    {
        public const int Width = 200;
        public const int Height = 50;

        private readonly GameObject _contentBase;
        private readonly CanvasGroup _canvasGroup;
        private readonly Image _background;
        private readonly TextMeshProUGUI _messageText;

        private readonly FrameTimer _timer = new();
        private int _burstTimeRemainingMs = 0;
        private const int TaskIterationDelay = 15;
        
        // Timeline:
        // fade in -> visible -> fade out
        
        // In animation order:
        private const int FadeInLengthMs = 500;
        private const int VisibleLengthMs = 2000;
        private const int FadeOutLengthMs = 500;

        // Time remaining constants
        private const int FadeInEnd = VisibleLengthMs + FadeOutLengthMs;
        private const int BurstAnimationLength = FadeInLengthMs + VisibleLengthMs + FadeOutLengthMs;

        public bool IsActive => _contentBase.active;

        public event EventHandler NotificationOver;

        public Notification(GameObject panel)
        {
            // This is the base panel for the bar
            _contentBase = UIFactory.CreateUIObject("NotificationBase", panel, new Vector2(Width, Height));
            _canvasGroup = _contentBase.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0.0f;
            UIFactory.SetLayoutElement(_contentBase, minWidth: Width, minHeight: Height, flexibleWidth: 1, flexibleHeight: 0, preferredHeight: Height);
            _background = _contentBase.AddComponent<Image>();
            
            _messageText = UIFactory.CreateLabel(_contentBase, "message", "");
            var textRect = _messageText.gameObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            
            _contentBase.SetActive(false);
            
            // Initialise the timer, so we can start/stop it as necessary
            _timer.Initialise(
                BurstIteration,
                TimeSpan.FromMilliseconds(TaskIterationDelay),
                false);
        }

        public void SetNotification(String message, Color colour)
        {
            _messageText.text = message;
            _background.color = colour;
            _burstTimeRemainingMs = BurstAnimationLength;
            _contentBase.transform.SetAsFirstSibling();
            if (!IsActive) _contentBase.SetActive(true);
            _timer.Start();
        }

        // See constants section for timeline
        private void BurstIteration()
        {
            switch (_burstTimeRemainingMs)
            {
                case > FadeInEnd:
                    // Fade in to full colour
                    float fadeInRemaining = _burstTimeRemainingMs - FadeInEnd;
                    _canvasGroup.alpha = Math.Min((FadeInLengthMs - fadeInRemaining) / FadeInLengthMs, 1.0f);
                    break;
                case > FadeOutLengthMs:
                    break;
                case > 0:
                    // Fade out overtime
                    _canvasGroup.alpha = Math.Min((float)_burstTimeRemainingMs / FadeOutLengthMs, 1.0f);
                    break;
                default:
                    _timer.Stop();
                    OnTimerFinished();
                    break;
            }
            
            _burstTimeRemainingMs = Math.Max(_burstTimeRemainingMs - TaskIterationDelay, 0);
        }

        private void OnTimerFinished()
        {
            if (IsActive) _contentBase.SetActive(false);
            NotificationOver?.Invoke(this, EventArgs.Empty);
        }
    }
}