using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class MaterialsView : UserControl, IPageView
{
    private readonly IDesktopHost _host;
    private List<WarehouseDto> _warehouses = new();
    private List<MaterialItemDto> _allMaterials = new();
    private MaterialItemDto? _selectedMaterial;

    public MaterialsView() : this(null!) { }

    public MaterialsView(IDesktopHost host)
    {
        InitializeComponent();
        _host = host;
    }

    public async Task LoadAsync()
    {
        if (_host == null) return;
        _host.SetBreadcrumb("Планирование и ресурсы", "Материалы и склады");
        _host.SetLoading(true, "Загрузка складских остатков...");
        try
        {
            if (_warehouses.Count == 0)
            {
                var whRes = await _host.Api.GetWarehousesAsync();
                if (whRes.IsSuccess && whRes.Data != null)
                {
                    _warehouses = new List<WarehouseDto> { new() { WarehouseId = 0, WarehouseName = "Все склады" } };
                    _warehouses.AddRange(whRes.Data);
                    WarehouseFilter.ItemsSource = _warehouses.Select(w => w.WarehouseName).ToList();
                    WarehouseFilter.SelectedIndex = 0;
                }
            }

            int? whId = WarehouseFilter.SelectedIndex > 0 && WarehouseFilter.SelectedIndex < _warehouses.Count
                ? _warehouses[WarehouseFilter.SelectedIndex].WarehouseId
                : null;
            bool? lowOnly = LowStockCheck.IsChecked == true ? true : null;

            var matRes = await _host.Api.GetMaterialsAsync(whId, lowOnly);
            if (!matRes.IsSuccess || matRes.Data == null)
            {
                _host.ShowErrorModal("Ошибка загрузки материалов", matRes.ErrorMessage, LoadAsync);
                return;
            }
            _allMaterials = matRes.Data;
            ApplyLocalSearch();

            var supRes = await _host.Api.GetSuppliersAsync();
            if (supRes.IsSuccess && supRes.Data != null)
                SuppliersGrid.ItemsSource = supRes.Data;
        }
        finally
        {
            _host.SetLoading(false);
        }
    }

    private void ApplyLocalSearch()
    {
        var q = SearchBox.Text?.Trim().ToLower() ?? "";
        var filtered = string.IsNullOrEmpty(q)
            ? _allMaterials
            : _allMaterials.Where(m =>
                m.MaterialName.ToLower().Contains(q) ||
                m.MaterialCode.ToLower().Contains(q) ||
                (m.SupplierName?.ToLower().Contains(q) ?? false)).ToList();

        MaterialsGrid.ItemsSource = filtered;
    }

    public void FocusSearch() => SearchBox.Focus();

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => ApplyLocalSearch();
    private async void OnFilterChanged(object? sender, SelectionChangedEventArgs e) { if (IsVisible && _warehouses.Count > 0) await LoadAsync(); }
    private async void OnLowStockChanged(object? sender, RoutedEventArgs e) { if (IsVisible) await LoadAsync(); }
    private async void OnRefreshClick(object? sender, RoutedEventArgs e) => await LoadAsync();

    private void OnAdjustClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MaterialItemDto item })
        {
            _selectedMaterial = item;
            AdjustTargetText.Text = $"{item.MaterialName}\nСклад: {item.WarehouseName} (текущий остаток: {item.CurrentStock} {item.UnitOfMeasure})";
            AdjustDeltaBox.Text = "5";
            AdjustReasonBox.Text = "";
            AdjustModal.IsVisible = true;
        }
    }

    private void OnCancelAdjust(object? sender, RoutedEventArgs e) => AdjustModal.IsVisible = false;

    private async void OnConfirmAdjust(object? sender, RoutedEventArgs e)
    {
        if (_selectedMaterial == null) return;
        if (!decimal.TryParse((AdjustDeltaBox.Text ?? "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var delta) || delta == 0)
        {
            _host.ShowToast("Введите ненулевое числовое значение изменения.", true);
            return;
        }

        _host.SetLoading(true, "Обновление остатка...");
        try
        {
            var res = await _host.Api.AdjustMaterialStockAsync(_selectedMaterial.MaterialId, _selectedMaterial.WarehouseId, delta, AdjustReasonBox.Text);
            if (!res.IsSuccess)
            {
                _host.ShowErrorModal("Ошибка корректировки", res.ErrorMessage);
                return;
            }
            AdjustModal.IsVisible = false;
            _host.ShowToast("Складской остаток обновлён.");
            await LoadAsync();
        }
        finally
        {
            _host.SetLoading(false);
        }
    }
}
