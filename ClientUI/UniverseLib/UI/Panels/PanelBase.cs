﻿using System.Collections;
using BepInEx.Logging;
using ClientUI.UI.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ButtonRef = ClientUI.UniverseLib.UI.Models.ButtonRef;
using UIBehaviourModel = ClientUI.UniverseLib.UI.Models.UIBehaviourModel;

namespace ClientUI.UniverseLib.UI.Panels;

public abstract class PanelBase : UIBehaviourModel
{
    public UIBase Owner { get; }

    public abstract string Name { get; }

    public abstract int MinWidth { get; }
    public abstract int MinHeight { get; }
    public abstract Vector2 DefaultAnchorMin { get; }
    public abstract Vector2 DefaultAnchorMax { get; }
    public virtual Vector2 DefaultPosition { get; }

    public virtual bool CanDragAndResize => true;
    public PanelDragger Dragger { get; internal set; }

    public override GameObject UIRoot => uiRoot;
    protected GameObject uiRoot;
    public RectTransform Rect { get; private set; }
    public GameObject ContentRoot { get; protected set; }

    public GameObject TitleBar { get; private set; }

    public PanelBase(UIBase owner)
    {
        Owner = owner;

        ConstructUI();

        // Add to owner
        Owner.Panels.AddPanel(this);
    }

    public override void Destroy()
    {
        Owner.Panels.RemovePanel(this);
        base.Destroy();
    }

    public virtual void OnFinishResize()
    {
    }

    public virtual void OnFinishDrag()
    {
    }

    public override void SetActive(bool active)
    {
        if (Enabled != active)
            base.SetActive(active);

        if (!active)
        {
            Dragger.WasDragging = false;
        }
        else
        {
            UIRoot.transform.SetAsLastSibling();
            Owner.Panels.InvokeOnPanelsReordered();
        }
    }
        
    protected virtual void OnClosePanelClicked()
    {
        SetActive(false);
    }

    // Setting size and position

    public virtual void SetDefaultSizeAndPosition()
    {
        Rect.localPosition = DefaultPosition;
        Rect.pivot = new Vector2(0f, 1f);

        Rect.anchorMin = DefaultAnchorMin;
        Rect.anchorMax = DefaultAnchorMax;

        LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);

        EnsureValidPosition();
        EnsureValidSize();

        Dragger.OnEndResize();
    }

    public virtual void EnsureValidSize()
    {
        if (Rect.rect.width < MinWidth)
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);

        if (Rect.rect.height < MinHeight)
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);

        Dragger.OnEndResize();
    }

    public virtual void EnsureValidPosition()
    {
        // Prevent panel going oustide screen bounds

        Vector3 pos = Rect.localPosition;
        Vector2 dimensions = Owner.Panels.ScreenDimensions;

        float halfW = dimensions.x * 0.5f;
        float halfH = dimensions.y * 0.5f;

        pos.x = Math.Max(-halfW - Rect.rect.width + 50, Math.Min(pos.x, halfW - 50));
        pos.y = Math.Max(-halfH + 50, Math.Min(pos.y, halfH));

        Rect.localPosition = pos;
    }

    // UI Construction

    protected abstract void ConstructPanelContent();

    protected virtual PanelDragger CreatePanelDragger() => new(this);

    public virtual void ConstructUI()
    {
        // create core canvas 
        uiRoot = UIFactory.CreatePanel(Name, Owner.Panels.PanelHolder, out GameObject contentRoot);
        ContentRoot = contentRoot;
        Rect = uiRoot.GetComponent<RectTransform>();

        UIFactory.SetLayoutElement(ContentRoot, 0, 0, flexibleWidth: 9999, flexibleHeight: 9999);

        // Title bar
        TitleBar = UIFactory.CreateHorizontalGroup(ContentRoot, "TitleBar", false, true, true, true, 2,
            new Vector4(2, 2, 2, 2), Colour.DarkBackground);
        UIFactory.SetLayoutElement(TitleBar, minHeight: 25, flexibleHeight: 0);

        // Title text

        TextMeshProUGUI titleTxt = UIFactory.CreateLabel(TitleBar, "TitleBar", Name, TextAlignmentOptions.MidlineLeft);
        UIFactory.SetLayoutElement(titleTxt.gameObject, 50, 25, 9999, 0);

        // close button

        GameObject closeHolder = UIFactory.CreateUIObject("CloseHolder", TitleBar);
        UIFactory.SetLayoutElement(closeHolder, minHeight: 25, flexibleHeight: 0, minWidth: 30, flexibleWidth: 9999);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(closeHolder, false, false, true, true, 3, childAlignment: TextAnchor.MiddleRight);
        ButtonRef closeBtn = UIFactory.CreateButton(closeHolder, "CloseButton", "—");
        UIFactory.SetLayoutElement(closeBtn.Component.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0);
        closeBtn.Component.colors = new ColorBlock()
        {
            normalColor = Colour.SliderHandle,
            colorMultiplier = 1
        };

        closeBtn.OnClick += () =>
        {
            OnClosePanelClicked();
        };

        if (!CanDragAndResize) TitleBar.SetActive(false);

        // Panel dragger

        Dragger = CreatePanelDragger();
        Dragger.OnFinishResize += OnFinishResize;
        Dragger.OnFinishDrag += OnFinishDrag;

        // content (abstract)

        ConstructPanelContent();
        SetDefaultSizeAndPosition();

        CoroutineUtility.StartCoroutine(LateSetupCoroutine());
    }

    private IEnumerator LateSetupCoroutine()
    {
        yield return null;

        LateConstructUI();
    }

    protected virtual void LateConstructUI()
    {
        SetDefaultSizeAndPosition();
    }
}