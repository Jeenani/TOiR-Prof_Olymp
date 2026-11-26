using Avalonia;
using QuestPDF.Infrastructure;
using System;

namespace ToirProf.Desktop;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
