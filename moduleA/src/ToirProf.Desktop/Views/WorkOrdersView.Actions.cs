using Avalonia.Interactivity;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace ToirProf.Desktop.Views;

public partial class WorkOrdersView
{
    private async void OnAssignTeamClick(object? sender, RoutedEventArgs e)
    {
        if (_currentDetail == null || AssignTeamBox.SelectedIndex < 0) return;
        var team = _teams[AssignTeamBox.SelectedIndex];
        _host.SetLoading(true, "Назначение бригады...");
        try
        {
            var res = await _host.Api.AssignTeamAsync(_currentDetail.WorkOrderId, team.TeamId);
            if (!res.IsSuccess)
            {
                _host.ShowErrorModal(res.StatusCode == 409 ? "Конфликт занятости бригады (409)" : "Ошибка назначения", res.ErrorMessage);
                return;
            }
            _host.ShowToast($"Бригада «{team.TeamName}» назначена.");
            await OpenDetailAsync(_currentDetail.WorkOrderId);
            await LoadAsync();
        }
        finally { _host.SetLoading(false); }
    }

    private async void OnAddMaterialClick(object? sender, RoutedEventArgs e)
    {
        if (_currentDetail == null || AddMaterialBox.SelectedIndex < 0) return;
        if (!decimal.TryParse((AddMaterialQtyBox.Text ?? "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
        {
            _host.ShowToast("Укажите корректное количество (> 0).", true);
            return;
        }
        var mat = _materials[AddMaterialBox.SelectedIndex];
        _host.SetLoading(true, "Добавление материала к заявке...");
        try
        {
            var res = await _host.Api.AddOrderMaterialAsync(_currentDetail.WorkOrderId, mat.WarehouseId, mat.MaterialId, qty);
            if (!res.IsSuccess) { _host.ShowErrorModal("Ошибка", res.ErrorMessage); return; }
            _host.ShowToast($"Материал «{mat.MaterialName}» добавлен.");
            await OpenDetailAsync(_currentDetail.WorkOrderId);
        }
        finally { _host.SetLoading(false); }
    }

    private async void OnTakeInWorkClick(object? sender, RoutedEventArgs e) => await ChangeOrderStatusAsync(3, "Заявка переведена в работу.");
    private async void OnCompleteOrderClick(object? sender, RoutedEventArgs e) => await ChangeOrderStatusAsync(5, "Работы завершены, материалы списаны.");
    private async void OnCloseOrderClick(object? sender, RoutedEventArgs e) => await ChangeOrderStatusAsync(6, "Заявка закрыта мастером.");

    private async Task ChangeOrderStatusAsync(int statusId, string okMsg)
    {
        if (_currentDetail == null) return;
        _host.SetLoading(true, "Обновление статуса заявки...");
        try
        {
            var res = await _host.Api.UpdateOrderStatusAsync(_currentDetail.WorkOrderId, statusId, ReportBox.Text);
            if (!res.IsSuccess) { _host.ShowErrorModal("Ошибка смены статуса", res.ErrorMessage); return; }
            _host.ShowToast(okMsg);
            await OpenDetailAsync(_currentDetail.WorkOrderId);
            await LoadAsync();
        }
        finally { _host.SetLoading(false); }
    }
}
