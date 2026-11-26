using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public class ChartBarVm
{
    public string Label { get; set; } = "";
    public string ValueText { get; set; } = "";
    public double BarWidth { get; set; }
    public IBrush BarColor { get; set; } = Brushes.RoyalBlue;
}

public partial class ReportsView : UserControl, IPageView
{
    private readonly IDesktopHost _host;
    private List<WorkshopDto> _workshops = new();
    private List<ReportRowItem> _rows = new();
    private string _currentTitle = "Отчёт ТОиР-Проф";
    private string[] _headers = { "Объект", "Подразделение", "Значение", "Ед." };

    public ReportsView() : this(null!) { }
    public ReportsView(IDesktopHost host) { InitializeComponent(); _host = host; }

    public async Task LoadAsync()
    {
        if (_host == null) return;
        _host.SetBreadcrumb("Аналитика и система", "Отчёты");
        if (_workshops.Count == 0)
        {
            var res = await _host.Api.GetWorkshopsAsync();
            if (res.IsSuccess && res.Data != null)
            {
                _workshops = res.Data;
                WorkshopBox.ItemsSource = _workshops.Select(w => w.WorkshopName).ToList();
                WorkshopBox.SelectedIndex = 0;
            }
        }
        RefreshTemplatesDropdown();
        await GenerateAsync();
    }

    private string GetSelectedReportType() => (ReportTypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "downtime";
    private int GetSelectedPeriodDays() => int.TryParse((PeriodBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var d) ? d : 30;
    private async void OnGenerateClick(object? sender, RoutedEventArgs e) => await GenerateAsync();

    private async Task GenerateAsync()
    {
        var rType = GetSelectedReportType();
        var days = GetSelectedPeriodDays();
        var wsIdx = Math.Max(0, WorkshopBox.SelectedIndex);
        var wsId = _workshops.Count > wsIdx ? _workshops[wsIdx].WorkshopId : 1;
        var wsName = _workshops.Count > wsIdx ? _workshops[wsIdx].WorkshopName : "Цех";

        _host.SetLoading(true, "Формирование отчёта...");
        try
        {
            _rows.Clear();
            if (rType == "downtime")
            {
                _currentTitle = "Отчёт по простоям оборудования (Топ-5)";
                _headers = new[] { "Оборудование", "Цех", "Простой (ч)", "Инв. №" };
                var dash = await _host.Api.GetDashboardAsync(wsId);
                if (dash.IsSuccess && dash.Data != null)
                    _rows = dash.Data.Top5Downtime.Select(x => new ReportRowItem { Col1 = x.EquipmentName, Col2 = x.WorkshopName, Col3 = $"{x.DowntimeHours:0.##}", Col4 = x.InventoryNumber, NumericValue = (double)x.DowntimeHours }).ToList();
            }
            else if (rType == "to")
            {
                _currentTitle = "Отчёт по выполнению графика ТО";
                _headers = new[] { "Оборудование", "Вид ТО", "План. дата", "Статус" };
                var sch = await _host.Api.GetSchedulesAsync(wsId);
                if (sch.IsSuccess && sch.Data != null)
                    _rows = sch.Data.Select(s => new ReportRowItem { Col1 = $"{s.InventoryNumber} — {s.EquipmentName}", Col2 = s.TypeCode, Col3 = s.PlannedDate, Col4 = s.StatusBadgeText, NumericValue = s.ScheduleStatus == "completed" ? 100 : s.IsOverdue ? 25 : 60 }).ToList();
            }
            else if (rType == "workshop")
            {
                _currentTitle = $"Сводный отчёт по подразделению: {wsName}";
                _headers = new[] { "Метрика", "Цех", "Значение", "Единица" };
                var rep = await _host.Api.GetWorkshopSummaryAsync(wsId, DateTime.UtcNow.AddDays(-days), DateTime.UtcNow);
                if (rep.IsSuccess && rep.Data != null)
                    _rows = rep.Data.Select(m => new ReportRowItem { Col1 = m.MetricName, Col2 = wsName, Col3 = $"{m.MetricValue:0.##}", Col4 = m.Unit, NumericValue = (double)m.MetricValue }).ToList();
            }
            else
            {
                _currentTitle = "Ведомость складских остатков и стоимости";
                _headers = new[] { "Материал", "Склад", "Остаток", "Стоимость (₽)" };
                var mats = await _host.Api.GetMaterialsAsync();
                if (mats.IsSuccess && mats.Data != null)
                    _rows = mats.Data.Select(m => new ReportRowItem { Col1 = m.MaterialName, Col2 = m.WarehouseName, Col3 = $"{m.CurrentStock} {m.UnitOfMeasure}", Col4 = $"{(m.CurrentStock * m.UnitPrice):N2}", NumericValue = (double)(m.CurrentStock * m.UnitPrice) }).ToList();
            }

            PreviewGrid.Columns[0].Header = _headers[0];
            PreviewGrid.Columns[1].Header = _headers[1];
            PreviewGrid.Columns[2].Header = _headers[2];
            PreviewGrid.Columns[3].Header = _headers[3];
            PreviewTitle.Text = _currentTitle;
            PreviewGrid.ItemsSource = _rows.ToList();

            var maxVal = Math.Max(1.0, _rows.Select(r => r.NumericValue).DefaultIfEmpty(1.0).Max());
            ChartBarsList.ItemsSource = _rows.Take(10).Select((r, i) => new ChartBarVm
            {
                Label = r.Col1, ValueText = string.IsNullOrWhiteSpace(r.Col3) ? r.Col4 : r.Col3,
                BarWidth = Math.Max(14, (r.NumericValue / maxVal) * 280.0),
                BarColor = new SolidColorBrush(Color.Parse(i % 2 == 0 ? "#1D4ED8" : "#0F766E"))
            }).ToList();
        }
        finally { _host.SetLoading(false); }
    }
}
