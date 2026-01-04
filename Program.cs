using System;
using System.IO;
using Avalonia;
using JetBrains.Annotations;

namespace DialogueHelper;

[UsedImplicitly]
class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (obj, e) =>
        {
            using var f = new StreamWriter(File.Open("crash.txt", FileMode.Create));
            f.Write(((Exception)e.ExceptionObject).ToString());
            f.Flush();
            f.Close();
        };
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
