using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Reticle.Engine;

namespace Reticle.Controls;

public enum DwellButtonState
{
    NotSelected,
    /// <summary>Applies to the next action only.</summary>
    Selected,
    /// <summary>Stays selected after actions.</summary>
    Locked,
}

/// <summary>
/// Toolbar button activated by dwelling (or a physical click). Registers itself with the dwell engine while
/// loaded and reports its physical screen bounds to it.
/// </summary>
public sealed class DwellButton : ContentControl
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(DwellButton));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(DwellButton));

    public static readonly DependencyProperty ButtonStateProperty = DependencyProperty.Register(
        nameof(ButtonState), typeof(DwellButtonState), typeof(DwellButton), new PropertyMetadata(DwellButtonState.NotSelected));

    public static readonly DependencyProperty ShowLockBadgeProperty = DependencyProperty.Register(
        nameof(ShowLockBadge), typeof(bool), typeof(DwellButton), new PropertyMetadata(false));

    public static readonly DependencyProperty DwellProgressProperty = DependencyProperty.Register(
        nameof(DwellProgress), typeof(double), typeof(DwellButton), new PropertyMetadata(0.0));

    /// <summary>Matches the template's chrome: corner radius 14 with a 1 px border.</summary>
    private const double ChromeCornerRadius = 14;
    private const double ChromeBorder = 1;

    private readonly DwellableButton _dwellable;
    private DwellEngine? _engine;
    private Window? _window;
    private FrameworkElement? _fillHost;

    public DwellButton()
    {
        _dwellable = new DwellableButton(ExecuteCommand);
        _dwellable.ActionInvoked += (_, _) => Flash();
        _dwellable.PropertyChanged += OnDwellablePropertyChanged;
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        LayoutUpdated += (_, _) => UpdateBounds();
        IsVisibleChanged += (_, _) => UpdateBounds();
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public DwellButtonState ButtonState
    {
        get => (DwellButtonState)GetValue(ButtonStateProperty);
        set => SetValue(ButtonStateProperty, value);
    }

    /// <summary>Show a padlock when <see cref="ButtonState"/> is <see cref="DwellButtonState.Locked"/>.</summary>
    public bool ShowLockBadge
    {
        get => (bool)GetValue(ShowLockBadgeProperty);
        set => SetValue(ShowLockBadgeProperty, value);
    }

    /// <summary>0..1 while being dwelt on.</summary>
    public double DwellProgress
    {
        get => (double)GetValue(DwellProgressProperty);
        private set => SetValue(DwellProgressProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _fillHost = GetTemplateChild("PART_FillHost") as FrameworkElement;
        UpdateFillClip();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateFillClip();
    }

    /// <summary>Clips the progress fill to the inside of the rounded border.</summary>
    private void UpdateFillClip()
    {
        if (_fillHost is null)
        {
            return;
        }
        var inner = new Rect(ChromeBorder, ChromeBorder,
            Math.Max(0, ActualWidth - 2 * ChromeBorder), Math.Max(0, ActualHeight - 2 * ChromeBorder));
        double radius = ChromeCornerRadius - ChromeBorder;
        var clip = new RectangleGeometry(inner, radius, radius);
        clip.Freeze();
        _fillHost.Clip = clip;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _engine = AppServices.Engine;
        _dwellable.Name = Name;
        _engine.AddButton(_dwellable);
        _window = Window.GetWindow(this);
        if (_window is not null)
        {
            _window.LocationChanged += OnWindowMoved;
        }
        UpdateBounds();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _engine?.RemoveButton(_dwellable);
        _engine = null;
        if (_window is not null)
        {
            _window.LocationChanged -= OnWindowMoved;
            _window = null;
        }
    }

    private void OnWindowMoved(object? sender, EventArgs e) => UpdateBounds();

    private void UpdateBounds()
    {
        if (PresentationSource.FromVisual(this) is null || !IsVisible)
        {
            _dwellable.Bounds = Rect.Empty;
            return;
        }
        // PointToScreen yields physical pixels, matching the physical cursor position the engine uses.
        _dwellable.Bounds = new Rect(PointToScreen(new Point(0, 0)), PointToScreen(new Point(ActualWidth, ActualHeight)));
    }

    private void OnDwellablePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DwellableButton.DwellProgress))
        {
            DwellProgress = _dwellable.DwellProgress;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        e.Handled = true;
        _dwellable.InvokeAction();
    }

    /// <summary>
    /// Deferred so that commands opening modal windows never run inside the dwell tick
    /// (the nested message loop would freeze dwelling).
    /// </summary>
    private void ExecuteCommand() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        });

    private void Flash()
    {
        if (GetTemplateChild("PART_Flash") is UIElement flash)
        {
            flash.BeginAnimation(OpacityProperty, new DoubleAnimation(0.55, 0, TimeSpan.FromMilliseconds(450))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            });
        }
    }
}
