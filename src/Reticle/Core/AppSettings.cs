using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using System.Xml.Linq;
using Reticle.Engine;
using Reticle.Platform;

namespace Reticle.Core;

public enum HorizontalPosition
{
    Left,
    Center,
    Right,
}

public enum VerticalPosition
{
    Top,
    Center,
    Bottom,
}

public enum AppTheme
{
    Midnight,
    Daylight,
    Ocean,
}

public enum AppLanguage
{
    English,
    PortuguesePortugal,
}

/// <summary>Where the toolbar docks on the primary screen.</summary>
public readonly record struct ToolbarPosition(HorizontalPosition X, VerticalPosition Y)
{
    public static readonly ToolbarPosition TopLeft = new(HorizontalPosition.Left, VerticalPosition.Top);
    public static readonly ToolbarPosition TopCenter = new(HorizontalPosition.Center, VerticalPosition.Top);
    public static readonly ToolbarPosition TopRight = new(HorizontalPosition.Right, VerticalPosition.Top);
    public static readonly ToolbarPosition MiddleLeft = new(HorizontalPosition.Left, VerticalPosition.Center);
    public static readonly ToolbarPosition MiddleRight = new(HorizontalPosition.Right, VerticalPosition.Center);
    public static readonly ToolbarPosition BottomLeft = new(HorizontalPosition.Left, VerticalPosition.Bottom);
    public static readonly ToolbarPosition BottomCenter = new(HorizontalPosition.Center, VerticalPosition.Bottom);
    public static readonly ToolbarPosition BottomRight = new(HorizontalPosition.Right, VerticalPosition.Bottom);

    /// <summary>Docked to a side edge the toolbar stacks vertically.</summary>
    public bool IsVertical => X != HorizontalPosition.Center;
}

/// <summary>
/// User settings persisted to <c>%ProgramData%\Reticle\settings.xml</c>. The folder is shared with the
/// sign-in screen instance (which runs as SYSTEM), hence machine-wide storage writable by everyone.
/// </summary>
public sealed class AppSettings : IDwellSettings, INotifyPropertyChanged
{
    public const int MinTargetSize = 30;
    public const int MaxTargetSize = 100;
    public const int TargetSizeStep = 5;
    public const int MinDwellTime = 300;
    public const int MaxDwellTime = 5000;
    public const int DwellTimeStep = 100;
    public const int MinButtonSize = 40;
    public const int MaxButtonSize = 120;
    public const int ButtonSizeStep = 5;

    private const int DefaultTargetSize = 60;
    private const int DefaultDwellTime = 2000;
    private const int DefaultButtonSize = 50;

    private readonly string? _filePath;
    private int _targetSize = DefaultTargetSize;
    private int _dwellTime = DefaultDwellTime;
    private int _buttonSize = DefaultButtonSize;
    private AppTheme _theme = AppTheme.Midnight;
    private AppLanguage _language = AppLanguage.English;
    private bool _soundEnabled = true;
    private ToolbarPosition _position = ToolbarPosition.TopCenter;

    private AppSettings(string? filePath) => _filePath = filePath;

    public static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Reticle", "settings.xml");

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>DIPs, so the target looks the same size on every monitor.</summary>
    public int TargetSize
    {
        get => _targetSize;
        set => Set(ref _targetSize, Math.Clamp(value, MinTargetSize, MaxTargetSize));
    }

    /// <summary>Side of each toolbar button, in DIPs.</summary>
    public int ButtonSize
    {
        get => _buttonSize;
        set => Set(ref _buttonSize, Math.Clamp(value, MinButtonSize, MaxButtonSize));
    }

    public int DwellTime
    {
        get => _dwellTime;
        set => Set(ref _dwellTime, Math.Clamp(value, MinDwellTime, MaxDwellTime));
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set => Set(ref _soundEnabled, value);
    }

    public ToolbarPosition Position
    {
        get => _position;
        set => Set(ref _position, value);
    }

    public AppTheme Theme
    {
        get => _theme;
        set => Set(ref _theme, value);
    }

    public AppLanguage Language
    {
        get => _language;
        set => Set(ref _language, value);
    }

    /// <summary>Settings that live only in memory; for tests and previews.</summary>
    public static AppSettings InMemory() => new(null);

    public static AppSettings Load(string filePath)
    {
        var settings = new AppSettings(filePath);
        EnsureSharedFolder(Path.GetDirectoryName(filePath)!);
        if (!File.Exists(filePath))
        {
            Log.Info("No settings file; using defaults.");
            return settings;
        }
        try
        {
            settings.ReadXml(XDocument.Load(filePath).Root!);
        }
        catch (XmlException)
        {
            Log.Error("Settings file is not valid XML; using defaults.");
            TryDelete(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Settings file could not be read: {ex.Message}");
        }
        return settings;
    }

    internal void ReadXml(XElement root)
    {
        _targetSize = Math.Clamp(ReadInt(root, "TargetSize", DefaultTargetSize), MinTargetSize, MaxTargetSize);
        _dwellTime = Math.Clamp(ReadInt(root, "DwellTime", DefaultDwellTime), MinDwellTime, MaxDwellTime);
        _buttonSize = Math.Clamp(ReadInt(root, "ButtonSize", DefaultButtonSize), MinButtonSize, MaxButtonSize);
        _soundEnabled = !bool.TryParse(root.Element("SoundEnabled")?.Value, out bool sound) || sound;
        _position = new ToolbarPosition(
            ReadEnum(root, "PositionX", HorizontalPosition.Center),
            ReadEnum(root, "PositionY", VerticalPosition.Top));
        _theme = ReadEnum(root, "Theme", AppTheme.Midnight);
        _language = ReadEnum(root, "Language", AppLanguage.English);
    }

    internal XDocument WriteXml() => new(new XElement("Settings",
        new XElement("TargetSize", _targetSize),
        new XElement("SoundEnabled", _soundEnabled),
        new XElement("PositionX", _position.X),
        new XElement("PositionY", _position.Y),
        new XElement("DwellTime", _dwellTime),
        new XElement("ButtonSize", _buttonSize),
        new XElement("Theme", _theme),
        new XElement("Language", _language)));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        Save();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Save()
    {
        if (_filePath is null)
        {
            return;
        }
        try
        {
            File.WriteAllText(_filePath, WriteXml().ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Failed to write settings: {ex.Message}");
        }
    }

    private static int ReadInt(XElement root, string name, int fallback) =>
        int.TryParse(root.Element(name)?.Value, out int value) ? value : fallback;

    private static T ReadEnum<T>(XElement root, string name, T fallback) where T : struct, Enum =>
        Enum.TryParse(root.Element(name)?.Value, ignoreCase: true, out T value) && Enum.IsDefined(value) ? value : fallback;

    private static void EnsureSharedFolder(string folder)
    {
        if (Directory.Exists(folder))
        {
            return;
        }
        try
        {
            var security = new DirectorySecurity();
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            new DirectoryInfo(folder).Create(security);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Failed to create settings folder: {ex.Message}");
        }
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Failed to delete invalid settings file: {ex.Message}");
        }
    }
}
