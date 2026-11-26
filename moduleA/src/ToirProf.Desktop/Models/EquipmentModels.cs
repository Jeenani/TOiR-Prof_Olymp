using System;
using System.Collections.Generic;

namespace ToirProf.Desktop.Models;

public class EquipmentStatusDto
{
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = "";
    public string StatusName { get; set; } = "";
    public string? Description { get; set; }
}

public class EquipmentItemDto
{
    public int EquipmentId { get; set; }
    public int WorkshopId { get; set; }
    public string WorkshopName { get; set; } = "";
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = "";
    public string StatusName { get; set; } = "";
    public string InventoryNumber { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? WarrantyUntil { get; set; }
    public string? PassportUrl { get; set; }
    public string CommissioningDate { get; set; } = "";
    public string OperationDate { get; set; } = "";
    public string? DecommissioningDate { get; set; }
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
    public string StatusName { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class CreateEquipmentDto
{
    public int WorkshopId { get; set; }
    public int StatusId { get; set; } = 1;
    public string InventoryNumber { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string CommissioningDate { get; set; } = "";
    public string OperationDate { get; set; } = "";
    public string? WarrantyUntil { get; set; }
}

public class UpdateEquipmentDto
{
    public int WorkshopId { get; set; }
    public int StatusId { get; set; }
    public string EquipmentName { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string CommissioningDate { get; set; } = "";
    public string OperationDate { get; set; } = "";
    public string? WarrantyUntil { get; set; }
    public string? StatusChangeReason { get; set; }
}
