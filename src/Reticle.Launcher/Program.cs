using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Reticle.Launcher;

/// <summary>Starts Reticle.exe from the same folder through the shell, forwarding the command line.</summary>
internal static class Program
{
    private const string AppExe = "Reticle.exe";

    [STAThread]
    private static int Main(string[] args)
    {
        string exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppExe);
        var startInfo = new ProcessStartInfo(exe, string.Join(" ", args.Select(Quote)))
        {
            UseShellExecute = true,
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
        };
        try
        {
            Process.Start(startInfo)?.Dispose();
            return 0;
        }
        catch (Win32Exception ex)
        {
            Trace.TraceError($"Failed to start {exe}: {ex.Message}");
            return 1;
        }
    }

    private static string Quote(string argument) =>
        argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"']) < 0 ? argument : "\"" + argument.Replace("\"", "\\\"") + "\"";
}
