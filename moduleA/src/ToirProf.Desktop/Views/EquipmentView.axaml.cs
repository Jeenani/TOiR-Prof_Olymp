using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class EquipmentView : UserControl, IPageView
{
    private readonly IDesktopHost _host;
    private List<WorkshopDto> _workshops = new();
    private List<EquipmentStatusDto> _statuses = new();
    private List<EquipmentItemDto> _items = new();
    private int _page = 1;
    private int _pageSize = 25;
    private int _totalPages = 1;
    private string _sortBy = "inventoryNumber";
    private bool _sortDesc;
    private int? _editingId;
    private int? _deletingId;
    private int? _detailEqId;
    private bool _isReady;
    private bool _isLoading;

    public EquipmentView() : this(null!) { }

    public EquipmentView(IDesktopHost host)
    {
        InitializeComponent();
        _host = host;
        _isReady = true;
    }

    public async Task LoadAsync()
    {
        if (_host == null || _isLoading) return;
        _isLoading = true;
        _host.SetBreadcrumb("Оперативный учёт", "Оборудование");
        ApplyColumnSettings();
        _host.SetLoading(true, "Загрузка оборудования...");
        try
        {
            if (_workshops.Count == 0)
            {
                var wsRes = await _host.Api.GetWorkshopsAsync();
                if (wsRes.IsSuccess && wsRes.Data != null)
                {
                    _workshops = wsRes.Data;
                    var filterList = new List<string> { "Все цехи" };
                    filterList.AddRange(_workshops.Select(w => w.WorkshopName));
                    WorkshopFilter.ItemsSource = filterList;
                    WorkshopFilter.SelectedIndex = 0;
                    FormWorkshopBox.ItemsSource = _workshops.Select(w => w.WorkshopName).ToList();
                }
            }
            if (_statuses.Count == 0)
            {
                var stRes = await _host.Api.GetEquipmentStatusesAsync();
                if (stRes.IsSuccess && stRes.Data != null)
                {
                    _statuses = stRes.Data;
                    var stList = new List<string> { "Все статусы" };
                    stList.AddRange(_statuses.Select(s => s.StatusName));
                    StatusFilter.ItemsSource = stList;
                    StatusFilter.SelectedIndex = 0;
                    FormStatusBox.ItemsSource = _statuses.Select(s => s.StatusName).ToList();
                }
            }

            int? wsId = WorkshopFilter.SelectedIndex > 0 ? _workshops[WorkshopFilter.SelectedIndex - 1].WorkshopId : null;
            int? stId = StatusFilter.SelectedIndex > 0 ? _statuses[StatusFilter.SelectedIndex - 1].StatusId : null;
            var res = await _host.Api.GetEquipmentAsync(wsId, stId, ModelFilter.Text, SearchBox.Text, _sortBy, _sortDesc, _page, _pageSize);
            if (!res.IsSuccess || res.Data == null)
            {
                _host.ShowErrorModal("Ошибка загрузки оборудования", res.ErrorMessage, LoadAsync);
                return;
            }

            _items = res.Data.Items;
            _totalPages = Math.Max(1, res.Data.TotalPages);
            EquipmentGrid.ItemsSource = _items;
            PagingInfo.Text = $"Всего единиц: {res.Data.Total}";
            PageLabel.Text = $"Стр. {_page} / {_totalPages}";
            PrevBtn.IsEnabled = _page > 1;
            NextBtn.IsEnabled = _page < _totalPages;
        }
        finally
        {
            _isLoading = false;
            _host.SetLoading(false);
        }
    }

    public void ApplyColumnSettings()
    {
        if (_host == null) return;
        EquipmentGrid.Columns[3].IsVisible = _host.Settings.ShowManufacturerColumn;
        EquipmentGrid.Columns[4].IsVisible = _host.Settings.ShowSerialColumn;
    }

    public void FocusSearch() => SearchBox.Focus();
    public void TriggerNew() => OnAddClick(this, new RoutedEventArgs());

    private async void OnSearchClick(object? sender, RoutedEventArgs e) { if (!_isReady || _host == null) return; _page = 1; await LoadAsync(); }
    private async void OnFilterChanged(object? sender, SelectionChangedEventArgs e) { if (_isReady && !_isLoading && IsVisible && _workshops.Count > 0) { _page = 1; await LoadAsync(); } }
    private async void OnResetClick(object? sender, RoutedEventArgs e)
    {
        if (!_isReady || _host == null) return;
        SearchBox.Text = "";
        ModelFilter.Text = "";
        WorkshopFilter.SelectedIndex = 0;
        StatusFilter.SelectedIndex = 0;
        _page = 1;
        await LoadAsync();
    }

    private async void OnPageSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isReady || _host == null || PageSizeBox == null) return;
        if (PageSizeBox.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var ps))
        {
            _pageSize = ps;
            _page = 1;
            if (IsVisible) await LoadAsync();
        }
    }

    private async void OnPrevPage(object? sender, RoutedEventArgs e) { if (_page > 1) { _page--; await LoadAsync(); } }
    private async void OnNextPage(object? sender, RoutedEventArgs e) { if (_page < _totalPages) { _page++; await LoadAsync(); } }

    private async void OnGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        var header = e.Column.Header?.ToString() ?? "";
        var key = header switch
        {
            "Наименование" => "name",
            "Статус" => "status",
            "Цех" => "workshop",
            _ => "inventoryNumber"
        };
        if (_sortBy == key) _sortDesc = !_sortDesc;
        else { _sortBy = key; _sortDesc = false; }
        e.Handled = true;
        await LoadAsync();
    }
}
