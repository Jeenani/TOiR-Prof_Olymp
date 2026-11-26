using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;
using ToirProf.Desktop.Services;

namespace ToirProf.Desktop.Views;

public partial class TeamsView : UserControl, IPageView
{
    private readonly IDesktopHost _host;
    private List<WorkshopDto> _workshops = new();

    public TeamsView() : this(null!) { }

    public TeamsView(IDesktopHost host)
    {
        InitializeComponent();
        _host = host;
    }

    public async Task LoadAsync()
    {
        if (_host == null) return;
        _host.SetBreadcrumb("Планирование и ресурсы", "Бригады");
        _host.SetLoading(true, "Загрузка бригад...");
        try
        {
            if (_workshops.Count == 0)
            {
                var wsRes = await _host.Api.GetWorkshopsAsync();
                if (wsRes.IsSuccess && wsRes.Data != null)
                {
                    _workshops = new List<WorkshopDto> { new() { WorkshopId = 0, WorkshopName = "Все цехи" } };
                    _workshops.AddRange(wsRes.Data);
                    WorkshopFilter.ItemsSource = _workshops.Select(w => w.WorkshopName).ToList();
                    WorkshopFilter.SelectedIndex = 0;
                }
            }

            int? wsId = WorkshopFilter.SelectedIndex > 0 && WorkshopFilter.SelectedIndex < _workshops.Count
                ? _workshops[WorkshopFilter.SelectedIndex].WorkshopId
                : null;

            var res = await _host.Api.GetTeamsAsync(wsId);
            if (!res.IsSuccess || res.Data == null)
            {
                _host.ShowErrorModal("Ошибка загрузки бригад", res.ErrorMessage, LoadAsync);
                return;
            }

            TeamsGrid.ItemsSource = res.Data;
            if (res.Data.Count > 0)
                TeamsGrid.SelectedIndex = 0;
        }
        finally
        {
            _host.SetLoading(false);
        }
    }

    private async void OnWorkshopChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsVisible && _workshops.Count > 0) await LoadAsync();
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e) => await LoadAsync();

    private void OnTeamSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (TeamsGrid.SelectedItem is TeamDto t)
        {
            TeamTitleText.Text = $"{t.TeamCode} — {t.TeamName}";
            TeamMetaText.Text = $"Специализация: {t.Specialization}\nОбслуживаемые цехи: {t.WorkshopsDisplay}\nТекущая загрузка: {t.WorkloadLabel}";
            MembersList.ItemsSource = t.Members;
            TeamOrdersGrid.ItemsSource = t.ActiveOrders;
        }
    }
}
