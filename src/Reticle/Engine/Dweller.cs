using System.Windows;

namespace Reticle.Engine;

/// <summary>
/// Turns a resting pointer into an action. One instance per dwell target (each toolbar button, plus the screen).
/// </summary>
/// <remarks>
/// States: <see cref="DwellState.PointerIsMoving"/> → (still for half the pre-dwell) <see cref="DwellState.Settling"/>
/// → (pre-dwell elapsed) <see cref="DwellState.Dwelling"/> → (100 %) action. Leaving the target restarts the cycle;
/// after an action the dweller stays idle until the pointer leaves the target.
/// </remarks>
public abstract class Dweller : IDisposable
{
    private readonly IClock _clock;
    private readonly IClickSound _sound;
    private Point? _initialMousePosition;
    private bool _mouseHasMoved;
    private int _dwellStartTime;
    private int _dwellDuration;
    private bool _actionCompleted;

    protected Dweller(IDwellSettings settings, IClock clock, IClickSound sound)
    {
        Settings = settings;
        _clock = clock;
        _sound = sound;
        _dwellStartTime = clock.TickCount;
    }

    protected IDwellSettings Settings { get; }

    public DwellState DwellState { get; private set; } = DwellState.PointerIsMoving;

    protected Rect Target { get; private set; }

    protected virtual bool AllowMultipleClicks => false;

    private int PreDwellTime => DwellTimings.PreDwellTime(Settings);

    private double PreDwellProgress => PreDwellTime == 0 ? 1 : (double)_dwellDuration / PreDwellTime;

    /// <summary>Progress of the dwell proper (excludes the pre-dwell); negative during the pre-dwell.</summary>
    protected double DwellProgress => (double)(_dwellDuration - PreDwellTime) / Settings.DwellTime;

    public virtual bool TargetContainsMouse(Point mousePosition) => Target.Contains(mousePosition);

    public void Update(Point mousePosition)
    {
        if (!IsActive(mousePosition))
        {
            _dwellStartTime = _clock.TickCount;
            DwellState = DwellState.PointerIsMoving;
            HideProgress();
            return;
        }

        UpdateActive(mousePosition);

        // Never act on a pointer that has not moved since the dweller appeared.
        _initialMousePosition ??= mousePosition;
        if (!_mouseHasMoved)
        {
            if (_initialMousePosition.Value == mousePosition)
            {
                return;
            }
            _mouseHasMoved = true;
        }

        bool inTarget = TargetContainsMouse(mousePosition);
        if (inTarget && _actionCompleted)
        {
            return;
        }
        if (!inTarget)
        {
            _actionCompleted = false;
            ResetTarget(mousePosition);
            return;
        }

        _dwellDuration = unchecked(_clock.TickCount - _dwellStartTime);
        switch (DwellState)
        {
            case DwellState.PointerIsMoving:
                if (PreDwellProgress >= DwellTimings.PreDwellSettleFraction)
                {
                    Target = GetTarget(mousePosition);
                    DwellState = DwellState.Settling;
                }
                break;

            case DwellState.Settling:
                if (PreDwellProgress >= 1)
                {
                    DwellState = DwellState.Dwelling;
                }
                break;

            case DwellState.Dwelling:
                UpdateProgress(mousePosition);
                if (DwellProgress >= 1)
                {
                    _sound.Play();
                    HideProgress();
                    Click(mousePosition);
                    ResetTarget(mousePosition);
                    if (!AllowMultipleClicks)
                    {
                        _actionCompleted = true;
                    }
                }
                break;
        }
    }

    protected virtual bool IsActive(Point mousePosition) => true;

    protected virtual void UpdateActive(Point mousePosition)
    {
    }

    protected virtual void UpdateProgress(Point mousePosition)
    {
    }

    protected virtual void HideProgress()
    {
    }

    protected abstract Rect GetTarget(Point cursorPosition);

    protected abstract void Click(Point mousePosition);

    /// <summary>The action ran by other means (e.g. a physical click); wait for the pointer to leave before dwelling again.</summary>
    protected void ActionCompleted()
    {
        _actionCompleted = true;
        ResetTarget(default);
    }

    private void ResetTarget(Point cursorPosition)
    {
        _dwellStartTime = _clock.TickCount;
        _dwellDuration = 0;
        Target = GetTarget(cursorPosition);
        DwellState = DwellState.PointerIsMoving;
        UpdateProgress(cursorPosition);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}
