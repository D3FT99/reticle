using System.Windows;
using Reticle.Core;
using Reticle.Engine;

namespace Reticle.Tests;

/// <summary>Drives the dwell engine with a fake clock and records the injected mouse input.</summary>
internal sealed class Harness
{
    public const int Frame = 16;

    public Harness(int dwellTime = 1000, int targetSize = 60)
    {
        Settings = AppSettings.InMemory();
        Settings.DwellTime = dwellTime;
        Settings.TargetSize = targetSize;
        Engine = new DwellEngine(Settings, Clock, new SilentSound(), Mouse, Feedback, new FixedDisplayScale(this));
    }

    public AppSettings Settings { get; }

    public FakeClock Clock { get; } = new();

    public RecordingMouse Mouse { get; } = new();

    public RecordingFeedback Feedback { get; } = new();

    public DwellEngine Engine { get; }

    /// <summary>DPI scale of every monitor (1.0 = 100 %).</summary>
    public double DisplayScale { get; set; } = 1.0;

    /// <summary>Holds the pointer at <paramref name="point"/> for <paramref name="milliseconds"/>, ticking every frame.</summary>
    public void Hold(Point point, int milliseconds)
    {
        Mouse.Cursor = point;
        for (int elapsed = 0; elapsed <= milliseconds; elapsed += Frame)
        {
            Engine.Update(point);
            Clock.TickCount += Frame;
        }
    }

    /// <summary>A dweller ignores the pointer until it has moved once; this performs that first move.</summary>
    public void Wake(Point start, Point destination)
    {
        Hold(start, 0);
        Hold(destination, 0);
    }

    public sealed class FakeClock : IClock
    {
        public int TickCount { get; set; } = 1_000;
    }

    private sealed class FixedDisplayScale(Harness harness) : IDisplayScale
    {
        public double ScaleAt(Point physicalPoint) => harness.DisplayScale;
    }

    private sealed class SilentSound : IClickSound
    {
        public void Play()
        {
        }
    }
}

internal sealed class RecordingMouse : IMouse
{
    public Point Cursor { get; set; }

    public List<string> Events { get; } = [];

    public void Click(MouseButton button) => Events.Add($"click {button} @{Cursor.X},{Cursor.Y}");

    public void DoubleClick(MouseButton button) => Events.Add($"double {button} @{Cursor.X},{Cursor.Y}");

    public void Press(MouseButton button) => Events.Add($"press {button} @{Cursor.X},{Cursor.Y}");

    public void Release(MouseButton button) => Events.Add($"release {button} @{Cursor.X},{Cursor.Y}");
}

internal sealed class RecordingFeedback : IDwellFeedback
{
    public double? LastProgress { get; private set; }

    public Point? LastCentre { get; private set; }

    public void Show(Point centre, double progress, bool dragging)
    {
        LastProgress = progress;
        LastCentre = centre;
    }

    public void Hide()
    {
        LastProgress = null;
        LastCentre = null;
    }
}
