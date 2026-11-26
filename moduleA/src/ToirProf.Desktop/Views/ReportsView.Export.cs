using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class ReportsView
{
    private void RefreshTemplatesDropdown()
    {
        TemplatesBox.ItemsSource = _host.Settings.ReportTemplates.Select(t => t.Name).ToList();
        if (_host.Settings.ReportTemplates.Count > 0 && TemplatesBox.SelectedIndex < 0)
            TemplatesBox.SelectedIndex = 0;
    }

    private void OnSaveTemplateClick(object? sender, RoutedEventArgs e)
    {
        var name = TemplateNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _host.ShowToast("Введите название шаблона отчёта.", true);
            return;
        }
        var wsIdx = Math.Max(0, WorkshopBox.SelectedIndex);
        var wsId = _workshops.Count > wsIdx ? _workshops[wsIdx].WorkshopId : 0;
        _host.Settings.ReportTemplates.RemoveAll(t => t.Name == name);
        _host.Settings.ReportTemplates.Add(new ReportTemplate
        {
            Name = name,
            ReportType = GetSelectedReportType(),
            WorkshopId = wsId,
            PeriodDays = GetSelectedPeriodDays()
        });
        SettingsService.Save(_host.Settings);
        RefreshTemplatesDropdown();
        TemplatesBox.SelectedItem = name;
        _host.ShowToast($"Шаблон «{name}» сохранён.");
    }

    private async void OnApplyTemplateClick(object? sender, RoutedEventArgs e)
    {
        if (TemplatesBox.SelectedIndex < 0 || TemplatesBox.SelectedIndex >= _host.Settings.ReportTemplates.Count) return;
        var tpl = _host.Settings.ReportTemplates[TemplatesBox.SelectedIndex];
        ReportTypeBox.SelectedIndex = tpl.ReportType switch { "to" => 1, "workshop" => 2, "materials" => 3, _ => 0 };
        var wsIdx = _workshops.FindIndex(w => w.WorkshopId == tpl.WorkshopId);
        if (wsIdx >= 0) WorkshopBox.SelectedIndex = wsIdx;
        PeriodBox.SelectedIndex = tpl.PeriodDays switch { 7 => 0, 90 => 2, 365 => 3, _ => 1 };
        await GenerateAsync();
        _host.ShowToast($"Применён шаблон «{tpl.Name}».");
    }

    private async void OnExportExcelClick(object? sender, RoutedEventArgs e) => await ExportAsync(false);
    private async void OnExportPdfClick(object? sender, RoutedEventArgs e) => await ExportAsync(true);

    private async Task ExportAsync(bool isPdf)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top == null) return;
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = isPdf ? "Сохранить отчёт в PDF" : "Сохранить отчёт в Excel",
            SuggestedFileName = $"toir_report_{DateTime.Now:yyyyMMdd_HHmm}.{(isPdf ? "pdf" : "xlsx")}"
        });
        if (file == null) return;

        var path = file.Path.LocalPath;
        var sub = $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm} • Строк: {_rows.Count}";
        if (isPdf) ReportExportService.ExportPdf(path, _currentTitle, sub, _headers, _rows);
        else ReportExportService.ExportExcel(path, _currentTitle, sub, _headers, _rows);
        _host.ShowToast($"Файл сохранён: {System.IO.Path.GetFileName(path)}");
    }
}
