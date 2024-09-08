#nullable enable

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
        if (!AllowDragAndResize) return;
        
        Vector3 resizePos = Rect.InverseTransformPoint(rawMousePos);
        ResizeTypes type = GetResizeType(resizePos);
        bool inResizePos = type != ResizeTypes.None;

        Vector3 dragPos = DraggableArea.InverseTransformPoint(rawMousePos);
        bool inDragPos = DraggableArea.rect.Contains(dragPos);
        
        if (state.HasFlag(MouseState.ButtonState.Clicked))
        {
            if (inDragPos || inResizePos)
                UIPanel.SetActive(true);

            // Resize with priority as actually shows an icon change (maybe show an icon for drag as well?)
            if (inResizePos)
            {
                OnBeginResize(type);
            }
            else if (inDragPos)
            {
                OnBeginDrag();
            }
        }
        else if (state.HasFlag(MouseState.ButtonState.Down))
        {
            if (WasDragging)
            {
                OnDrag();
            }
            else if (WasResizing)
            {
                OnResize();
            }
        }
        else if (state.HasFlag(MouseState.ButtonState.Released))
        {
            if (WasDragging)
            {
                OnEndDrag();
            }
            else if (WasResizing)
            {
                OnEndResize();
            }
            
            if (WasHoveringResize)
            {
                if (inResizePos)
                {
                    OnHoverResize(type);
                }
                else
                {
                    OnHoverResizeEnd();
                }
            }
        }
        else // mouse moving when not clicked
        {
            if (inResizePos)
            {
                OnHoverResize(type);
            }
            else if (!WasResizing)
            {
                OnHoverResizeEnd();
            }
        }
        
        if (WasHoveringResize && PanelManager.resizeCursor)
            UpdateHoverImagePos();
        
        PanelManager.draggerHandledThisFrame = true;
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
        var mousePos = (Vector2)UIPanel.Owner.Panels.MousePosition;

        var diff = mousePos - _lastDragPosition;
        _lastDragPosition = mousePos;

        Rect.anchoredPosition += diff / UIPanel.Owner.Canvas.scaleFactor;

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

        var mask = ResizeTypes.None;

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

        var newPivot = new Vector2(
            _currentResizeType.HasFlag(ResizeTypes.Left) ? 1 : 0,
            _currentResizeType.HasFlag(ResizeTypes.Bottom) ? 1 : 0
        );

        Rect.SetPivot(newPivot);
    }

    public virtual void OnResize()
    {
        Vector3 mousePos = UIPanel.Owner.Panels.MousePosition;
        Vector2 diff = (_lastResizePos - (Vector2)mousePos) / UIPanel.Owner.Canvas.scaleFactor;

        if ((Vector2)mousePos == _lastResizePos)
            return;

        Vector2 screenDimensions = UIPanel.Owner.Panels.ScreenDimensions;

        if (mousePos.x < 0 || mousePos.y < 0 || mousePos.x > screenDimensions.x || mousePos.y > screenDimensions.y)
            return;

        _lastResizePos = mousePos;

        var width = Rect.rect.width;
        var height = Rect.rect.height;
        if (_currentResizeType.HasFlag(ResizeTypes.Left))
        {
            width = Math.Max(width + diff.x, UIPanel.MinWidth);
        }
        else if (_currentResizeType.HasFlag(ResizeTypes.Right))
        {
            width = Math.Max(width - diff.x, UIPanel.MinWidth);
        }

        if (_currentResizeType.HasFlag(ResizeTypes.Top))
        {
            height = Math.Max(height - diff.y, UIPanel.MinHeight);
        }
        else if (_currentResizeType.HasFlag(ResizeTypes.Bottom))
        {
            height = Math.Max(height + diff.y, UIPanel.MinHeight);
        }

        Rect.sizeDelta = new Vector2(width, height);
    }

    public virtual void OnEndResize()
    {
        WasResizing = false;
        PanelManager.Resizing = false;
        try { OnHoverResizeEnd(); } catch { }

        Rect.SetPivot(new Vector2(0.5f, 0.5f));
        UpdateResizeCache();
        OnFinishResize?.Invoke();
    }

    #endregion
}