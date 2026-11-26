using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class SchedulesView : UserControl, IPageView
{
    private readonly IDesktopHost _host;
    private int _year = DateTime.Today.Year;
    private int _month = DateTime.Today.Month;
    private DateOnly? _selectedDay;
    private List<ScheduleItemDto> _monthItems = new();
    private List<EquipmentItemDto> _eqList = new();
    private List<MaintenanceTypeDto> _types = new();
    private List<TeamDto> _teams = new();

    public SchedulesView() : this(null!) { }

    public SchedulesView(IDesktopHost host)
    {
        InitializeComponent();
        _host = host;
    }

    public async Task LoadAsync()
    {
        if (_host == null) return;
        _host.SetBreadcrumb("Планирование и ресурсы", "График ТО");
        _host.SetLoading(true, "Загрузка графика ТО...");
        try
        {
            var res = await _host.Api.GetSchedulesAsync(null, _month, _year);
            if (!res.IsSuccess || res.Data == null)
            {
                _host.ShowErrorModal("Ошибка загрузки графика ТО", res.ErrorMessage, LoadAsync);
                return;
            }
            _monthItems = res.Data;
            RenderCalendar();
            ApplyDayFilter();
        }
        finally { _host.SetLoading(false); }
    }

    private void RenderCalendar()
    {
        var ru = new CultureInfo("ru-RU");
        MonthTitle.Text = ru.DateTimeFormat.GetMonthName(_month).ToUpper() + $" {_year}";
        CalendarGrid.Children.Clear();

        var first = new DateOnly(_year, _month, 1);
        int offset = ((int)first.DayOfWeek + 6) % 7;
        int days = DateTime.DaysInMonth(_year, _month);

        for (int i = 0; i < offset; i++)
            CalendarGrid.Children.Add(new Border());

        for (int day = 1; day <= days; day++)
        {
            var date = new DateOnly(_year, _month, day);
            var dayItems = _monthItems.Where(s => s.ParsedDate == date).ToList();
            bool hasOverdue = dayItems.Any(x => x.IsOverdue);
            bool hasSoon = dayItems.Any(x => x.IsSoon);
            bool isSelected = _selectedDay == date;

            var bg = isSelected ? Color.Parse("#1E40AF") :
                     hasOverdue ? Color.Parse("#FEE2E2") :
                     hasSoon ? Color.Parse("#FEF3C7") :
                     dayItems.Count > 0 ? Color.Parse("#DBEAFE") : Color.Parse("#F8FAFC");
            var fg = isSelected ? Colors.White :
                     hasOverdue ? Color.Parse("#B91C1C") :
                     hasSoon ? Color.Parse("#B45309") : Color.Parse("#0F172A");

            var btn = new Button
            {
                Margin = new Avalonia.Thickness(2),
                Padding = new Avalonia.Thickness(4),
                Height = 46,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(fg),
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = day.ToString(), FontWeight = FontWeight.Bold, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock { Text = dayItems.Count > 0 ? $"ТО: {dayItems.Count}" : "", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center }
                    }
                }
            };
            btn.Click += (_, _) =>
            {
                _selectedDay = _selectedDay == date ? null : date;
                RenderCalendar();
                ApplyDayFilter();
            };
            CalendarGrid.Children.Add(btn);
        }
    }

    private void ApplyDayFilter()
    {
        if (_selectedDay.HasValue)
        {
            TableHeaderText.Text = $"Записи на {_selectedDay.Value:dd.MM.yyyy}";
            SchedulesGrid.ItemsSource = _monthItems.Where(s => s.ParsedDate == _selectedDay.Value).ToList();
        }
        else
        {
            TableHeaderText.Text = $"Все записи за месяц ({_monthItems.Count})";
            SchedulesGrid.ItemsSource = _monthItems;
        }
    }

    private void OnScheduleLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is ScheduleItemDto item)
        {
            if (item.IsOverdue) e.Row.Background = new SolidColorBrush(Color.Parse("#30EF4444"));
            else if (item.IsSoon) e.Row.Background = new SolidColorBrush(Color.Parse("#30F59E0B"));
            else e.Row.Background = Brushes.Transparent;
        }
    }
}
