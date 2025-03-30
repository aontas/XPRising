using TMPro;
using UnityEngine;
using UIBase = ClientUI.UniverseLib.UI.UIBase;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class TextPanel : ResizeablePanelBase
{
    public override string Name => "ClientUIText";
    public override int MinWidth => 340;
    public override int MinHeight => 75;
    public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
    public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
    public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
    public override bool ResizeWholePanel => false;

    // Set the default position to the top center of the screen
    public override Vector2 DefaultPosition => new Vector2(0, Owner.Scaler.m_ReferenceResolution.y);

    private TextMeshProUGUI _text;
    
    public TextPanel(UIBase owner) : base(owner)
    {
    }

    protected override UIManager.Panels PanelType => UIManager.Panels.Base;

    protected override void ConstructPanelContent()
    {
        var scrollView = UIFactory.CreateScrollView(ContentRoot, "TextScroll", out var scrollContent, out var autoScrollbar);

        _text = UIFactory.CreateLabel(scrollContent, "Text", "", TextAlignmentOptions.Left);
        _text.margin = new Vector4(15, 15, 15, 15);
        
        SetDefaultSizeAndPosition();
    }
    
    protected override void LateConstructUI()
    {
        base.LateConstructUI();
    }

    public override void Update()
    {
        base.Update();
    }

    internal override void Reset()
    {
        SetTitle("");
        _text.SetText("");
    }
    
    protected override void OnClosePanelClicked()
    {
        SetActive(false);
    }

    internal void SetText(string title, string text)
    {
        SetActive(true);
        SetTitle(title);
        _text.SetText(text);
    }

    internal void AddText(string text)
    {
        _text.SetText(_text.text + "\n" + text);
    }
}