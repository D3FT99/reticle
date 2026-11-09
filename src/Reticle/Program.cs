using Reticle.Platform;

namespace Reticle;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == ElevatedHelper.Argument)
        {
            return ElevatedHelper.Main(args[1..]);
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
