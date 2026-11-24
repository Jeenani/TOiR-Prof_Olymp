namespace ToirProf.Api.DTOs;

public class ScheduleItemDto
{
    public int ScheduleId { get; set; }
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string InventoryNumber { get; set; } = string.Empty;
    public string WorkshopName { get; set; } = string.Empty;
    public int MaintenanceTypeId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public int? AssignedTeamId { get; set; }
    public string? TeamName { get; set; }
    public DateOnly PlannedDate { get; set; }
    public string ScheduleStatus { get; set; } = "planned";
    public DateTime CreatedAt { get; set; }
}

public class CreateScheduleRequest
{
    public int EquipmentId { get; set; }
    public int MaintenanceTypeId { get; set; }
    public int? AssignedTeamId { get; set; }
    public DateOnly PlannedDate { get; set; }
}

public class GenerateOrderFromScheduleRequest
{
    public int PriorityId { get; set; } = 2; // medium by default
    public string? Description { get; set; }
}

public class MaintenanceTypeDto
{
    public int MaintenanceTypeId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public int PeriodicityDays { get; set; }
    public decimal StandardDurationHours { get; set; }
    public bool RequiresStop { get; set; }
    public string? Description { get; set; }
}
