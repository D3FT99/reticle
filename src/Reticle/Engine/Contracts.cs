using System.Windows;

namespace Reticle.Engine;

public enum ClickType
{
    None,
    LeftClick,
    RightClick,
    DoubleLeftClick,
    Drag,
}

public enum DwellState
{
    PointerIsMoving,
    /// <summary>The pointer has rested for half the pre-dwell; the target square is fixed where it settled.</summary>
    Settling,
    Dwelling,
}

/// <summary>Logical mouse buttons: <see cref="Left"/> is the user's primary button, even when buttons are swapped.</summary>
public enum MouseButton
{
    Left,
    Right,
}

/// <summary>Live dwell settings; read on every tick so changes apply immediately.</summary>
public interface IDwellSettings
{
    /// <summary>Milliseconds the pointer must rest before a click, after the pre-dwell.</summary>
    int DwellTime { get; }

    /// <summary>Side, in DIPs (effective pixels), of the square the pointer may wander in while dwelling.</summary>
    int TargetSize { get; }
}

/// <summary>DPI scale of the monitors, so DIP sizes look the same on every monitor.</summary>
public interface IDisplayScale
{
    /// <summary>Physical pixels per DIP (1.0 at 96 DPI) on the monitor containing <paramref name="physicalPoint"/>.</summary>
    double ScaleAt(Point physicalPoint);
}

public interface IClock
{
    /// <summary>Monotonic milliseconds (wraps like <see cref="Environment.TickCount"/>).</summary>
    int TickCount { get; }
}

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    public int TickCount => Environment.TickCount;
}

public interface IClickSound
{
    void Play();
}

/// <summary>Synthesised clicks, sent at the current pointer position.</summary>
public interface IMouse
{
    void Click(MouseButton button);

    void DoubleClick(MouseButton button);

    void Press(MouseButton button);

    void Release(MouseButton button);
}

/// <summary>Visual feedback for screen dwelling (the ring that follows the pointer).</summary>
public interface IDwellFeedback
{
    /// <param name="centre">Physical screen pixels.</param>
    /// <param name="progress">0..1 of the dwell.</param>
    /// <param name="dragging">True while a drag is held: the ring shows its track.</param>
    void Show(Point centre, double progress, bool dragging);

    void Hide();
}

internal static class DwellTimings
{
    /// <summary>Fraction of the pre-dwell after which the target square is re-centred where the pointer settled.</summary>
    public const double PreDwellSettleFraction = 0.5;

    public static int PreDwellTime(IDwellSettings settings) => settings.DwellTime / 10;
}
