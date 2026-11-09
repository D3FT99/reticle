using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Reticle.Engine;
using static Reticle.Platform.NativeMethods;

namespace Reticle.Views;

/// <summary>Click-through ring drawn around the pointer while dwelling. Positioned in physical pixels with SetWindowPos.</summary>
/// <remarks>
/// The WPF size is <see cref="IDwellSettings.TargetSize"/> DIPs, which WPF scales to each monitor's DPI on its own.
/// <c>SetWindowPos</c> sizes the window with the DPI of the monitor under the pointer, so both agree on every monitor
/// and the ring stays centred on the pointer.
/// </remarks>
public partial class DwellOverlayWindow : Window, IDwellFeedback
{
    private readonly IDwellSettings _settings;
    private readonly IDisplayScale _displayScale;
    private IntPtr _hwnd;
    private bool _shown;
    private int _appliedSize;

    public DwellOverlayWindow(IDwellSettings settings, IDisplayScale displayScale)
    {
        _settings = settings;
        _displayScale = displayScale;
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        // Transparent to the mouse, invisible to Alt+Tab and never activated.
        SetWindowLong(_hwnd, GWL_EXSTYLE,
            GetWindowLong(_hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    /// <summary>Creates the window handle without showing anything.</summary>
    public void Initialise()
    {
        ApplySize();
        Show();
        Visibility = Visibility.Hidden;
    }

    public void Show(Point centre, double progress, bool dragging)
    {
        Ring.Progress = progress;
        Ring.Track = dragging ? (Brush)FindResource("DragTrackBrush") : null;
        if (_appliedSize != _settings.TargetSize)
        {
            ApplySize();
        }
        int size = (int)Math.Round(_settings.TargetSize * _displayScale.ScaleAt(centre));
        SetWindowPos(_hwnd, HWND_TOPMOST, (int)centre.X - size / 2, (int)centre.Y - size / 2, size, size, SWP_NOACTIVATE);
        if (!_shown)
        {
            Visibility = Visibility.Visible;
            _shown = true;
        }
    }

    void IDwellFeedback.Hide()
    {
        if (_shown)
        {
            Ring.Progress = 0;
            Visibility = Visibility.Hidden;
            _shown = false;
        }
    }

    private void ApplySize()
    {
        Width = _settings.TargetSize;
        Height = _settings.TargetSize;
        // Thickness follows the radius so the ring reads the same at every target size.
        Ring.Thickness = Math.Clamp(_settings.TargetSize * 0.11, 3, 9);
        _appliedSize = _settings.TargetSize;
    }
}
