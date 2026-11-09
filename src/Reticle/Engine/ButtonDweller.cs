using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Reticle.Engine;

/// <summary>A UI element that can be activated by dwelling on it. Bounds are physical screen pixels.</summary>
public sealed class DwellableButton(Action action) : INotifyPropertyChanged
{
    private double _dwellProgress;

    public string Name { get; set; } = string.Empty;

    public Rect Bounds { get; set; } = Rect.Empty;

    public bool AllowMultipleClicks { get; set; }

    /// <summary>0..1 while the pointer dwells on the button.</summary>
    public double DwellProgress
    {
        get => _dwellProgress;
        set
        {
            if (_dwellProgress != value)
            {
                _dwellProgress = value;
                OnPropertyChanged();
            }
        }
    }

    public event EventHandler? ActionInvoked;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Runs the action. Called by the dweller or by a physical click.</summary>
    public void InvokeAction()
    {
        ActionInvoked?.Invoke(this, EventArgs.Empty);
        action();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ButtonDweller : Dweller
{
    public ButtonDweller(DwellableButton button, IDwellSettings settings, IClock clock, IClickSound sound)
        : base(settings, clock, sound)
    {
        Button = button;
        Button.ActionInvoked += OnActionInvoked;
    }

    public DwellableButton Button { get; }

    protected override bool AllowMultipleClicks => Button.AllowMultipleClicks;

    protected override Rect GetTarget(Point cursorPosition) => Button.Bounds;

    public override bool TargetContainsMouse(Point mousePosition) => Button.Bounds.Contains(mousePosition);

    protected override void UpdateProgress(Point mousePosition) =>
        Button.DwellProgress = Math.Clamp(DwellProgress, 0, 1);

    protected override void Click(Point mousePosition) => Button.InvokeAction();

    private void OnActionInvoked(object? sender, EventArgs e) => ActionCompleted();

    protected override void Dispose(bool disposing)
    {
        Button.ActionInvoked -= OnActionInvoked;
        Button.DwellProgress = 0;
    }
}
