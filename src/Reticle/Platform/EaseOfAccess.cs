using Microsoft.Win32;
using static Reticle.Platform.NativeMethods;

namespace Reticle.Platform;

/// <summary>
/// Windows Ease of Access integration: registers Reticle as an Assistive Technology and toggles
/// whether ATBroker starts it on the desktop (per user) and on the sign-in screen (machine-wide).
/// </summary>
internal static class EaseOfAccess
{
    public const string AtId = "reticle";

    private const string AccessibilityKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Accessibility";
    private const string AtsKey = AccessibilityKey + @"\ATs\" + AtId;
    private const string ConfigurationValue = "Configuration";

    private static readonly Lazy<bool> IsSecureDesktopLazy = new(DetectSecureDesktop);

    /// <summary>True when running on the Winlogon (sign-in / UAC) desktop.</summary>
    public static bool IsSecureDesktop => IsSecureDesktopLazy.Value;

    /// <summary>Installed next to Reticle.exe; ATBroker can start it where it cannot start a uiAccess exe.</summary>
    public const string LauncherExe = "Reticle.Launcher.exe";

    /// <summary>True when the AT registration exists and starts <paramref name="exePath"/>.</summary>
    public static bool IsRegistered(string exePath)
    {
        using RegistryKey? key = OpenBase(RegistryHive.LocalMachine).OpenSubKey(AtsKey);
        return key?.GetValue("StartExe") is string startExe
            && string.Equals(startExe.Trim('"'), GetStartExe(exePath), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The launcher when it is installed alongside <paramref name="exePath"/>, otherwise the app itself.</summary>
    private static string GetStartExe(string exePath)
    {
        string launcher = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exePath)!, LauncherExe);
        return System.IO.File.Exists(launcher) ? launcher : exePath;
    }

    /// <summary>Writes the AT registration. Requires elevation.</summary>
    public static void Register(string exePath)
    {
        using RegistryKey key = OpenBase(RegistryHive.LocalMachine).CreateSubKey(AtsKey, writable: true);
        key.SetValue("ApplicationName", "Reticle");
        key.SetValue("Description", "Hover the mouse to click");
        key.SetValue("Profile", "<HCIModel><Accommodation type=\"mild dexterity\" /><Accommodation type=\"severe dexterity\" /></HCIModel>");
        key.SetValue("SimpleProfile", "Reticle");
        // ATExe names the process Windows watches for; StartExe is what it runs (possibly the launcher).
        key.SetValue("ATExe", System.IO.Path.GetFileName(exePath));
        key.SetValue("StartExe", GetStartExe(exePath));
    }

    /// <summary>Starts with the user's desktop (HKCU, no elevation needed once registered).</summary>
    public static bool QueryDesktop() => IsConfigured(RegistryHive.CurrentUser);

    public static void SetDesktop(bool enabled) => SetConfigured(RegistryHive.CurrentUser, enabled);

    /// <summary>Starts on the sign-in screen (HKLM, requires elevation).</summary>
    public static bool QuerySignIn() => IsConfigured(RegistryHive.LocalMachine);

    public static void SetSignIn(bool enabled) => SetConfigured(RegistryHive.LocalMachine, enabled);

    /// <summary>Stops starting on the desktop and sign-in screen and deletes the AT registration. Requires elevation.</summary>
    public static void Unregister()
    {
        SetDesktop(false);
        SetSignIn(false);
        OpenBase(RegistryHive.LocalMachine).DeleteSubKeyTree(AtsKey, throwOnMissingSubKey: false);
        Log.Info("Ease of Access registration removed.");
    }

    private static RegistryKey OpenBase(RegistryHive hive) => RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);

    private static bool IsConfigured(RegistryHive hive)
    {
        using RegistryKey? key = OpenBase(hive).OpenSubKey(AccessibilityKey);
        string value = key?.GetValue(ConfigurationValue) as string ?? string.Empty;
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(AtId);
    }

    private static void SetConfigured(RegistryHive hive, bool enabled)
    {
        using RegistryKey key = OpenBase(hive).CreateSubKey(AccessibilityKey, writable: true);
        string current = key.GetValue(ConfigurationValue) as string ?? string.Empty;
        var ids = current.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (enabled && !ids.Contains(AtId))
        {
            ids.Add(AtId);
        }
        else if (!enabled)
        {
            ids.RemoveAll(id => id == AtId);
        }
        string updated = string.Join(",", ids);
        if (updated != current)
        {
            key.SetValue(ConfigurationValue, updated);
        }
        Log.Info($"Ease of Access {hive} start set to {enabled}.");
    }

    private static bool DetectSecureDesktop()
    {
        IntPtr desktop = OpenInputDesktop(0, false, READ_CONTROL | DESKTOP_READOBJECTS);
        if (desktop == IntPtr.Zero)
        {
            // The input desktop is not readable from a normal session while the secure desktop is up,
            // but then we would not be running on it either.
            return false;
        }
        const int bufferBytes = 512;
        IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(bufferBytes);
        try
        {
            if (!GetUserObjectInformation(desktop, UOI_NAME, buffer, bufferBytes, out _))
            {
                return false;
            }
            string name = System.Runtime.InteropServices.Marshal.PtrToStringUni(buffer) ?? string.Empty;
            return name.StartsWith("Winlogon", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            CloseDesktop(desktop);
        }
    }
}
