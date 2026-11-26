using System;

namespace ToirProf.Desktop.Models;

public class ScheduleItemDto
{
    public int ScheduleId { get; set; }
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = "";
    public string InventoryNumber { get; set; } = "";
    public string WorkshopName { get; set; } = "";
    public int MaintenanceTypeId { get; set; }
    public string TypeCode { get; set; } = "";
    public string TypeName { get; set; } = "";
    public int? AssignedTeamId { get; set; }
    public string? TeamName { get; set; }
    public string PlannedDate { get; set; } = "";
    public string ScheduleStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public DateOnly ParsedDate => DateOnly.TryParse(PlannedDate, out var d) ? d : DateOnly.FromDateTime(DateTime.Today);
    public bool IsOverdue => ScheduleStatus == "planned" && ParsedDate < DateOnly.FromDateTime(DateTime.Today);
    public bool IsSoon => ScheduleStatus == "planned" && !IsOverdue && ParsedDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(3));
    public string StatusBadgeText => IsOverdue ? "Просрочено" : IsSoon ? "Скоро (<=3 дн.)" : ScheduleStatus switch
    {
        "completed" => "Выполнено",
        "in_progress" => "В работе",
        _ => "Запланировано"
    };
}

public class MaintenanceTypeDto
{
    public int MaintenanceTypeId { get; set; }
    public string TypeCode { get; set; } = "";
    public string TypeName { get; set; } = "";
    public int PeriodicityDays { get; set; }
    public decimal StandardDurationHours { get; set; }
    public bool RequiresStop { get; set; }
    public string? Description { get; set; }
}

public class MaterialItemDto
{
    public int MaterialId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string? StorageCell { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SupplierName { get; set; }
    public bool IsLowStock { get; set; }
    public string DisplayWithWarehouse => $"{MaterialName} ({WarehouseName} — ост. {CurrentStock} {UnitOfMeasure})";
}

public class WarehouseDto
{
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string? LocationDescription { get; set; }
}

public class SupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string? Inn { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
}
