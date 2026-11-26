using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Services;

public partial class ApiClient
{
    public Task<ApiResult<LoginResponseDto>> LoginAsync(string email, string password, bool rememberMe) =>
        SendAsync<LoginResponseDto>(HttpMethod.Post, "api/auth/login", new LoginRequestDto { Email = email, Password = password, RememberMe = rememberMe });

    public Task<ApiResult<UserProfileDto>> GetMeAsync() =>
        SendAsync<UserProfileDto>(HttpMethod.Get, "api/auth/me");

    public Task<ApiResult<List<WorkshopDto>>> GetWorkshopsAsync() =>
        SendAsync<List<WorkshopDto>>(HttpMethod.Get, "api/auth/workshops");

    public Task<ApiResult<List<TeamDto>>> GetTeamsAsync(int? workshopId = null) =>
        SendAsync<List<TeamDto>>(HttpMethod.Get, workshopId > 0 ? $"api/auth/teams?workshopId={workshopId}" : "api/auth/teams");

    public Task<ApiResult<DashboardKpiDto>> GetDashboardAsync(int? workshopId = null) =>
        SendAsync<DashboardKpiDto>(HttpMethod.Get, workshopId > 0 ? $"api/analytics/dashboard?workshopId={workshopId}" : "api/analytics/dashboard");

    public Task<ApiResult<List<WorkshopSummaryReportDto>>> GetWorkshopSummaryAsync(int workshopId, DateTime? start = null, DateTime? end = null)
    {
        var qs = new List<string>();
        if (start.HasValue) qs.Add($"startDate={Uri.EscapeDataString(start.Value.ToString("O"))}");
        if (end.HasValue) qs.Add($"endDate={Uri.EscapeDataString(end.Value.ToString("O"))}");
        var q = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        return SendAsync<List<WorkshopSummaryReportDto>>(HttpMethod.Get, $"api/analytics/workshop-summary/{workshopId}{q}");
    }

    public Task<ApiResult<List<ScheduleItemDto>>> GetSchedulesAsync(int? workshopId = null, int? month = null, int? year = null, string? status = null)
    {
        var qs = new List<string>();
        if (workshopId > 0) qs.Add($"workshopId={workshopId}");
        if (month is >= 1 and <= 12) qs.Add($"month={month}");
        if (year > 2000) qs.Add($"year={year}");
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        var q = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        return SendAsync<List<ScheduleItemDto>>(HttpMethod.Get, $"api/schedules{q}");
    }

    public Task<ApiResult<List<MaintenanceTypeDto>>> GetMaintenanceTypesAsync() =>
        SendAsync<List<MaintenanceTypeDto>>(HttpMethod.Get, "api/schedules/types");

    public Task<ApiResult<ScheduleItemDto>> CreateScheduleAsync(int equipmentId, int maintenanceTypeId, int? assignedTeamId, string plannedDate) =>
        SendAsync<ScheduleItemDto>(HttpMethod.Post, "api/schedules", new { equipmentId, maintenanceTypeId, assignedTeamId, plannedDate });

    public Task<ApiResult<string>> GenerateOrderFromScheduleAsync(int scheduleId, int priorityId = 2, string? description = null) =>
        SendAsync<string>(HttpMethod.Post, $"api/schedules/{scheduleId}/generate-order", new { priorityId, description });

    public Task<ApiResult<List<MaterialItemDto>>> GetMaterialsAsync(int? warehouseId = null, bool? onlyLowStock = null)
    {
        var qs = new List<string>();
        if (warehouseId > 0) qs.Add($"warehouseId={warehouseId}");
        if (onlyLowStock == true) qs.Add("onlyLowStock=true");
        var q = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        return SendAsync<List<MaterialItemDto>>(HttpMethod.Get, $"api/materials{q}");
    }

    public Task<ApiResult<List<WarehouseDto>>> GetWarehousesAsync() =>
        SendAsync<List<WarehouseDto>>(HttpMethod.Get, "api/materials/warehouses");

    public Task<ApiResult<List<SupplierDto>>> GetSuppliersAsync() =>
        SendAsync<List<SupplierDto>>(HttpMethod.Get, "api/materials/suppliers");

    public Task<ApiResult<string>> AdjustMaterialStockAsync(int materialId, int warehouseId, decimal quantityDelta, string? reason = null) =>
        SendAsync<string>(HttpMethod.Post, $"api/materials/{materialId}/adjust", new { warehouseId, quantityDelta, reason });
}
