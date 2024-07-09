using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine.UI;
using UnityEngine;
using UniverseLib.UI;

namespace ClientUI.Client.UI.Panel
{
    internal class ProgressPanelBase : UniverseLib.UI.Panels.PanelBase
    {
        public ProgressPanelBase(UIBase owner) : base(owner) { }

        public static ProgressPanelBase Instance { get; private set; }

        public override string Name => "XPRising";
        public override int MinWidth => 600;
        public override int MinHeight => Math.Max(bars.Count * 24, 1); // Bar height is 20 + 4 spacing
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 1f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 1f);
        public override Vector2 DefaultPosition => new Vector2(238, -565);
        public static float CurrentPanelWidth => Instance.Rect.rect.width;
        public static float CurrentPanelHeight => Instance.Rect.rect.height;
        public override bool CanDragAndResize => true;

        private static Dictionary<string, ProgressBar> bars = new();

        protected override void ConstructPanelContent()
        {
            Instance = this;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false, true, true, 4);
            
            // ChangeProgress("XP", 9, 0.2f,$"1249/5879");
            // ChangeProgress("mastery", 20, 40, "1/2");
            // ChangeProgress("bloodline", 46,80, "blah");
            // ChangeProgress("test", 84, 100, "test");
            
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
            
            progressBar.SetProgress(progress, $"{level:D2}", $"{tooltip} ({progress:P}) {label}");
        }

        private ProgressBar AddBar(string label)
        {
            Plugin.Log(LogLevel.Warning, $"adding bar {label}");
            var progressBar = new ProgressBar(ContentRoot.gameObject, new Color(0.5f, 0.8f, 0.1f));
            bars.Add(label, progressBar);
            Instance.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bars.Count * 24);
            return progressBar;
        }

        protected override void OnClosePanelClicked()
        {
            // Do nothing for now
        }

        private class ProgressBar
        {
            private readonly LayoutElement _layoutBackground;
            private readonly LayoutElement _layoutFilled;
            private readonly Text _expTxt;
            private readonly Text _levelTxt;

            public ProgressBar(GameObject panel, Color colour)
            {
                // This is the base panel for the bar
                var contentBase = UIFactory.CreateHorizontalGroup(panel, "ProgressBarBase", true, true, true, true, 0, default, new Color(0.1f, 0.1f, 0.1f));
                UIFactory.SetLayoutElement(contentBase, minWidth: 580, minHeight: 20, flexibleWidth: 0, flexibleHeight: 0, preferredHeight: 20);
        
                // Split the base bar panel into LevelTxt, progressBar and ExpText
                _levelTxt = UIFactory.CreateLabel(contentBase, "levelTxt", $"00", TextAnchor.MiddleCenter);
                UIFactory.SetLayoutElement(_levelTxt.gameObject, minWidth: 30, minHeight: 20, preferredHeight: 20, preferredWidth: 30);
                var contentLoader = UIFactory.CreateHorizontalGroup(contentBase, "ProgressBarSection", false, true, true, true, 0, default, Color.black);
                UIFactory.SetLayoutElement(contentLoader, minWidth: 350, minHeight: 20, flexibleWidth: 10000, flexibleHeight: 0, preferredHeight: 20, preferredWidth: 350);
        
                _expTxt = UIFactory.CreateLabel(contentBase, "exp", $"", TextAnchor.MiddleRight);
                UIFactory.SetLayoutElement(_expTxt.gameObject, minWidth: 200, minHeight: 20, flexibleWidth: 0, flexibleHeight: 0, preferredHeight: 20, preferredWidth: 200);
                
                var progressFilled = UIFactory.CreateUIObject("ProgressFilled", contentLoader);
                var filledImage = progressFilled.AddComponent<Image>();
                filledImage.color = colour;
                _layoutFilled = UIFactory.SetLayoutElement(progressFilled, minWidth: 0, flexibleWidth: 1);
                var progressBackground = UIFactory.CreateUIObject("ProgressBackground", contentLoader);
                var backgroundImage = progressBackground.AddComponent<Image>();
                backgroundImage.color = Color.black;
                _layoutBackground = UIFactory.SetLayoutElement(progressBackground, minWidth: 0, flexibleWidth: 1);
            }
            
            public void SetProgress(float progress, string level, string tooltip)
            {
                _layoutBackground.flexibleWidth = 1.0f - progress;
                _layoutFilled.flexibleWidth = progress;
                _levelTxt.text = level;
                _expTxt.text = tooltip;
            }
        }
    }
}
