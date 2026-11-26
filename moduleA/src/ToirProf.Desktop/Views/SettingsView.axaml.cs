using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class SettingsView : UserControl, IPageView
{
    private readonly IDesktopHost _host;

    public SettingsView() : this(null!) { }

    public SettingsView(IDesktopHost host)
    {
        InitializeComponent();
        _host = host;
    }

    public Task LoadAsync()
    {
        if (_host == null) return Task.CompletedTask;
        _host.SetBreadcrumb("Аналитика и система", "Настройки");
        var s = _host.Settings;
        ThemeBox.SelectedIndex = s.ThemeMode == "Dark" ? 1 : 0;
        FontSizeBox.SelectedIndex = s.FontSize switch { <= 12 => 0, >= 16 => 2, _ => 1 };
        ApiUrlBox.Text = s.ApiBaseUrl;
        ColSerialCheck.IsChecked = s.ShowSerialColumn;
        ColManufacturerCheck.IsChecked = s.ShowManufacturerColumn;
        ColTeamCheck.IsChecked = s.ShowTeamColumn;

        DraftsGrid.ItemsSource = DraftQueueService.LoadAll();
        LogPathText.Text = ErrorLogService.LogFilePath;
        LogContentBox.Text = ErrorLogService.ReadRecent(80);
        return Task.CompletedTask;
    }

    private void OnSaveSettingsClick(object? sender, RoutedEventArgs e)
    {
        var s = _host.Settings;
        s.ThemeMode = ThemeBox.SelectedIndex == 1 ? "Dark" : "Light";
        s.FontSize = FontSizeBox.SelectedIndex switch { 0 => 12, 2 => 16, _ => 14 };
        if (!string.IsNullOrWhiteSpace(ApiUrlBox.Text))
        {
            s.ApiBaseUrl = ApiUrlBox.Text.Trim();
            _host.Api.SetBaseUrl(s.ApiBaseUrl);
        }
        s.ShowSerialColumn = ColSerialCheck.IsChecked == true;
        s.ShowManufacturerColumn = ColManufacturerCheck.IsChecked == true;
        s.ShowTeamColumn = ColTeamCheck.IsChecked == true;

        SettingsService.Save(s);
        _host.ApplyPreferences();
        _host.ShowToast("Настройки сохранены.");
    }

    private async void OnSyncDraftsClick(object? sender, RoutedEventArgs e)
    {
        var drafts = DraftQueueService.LoadAll();
        if (drafts.Count == 0) { _host.ShowToast("Очередь черновиков пуста."); return; }

        int sent = 0;
        foreach (var d in drafts)
        {
            var res = await _host.Api.CreateWorkOrderAsync(d.Payload);
            if (res.IsSuccess)
            {
                DraftQueueService.Remove(d.Id);
                sent++;
            }
        }
        DraftsGrid.ItemsSource = DraftQueueService.LoadAll();
        _host.ShowToast($"Отправлено черновиков: {sent} из {drafts.Count}");
    }

    private void OnRefreshLogClick(object? sender, RoutedEventArgs e)
    {
        LogContentBox.Text = ErrorLogService.ReadRecent(80);
    }
}
