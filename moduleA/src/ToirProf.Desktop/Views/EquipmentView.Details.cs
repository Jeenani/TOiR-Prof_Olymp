using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;

namespace ToirProf.Desktop.Views;

public partial class EquipmentView
{
    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id }) return;
        var eq = _items.FirstOrDefault(x => x.EquipmentId == id);
        if (eq == null) return;
        _deletingId = id;
        DeleteConfirmText.Text = $"Вы действительно хотите удалить оборудование '{eq.EquipmentName}' ({eq.InventoryNumber})?";
        DeleteConfirmModal.IsVisible = true;
    }

    private void OnCancelDelete(object? sender, RoutedEventArgs e) => DeleteConfirmModal.IsVisible = false;

    private async void OnConfirmDelete(object? sender, RoutedEventArgs e)
    {
        if (_deletingId == null) return;
        DeleteConfirmModal.IsVisible = false;
        _host.SetLoading(true, "Удаление оборудования...");
        try
        {
            var res = await _host.Api.DeleteEquipmentAsync(_deletingId.Value);
            if (!res.IsSuccess)
            {
                _host.ShowErrorModal(res.StatusCode == 409 ? "Удаление заблокировано (409 Conflict)" : "Ошибка удаления", res.ErrorMessage);
                return;
            }
            _host.ShowToast("Оборудование удалено.");
            await LoadAsync();
        }
        finally { _host.SetLoading(false); }
    }

    private async void OnDetailClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id }) return;
        _detailEqId = id;
        _host.SetLoading(true, "Загрузка карточки оборудования...");
        try
        {
            var res = await _host.Api.GetEquipmentDetailAsync(id);
            if (!res.IsSuccess || res.Data == null)
            {
                _host.ShowErrorModal("Ошибка", res.ErrorMessage);
                return;
            }
            var eq = res.Data;
            _host.SetBreadcrumb("Оперативный учёт", "Оборудование", eq.InventoryNumber);
            DetailTitle.Text = $"{eq.InventoryNumber} — {eq.EquipmentName}";
            DetailSubtitle.Text = $"{eq.WorkshopName} • Статус: {eq.StatusName}";
            DetailInfoText.Text =
                $"Модель: {eq.ModelName}\nПроизводитель: {eq.Manufacturer ?? "—"}\nСерийный №: {eq.SerialNumber ?? "—"}\n" +
                $"Цех: {eq.WorkshopName}\nТекущий статус: {eq.StatusName}\n" +
                $"Дата ввода в эксплуатацию: {eq.CommissioningDate}\nДата ввода в работу: {eq.OperationDate}\n" +
                $"Гарантия до: {eq.WarrantyUntil ?? "—"}\nПаспорт: {eq.PassportUrl ?? "—"}";
            DetailOrdersGrid.ItemsSource = eq.RecentWorkOrders;
            DetailSchedulesGrid.ItemsSource = eq.Schedules;
            DetailHistoryGrid.ItemsSource = eq.StatusHistory;
            DetailModal.IsVisible = true;
        }
        finally { _host.SetLoading(false); }
    }

    private void OnCloseDetailModal(object? sender, RoutedEventArgs e)
    {
        DetailModal.IsVisible = false;
        _host.SetBreadcrumb("Оперативный учёт", "Оборудование");
    }

    private void OnCreateOrderForEq(object? sender, RoutedEventArgs e)
    {
        DetailModal.IsVisible = false;
        _host.NavigateTo("WorkOrders", _detailEqId, true);
    }
}
