using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Reticle.Core;
using Reticle.Platform;
using Reticle.ViewModels;
using static Reticle.Platform.NativeMethods;

namespace Reticle.Views;

/// <summary>The floating click-type toolbar, docked to one of eight positions on the primary screen.</summary>
public partial class MainWindow : Window
{
    /// <summary>Gap between the visible panel and the screen edge, in DIPs.</summary>
    private const double EdgeGap = 8;

    private readonly MainViewModel _viewModel;
    private readonly Func<bool> _confirmExit;

    public MainWindow(MainViewModel viewModel, Func<bool> confirmExit)
    {
        _viewModel = viewModel;
        _confirmExit = confirmExit;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.PropertyChanged += OnViewModelChanged;
        SizeChanged += (_, _) => Dock();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    /// <summary>Set when the session is ending or the user already confirmed exit.</summary>
    public bool SuppressExitConfirmation { get; set; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        // Never take focus from the application the user is working in.
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!SuppressExitConfirmation && !_confirmExit())
        {
            e.Cancel = true;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        base.OnClosed(e);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Position))
        {
            Dock();
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(Dock);

    private void Dock()
    {
        if (!IsLoaded && ActualWidth == 0)
        {
            return;
        }
        Rect area = SystemParameters.WorkArea;
        Thickness shadow = ((FrameworkElement)Content).Margin;
        ToolbarPosition position = _viewModel.Position;
        Left = position.X switch
        {
            HorizontalPosition.Left => area.Left + EdgeGap - shadow.Left,
            HorizontalPosition.Right => area.Right - ActualWidth - EdgeGap + shadow.Right,
            _ => Math.Round(area.Left + (area.Width - ActualWidth) / 2),
        };
        Top = position.Y switch
        {
            VerticalPosition.Top => area.Top + EdgeGap - shadow.Top,
            VerticalPosition.Bottom => area.Bottom - ActualHeight - EdgeGap + shadow.Bottom,
            _ => Math.Round(area.Top + (area.Height - ActualHeight) / 2),
        };
    }
}
