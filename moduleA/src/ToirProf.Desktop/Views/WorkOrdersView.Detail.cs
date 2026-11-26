using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Views;

public partial class WorkOrdersView
{
    private async void OnOpenOrderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id }) await OpenDetailAsync(id);
    }

    public async Task OpenDetailAsync(int workOrderId)
    {
        _host.SetLoading(true, "Загрузка карточки заявки...");
        try
        {
            var res = await _host.Api.GetWorkOrderDetailAsync(workOrderId);
            if (!res.IsSuccess || res.Data == null) { _host.ShowErrorModal("Ошибка", res.ErrorMessage); return; }
            _currentDetail = res.Data;
            _host.SetBreadcrumb("Оперативный учёт", "Заявки", _currentDetail.OrderNumber);

            var tmRes = await _host.Api.GetTeamsAsync();
            if (tmRes.IsSuccess && tmRes.Data != null) _teams = tmRes.Data;
            var matRes = await _host.Api.GetMaterialsAsync();
            if (matRes.IsSuccess && matRes.Data != null) _materials = matRes.Data;

            AssignTeamBox.ItemsSource = _teams.Select(t => $"{t.TeamName} (акт.: {t.ActiveOrdersCount})").ToList();
            if (_currentDetail.AssignedTeamId.HasValue)
                AssignTeamBox.SelectedIndex = Math.Max(0, _teams.FindIndex(t => t.TeamId == _currentDetail.AssignedTeamId.Value));
            else if (_teams.Count > 0) AssignTeamBox.SelectedIndex = 0;

            AddMaterialBox.ItemsSource = _materials.Select(m => m.DisplayWithWarehouse).ToList();
            if (_materials.Count > 0) AddMaterialBox.SelectedIndex = 0;

            DetailHeader.Text = $"Заявка {_currentDetail.OrderNumber} ({_currentDetail.OrderTypeRu})";
            DetailSub.Text = $"{_currentDetail.InventoryNumber} — {_currentDetail.EquipmentName} • {_currentDetail.WorkshopName}";
            DetailStatusBadge.Text = $"Статус: {_currentDetail.StatusName.ToUpper()}";
            TimelineText.Text = BuildTimeline(_currentDetail);
            DetailInfoText.Text = $"Мастер: {_currentDetail.MasterName} | Приоритет: {_currentDetail.PriorityName}\n" +
                $"Бригада: {_currentDetail.TeamName ?? "Не назначена"} | Простой: {_currentDetail.DowntimeHours} ч.\n" +
                $"Описание: {_currentDetail.Description}\nПричина: {_currentDetail.FailureReason ?? "—"}";
            ReportBox.Text = _currentDetail.ReportText ?? "";
            DetailMaterialsGrid.ItemsSource = _currentDetail.Materials;
            DetailModal.IsVisible = true;
        }
        finally { _host.SetLoading(false); }
    }

    private static string BuildTimeline(WorkOrderDetailDto d)
    {
        var steps = new[] { "Создана", "Назначена", "В работе", "Выполнена", "Закрыта" };
        var idx = d.OrderStatusId switch { 1 => 0, 2 => 1, 3 or 4 => 2, 5 => 3, 6 => 4, _ => 0 };
        return string.Join("  →  ", steps.Select((s, i) => i <= idx ? $"[✓ {s}]" : $"[  {s}  ]"));
    }

    private void OnCloseDetailModal(object? sender, RoutedEventArgs e)
    {
        DetailModal.IsVisible = false;
        _host.SetBreadcrumb("Оперативный учёт", "Заявки на ремонт");
    }
}
