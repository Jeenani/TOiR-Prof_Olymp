using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class WorkOrdersView : UserControl, IPageView
{
    private readonly IDesktopHost _host;
    private List<WorkshopDto> _workshops = new();
    private List<WorkOrderStatusDto> _statuses = new();
    private List<WorkOrderPriorityDto> _priorities = new();
    private List<EquipmentItemDto> _equipments = new();
    private List<TeamDto> _teams = new();
    private List<MaterialItemDto> _materials = new();
    private int _page = 1;
    private int _pageSize = 25;
    private int _totalPages = 1;
    private string _sortBy = "createdAt";
    private bool _sortDesc = true;
    private WorkOrderDetailDto? _currentDetail;

    public WorkOrdersView() : this(null!) { }

    public WorkOrdersView(IDesktopHost host)
    {
        InitializeComponent();
        _host = host;
    }

    public async Task LoadAsync()
    {
        if (_host == null) return;
        _host.SetBreadcrumb("Оперативный учёт", "Заявки на ремонт");
        OrdersGrid.Columns[6].IsVisible = _host.Settings.ShowTeamColumn;
        _host.SetLoading(true, "Загрузка заявок...");
        try
        {
            await EnsureDictionariesAsync();
            int? wsId = WorkshopFilter.SelectedIndex > 0 ? _workshops[WorkshopFilter.SelectedIndex - 1].WorkshopId : null;
            int? stId = StatusFilter.SelectedIndex > 0 ? _statuses[StatusFilter.SelectedIndex - 1].OrderStatusId : null;
            int? prId = PriorityFilter.SelectedIndex > 0 ? _priorities[PriorityFilter.SelectedIndex - 1].PriorityId : null;

            var res = await _host.Api.GetWorkOrdersAsync(stId, prId, wsId, SearchBox.Text, _sortBy, _sortDesc, _page, _pageSize);
            if (!res.IsSuccess || res.Data == null)
            {
                _host.ShowErrorModal("Ошибка загрузки заявок", res.ErrorMessage, LoadAsync);
                return;
            }

            var items = res.Data.Items;
            if (DateOnly.TryParse(DateFromBox.Text, out var df))
                items = items.Where(x => DateOnly.FromDateTime(x.CreatedAt) >= df || DateOnly.FromDateTime(x.PlannedStartDate) >= df).ToList();

            OrdersGrid.ItemsSource = items;
            _totalPages = Math.Max(1, res.Data.TotalPages);
            PagingInfo.Text = $"Всего заявок: {res.Data.Total}";
            PageLabel.Text = $"Стр. {_page} / {_totalPages}";
            PrevBtn.IsEnabled = _page > 1;
            NextBtn.IsEnabled = _page < _totalPages;
        }
        finally { _host.SetLoading(false); }
    }

    private async Task EnsureDictionariesAsync()
    {
        if (_workshops.Count == 0)
        {
            var r = await _host.Api.GetWorkshopsAsync();
            if (r.IsSuccess && r.Data != null)
            {
                _workshops = r.Data;
                var list = new List<string> { "Все цехи" };
                list.AddRange(_workshops.Select(w => w.WorkshopName));
                WorkshopFilter.ItemsSource = list;
                WorkshopFilter.SelectedIndex = 0;
            }
        }
        if (_statuses.Count == 0)
        {
            var r = await _host.Api.GetWorkOrderStatusesAsync();
            if (r.IsSuccess && r.Data != null)
            {
                _statuses = r.Data;
                var list = new List<string> { "Все статусы" };
                list.AddRange(_statuses.Select(s => s.StatusName));
                StatusFilter.ItemsSource = list;
                StatusFilter.SelectedIndex = 0;
            }
        }
        if (_priorities.Count == 0)
        {
            var r = await _host.Api.GetWorkOrderPrioritiesAsync();
            if (r.IsSuccess && r.Data != null)
            {
                _priorities = r.Data;
                var list = new List<string> { "Все приоритеты" };
                list.AddRange(_priorities.Select(p => p.PriorityName));
                PriorityFilter.ItemsSource = list;
                PriorityFilter.SelectedIndex = 0;
                CreatePriorityBox.ItemsSource = _priorities.Select(p => p.PriorityName).ToList();
                CreatePriorityBox.SelectedIndex = Math.Min(1, _priorities.Count - 1);
            }
        }
    }

    public void FocusSearch() => SearchBox.Focus();
    public void TriggerNew() => OpenCreateModal();
    private async void OnSearchClick(object? sender, RoutedEventArgs e) { _page = 1; await LoadAsync(); }
    private async void OnFilterChanged(object? sender, SelectionChangedEventArgs e) { if (IsVisible && _workshops.Count > 0) { _page = 1; await LoadAsync(); } }
    private async void OnResetClick(object? sender, RoutedEventArgs e) { SearchBox.Text = ""; DateFromBox.Text = ""; WorkshopFilter.SelectedIndex = 0; StatusFilter.SelectedIndex = 0; PriorityFilter.SelectedIndex = 0; _page = 1; await LoadAsync(); }
    private async void OnPageSizeChanged(object? sender, SelectionChangedEventArgs e) { if (PageSizeBox.SelectedItem is ComboBoxItem it && int.TryParse(it.Content?.ToString(), out var ps)) { _pageSize = ps; _page = 1; if (IsVisible) await LoadAsync(); } }
    private async void OnPrevPage(object? sender, RoutedEventArgs e) { if (_page > 1) { _page--; await LoadAsync(); } }
    private async void OnNextPage(object? sender, RoutedEventArgs e) { if (_page < _totalPages) { _page++; await LoadAsync(); } }
    private async void OnGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        var key = (e.Column.Header?.ToString()) switch { "Номер" => "ordernumber", "Приоритет" => "priority", "Статус" => "status", _ => "createdAt" };
        if (_sortBy == key) _sortDesc = !_sortDesc; else { _sortBy = key; _sortDesc = false; }
        e.Handled = true;
        await LoadAsync();
    }
}
