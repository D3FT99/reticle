using System.Windows;
using Reticle.Engine;
using static Reticle.Platform.NativeMethods;

namespace Reticle.Platform;

/// <summary>Effective DPI of the monitor under a physical screen point (or the nearest monitor).</summary>
internal sealed class MonitorDisplayScale : IDisplayScale
{
    public static readonly MonitorDisplayScale Instance = new();

    private const double DefaultDpi = 96;

    public double ScaleAt(Point physicalPoint)
    {
        var point = new POINT { X = (int)Math.Round(physicalPoint.X), Y = (int)Math.Round(physicalPoint.Y) };
        IntPtr monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
        return GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 ? dpiX / DefaultDpi : 1.0;
    }
}
