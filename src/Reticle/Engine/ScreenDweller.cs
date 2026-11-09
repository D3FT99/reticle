using System.Windows;

namespace Reticle.Engine;

/// <summary>Dwells anywhere on screen and performs the selected click type where the pointer is.</summary>
public sealed class ScreenDweller : Dweller
{
    private readonly DwellEngine _engine;
    private readonly IMouse _mouse;
    private readonly IDwellFeedback _feedback;
    private readonly IDisplayScale _displayScale;
    private ClickType _clickType;
    private bool _dragging;

    public ScreenDweller(
        DwellEngine engine,
        IDwellSettings settings,
        IClock clock,
        IClickSound sound,
        IMouse mouse,
        IDwellFeedback feedback,
        IDisplayScale displayScale)
        : base(settings, clock, sound)
    {
        _engine = engine;
        _mouse = mouse;
        _feedback = feedback;
        _displayScale = displayScale;
    }

    public bool IsDragging => _dragging;

    public ClickType ClickType
    {
        get => _clickType;
        set
        {
            _clickType = value;
            FinishDrag();
        }
    }

    protected override bool IsActive(Point mousePosition) =>
        ClickType != ClickType.None && !_engine.IsMouseOverButton(mousePosition);

    protected override Rect GetTarget(Point cursorPosition)
    {
        double side = Settings.TargetSize * _displayScale.ScaleAt(cursorPosition);
        return new Rect(cursorPosition.X - side / 2, cursorPosition.Y - side / 2, side, side);
    }

    protected override void Click(Point mousePosition)
    {
        switch (ClickType)
        {
            case ClickType.LeftClick:
                _mouse.Click(MouseButton.Left);
                break;
            case ClickType.RightClick:
                _mouse.Click(MouseButton.Right);
                _engine.AfterClick();
                break;
            case ClickType.DoubleLeftClick:
                _mouse.DoubleClick(MouseButton.Left);
                _engine.AfterClick();
                break;
            case ClickType.Drag:
                if (!_dragging)
                {
                    _mouse.Press(MouseButton.Left);
                    _dragging = true;
                }
                else
                {
                    FinishDrag();
                    _engine.AfterClick();
                }
                break;
        }
    }

    private void FinishDrag()
    {
        if (_dragging)
        {
            _mouse.Release(MouseButton.Left);
        }
        _dragging = false;
    }

    protected override void UpdateActive(Point mousePosition)
    {
        if (_dragging)
        {
            _feedback.Show(mousePosition, 0, dragging: true);
        }
    }

    protected override void UpdateProgress(Point mousePosition)
    {
        if (DwellState == DwellState.Dwelling)
        {
            _feedback.Show(mousePosition, Math.Max(DwellProgress, 0), _dragging);
        }
        else if (!_dragging)
        {
            _feedback.Hide();
        }
    }

    protected override void HideProgress()
    {
        if (!_dragging)
        {
            _feedback.Hide();
        }
    }

    protected override void Dispose(bool disposing)
    {
        FinishDrag();
        _feedback.Hide();
    }
}
