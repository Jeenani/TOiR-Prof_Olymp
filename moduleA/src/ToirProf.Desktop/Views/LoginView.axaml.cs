using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class LoginView : UserControl
{
    private readonly ApiClient _api;
    private bool _passwordVisible;
    public event Action<LoginResponseDto, bool>? LoginSucceeded;

    public LoginView() : this(new ApiClient()) { }

    public LoginView(ApiClient api)
    {
        InitializeComponent();
        _api = api;

        var settings = SettingsService.Load();
        if (!string.IsNullOrWhiteSpace(settings.SavedEmail))
            EmailBox.Text = settings.SavedEmail;
        RememberCheck.IsChecked = settings.RememberMe;

        EmailBox.TextChanged += (_, _) => ValidateEmailLive();
        PasswordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) OnLoginClick(this, new RoutedEventArgs());
        };
    }

    private bool ValidateEmailLive()
    {
        var email = EmailBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            EmailErrorText.Text = "Введите корректный адрес электронной почты (например, user@pmz.ru)";
            EmailErrorText.IsVisible = true;
            return false;
        }
        EmailErrorText.IsVisible = false;
        return true;
    }

    private void OnTogglePasswordClick(object? sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        PasswordBox.PasswordChar = _passwordVisible ? '\0' : '•';
        TogglePasswordBtn.Content = _passwordVisible ? "🙈" : "👁";
    }

    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        await PerformLoginAsync();
    }

    private async Task PerformLoginAsync()
    {
        ErrorBanner.IsVisible = false;
        if (!ValidateEmailLive()) return;

        var email = EmailBox.Text?.Trim() ?? "";
        var pwd = PasswordBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(pwd))
        {
            ShowError("Введите пароль для входа в систему.");
            return;
        }

        SetBusy(true);
        try
        {
            var remember = RememberCheck.IsChecked == true;
            var res = await _api.LoginAsync(email, pwd, remember);
            if (!res.IsSuccess || res.Data == null)
            {
                var msg = res.StatusCode switch
                {
                    401 => "Ошибка авторизации (401): неверный email или пароль.",
                    403 => "Доступ запрещён (403): учётная запись заблокирована.",
                    500 => "Внутренняя ошибка сервера (500). Обратитесь к администратору.",
                    _ when res.IsNetworkError => $"Нет связи с сервером API ({_api.BaseUrl}). Проверьте запуск сервера.",
                    _ => res.ErrorMessage
                };
                ShowError(msg);
                return;
            }

            _api.SetToken(res.Data.Token);
            LoginSucceeded?.Invoke(res.Data, remember);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        EmailBox.IsEnabled = !busy;
        PasswordBox.IsEnabled = !busy;
        RememberCheck.IsEnabled = !busy;
        LoginSpinner.IsVisible = busy;
        LoginButtonText.Text = busy ? "Выполняется вход..." : "Войти в систему";
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorBanner.IsVisible = true;
    }
}
