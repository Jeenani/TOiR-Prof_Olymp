namespace ToirProf.Api.DTOs;

public class WorkOrderItemDto
{
    public int WorkOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public int PriorityId { get; set; }
    public string PriorityCode { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
    public int OrderStatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string InventoryNumber { get; set; } = string.Empty;
    public int WorkshopId { get; set; }
    public string WorkshopName { get; set; } = string.Empty;
    public int MasterUserId { get; set; }
    public string MasterName { get; set; } = string.Empty;
    public int? AssignedTeamId { get; set; }
    public string? TeamName { get; set; }
    public int? ScheduleId { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal DowntimeHours { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WorkOrderDetailDto : WorkOrderItemDto
{
    public string Description { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string? ReportText { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<WorkOrderMaterialDto> Materials { get; set; } = new();
}

public class WorkOrderMaterialDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public decimal HistoricalUnitPrice { get; set; }
    public decimal TotalCost => QuantityUsed * HistoricalUnitPrice;
    public DateTime UsedAt { get; set; }
    public string? CommentText { get; set; }
}

public class CreateWorkOrderRequest
{
    public string OrderType { get; set; } = "emergency"; // emergency, planned, capital, current
    public int PriorityId { get; set; }
    public int EquipmentId { get; set; }
    public int? ScheduleId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
}

public class AssignTeamRequest
{
    public int TeamId { get; set; }
}

public class UpdateOrderStatusRequest
{
    public int OrderStatusId { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? ReportText { get; set; }
    public string? FailureReason { get; set; }
    public decimal? DowntimeHours { get; set; }
}

public class AddWorkOrderMaterialRequest
{
    public int? WarehouseId { get; set; }
    public int MaterialId { get; set; }
    public decimal QuantityUsed { get; set; }
    public string? CommentText { get; set; }
}

public class WorkOrderStatusDto
{
    public int OrderStatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
}

public class WorkOrderPriorityDto
{
    public int PriorityId { get; set; }
    public string PriorityCode { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
}
