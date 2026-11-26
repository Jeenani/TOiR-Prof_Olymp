using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public class UpcomingToItemVm
{
    public string Header { get; set; } = "";
    public string DateLabel { get; set; } = "";
    public string SubTitle { get; set; } = "";
    public IBrush CardBg { get; set; } = Brushes.White;
    public IBrush TextColor { get; set; } = Brushes.Black;
}

public partial class DashboardView : UserControl, IPageView
{
    private readonly IDesktopHost _host;
    private readonly DispatcherTimer _autoRefreshTimer;

    public DashboardView() : this(null!) { }

    public DashboardView(IDesktopHost host)
    {
        InitializeComponent();
        _host = host;
        _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _autoRefreshTimer.Tick += async (_, _) =>
        {
            if (IsVisible) await LoadAsync();
        };
        _autoRefreshTimer.Start();
    }

    public async Task LoadAsync()
    {
        if (_host == null) return;
        _host.SetBreadcrumb("Главная", "Оперативная сводка");
        _host.SetLoading(true, "Загрузка сводки...");
        try
        {
            var dashRes = await _host.Api.GetDashboardAsync();
            if (!dashRes.IsSuccess || dashRes.Data == null)
            {
                _host.ShowErrorModal("Ошибка загрузки дашборда", dashRes.ErrorMessage, LoadAsync);
                return;
            }

            var d = dashRes.Data;
            var inRepair = d.EquipmentByStatus.FirstOrDefault(s => s.StatusCode == "in_repair")?.Count ?? 0;
            var downtimeEq = d.EquipmentByStatus.FirstOrDefault(s => s.StatusCode == "downtime")?.Count ?? 0;

            KpiTotalEq.Text = d.TotalEquipment.ToString();
            KpiEqTrend.Text = $"в ремонте: {inRepair} | простой: {downtimeEq}";
            KpiActiveOrders.Text = d.ActiveWorkOrders.ToString();
            KpiOrdersTrend.Text = $"▲ завершено всего: {d.CompletedWorkOrders}";

            var today = DateTime.Today;
            var todayCount = d.RecentWorkOrders.Count(o => o.PlannedStartDate.Date == today || o.CreatedAt.Date == today);
            KpiTodayOrders.Text = todayCount.ToString();
            KpiTodayTrend.Text = $"суммарный простой: {d.TotalDowntimeHours:0.##} ч.";

            KpiAvgRepair.Text = $"{d.AverageRepairHours:0.0} ч.";
            KpiComplianceTrend.Text = $"▲ График ТО: {d.MaintenanceComplianceRate:0.0}%";

            RecentOrdersGrid.ItemsSource = d.RecentWorkOrders.Take(10).ToList();
            _host.UpdateNotifications(d.Violations);

            var schedRes = await _host.Api.GetSchedulesAsync();
            if (schedRes.IsSuccess && schedRes.Data != null)
            {
                var limit = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
                var items = schedRes.Data
                    .Where(s => s.ScheduleStatus == "planned" && s.ParsedDate <= limit)
                    .OrderBy(s => s.ParsedDate)
                    .Take(7)
                    .Select(s => new UpcomingToItemVm
                    {
                        Header = $"{s.InventoryNumber} — {s.TypeCode}",
                        DateLabel = s.IsOverdue ? $"ПРОСРОЧЕНО ({s.ParsedDate:dd.MM})" : $"{s.ParsedDate:dd.MM.yyyy}",
                        SubTitle = $"{s.EquipmentName} ({s.WorkshopName})",
                        CardBg = s.IsOverdue ? new SolidColorBrush(Color.Parse("#FEE2E2")) :
                                 s.IsSoon ? new SolidColorBrush(Color.Parse("#FEF3C7")) :
                                            new SolidColorBrush(Color.Parse("#F8FAFC")),
                        TextColor = s.IsOverdue ? new SolidColorBrush(Color.Parse("#B91C1C")) :
                                    s.IsSoon ? new SolidColorBrush(Color.Parse("#B45309")) :
                                               new SolidColorBrush(Color.Parse("#0F172A"))
                    }).ToList();

                if (items.Count == 0)
                {
                    items.Add(new UpcomingToItemVm
                    {
                        Header = "Ближайших ТО нет",
                        DateLabel = "OK",
                        SubTitle = "На ближайшие 7 дней плановых работ не найдено.",
                        CardBg = new SolidColorBrush(Color.Parse("#ECFDF5")),
                        TextColor = new SolidColorBrush(Color.Parse("#047857"))
                    });
                }
                UpcomingToList.ItemsSource = items;
            }

            LastUpdatedText.Text = $"Последнее обновление: {DateTime.Now:HH:mm:ss} (автообновление каждые 60 сек.)";
        }
        finally
        {
            _host.SetLoading(false);
        }
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e) => await LoadAsync();
    private void OnNewOrderClick(object? sender, RoutedEventArgs e) => _host.NavigateTo("WorkOrders", null, true);
    private void OnAllOrdersClick(object? sender, RoutedEventArgs e) => _host.NavigateTo("WorkOrders");
    private void OnAllSchedulesClick(object? sender, RoutedEventArgs e) => _host.NavigateTo("Schedules");

    private void OnOrderDetailsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id })
            _host.NavigateTo("WorkOrders", id);
    }
}
