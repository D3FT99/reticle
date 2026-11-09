using System.ComponentModel;
using Reticle.Core;
using Reticle.Localization;
using Reticle.Platform;

namespace Reticle.ViewModels;

/// <summary>UI services the settings screen needs from its window.</summary>
public interface ISettingsDialogs
{
    bool ConfirmExit();

    ToolbarPosition? PickPosition();

    void NotifyTakesEffectAfterRestart();
}

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly string _exePath;
    private bool? _runAtDesktop;
    private bool? _runAtSignIn;

    public SettingsViewModel(AppSettings settings, string exePath)
    {
        _settings = settings;
        _exePath = exePath;
        _settings.PropertyChanged += OnSettingsChanged;
        // Every text property is localized; an empty name refreshes all bindings.
        Localizer.Changed += (_, _) => OnPropertyChanged(string.Empty);

        DecreaseTargetSizeCommand = new RelayCommand(() => TargetSize -= AppSettings.TargetSizeStep);
        IncreaseTargetSizeCommand = new RelayCommand(() => TargetSize += AppSettings.TargetSizeStep);
        DecreaseButtonSizeCommand = new RelayCommand(() => ButtonSize -= AppSettings.ButtonSizeStep);
        IncreaseButtonSizeCommand = new RelayCommand(() => ButtonSize += AppSettings.ButtonSizeStep);
        DecreaseDwellTimeCommand = new RelayCommand(() => DwellTime -= AppSettings.DwellTimeStep);
        IncreaseDwellTimeCommand = new RelayCommand(() => DwellTime += AppSettings.DwellTimeStep);
        ChangePositionCommand = new RelayCommand(ChangePosition);
        ExitCommand = new RelayCommand(Exit);
        DoneCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    public ISettingsDialogs? Dialogs { get; set; }

    /// <summary>The user confirmed they want to quit the application.</summary>
    public event EventHandler? ExitRequested;

    public event EventHandler? CloseRequested;

    public string VersionText =>
        Localizer.Format("Settings_VersionFormat", typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? string.Empty);

    public int MinTargetSize => AppSettings.MinTargetSize;

    public int MaxTargetSize => AppSettings.MaxTargetSize;

    public int TargetSizeStep => AppSettings.TargetSizeStep;

    public int MinDwellTime => AppSettings.MinDwellTime;

    public int MaxDwellTime => AppSettings.MaxDwellTime;

    public int DwellTimeStep => AppSettings.DwellTimeStep;

    public int MinButtonSize => AppSettings.MinButtonSize;

    public int MaxButtonSize => AppSettings.MaxButtonSize;

    public int ButtonSizeStep => AppSettings.ButtonSizeStep;

    /// <summary>DIPs.</summary>
    public int TargetSize
    {
        get => _settings.TargetSize;
        set => _settings.TargetSize = value;
    }

    public string TargetSizeText => Localizer.Format("Common_PixelsFormat", TargetSize);

    /// <summary>DIPs.</summary>
    public int ButtonSize
    {
        get => _settings.ButtonSize;
        set => _settings.ButtonSize = value;
    }

    public string ButtonSizeText => Localizer.Format("Common_PixelsFormat", ButtonSize);

    public int DwellTime
    {
        get => _settings.DwellTime;
        set => _settings.DwellTime = value;
    }

    public string DwellTimeText => Localizer.Format("DwellTime_SecondsFormat", DwellTime / 1000.0);

    public bool SoundEnabled
    {
        get => _settings.SoundEnabled;
        set => _settings.SoundEnabled = value;
    }

    public string SoundEnabledText => Localizer.Get(SoundEnabled ? "Sound_On" : "Sound_Off");

    public ToolbarPosition Position => _settings.Position;

    public string PositionText => Localizer.Get(Position switch
    {
        { X: HorizontalPosition.Center, Y: VerticalPosition.Top } => "Position_TopCenter",
        { X: HorizontalPosition.Center, Y: VerticalPosition.Bottom } => "Position_BottomCenter",
        { X: HorizontalPosition.Left, Y: VerticalPosition.Center } => "Position_MiddleLeft",
        { X: HorizontalPosition.Right, Y: VerticalPosition.Center } => "Position_MiddleRight",
        { X: HorizontalPosition.Left, Y: VerticalPosition.Top } => "Position_TopLeft",
        { X: HorizontalPosition.Right, Y: VerticalPosition.Top } => "Position_TopRight",
        { X: HorizontalPosition.Left, Y: VerticalPosition.Bottom } => "Position_BottomLeft",
        _ => "Position_BottomRight",
    });

    public AppTheme Theme
    {
        get => _settings.Theme;
        set => _settings.Theme = value;
    }

    public AppLanguage Language
    {
        get => _settings.Language;
        set => _settings.Language = value;
    }

    /// <summary>Start-up options make no sense on the sign-in desktop itself.</summary>
    public bool ShowStartupOptions => !EaseOfAccess.IsSecureDesktop;

    public bool RunAtDesktop
    {
        get => _runAtDesktop ??= EaseOfAccess.QueryDesktop();
        set
        {
            if (value == RunAtDesktop)
            {
                return;
            }
            if (!value || EnsureRegistered())
            {
                try
                {
                    EaseOfAccess.SetDesktop(value);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
                {
                    Log.Error($"Failed to change desktop start-up: {ex.Message}");
                }
            }
            ApplyStartupResult(ref _runAtDesktop, EaseOfAccess.QueryDesktop(), nameof(RunAtDesktop));
        }
    }

    public bool RunAtSignIn
    {
        get => _runAtSignIn ??= EaseOfAccess.QuerySignIn();
        set
        {
            if (value == RunAtSignIn)
            {
                return;
            }
            var operations = new List<string>();
            if (value && !EaseOfAccess.IsRegistered(_exePath))
            {
                operations.AddRange(["register", _exePath]);
            }
            operations.AddRange(["signin", value.ToString()]);
            ElevatedHelper.Run([.. operations]);
            ApplyStartupResult(ref _runAtSignIn, EaseOfAccess.QuerySignIn(), nameof(RunAtSignIn));
        }
    }

    public RelayCommand DecreaseTargetSizeCommand { get; }

    public RelayCommand IncreaseTargetSizeCommand { get; }

    public RelayCommand DecreaseButtonSizeCommand { get; }

    public RelayCommand IncreaseButtonSizeCommand { get; }

    public RelayCommand DecreaseDwellTimeCommand { get; }

    public RelayCommand IncreaseDwellTimeCommand { get; }

    public RelayCommand ChangePositionCommand { get; }

    public RelayCommand ExitCommand { get; }

    public RelayCommand DoneCommand { get; }

    private bool EnsureRegistered() =>
        EaseOfAccess.IsRegistered(_exePath) || (ElevatedHelper.Run("register", _exePath) && EaseOfAccess.IsRegistered(_exePath));

    private void ApplyStartupResult(ref bool? field, bool actual, string propertyName)
    {
        bool changed = field != actual;
        field = actual;
        // Raise even when unchanged so a toggle the user flipped snaps back after a declined UAC prompt.
        OnPropertyChanged(propertyName);
        if (changed)
        {
            Dialogs?.NotifyTakesEffectAfterRestart();
        }
    }

    private void ChangePosition()
    {
        if (Dialogs?.PickPosition() is { } position)
        {
            _settings.Position = position;
        }
    }

    private void Exit()
    {
        if (Dialogs?.ConfirmExit() ?? true)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettings.TargetSize):
                OnPropertyChanged(nameof(TargetSize));
                OnPropertyChanged(nameof(TargetSizeText));
                break;
            case nameof(AppSettings.ButtonSize):
                OnPropertyChanged(nameof(ButtonSize));
                OnPropertyChanged(nameof(ButtonSizeText));
                break;
            case nameof(AppSettings.Theme):
                OnPropertyChanged(nameof(Theme));
                break;
            case nameof(AppSettings.Language):
                OnPropertyChanged(nameof(Language));
                break;
            case nameof(AppSettings.DwellTime):
                OnPropertyChanged(nameof(DwellTime));
                OnPropertyChanged(nameof(DwellTimeText));
                break;
            case nameof(AppSettings.SoundEnabled):
                OnPropertyChanged(nameof(SoundEnabled));
                OnPropertyChanged(nameof(SoundEnabledText));
                break;
            case nameof(AppSettings.Position):
                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(PositionText));
                break;
        }
    }
}
