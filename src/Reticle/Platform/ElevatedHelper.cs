using System.ComponentModel;
using System.Diagnostics;

namespace Reticle.Platform;

/// <summary>Elevated helper mode (<c>Reticle.exe --eoa …</c>) for the machine-wide Ease of Access settings.</summary>
internal static class ElevatedHelper
{
    public const string Argument = "--eoa";

    private const int ErrorCancelled = 1223;

    /// <summary>Runs this exe elevated with <paramref name="operations"/>; false if the user declined UAC or it failed.</summary>
    public static bool Run(params string[] operations)
    {
        var startInfo = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = true,
            Verb = "runas",
        };
        startInfo.ArgumentList.Add(Argument);
        foreach (string operation in operations)
        {
            startInfo.ArgumentList.Add(operation);
        }
        try
        {
            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            Log.Info("Elevation declined.");
            return false;
        }
        catch (Win32Exception ex)
        {
            Log.Error($"Elevated helper failed to start: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Entry point inside the elevated process. Operations: <c>register &lt;exe&gt;</c>, <c>signin true|false</c>,
    /// <c>unregister</c> (run by the uninstaller).
    /// </summary>
    public static int Main(IReadOnlyList<string> args)
    {
        try
        {
            for (int i = 0; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case "register" when i + 1 < args.Count:
                        EaseOfAccess.Register(args[++i]);
                        break;
                    case "signin" when i + 1 < args.Count:
                        EaseOfAccess.SetSignIn(bool.Parse(args[++i]));
                        break;
                    case "unregister":
                        EaseOfAccess.Unregister();
                        break;
                    default:
                        Log.Error($"Unknown or incomplete helper operation '{args[i]}'.");
                        return 2;
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error($"Elevated helper failed: {ex}");
            return 1;
        }
    }
}
