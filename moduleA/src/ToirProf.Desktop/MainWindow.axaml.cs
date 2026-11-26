using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;
using ToirProf.Desktop.Views;

namespace ToirProf.Desktop;

public partial class MainWindow : Window, IDesktopHost
{
    public ApiClient Api { get; }
    public DesktopSettings Settings { get; private set; }
    public UserProfileDto? CurrentUser { get; private set; }

    private readonly Dictionary<string, UserControl> _views = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private string _activeSection = "Dashboard";
    private Func<Task>? _retryAction;
    private int _idleSeconds;
    private readonly DispatcherTimer _idleTimer;
    private readonly DispatcherTimer _toastTimer;
    private readonly DispatcherTimer _draftSyncTimer;

    public MainWindow()
    {
        InitializeComponent();
        Settings = SettingsService.Load();
        Api = new ApiClient(Settings.ApiBaseUrl);

        _navButtons["Dashboard"] = NavDashboard;
        _navButtons["WorkOrders"] = NavOrders;
        _navButtons["Equipment"] = NavEquipment;
        _navButtons["Schedules"] = NavSchedules;
        _navButtons["Materials"] = NavMaterials;
        _navButtons["Teams"] = NavTeams;
        _navButtons["Reports"] = NavReports;
        _navButtons["Settings"] = NavSettings;

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleTimer.Tick += OnIdleTick;
        _idleTimer.Start();

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.8) };
        _toastTimer.Tick += (_, _) => { ToastBanner.IsVisible = false; _toastTimer.Stop(); };

        _draftSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _draftSyncTimer.Tick += async (_, _) => await SyncOfflineDraftsAsync();
        _draftSyncTimer.Start();

        AddHandler(PointerMovedEvent, (_, _) => ResetIdle(), RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, (_, _) => ResetIdle(), RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);

        ApplyPreferences();
        ShowLoginOrAutoLogin();
    }

    private async void ShowLoginOrAutoLogin()
    {
        if (Settings.RememberMe && !string.IsNullOrWhiteSpace(Settings.SavedToken))
        {
            Api.SetToken(Settings.SavedToken);
            var me = await Api.GetMeAsync();
            if (me.IsSuccess && me.Data != null)
            {
                EnterShell(me.Data);
                return;
            }
        }
        ShowLoginScreen();
    }

    private void ShowLoginScreen()
    {
        CurrentUser = null;
        ShellGrid.IsVisible = false;
        LockOverlay.IsVisible = false;
        var login = new LoginView(Api);
        login.LoginSucceeded += (resp, remember) =>
        {
            Settings.RememberMe = remember;
            Settings.SavedEmail = resp.User.Email;
            Settings.SavedToken = remember ? resp.Token : null;
            SettingsService.Save(Settings);
            EnterShell(resp.User);
        };
        LoginContainer.Content = login;
        LoginContainer.IsVisible = true;
    }

    private void EnterShell(UserProfileDto user)
    {
        CurrentUser = user;
        _idleSeconds = 0;
        LoginContainer.IsVisible = false;
        ShellGrid.IsVisible = true;

        UserAvatarText.Text = user.Initials;
        UserNameText.Text = user.DisplayFullName;
        UserRoleText.Text = $"{user.RoleName}" + (string.IsNullOrWhiteSpace(user.WorkshopName) ? "" : $" • {user.WorkshopName}");
        LockUserLabel.Text = $"{user.DisplayFullName} ({user.Email})";

        EnsureViews();
        NavigateTo("Dashboard");
    }

    private void EnsureViews()
    {
        if (_views.Count > 0) return;
        _views["Dashboard"] = new DashboardView(this);
        _views["WorkOrders"] = new WorkOrdersView(this);
        _views["Equipment"] = new EquipmentView(this);
        _views["Schedules"] = new SchedulesView(this);
        _views["Materials"] = new MaterialsView(this);
        _views["Teams"] = new TeamsView(this);
        _views["Reports"] = new ReportsView(this);
        _views["Settings"] = new SettingsView(this);
    }
}
