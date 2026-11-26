using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Services;
using ToirProf.Desktop.Views;

namespace ToirProf.Desktop;

public partial class MainWindow
{
    private void ResetIdle()
    {
        _idleSeconds = 0;
        IdleCountdownText.Text = "Автоблокировка: 15с";
    }

    private void OnIdleTick(object? sender, EventArgs e)
    {
        if (CurrentUser == null || !ShellGrid.IsVisible || LockOverlay.IsVisible) return;
        _idleSeconds++;
        var rem = Math.Max(0, 15 - _idleSeconds);
        IdleCountdownText.Text = $"Автоблокировка: {rem}с";
        if (_idleSeconds >= 15) LockSession();
    }

    private void OnManualLockClick(object? sender, RoutedEventArgs e) => LockSession();

    private void LockSession()
    {
        UnlockPasswordBox.Text = "";
        UnlockErrorText.IsVisible = false;
        LockOverlay.IsVisible = true;
        UnlockPasswordBox.Focus();
    }

    private async void OnUnlockClick(object? sender, RoutedEventArgs e)
    {
        if (CurrentUser == null) return;
        var pwd = UnlockPasswordBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(pwd))
        {
            UnlockErrorText.Text = "Введите пароль.";
            UnlockErrorText.IsVisible = true;
            return;
        }
        var res = await Api.LoginAsync(CurrentUser.Email, pwd, Settings.RememberMe);
        if (!res.IsSuccess)
        {
            UnlockErrorText.Text = "Неверный пароль разблокировки.";
            UnlockErrorText.IsVisible = true;
            return;
        }
        if (res.Data != null) Api.SetToken(res.Data.Token);
        LockOverlay.IsVisible = false;
        ResetIdle();
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        Api.SetToken(null);
        Settings.SavedToken = null;
        Settings.RememberMe = false;
        SettingsService.Save(Settings);
        ShowLoginScreen();
    }

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        ResetIdle();
        if (CurrentUser == null || LockOverlay.IsVisible) return;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (ctrl && e.Key == Key.N && PageHost.Content is IPageView pv1)
        {
            if (PageHost.Content is WorkOrdersView or EquipmentView) pv1.TriggerNew();
            else NavigateTo("WorkOrders", null, true);
            e.Handled = true;
        }
        else if ((e.Key == Key.F5 || (ctrl && e.Key == Key.R)) && PageHost.Content is IPageView pv2)
        {
            await pv2.LoadAsync();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.F && PageHost.Content is IPageView pv3)
        {
            pv3.FocusSearch();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.L)
        {
            LockSession();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            NotifPopup.IsVisible = false;
            ErrorModal.IsVisible = false;
        }
    }

    private async Task SyncOfflineDraftsAsync()
    {
        var drafts = DraftQueueService.LoadAll();
        DraftsBadgeText.Text = drafts.Count > 0 ? $"Ожидают отправки: {drafts.Count} черн." : "";
        if (CurrentUser == null || drafts.Count == 0) return;

        int sent = 0;
        foreach (var d in drafts.ToList())
        {
            var res = await Api.CreateWorkOrderAsync(d.Payload);
            if (res.IsSuccess)
            {
                DraftQueueService.Remove(d.Id);
                sent++;
            }
        }
        if (sent > 0)
        {
            var remaining = DraftQueueService.LoadAll().Count;
            DraftsBadgeText.Text = remaining > 0 ? $"Ожидают отправки: {remaining} черн." : "";
            ShowToast($"Связь восстановлена! Автоматически отправлено черновиков: {sent}");
            if (_activeSection == "WorkOrders" && PageHost.Content is IPageView pv) await pv.LoadAsync();
        }
    }
}
