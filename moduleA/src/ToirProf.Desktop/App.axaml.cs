using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.Threading.Tasks;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                ErrorLogService.Log("UnhandledException", "Fatal", null, ex.Message, ex.ToString());
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ErrorLogService.Log("UnobservedTask", "AsyncError", null, e.Exception.Message, e.Exception.ToString());
            e.SetObserved();
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = SettingsService.Load();
        ApplyTheme(settings.ThemeMode);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }

    public static void ApplyTheme(string themeMode)
    {
        if (Current != null)
        {
            Current.RequestedThemeVariant = string.Equals(themeMode, "Dark", StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }
}
