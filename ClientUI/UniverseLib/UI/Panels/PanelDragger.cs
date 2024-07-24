#nullable enable

using BepInEx.Logging;
using ClientUI.UI;
using UnityEngine;

namespace ClientUI.UniverseLib.UI.Panels;

public class PanelDragger
{
    // Static

    private const int ResizeThickness = 10;

    // Instance

    public PanelBase UIPanel { get; private set; }
    public bool AllowDragAndResize => UIPanel.CanDragAndResize;

    public RectTransform Rect { get; set; }
    public event Action? OnFinishResize;
    public event Action? OnFinishDrag;

    // Dragging
    public RectTransform DraggableArea { get; set; }
    public bool WasDragging { get; set; }
    private Vector2 _lastDragPosition;

    // Resizing
    public bool WasResizing { get; internal set; }
    private bool WasHoveringResize =>
        PanelManager.resizeCursor != null &&
        PanelManager.resizeCursor.activeInHierarchy;

    private ResizeTypes _currentResizeType = ResizeTypes.None;
    private Vector2 _lastResizePos;
    private ResizeTypes _lastResizeHoverType;
    private Rect _totalResizeRect;

    public PanelDragger(PanelBase uiPanel)
    {
        UIPanel = uiPanel;
        DraggableArea = uiPanel.TitleBar.GetComponent<RectTransform>();
        Rect = uiPanel.Rect;

        UpdateResizeCache();
    }

    protected internal virtual void Update(MouseState.ButtonState state, Vector3 rawMousePos)
    {
        ResizeTypes type;
        Vector3 resizePos = Rect.InverseTransformPoint(rawMousePos);
        bool inResizePos = MouseInResizeArea(resizePos);

        Vector3 dragPos = DraggableArea.InverseTransformPoint(rawMousePos);
        bool inDragPos = DraggableArea.rect.Contains(dragPos);

        if (WasHoveringResize && PanelManager.resizeCursor)
            UpdateHoverImagePos();

        if (state.HasFlag(MouseState.ButtonState.Clicked))
        {
            if (inDragPos || inResizePos)
                UIPanel.SetActive(true);

            if (inDragPos)
            {
                if (AllowDragAndResize)
                    OnBeginDrag();
                PanelManager.draggerHandledThisFrame = true;
                return;
            }

            if (inResizePos)
            {
                type = GetResizeType(resizePos);
                if (type != ResizeTypes.None)
                    OnBeginResize(type);

                PanelManager.draggerHandledThisFrame = true;
            }
        } else if (state.HasFlag(MouseState.ButtonState.Down))
        {
            if (WasDragging)
            {
                OnDrag();
                PanelManager.draggerHandledThisFrame = true;
            }
            else if (WasResizing)
            {
                OnResize();
                PanelManager.draggerHandledThisFrame = true;
            }
        }
        else
        {
            if (AllowDragAndResize && inDragPos)
            {
                if (WasDragging)
                    OnEndDrag();

                if (WasHoveringResize)
                    OnHoverResizeEnd();

                PanelManager.draggerHandledThisFrame = true;
            }
            else if (inResizePos || WasResizing)
            {
                if (WasResizing)
                    OnEndResize();

                type = GetResizeType(resizePos);
                if (type != ResizeTypes.None)
                    OnHoverResize(type);
                else if (WasHoveringResize)
                    OnHoverResizeEnd();

                PanelManager.draggerHandledThisFrame = true;
            }
            else if (WasHoveringResize)
                OnHoverResizeEnd();
        }
    }

    #region DRAGGING

    public virtual void OnBeginDrag()
    {
        PanelManager.wasAnyDragging = true;
        WasDragging = true;
        _lastDragPosition = UIPanel.Owner.Panels.MousePosition;
    }

    public virtual void OnDrag()
    {
        Vector3 mousePos = UIPanel.Owner.Panels.MousePosition;

        Vector2 diff = (Vector2)mousePos - _lastDragPosition;
        _lastDragPosition = mousePos;

        Rect.localPosition = Rect.localPosition + (Vector3)diff;

        UIPanel.EnsureValidPosition();
    }

    public virtual void OnEndDrag()
    {
        WasDragging = false;

        OnFinishDrag?.Invoke();
    }

    #endregion

    #region RESIZE

    private readonly Dictionary<ResizeTypes, Rect> _resizeMask = new()
    {
        { ResizeTypes.Top, default },
        { ResizeTypes.Left, default },
        { ResizeTypes.Right, default },
        { ResizeTypes.Bottom, default },
    };

    [Flags]
    public enum ResizeTypes : ulong
    {
        None = 0,
        Top = 1,
        Left = 2,
        Right = 4,
        Bottom = 8,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right,
    }

    private const int DoubleThickness = ResizeThickness * 2;

    private void UpdateResizeCache()
    {
        _totalResizeRect = new Rect(Rect.rect.x - ResizeThickness + 1,
            Rect.rect.y - ResizeThickness + 1,
            Rect.rect.width + DoubleThickness - 2,
            Rect.rect.height + DoubleThickness - 2);

        // calculate the four cross sections to use as flags
        if (AllowDragAndResize)
        {
            _resizeMask[ResizeTypes.Bottom] = new Rect(
                _totalResizeRect.x,
                _totalResizeRect.y,
                _totalResizeRect.width,
                ResizeThickness);

            _resizeMask[ResizeTypes.Left] = new Rect(
                _totalResizeRect.x,
                _totalResizeRect.y,
                ResizeThickness,
                _totalResizeRect.height);

            _resizeMask[ResizeTypes.Top] = new Rect(
                _totalResizeRect.x,
                Rect.rect.y + Rect.rect.height - 2,
                _totalResizeRect.width,
                ResizeThickness);

            _resizeMask[ResizeTypes.Right] = new Rect(
                _totalResizeRect.x + Rect.rect.width + ResizeThickness - 2,
                _totalResizeRect.y,
                ResizeThickness,
                _totalResizeRect.height);
        }
    }

    protected virtual bool MouseInResizeArea(Vector2 mousePos)
    {
        return _totalResizeRect.Contains(mousePos);
    }

    private ResizeTypes GetResizeType(Vector2 mousePos)
    {
        // Calculate which part of the resize area we're in, if any.

        ResizeTypes mask = 0;

        if (_resizeMask[ResizeTypes.Top].Contains(mousePos))
            mask |= ResizeTypes.Top;
        else if (_resizeMask[ResizeTypes.Bottom].Contains(mousePos))
            mask |= ResizeTypes.Bottom;

        if (_resizeMask[ResizeTypes.Left].Contains(mousePos))
            mask |= ResizeTypes.Left;
        else if (_resizeMask[ResizeTypes.Right].Contains(mousePos))
            mask |= ResizeTypes.Right;

        return mask;
    }

    public virtual void OnHoverResize(ResizeTypes resizeType)
    {
        if (WasHoveringResize && _lastResizeHoverType == resizeType)
            return;

        // we are entering resize, or the resize type has changed.

        _lastResizeHoverType = resizeType;

        if (PanelManager.resizeCursorUIBase != null)
            PanelManager.resizeCursorUIBase.Enabled = true;
        if (PanelManager.resizeCursor == null)
            return;

        PanelManager.resizeCursor.SetActive(true);

        // set the rotation for the resize icon
        float iconRotation = 0f;
        switch (resizeType)
        {
            case ResizeTypes.TopRight:
            case ResizeTypes.BottomLeft:
                iconRotation = 45f; break;
            case ResizeTypes.Top:
            case ResizeTypes.Bottom:
                iconRotation = 90f; break;
            case ResizeTypes.TopLeft:
            case ResizeTypes.BottomRight:
                iconRotation = 135f; break;
        }

        Quaternion rot = PanelManager.resizeCursor.transform.rotation;
        rot.eulerAngles = new Vector3(0, 0, iconRotation);
        PanelManager.resizeCursor.transform.rotation = rot;

        UpdateHoverImagePos();
    }

    // update the resize icon position to be above the mouse
    private void UpdateHoverImagePos()
    {
        Vector3 mousePos = UIPanel.Owner.Panels.MousePosition;
        RectTransform rect = UIPanel.Owner.RootRect;
        if (PanelManager.resizeCursorUIBase != null)
            PanelManager.resizeCursorUIBase.SetOnTop();

        if (PanelManager.resizeCursor != null)
            PanelManager.resizeCursor.transform.localPosition = rect.InverseTransformPoint(mousePos);
    }

    public virtual void OnHoverResizeEnd()
    {
        if(PanelManager.resizeCursorUIBase != null)
            PanelManager.resizeCursorUIBase.Enabled = false;
        if (PanelManager.resizeCursor != null)
            PanelManager.resizeCursor.SetActive(false);
    }

    public virtual void OnBeginResize(ResizeTypes resizeType)
    {
        _currentResizeType = resizeType;
        _lastResizePos = UIPanel.Owner.Panels.MousePosition;
        WasResizing = true;
        PanelManager.Resizing = true;
    }

    public virtual void OnResize()
    {
        Vector3 mousePos = UIPanel.Owner.Panels.MousePosition;
        Vector2 diff = _lastResizePos - (Vector2)mousePos;

        if ((Vector2)mousePos == _lastResizePos)
            return;

        Vector2 screenDimensions = UIPanel.Owner.Panels.ScreenDimensions;

        if (mousePos.x < 0 || mousePos.y < 0 || mousePos.x > screenDimensions.x || mousePos.y > screenDimensions.y)
            return;

        _lastResizePos = mousePos;

        float diffX = (float)((decimal)diff.x / (decimal)screenDimensions.x);
        float diffY = (float)((decimal)diff.y / (decimal)screenDimensions.y);

        Vector2 anchorMin = Rect.anchorMin;
        Vector2 anchorMax = Rect.anchorMax;

        if (_currentResizeType.HasFlag(ResizeTypes.Left))
            anchorMin.x -= diffX;
        else if (_currentResizeType.HasFlag(ResizeTypes.Right))
            anchorMax.x -= diffX;

        if (_currentResizeType.HasFlag(ResizeTypes.Top))
            anchorMax.y -= diffY;
        else if (_currentResizeType.HasFlag(ResizeTypes.Bottom))
            anchorMin.y -= diffY;

        Vector2 prevMin = Rect.anchorMin;
        Vector2 prevMax = Rect.anchorMax;

        Rect.anchorMin = new Vector2(anchorMin.x, anchorMin.y);
        Rect.anchorMax = new Vector2(anchorMax.x, anchorMax.y);

        if (Rect.rect.width < UIPanel.MinWidth)
        {
            Rect.anchorMin = new Vector2(prevMin.x, Rect.anchorMin.y);
            Rect.anchorMax = new Vector2(prevMax.x, Rect.anchorMax.y);
        }
        if (Rect.rect.height < UIPanel.MinHeight)
        {
            Rect.anchorMin = new Vector2(Rect.anchorMin.x, prevMin.y);
            Rect.anchorMax = new Vector2(Rect.anchorMax.x, prevMax.y);
        }
    }

    public virtual void OnEndResize()
    {
        WasResizing = false;
        PanelManager.Resizing = false;
        try { OnHoverResizeEnd(); } catch { }
        UpdateResizeCache();
        OnFinishResize?.Invoke();
    }

    #endregion
}