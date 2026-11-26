using System;
using System.Collections.Generic;

namespace ToirProf.Desktop.Models;

public class WorkOrderStatusDto
{
    public int OrderStatusId { get; set; }
    public string StatusCode { get; set; } = "";
    public string StatusName { get; set; } = "";
}

public class WorkOrderPriorityDto
{
    public int PriorityId { get; set; }
    public string PriorityCode { get; set; } = "";
    public string PriorityName { get; set; } = "";
}

public class WorkOrderItemDto
{
    public int WorkOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string OrderType { get; set; } = "";
    public int PriorityId { get; set; }
    public string PriorityCode { get; set; } = "";
    public string PriorityName { get; set; } = "";
    public int OrderStatusId { get; set; }
    public string StatusCode { get; set; } = "";
    public string StatusName { get; set; } = "";
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = "";
    public string InventoryNumber { get; set; } = "";
    public int WorkshopId { get; set; }
    public string WorkshopName { get; set; } = "";
    public int MasterUserId { get; set; }
    public string MasterName { get; set; } = "";
    public int? AssignedTeamId { get; set; }
    public string? TeamName { get; set; }
    public int? ScheduleId { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal DowntimeHours { get; set; }
    public DateTime CreatedAt { get; set; }

    public string OrderTypeRu => OrderType.ToLower() switch
    {
        "emergency" => "Аварийная",
        "planned" => "Плановое ТО",
        "capital" => "Капитальный",
        "current" => "Текущий",
        _ => OrderType
    };
}

public class WorkOrderDetailDto : WorkOrderItemDto
{
    public string Description { get; set; } = "";
    public string? FailureReason { get; set; }
    public string? ReportText { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<WorkOrderMaterialDto> Materials { get; set; } = new();
}

public class WorkOrderMaterialDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "";
    public decimal QuantityUsed { get; set; }
    public decimal HistoricalUnitPrice { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime UsedAt { get; set; }
    public string? CommentText { get; set; }
}

public class CreateWorkOrderDto
{
    public string OrderType { get; set; } = "planned";
    public int PriorityId { get; set; }
    public int EquipmentId { get; set; }
    public int? ScheduleId { get; set; }
    public string Description { get; set; } = "";
    public string? FailureReason { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
}
