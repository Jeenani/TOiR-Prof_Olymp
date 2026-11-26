using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;
using ToirProf.Desktop.Views;

namespace ToirProf.Desktop;

public partial class MainWindow
{
    public async void NavigateTo(string section, int? targetId = null, bool openNewModal = false)
    {
        EnsureViews();
        if (!_views.TryGetValue(section, out var view)) return;

        _activeSection = section;
        PageHost.Content = view;
        UpdateSidebarHighlight();

        if (view is IPageView page)
            await page.LoadAsync();

        if (section == "WorkOrders" && view is WorkOrdersView woView)
        {
            if (openNewModal) woView.OpenCreateModal(targetId);
            else if (targetId.HasValue) await woView.OpenDetailAsync(targetId.Value);
        }
    }

    private void UpdateSidebarHighlight()
    {
        foreach (var kv in _navButtons)
        {
            var isActive = kv.Key == _activeSection;
            kv.Value.Background = isActive ? new SolidColorBrush(Color.Parse("#1E40AF")) : Brushes.Transparent;
            kv.Value.Foreground = new SolidColorBrush(isActive ? Colors.White : Color.Parse("#CBD5E1"));
            kv.Value.FontWeight = isActive ? FontWeight.Bold : FontWeight.Normal;
        }
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string sec })
            NavigateTo(sec);
    }

    public void SetLoading(bool isLoading, string? text = null)
    {
        GlobalProgress.IsVisible = isLoading;
        StatusBarText.Text = isLoading ? (text ?? "Выполняется запрос...") : "Готово к работе";
    }

    public void ShowToast(string message, bool isError = false)
    {
        ToastText.Text = message;
        ToastBanner.Background = new SolidColorBrush(Color.Parse(isError ? "#DC2626" : "#0F766E"));
        ToastBanner.IsVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    public void ShowErrorModal(string title, string message, Func<Task>? retryAction = null)
    {
        _retryAction = retryAction;
        ErrorModalTitle.Text = title;
        ErrorModalMessage.Text = message;
        ErrorRetryBtn.IsVisible = retryAction != null;
        ErrorModal.IsVisible = true;
    }

    private void OnErrorModalCancel(object? sender, RoutedEventArgs e) => ErrorModal.IsVisible = false;

    private async void OnErrorModalRetry(object? sender, RoutedEventArgs e)
    {
        ErrorModal.IsVisible = false;
        if (_retryAction != null) await _retryAction();
    }

    public void SetBreadcrumb(params string[] items) => BreadcrumbText.Text = string.Join("  /  ", items);

    public void UpdateNotifications(List<ViolationAlertDto> alerts)
    {
        NotifList.ItemsSource = alerts;
        NotifCountText.Text = alerts.Count.ToString();
        NotifBadge.IsVisible = alerts.Count > 0;
    }

    public void ApplyPreferences()
    {
        App.ApplyTheme(Settings.ThemeMode);
        FontSize = Settings.FontSize;
        if (_views.TryGetValue("Equipment", out var eqView) && eqView is EquipmentView ev)
            ev.ApplyColumnSettings();
    }

    private void OnToggleNotificationsClick(object? sender, RoutedEventArgs e) => NotifPopup.IsVisible = !NotifPopup.IsVisible;
    private void OnCloseNotificationsClick(object? sender, RoutedEventArgs e) => NotifPopup.IsVisible = false;
}
