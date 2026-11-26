using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Views;

public partial class EquipmentView
{
    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        _editingId = null;
        FormTitle.Text = "Добавить оборудование";
        FormInvBox.IsEnabled = true;
        FormInvBox.Text = "";
        FormNameBox.Text = "";
        FormModelBox.Text = "";
        FormManufacturerBox.Text = "";
        FormSerialBox.Text = "";
        FormWorkshopBox.SelectedIndex = 0;
        FormStatusBox.SelectedIndex = 0;
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        FormCommDateBox.Text = today;
        FormOpDateBox.Text = today;
        FormErrorText.IsVisible = false;
        FormModal.IsVisible = true;
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id }) return;
        var eq = _items.FirstOrDefault(x => x.EquipmentId == id);
        if (eq == null) return;

        _editingId = id;
        FormTitle.Text = $"Редактирование {eq.InventoryNumber}";
        FormInvBox.IsEnabled = false;
        FormInvBox.Text = eq.InventoryNumber;
        FormNameBox.Text = eq.EquipmentName;
        FormModelBox.Text = eq.ModelName;
        FormManufacturerBox.Text = eq.Manufacturer ?? "";
        FormSerialBox.Text = eq.SerialNumber ?? "";
        FormWorkshopBox.SelectedIndex = Math.Max(0, _workshops.FindIndex(w => w.WorkshopId == eq.WorkshopId));
        FormStatusBox.SelectedIndex = Math.Max(0, _statuses.FindIndex(s => s.StatusId == eq.StatusId));
        FormCommDateBox.Text = eq.CommissioningDate;
        FormOpDateBox.Text = eq.OperationDate;
        FormErrorText.IsVisible = false;
        FormModal.IsVisible = true;
    }

    private void OnCloseFormModal(object? sender, RoutedEventArgs e) => FormModal.IsVisible = false;

    private async void OnSaveFormClick(object? sender, RoutedEventArgs e)
    {
        FormErrorText.IsVisible = false;
        if (string.IsNullOrWhiteSpace(FormInvBox.Text) || string.IsNullOrWhiteSpace(FormNameBox.Text) || string.IsNullOrWhiteSpace(FormModelBox.Text))
        {
            FormErrorText.Text = "Заполните обязательные поля: инвентарный номер, наименование и модель.";
            FormErrorText.IsVisible = true;
            return;
        }
        if (!DateOnly.TryParse(FormCommDateBox.Text, out var commDate) || !DateOnly.TryParse(FormOpDateBox.Text, out var opDate))
        {
            FormErrorText.Text = "Введите корректные даты в формате ГГГГ-ММ-ДД.";
            FormErrorText.IsVisible = true;
            return;
        }

        var wsId = _workshops[Math.Max(0, FormWorkshopBox.SelectedIndex)].WorkshopId;
        var stId = _statuses[Math.Max(0, FormStatusBox.SelectedIndex)].StatusId;
        _host.SetLoading(true, "Сохранение оборудования...");
        try
        {
            if (_editingId == null)
            {
                var res = await _host.Api.CreateEquipmentAsync(new CreateEquipmentDto
                {
                    WorkshopId = wsId, StatusId = stId, InventoryNumber = FormInvBox.Text.Trim(),
                    EquipmentName = FormNameBox.Text.Trim(), ModelName = FormModelBox.Text.Trim(),
                    Manufacturer = FormManufacturerBox.Text?.Trim(), SerialNumber = FormSerialBox.Text?.Trim(),
                    CommissioningDate = commDate.ToString("yyyy-MM-dd"), OperationDate = opDate.ToString("yyyy-MM-dd")
                });
                if (!res.IsSuccess) { FormErrorText.Text = res.ErrorMessage; FormErrorText.IsVisible = true; return; }
                _host.ShowToast("Оборудование успешно добавлено.");
            }
            else
            {
                var res = await _host.Api.UpdateEquipmentAsync(_editingId.Value, new UpdateEquipmentDto
                {
                    WorkshopId = wsId, StatusId = stId, EquipmentName = FormNameBox.Text.Trim(),
                    ModelName = FormModelBox.Text.Trim(), Manufacturer = FormManufacturerBox.Text?.Trim(),
                    SerialNumber = FormSerialBox.Text?.Trim(),
                    CommissioningDate = commDate.ToString("yyyy-MM-dd"), OperationDate = opDate.ToString("yyyy-MM-dd")
                });
                if (!res.IsSuccess) { FormErrorText.Text = res.ErrorMessage; FormErrorText.IsVisible = true; return; }
                _host.ShowToast("Данные оборудования обновлены.");
            }
            FormModal.IsVisible = false;
            await LoadAsync();
        }
        finally { _host.SetLoading(false); }
    }
}
