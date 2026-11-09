using System.ComponentModel;
using Reticle.Controls;
using Reticle.Core;
using Reticle.Engine;

namespace Reticle.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly DwellEngine _engine;
    private readonly AppSettings _settings;
    private readonly Action _showSettings;

    public MainViewModel(DwellEngine engine, AppSettings settings, Action showSettings)
    {
        _engine = engine;
        _settings = settings;
        _showSettings = showSettings;
        _engine.ClickTypeChanged += (_, _) => RaiseButtonStates();
        _settings.PropertyChanged += OnSettingsChanged;

        ChangeClickTypeCommand = new RelayCommand(p => _engine.ClickType = (ClickType)p!);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
    }

    public string Title => "Reticle";

    /// <summary>DIPs.</summary>
    public int ButtonSize => _settings.ButtonSize;

    public ToolbarPosition Position => _settings.Position;

    public bool IsVertical => _settings.Position.IsVertical;

    public DwellButtonState RestState => StateOf(ClickType.None);

    public DwellButtonState LeftClickState => StateOf(ClickType.LeftClick);

    public DwellButtonState RightClickState => StateOf(ClickType.RightClick);

    public DwellButtonState DoubleClickState => StateOf(ClickType.DoubleLeftClick);

    public DwellButtonState DragState => StateOf(ClickType.Drag);

    public RelayCommand ChangeClickTypeCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    private DwellButtonState StateOf(ClickType clickType) =>
        _engine.ClickType != clickType ? DwellButtonState.NotSelected
        : _engine.ClickTypeLocked ? DwellButtonState.Locked
        : DwellButtonState.Selected;

    private void RaiseButtonStates()
    {
        OnPropertyChanged(nameof(RestState));
        OnPropertyChanged(nameof(LeftClickState));
        OnPropertyChanged(nameof(RightClickState));
        OnPropertyChanged(nameof(DoubleClickState));
        OnPropertyChanged(nameof(DragState));
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettings.ButtonSize):
                OnPropertyChanged(nameof(ButtonSize));
                break;
            case nameof(AppSettings.Position):
                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(IsVertical));
                break;
        }
    }

    private void OpenSettings()
    {
        _engine.ClickType = ClickType.LeftClick;
        _showSettings();
    }
}
