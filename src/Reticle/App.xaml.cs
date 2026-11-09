using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Reticle.Core;
using Reticle.Engine;
using Reticle.Localization;
using Reticle.Platform;
using Reticle.Themes;
using Reticle.ViewModels;
using Reticle.Views;

namespace Reticle;

public partial class App : Application
{
    private const int MinimumFrameMilliseconds = 16;

    private Mutex? _singleInstance;
    private DwellEngine? _engine;
    private Win32Mouse? _mouse;
    private DwellOverlayWindow? _overlay;
    private MainWindow? _mainWindow;
    private SettingsViewModel? _settingsViewModel;
    private SettingsWindow? _settingsWindow;
    private TrayIcon? _trayIcon;
    private int _lastFrame;
    private bool _ticking;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log.Error($"Unhandled exception: {args.ExceptionObject}");
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        Log.Info($"Starting Reticle {typeof(App).Assembly.GetName().Version}{(EaseOfAccess.IsSecureDesktop ? " on the secure desktop" : string.Empty)}.");

        // One instance per desktop: the sign-in screen copy runs alongside the user's.
        string mutexName = EaseOfAccess.IsSecureDesktop ? @"Global\Reticle.SecureDesktop" : @"Local\Reticle";
        _singleInstance = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            Log.Warn("Reticle is already running.");
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown(1);
            return;
        }

        AppSettings settings = AppSettings.Load(AppSettings.DefaultFilePath);
        AppServices.Settings = settings;
        ThemeManager.Apply(settings.Theme);
        Localizer.Apply(settings.Language);
        settings.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppSettings.Theme))
            {
                ThemeManager.Apply(settings.Theme);
            }
            else if (args.PropertyName == nameof(AppSettings.Language))
            {
                Localizer.Apply(settings.Language);
            }
        };

        _overlay = new DwellOverlayWindow(settings, MonitorDisplayScale.Instance);
        _overlay.Initialise();

        _mouse = new Win32Mouse();
        _engine = new DwellEngine(settings, SystemClock.Instance, new ClickSound(settings), _mouse, _overlay, MonitorDisplayScale.Instance);
        AppServices.Engine = _engine;

        _settingsViewModel = new SettingsViewModel(settings, Environment.ProcessPath!);
        _settingsViewModel.ExitRequested += (_, _) => ExitApplication();

        var mainViewModel = new MainViewModel(_engine, settings, ShowSettings);
        _mainWindow = new MainWindow(mainViewModel, () => DialogWindow.ConfirmExit(_mainWindow));
        _mainWindow.Closed += (_, _) => OnMainWindowClosed();
        MainWindow = _mainWindow;
        _mainWindow.Show();

        _trayIcon = new TrayIcon(ShowSettings, ExitApplication);

        CompositionTarget.Rendering += OnRendering;
        Log.Info("Started.");
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        if (_mainWindow is not null)
        {
            _mainWindow.SuppressExitConfirmation = true;
        }
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_singleInstance is not null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        Log.Info("Exiting.");
        base.OnExit(e);
    }

    /// <summary>Drives the dwell engine once per rendered frame, at most every 16 ms.</summary>
    private void OnRendering(object? sender, EventArgs e)
    {
        if (_ticking || _engine is null || _mouse is null)
        {
            return;
        }
        int now = Environment.TickCount;
        if (_lastFrame != 0 && unchecked(now - _lastFrame) < MinimumFrameMilliseconds)
        {
            return;
        }
        _ticking = true;
        try
        {
            _engine.Update(_mouse.GetCursorPosition());
            _lastFrame = Environment.TickCount;
        }
        finally
        {
            _ticking = false;
        }
    }

    private void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_settingsViewModel!);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /// <summary>Quits without asking: used after the user confirmed in Settings, and by the tray's Close application.</summary>
    private void ExitApplication()
    {
        if (_mainWindow is null)
        {
            return;
        }
        _mainWindow.SuppressExitConfirmation = true;
        _mainWindow.Close();
    }

    private void OnMainWindowClosed()
    {
        CompositionTarget.Rendering -= OnRendering;
        _trayIcon?.Dispose();
        _trayIcon = null;
        foreach (Window window in Windows.Cast<Window>().Where(w => w != _mainWindow).ToList())
        {
            window.Close();
        }
        _engine?.Dispose();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error($"Unhandled UI exception: {e.Exception}");
        // An assistive tool that vanishes strands its user; keep running.
        e.Handled = true;
    }
}
