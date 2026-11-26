using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Services;

public partial class ApiClient
{
    public Task<ApiResult<PagedResultDto<EquipmentItemDto>>> GetEquipmentAsync(
        int? workshopId = null, int? statusId = null, string? model = null,
        string? search = null, string sortBy = "inventoryNumber", bool sortDesc = false,
        int page = 1, int pageSize = 25)
    {
        var qs = new List<string> { $"sortBy={Uri.EscapeDataString(sortBy)}", $"sortDesc={sortDesc.ToString().ToLower()}", $"page={page}", $"pageSize={pageSize}" };
        if (workshopId > 0) qs.Add($"workshopId={workshopId}");
        if (statusId > 0) qs.Add($"statusId={statusId}");
        if (!string.IsNullOrWhiteSpace(model)) qs.Add($"model={Uri.EscapeDataString(model)}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        return SendAsync<PagedResultDto<EquipmentItemDto>>(HttpMethod.Get, $"api/equipment?{string.Join("&", qs)}");
    }

    public Task<ApiResult<List<EquipmentStatusDto>>> GetEquipmentStatusesAsync() =>
        SendAsync<List<EquipmentStatusDto>>(HttpMethod.Get, "api/equipment/statuses");

    public Task<ApiResult<EquipmentDetailDto>> GetEquipmentDetailAsync(int id) =>
        SendAsync<EquipmentDetailDto>(HttpMethod.Get, $"api/equipment/{id}");

    public Task<ApiResult<EquipmentItemDto>> CreateEquipmentAsync(CreateEquipmentDto dto) =>
        SendAsync<EquipmentItemDto>(HttpMethod.Post, "api/equipment", dto);

    public Task<ApiResult<string>> UpdateEquipmentAsync(int id, UpdateEquipmentDto dto) =>
        SendAsync<string>(HttpMethod.Put, $"api/equipment/{id}", dto);

    public Task<ApiResult<string>> DeleteEquipmentAsync(int id) =>
        SendAsync<string>(HttpMethod.Delete, $"api/equipment/{id}");

    public Task<ApiResult<PagedResultDto<WorkOrderItemDto>>> GetWorkOrdersAsync(
        int? statusId = null, int? priorityId = null, int? workshopId = null,
        string? search = null, string sortBy = "createdAt", bool sortDesc = true,
        int page = 1, int pageSize = 25)
    {
        var qs = new List<string> { $"sortBy={Uri.EscapeDataString(sortBy)}", $"sortDesc={sortDesc.ToString().ToLower()}", $"page={page}", $"pageSize={pageSize}" };
        if (statusId > 0) qs.Add($"statusId={statusId}");
        if (priorityId > 0) qs.Add($"priorityId={priorityId}");
        if (workshopId > 0) qs.Add($"workshopId={workshopId}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        return SendAsync<PagedResultDto<WorkOrderItemDto>>(HttpMethod.Get, $"api/work-orders?{string.Join("&", qs)}");
    }

    public Task<ApiResult<List<WorkOrderStatusDto>>> GetWorkOrderStatusesAsync() =>
        SendAsync<List<WorkOrderStatusDto>>(HttpMethod.Get, "api/work-orders/statuses");

    public Task<ApiResult<List<WorkOrderPriorityDto>>> GetWorkOrderPrioritiesAsync() =>
        SendAsync<List<WorkOrderPriorityDto>>(HttpMethod.Get, "api/work-orders/priorities");

    public Task<ApiResult<WorkOrderDetailDto>> GetWorkOrderDetailAsync(int id) =>
        SendAsync<WorkOrderDetailDto>(HttpMethod.Get, $"api/work-orders/{id}");

    public Task<ApiResult<WorkOrderItemDto>> CreateWorkOrderAsync(CreateWorkOrderDto dto) =>
        SendAsync<WorkOrderItemDto>(HttpMethod.Post, "api/work-orders", dto);

    public Task<ApiResult<string>> AssignTeamAsync(int workOrderId, int teamId) =>
        SendAsync<string>(HttpMethod.Put, $"api/work-orders/{workOrderId}/assign-team", new { teamId });

    public Task<ApiResult<string>> UpdateOrderStatusAsync(int workOrderId, int orderStatusId, string? reportText = null, decimal? downtimeHours = null) =>
        SendAsync<string>(HttpMethod.Put, $"api/work-orders/{workOrderId}/status", new { orderStatusId, reportText, downtimeHours });

    public Task<ApiResult<string>> AddOrderMaterialAsync(int workOrderId, int warehouseId, int materialId, decimal quantityUsed, string? commentText = null) =>
        SendAsync<string>(HttpMethod.Post, $"api/work-orders/{workOrderId}/materials", new { warehouseId, materialId, quantityUsed, commentText });
}
