using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class WorkOrdersView
{
    private void OnNewClick(object? sender, RoutedEventArgs e) => OpenCreateModal();

    public async void OpenCreateModal(int? preselectEquipmentId = null)
    {
        _host.SetLoading(true, "Подготовка формы...");
        try
        {
            await EnsureDictionariesAsync();
            if (_equipments.Count == 0)
            {
                var r = await _host.Api.GetEquipmentAsync(pageSize: 100);
                if (r.IsSuccess && r.Data != null) _equipments = r.Data.Items;
            }
            CreateEqBox.ItemsSource = _equipments.Select(x => $"{x.InventoryNumber} — {x.EquipmentName}").ToList();
            if (preselectEquipmentId.HasValue)
            {
                var idx = _equipments.FindIndex(x => x.EquipmentId == preselectEquipmentId.Value);
                CreateEqBox.SelectedIndex = Math.Max(0, idx);
            }
            else if (_equipments.Count > 0) CreateEqBox.SelectedIndex = 0;

            CreateDescBox.Text = "";
            CreateFailureBox.Text = "";
            CreateErrorText.IsVisible = false;
            CreateModal.IsVisible = true;
        }
        finally { _host.SetLoading(false); }
    }

    private void OnCloseCreateModal(object? sender, RoutedEventArgs e) => CreateModal.IsVisible = false;

    private async void OnSubmitCreateClick(object? sender, RoutedEventArgs e)
    {
        CreateErrorText.IsVisible = false;
        if (CreateEqBox.SelectedIndex < 0 || CreatePriorityBox.SelectedIndex < 0 || string.IsNullOrWhiteSpace(CreateDescBox.Text))
        {
            CreateErrorText.Text = "Выберите оборудование, приоритет и введите описание работ.";
            CreateErrorText.IsVisible = true;
            return;
        }

        var orderType = (CreateTypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "planned";
        if (orderType == "emergency" && string.IsNullOrWhiteSpace(CreateFailureBox.Text))
        {
            CreateErrorText.Text = "Для аварийной заявки обязательно укажите причину отказа.";
            CreateErrorText.IsVisible = true;
            return;
        }

        var eq = _equipments[CreateEqBox.SelectedIndex];
        var dto = new CreateWorkOrderDto
        {
            OrderType = orderType,
            PriorityId = _priorities[CreatePriorityBox.SelectedIndex].PriorityId,
            EquipmentId = eq.EquipmentId,
            Description = CreateDescBox.Text.Trim(),
            FailureReason = string.IsNullOrWhiteSpace(CreateFailureBox.Text) ? null : CreateFailureBox.Text.Trim(),
            PlannedStartDate = DateTime.UtcNow,
            PlannedEndDate = DateTime.UtcNow.AddHours(4)
        };

        _host.SetLoading(true, "Создание заявки...");
        try
        {
            var res = await _host.Api.CreateWorkOrderAsync(dto);
            if (!res.IsSuccess)
            {
                if (res.IsNetworkError)
                {
                    DraftQueueService.Enqueue(new OfflineWorkOrderDraft
                    {
                        Payload = dto,
                        EquipmentLabel = $"{eq.InventoryNumber} — {eq.EquipmentName}",
                        LastError = res.ErrorMessage
                    });
                    CreateModal.IsVisible = false;
                    _host.ShowToast("Связь потеряна! Черновик заявки сохранён локально и будет отправлен автоматически.", true);
                    return;
                }
                CreateErrorText.Text = res.ErrorMessage;
                CreateErrorText.IsVisible = true;
                return;
            }
            CreateModal.IsVisible = false;
            _host.ShowToast($"Создана заявка {res.Data?.OrderNumber}");
            await LoadAsync();
        }
        finally { _host.SetLoading(false); }
    }
}
