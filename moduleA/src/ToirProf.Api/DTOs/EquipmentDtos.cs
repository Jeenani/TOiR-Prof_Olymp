namespace ToirProf.Api.DTOs;

public class EquipmentItemDto
{
    public int EquipmentId { get; set; }
    public int WorkshopId { get; set; }
    public string WorkshopName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string InventoryNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string? PassportUrl { get; set; }
    public DateOnly CommissioningDate { get; set; }
    public DateOnly OperationDate { get; set; }
    public DateOnly? DecommissioningDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EquipmentDetailDto : EquipmentItemDto
{
    public DateTime UpdatedAt { get; set; }
    public List<EquipmentStatusHistoryDto> StatusHistory { get; set; } = new();
    public List<ScheduleItemDto> Schedules { get; set; } = new();
    public List<WorkOrderItemDto> RecentWorkOrders { get; set; } = new();
}

public class EquipmentStatusHistoryDto
{
    public int HistoryId { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByName { get; set; }
}

public class CreateEquipmentRequest
{
    public int WorkshopId { get; set; }
    public int StatusId { get; set; } = 1;
    public string InventoryNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public DateOnly CommissioningDate { get; set; }
    public DateOnly OperationDate { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string? PassportUrl { get; set; }
}

public class UpdateEquipmentRequest
{
    public int WorkshopId { get; set; }
    public int StatusId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public DateOnly CommissioningDate { get; set; }
    public DateOnly OperationDate { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string? PassportUrl { get; set; }
    public string? StatusChangeReason { get; set; }
}

public class EquipmentStatusDto
{
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
