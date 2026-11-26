using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Services;

public interface IDesktopHost
{
    ApiClient Api { get; }
    DesktopSettings Settings { get; }
    UserProfileDto? CurrentUser { get; }

    void SetLoading(bool isLoading, string? text = null);
    void ShowToast(string message, bool isError = false);
    void ShowErrorModal(string title, string message, Func<Task>? retryAction = null);
    void SetBreadcrumb(params string[] items);
    void NavigateTo(string section, int? targetId = null, bool openNewModal = false);
    void UpdateNotifications(List<ViolationAlertDto> alerts);
    void ApplyPreferences();
}

public interface IPageView
{
    Task LoadAsync();
    void FocusSearch() { }
    void TriggerNew() { }
}
