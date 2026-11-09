using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Reticle.Core;
using Reticle.Engine;

namespace Reticle;

/// <summary>Process-wide singletons created at startup by <see cref="App"/>.</summary>
internal static class AppServices
{
    private static DwellEngine? _engine;
    private static AppSettings? _settings;

    public static DwellEngine Engine
    {
        get => _engine ?? throw new InvalidOperationException("Dwell engine not initialised.");
        set => _engine = value;
    }

    public static AppSettings Settings
    {
        get => _settings ?? throw new InvalidOperationException("Settings not initialised.");
        set => _settings = value;
    }
}

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public sealed class RelayCommand(Action<object?> execute) : ICommand
{
    public RelayCommand(Action execute)
        : this(_ => execute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute(parameter);
}
