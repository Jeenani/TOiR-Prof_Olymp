using System;
using System.Collections.Generic;

namespace ToirProf.Domain;

public class Role
{
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Workshop
{
    public int WorkshopId { get; set; }
    public string WorkshopCode { get; set; } = string.Empty;
    public string WorkshopName { get; set; } = string.Empty;
    public string? WorkshopDescription { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RepairTeam> RepairTeams { get; set; } = new List<RepairTeam>();
    public ICollection<Equipment> EquipmentList { get; set; } = new List<Equipment>();
}

public class RepairTeam
{
    public int TeamId { get; set; }
    public int? WorkshopId { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    public Workshop? Workshop { get; set; }
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int? WorkshopId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Role Role { get; set; } = null!;
    public Workshop? Workshop { get; set; }
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public ICollection<WorkOrder> CreatedWorkOrders { get; set; } = new List<WorkOrder>();
}

public class TeamMember
{
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public bool IsTeamLeader { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public RepairTeam RepairTeam { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class EquipmentStatus
{
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Equipment> EquipmentList { get; set; } = new List<Equipment>();
}

public class Equipment
{
    public int EquipmentId { get; set; }
    public int WorkshopId { get; set; }
    public int StatusId { get; set; }
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
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Workshop Workshop { get; set; } = null!;
    public EquipmentStatus Status { get; set; } = null!;
    public ICollection<EquipmentStatusHistory> StatusHistory { get; set; } = new List<EquipmentStatusHistory>();
    public ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}

public class EquipmentStatusHistory
{
    public int HistoryId { get; set; }
    public int EquipmentId { get; set; }
    public int StatusId { get; set; }
    public string? Reason { get; set; }
    public int? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Equipment Equipment { get; set; } = null!;
    public EquipmentStatus Status { get; set; } = null!;
}

public class MaintenanceType
{
    public int MaintenanceTypeId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public int PeriodicityDays { get; set; }
    public decimal StandardDurationHours { get; set; }
    public bool RequiresStop { get; set; } = true;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MaintenanceSchedule> Schedules { get; set; } = new List<MaintenanceSchedule>();
}

public class MaintenanceSchedule
{
    public int ScheduleId { get; set; }
    public int EquipmentId { get; set; }
    public int MaintenanceTypeId { get; set; }
    public int? AssignedTeamId { get; set; }
    public DateOnly PlannedDate { get; set; }
    public string ScheduleStatus { get; set; } = "planned";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Equipment Equipment { get; set; } = null!;
    public MaintenanceType MaintenanceType { get; set; } = null!;
    public RepairTeam? AssignedTeam { get; set; }
}

public class WorkOrderStatus
{
    public int OrderStatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}

public class WorkOrderPriority
{
    public int PriorityId { get; set; }
    public string PriorityCode { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}

public class WorkOrder
{
    public int WorkOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public int PriorityId { get; set; }
    public int OrderStatusId { get; set; }
    public int EquipmentId { get; set; }
    public int MasterUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public int? ScheduleId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string? ReportText { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal DowntimeHours { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Equipment Equipment { get; set; } = null!;
    public WorkOrderPriority Priority { get; set; } = null!;
    public WorkOrderStatus Status { get; set; } = null!;
    public User MasterUser { get; set; } = null!;
    public RepairTeam? AssignedTeam { get; set; }
    public ICollection<WorkOrderMaterial> WorkOrderMaterials { get; set; } = new List<WorkOrderMaterial>();
}

public class Warehouse
{
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? LocationDescription { get; set; }

    public ICollection<Material> Materials { get; set; } = new List<Material>();
}

public class Material
{
    public int MaterialId { get; set; }
    public int WarehouseId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string? StorageCell { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<WorkOrderMaterial> WorkOrderMaterials { get; set; } = new List<WorkOrderMaterial>();
}

public class WorkOrderMaterial
{
    public int WorkOrderId { get; set; }
    public int MaterialId { get; set; }
    public decimal QuantityUsed { get; set; }
    public decimal HistoricalUnitPrice { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public string? CommentText { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
    public Material Material { get; set; } = null!;
}

public class AuditLog
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
