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
    public Task<ApiResult<UserProfileDto>> GetMeAsync() => SendAsync<UserProfileDto>(HttpMethod.Get, "api/auth/me");
    public Task<ApiResult<List<WorkshopDto>>> GetWorkshopsAsync() => SendAsync<List<WorkshopDto>>(HttpMethod.Get, "api/auth/workshops");
    public Task<ApiResult<List<TeamDto>>> GetTeamsAsync(int? workshopId = null) =>
        SendAsync<List<TeamDto>>(HttpMethod.Get, workshopId > 0 ? $"api/auth/teams?workshopId={workshopId}" : "api/auth/teams");

    public Task<ApiResult<DashboardKpiDto>> GetDashboardAsync(int? workshopId = null) =>
        SendAsync<DashboardKpiDto>(HttpMethod.Get, workshopId > 0 ? $"api/analytics/dashboard?workshopId={workshopId}" : "api/analytics/dashboard");
    public Task<ApiResult<List<WorkshopSummaryReportDto>>> GetWorkshopSummaryAsync(int workshopId, DateTime? start = null, DateTime? end = null) =>
        SendAsync<List<WorkshopSummaryReportDto>>(HttpMethod.Get, $"api/analytics/workshop-summary/{workshopId}?startDate={Uri.EscapeDataString((start ?? DateTime.UtcNow.AddDays(-30)).ToString("O"))}&endDate={Uri.EscapeDataString((end ?? DateTime.UtcNow).ToString("O"))}");

    public Task<ApiResult<PagedResultDto<EquipmentItemDto>>> GetEquipmentAsync(int? workshopId = null, int? statusId = null, string? model = null, string? search = null, string sortBy = "inventoryNumber", bool sortDesc = false, int page = 1, int pageSize = 25)
    {
        var qs = $"sortBy={Uri.EscapeDataString(sortBy)}&sortDesc={sortDesc.ToString().ToLower()}&page={page}&pageSize={pageSize}"
            + (workshopId > 0 ? $"&workshopId={workshopId}" : "") + (statusId > 0 ? $"&statusId={statusId}" : "")
            + (!string.IsNullOrWhiteSpace(model) ? $"&model={Uri.EscapeDataString(model)}" : "")
            + (!string.IsNullOrWhiteSpace(search) ? $"&search={Uri.EscapeDataString(search)}" : "");
        return SendAsync<PagedResultDto<EquipmentItemDto>>(HttpMethod.Get, $"api/equipment?{qs}");
    }
    public Task<ApiResult<List<EquipmentStatusDto>>> GetEquipmentStatusesAsync() => SendAsync<List<EquipmentStatusDto>>(HttpMethod.Get, "api/equipment/statuses");
    public Task<ApiResult<EquipmentDetailDto>> GetEquipmentDetailAsync(int id) => SendAsync<EquipmentDetailDto>(HttpMethod.Get, $"api/equipment/{id}");
    public Task<ApiResult<EquipmentItemDto>> CreateEquipmentAsync(CreateEquipmentDto dto) => SendAsync<EquipmentItemDto>(HttpMethod.Post, "api/equipment", dto);

    public Task<ApiResult<PagedResultDto<WorkOrderItemDto>>> GetWorkOrdersAsync(int? statusId = null, int? priorityId = null, int? workshopId = null, string? search = null, string sortBy = "createdAt", bool sortDesc = true, int page = 1, int pageSize = 25)
    {
        var qs = $"sortBy={Uri.EscapeDataString(sortBy)}&sortDesc={sortDesc.ToString().ToLower()}&page={page}&pageSize={pageSize}"
            + (statusId > 0 ? $"&statusId={statusId}" : "") + (priorityId > 0 ? $"&priorityId={priorityId}" : "")
            + (workshopId > 0 ? $"&workshopId={workshopId}" : "") + (!string.IsNullOrWhiteSpace(search) ? $"&search={Uri.EscapeDataString(search)}" : "");
        return SendAsync<PagedResultDto<WorkOrderItemDto>>(HttpMethod.Get, $"api/work-orders?{qs}");
    }
    public Task<ApiResult<List<WorkOrderStatusDto>>> GetWorkOrderStatusesAsync() => SendAsync<List<WorkOrderStatusDto>>(HttpMethod.Get, "api/work-orders/statuses");
    public Task<ApiResult<List<WorkOrderPriorityDto>>> GetWorkOrderPrioritiesAsync() => SendAsync<List<WorkOrderPriorityDto>>(HttpMethod.Get, "api/work-orders/priorities");
    public Task<ApiResult<WorkOrderDetailDto>> GetWorkOrderDetailAsync(int id) => SendAsync<WorkOrderDetailDto>(HttpMethod.Get, $"api/work-orders/{id}");
    public Task<ApiResult<WorkOrderItemDto>> CreateWorkOrderAsync(CreateWorkOrderDto dto) => SendAsync<WorkOrderItemDto>(HttpMethod.Post, "api/work-orders", dto);
    public Task<ApiResult<string>> AssignTeamAsync(int workOrderId, int teamId) => SendAsync<string>(HttpMethod.Put, $"api/work-orders/{workOrderId}/assign-team", new { teamId });
    public Task<ApiResult<string>> UpdateOrderStatusAsync(int workOrderId, int orderStatusId, string? reportText = null, decimal? downtimeHours = null) =>
        SendAsync<string>(HttpMethod.Put, $"api/work-orders/{workOrderId}/status", new { orderStatusId, reportText, downtimeHours });
    public Task<ApiResult<string>> AddOrderMaterialAsync(int workOrderId, int warehouseId, int materialId, decimal quantityUsed, string? commentText = null) =>
        SendAsync<string>(HttpMethod.Post, $"api/work-orders/{workOrderId}/materials", new { warehouseId, materialId, quantityUsed, commentText });

    public Task<ApiResult<List<ScheduleItemDto>>> GetSchedulesAsync(int? workshopId = null, int? month = null, int? year = null, string? status = null)
    {
        var qs = new List<string>();
        if (workshopId > 0) qs.Add($"workshopId={workshopId}");
        if (month is >= 1 and <= 12) qs.Add($"month={month}");
        if (year > 2000) qs.Add($"year={year}");
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        return SendAsync<List<ScheduleItemDto>>(HttpMethod.Get, qs.Count > 0 ? $"api/schedules?{string.Join("&", qs)}" : "api/schedules");
    }
    public Task<ApiResult<List<MaintenanceTypeDto>>> GetMaintenanceTypesAsync() => SendAsync<List<MaintenanceTypeDto>>(HttpMethod.Get, "api/schedules/types");
    public Task<ApiResult<ScheduleItemDto>> CreateScheduleAsync(int equipmentId, int maintenanceTypeId, int? assignedTeamId, string plannedDate) =>
        SendAsync<ScheduleItemDto>(HttpMethod.Post, "api/schedules", new { equipmentId, maintenanceTypeId, assignedTeamId, plannedDate });
    public Task<ApiResult<string>> GenerateOrderFromScheduleAsync(int scheduleId, int priorityId = 2, string? description = null) =>
        SendAsync<string>(HttpMethod.Post, $"api/schedules/{scheduleId}/generate-order", new { priorityId, description });

    public Task<ApiResult<List<MaterialItemDto>>> GetMaterialsAsync(int? warehouseId = null, bool? onlyLowStock = null)
    {
        var qs = new List<string>();
        if (warehouseId > 0) qs.Add($"warehouseId={warehouseId}");
        if (onlyLowStock == true) qs.Add("onlyLowStock=true");
        return SendAsync<List<MaterialItemDto>>(HttpMethod.Get, qs.Count > 0 ? $"api/materials?{string.Join("&", qs)}" : "api/materials");
    }
    public Task<ApiResult<List<WarehouseDto>>> GetWarehousesAsync() => SendAsync<List<WarehouseDto>>(HttpMethod.Get, "api/materials/warehouses");
    public Task<ApiResult<List<SupplierDto>>> GetSuppliersAsync() => SendAsync<List<SupplierDto>>(HttpMethod.Get, "api/materials/suppliers");
    public Task<ApiResult<string>> AdjustMaterialStockAsync(int materialId, int warehouseId, decimal quantityDelta, string? reason = null) =>
        SendAsync<string>(HttpMethod.Post, $"api/materials/{materialId}/adjust", new { warehouseId, quantityDelta, reason });
    public Task<ApiResult<string>> UpdateEquipmentAsync(int id, UpdateEquipmentDto dto) => SendAsync<string>(HttpMethod.Put, $"api/equipment/{id}", dto);
    public Task<ApiResult<string>> DeleteEquipmentAsync(int id) => SendAsync<string>(HttpMethod.Delete, $"api/equipment/{id}");
}
