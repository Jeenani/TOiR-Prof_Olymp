using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToirProf.Desktop.Views;

public partial class SchedulesView
{
    private async void OnPrevMonth(object? sender, RoutedEventArgs e)
    {
        if (_month == 1) { _month = 12; _year--; } else _month--;
        _selectedDay = null;
        await LoadAsync();
    }

    private async void OnNextMonth(object? sender, RoutedEventArgs e)
    {
        if (_month == 12) { _month = 1; _year++; } else _month++;
        _selectedDay = null;
        await LoadAsync();
    }

    private void OnClearDateClick(object? sender, RoutedEventArgs e)
    {
        _selectedDay = null;
        RenderCalendar();
        ApplyDayFilter();
    }

    private async void OnAddScheduleClick(object? sender, RoutedEventArgs e)
    {
        _host.SetLoading(true, "Загрузка справочников...");
        try
        {
            var eqRes = await _host.Api.GetEquipmentAsync(pageSize: 100);
            var tpRes = await _host.Api.GetMaintenanceTypesAsync();
            var tmRes = await _host.Api.GetTeamsAsync();
            if (eqRes.IsSuccess && eqRes.Data != null) _eqList = eqRes.Data.Items;
            if (tpRes.IsSuccess && tpRes.Data != null) _types = tpRes.Data;
            if (tmRes.IsSuccess && tmRes.Data != null) _teams = tmRes.Data;

            AddEqBox.ItemsSource = _eqList.Select(x => $"{x.InventoryNumber} — {x.EquipmentName}").ToList();
            AddTypeBox.ItemsSource = _types.Select(x => $"{x.TypeCode} — {x.TypeName}").ToList();
            var teamNames = new List<string> { "Не назначена" };
            teamNames.AddRange(_teams.Select(x => x.TeamName));
            AddTeamBox.ItemsSource = teamNames;
            AddEqBox.SelectedIndex = 0;
            AddTypeBox.SelectedIndex = 0;
            AddTeamBox.SelectedIndex = 0;
            AddDateBox.Text = (_selectedDay ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3))).ToString("yyyy-MM-dd");
            AddModal.IsVisible = true;
        }
        finally { _host.SetLoading(false); }
    }

    private void OnCloseAddModal(object? sender, RoutedEventArgs e) => AddModal.IsVisible = false;

    private async void OnSaveScheduleClick(object? sender, RoutedEventArgs e)
    {
        if (AddEqBox.SelectedIndex < 0 || AddTypeBox.SelectedIndex < 0 || !DateOnly.TryParse(AddDateBox.Text, out var dt))
        {
            _host.ShowToast("Проверьте выбранное оборудование, вид ТО и дату (ГГГГ-ММ-ДД).", true);
            return;
        }
        var eqId = _eqList[AddEqBox.SelectedIndex].EquipmentId;
        var tpId = _types[AddTypeBox.SelectedIndex].MaintenanceTypeId;
        int? teamId = AddTeamBox.SelectedIndex > 0 ? _teams[AddTeamBox.SelectedIndex - 1].TeamId : null;

        _host.SetLoading(true, "Создание записи графика ТО...");
        try
        {
            var res = await _host.Api.CreateScheduleAsync(eqId, tpId, teamId, dt.ToString("yyyy-MM-dd"));
            if (!res.IsSuccess) { _host.ShowErrorModal("Ошибка", res.ErrorMessage); return; }
            AddModal.IsVisible = false;
            _host.ShowToast("Плановое ТО добавлено в график.");
            await LoadAsync();
        }
        finally { _host.SetLoading(false); }
    }

    private async void OnGenerateOrderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Models.ScheduleItemDto item }) return;
        _host.SetLoading(true, "Генерация заявки из графика ТО...");
        try
        {
            var desc = $"Плановое обслуживание {item.TypeCode} ({item.TypeName}) для {item.InventoryNumber}";
            var res = await _host.Api.GenerateOrderFromScheduleAsync(item.ScheduleId, 2, desc);
            if (!res.IsSuccess) { _host.ShowErrorModal("Ошибка генерации заявки", res.ErrorMessage); return; }
            _host.ShowToast("Заявка успешно сформирована из графика ТО!");
            await LoadAsync();
        }
        finally { _host.SetLoading(false); }
    }
}
