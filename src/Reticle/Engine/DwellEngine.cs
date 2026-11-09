using System.Windows;

namespace Reticle.Engine;

/// <summary>
/// Owns every dweller and the selected click type. Call <see cref="Update"/> with the cursor position on every frame.
/// </summary>
public sealed class DwellEngine : IDisposable
{
    private readonly IDwellSettings _settings;
    private readonly IClock _clock;
    private readonly IClickSound _sound;
    private readonly List<ButtonDweller> _buttonDwellers = [];
    private readonly ScreenDweller _screenDweller;

    public DwellEngine(
        IDwellSettings settings,
        IClock clock,
        IClickSound sound,
        IMouse mouse,
        IDwellFeedback feedback,
        IDisplayScale displayScale)
    {
        _settings = settings;
        _clock = clock;
        _sound = sound;
        _screenDweller = new ScreenDweller(this, settings, clock, sound, mouse, feedback, displayScale);
        ClickType = ClickType.LeftClick;
    }

    public ClickType ClickType
    {
        get => _screenDweller.ClickType;
        set
        {
            // Rest and left click are always sticky; other types stick only when chosen twice in a row.
            ClickTypeLocked = value is ClickType.None or ClickType.LeftClick || _screenDweller.ClickType == value;
            _screenDweller.ClickType = value;
            ClickTypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>False when the click type reverts to left click after one action.</summary>
    public bool ClickTypeLocked { get; private set; }

    public bool IsDragging => _screenDweller.IsDragging;

    public event EventHandler? ClickTypeChanged;

    public void AddButton(DwellableButton button) =>
        _buttonDwellers.Add(new ButtonDweller(button, _settings, _clock, _sound));

    public void RemoveButton(DwellableButton button)
    {
        ButtonDweller? dweller = _buttonDwellers.SingleOrDefault(d => d.Button == button);
        if (dweller is not null)
        {
            dweller.Dispose();
            _buttonDwellers.Remove(dweller);
        }
    }

    internal bool IsMouseOverButton(Point mousePosition) =>
        _buttonDwellers.Any(d => d.TargetContainsMouse(mousePosition));

    /// <summary>Called after a one-shot action (right, double, drag end).</summary>
    internal void AfterClick()
    {
        if (!ClickTypeLocked)
        {
            ClickType = ClickType.LeftClick;
        }
    }

    public void Update(Point cursorPosition)
    {
        // Buttons may add or remove buttons from their actions (e.g. opening a window); iterate a snapshot.
        foreach (ButtonDweller dweller in _buttonDwellers.ToArray())
        {
            dweller.Update(cursorPosition);
        }
        _screenDweller.Update(cursorPosition);
    }

    public void Dispose()
    {
        foreach (ButtonDweller dweller in _buttonDwellers)
        {
            dweller.Dispose();
        }
        _buttonDwellers.Clear();
        _screenDweller.Dispose();
    }
}
