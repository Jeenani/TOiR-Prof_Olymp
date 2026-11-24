using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ToirProf.Domain;

public class ToirDbContext : DbContext
{
    public ToirDbContext(DbContextOptions<ToirDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Workshop> Workshops => Set<Workshop>();
    public DbSet<RepairTeam> RepairTeams => Set<RepairTeam>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<EquipmentStatus> EquipmentStatuses => Set<EquipmentStatus>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentStatusHistory> EquipmentStatusHistories => Set<EquipmentStatusHistory>();
    public DbSet<MaintenanceType> MaintenanceTypes => Set<MaintenanceType>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
    public DbSet<WorkOrderStatus> WorkOrderStatuses => Set<WorkOrderStatus>();
    public DbSet<WorkOrderPriority> WorkOrderPriorities => Set<WorkOrderPriority>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<WorkOrderMaterial> WorkOrderMaterials => Set<WorkOrderMaterial>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Маппинг таблиц
        modelBuilder.Entity<Role>().ToTable("roles").HasKey(r => r.RoleId);
        modelBuilder.Entity<Workshop>().ToTable("workshops").HasKey(w => w.WorkshopId);
        modelBuilder.Entity<RepairTeam>().ToTable("repair_teams").HasKey(t => t.TeamId);
        modelBuilder.Entity<User>().ToTable("users").HasKey(u => u.UserId);
        modelBuilder.Entity<TeamMember>().ToTable("team_members").HasKey(tm => new { tm.TeamId, tm.UserId });
        modelBuilder.Entity<EquipmentStatus>().ToTable("equipment_statuses").HasKey(s => s.StatusId);
        modelBuilder.Entity<Equipment>().ToTable("equipment").HasKey(e => e.EquipmentId);
        modelBuilder.Entity<EquipmentStatusHistory>().ToTable("equipment_status_history").HasKey(h => h.HistoryId);
        modelBuilder.Entity<MaintenanceType>().ToTable("maintenance_types").HasKey(m => m.MaintenanceTypeId);
        modelBuilder.Entity<MaintenanceSchedule>().ToTable("maintenance_schedules").HasKey(ms => ms.ScheduleId);
        modelBuilder.Entity<WorkOrderStatus>().ToTable("work_order_statuses").HasKey(os => os.OrderStatusId);
        modelBuilder.Entity<WorkOrderPriority>().ToTable("work_order_priorities").HasKey(p => p.PriorityId);
        modelBuilder.Entity<WorkOrder>().ToTable("work_orders").HasKey(wo => wo.WorkOrderId);
        modelBuilder.Entity<Warehouse>().ToTable("warehouses").HasKey(wh => wh.WarehouseId);
        modelBuilder.Entity<Material>().ToTable("materials").HasKey(m => m.MaterialId);
        modelBuilder.Entity<WorkOrderMaterial>().ToTable("work_order_materials").HasKey(wom => new { wom.WorkOrderId, wom.MaterialId });
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs").HasKey(al => al.LogId);

        // Автоматический snake_case маппинг колонок
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var columnName = ToSnakeCase(property.Name);
                property.SetColumnName(columnName);
            }
        }

        // Каскадные правила и связи
        modelBuilder.Entity<WorkOrderMaterial>()
            .HasOne(wom => wom.WorkOrder)
            .WithMany(wo => wo.WorkOrderMaterials)
            .HasForeignKey(wom => wom.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(wo => wo.Equipment)
            .WithMany(e => e.WorkOrders)
            .HasForeignKey(wo => wo.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(wo => wo.Status)
            .WithMany(s => s.WorkOrders)
            .HasForeignKey(wo => wo.OrderStatusId);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(wo => wo.Priority)
            .WithMany(p => p.WorkOrders)
            .HasForeignKey(wo => wo.PriorityId);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(wo => wo.MasterUser)
            .WithMany(u => u.CreatedWorkOrders)
            .HasForeignKey(wo => wo.MasterUserId);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(wo => wo.AssignedTeam)
            .WithMany(t => t.WorkOrders)
            .HasForeignKey(wo => wo.AssignedTeamId);
    }


    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}

