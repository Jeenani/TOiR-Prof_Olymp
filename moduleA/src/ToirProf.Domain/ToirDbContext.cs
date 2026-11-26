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
    public DbSet<TeamWorkshop> TeamWorkshops => Set<TeamWorkshop>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<EquipmentStatus> EquipmentStatuses => Set<EquipmentStatus>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<EquipmentModel> EquipmentModels => Set<EquipmentModel>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentStatusHistory> EquipmentStatusHistories => Set<EquipmentStatusHistory>();
    public DbSet<MaintenanceType> MaintenanceTypes => Set<MaintenanceType>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
    public DbSet<WorkOrderStatus> WorkOrderStatuses => Set<WorkOrderStatus>();
    public DbSet<WorkOrderPriority> WorkOrderPriorities => Set<WorkOrderPriority>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<WarehouseMaterial> WarehouseMaterials => Set<WarehouseMaterial>();
    public DbSet<WorkOrderMaterial> WorkOrderMaterials => Set<WorkOrderMaterial>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Маппинг таблиц
        modelBuilder.Entity<Role>().ToTable("roles").HasKey(r => r.RoleId);
        modelBuilder.Entity<Workshop>().ToTable("workshops").HasKey(w => w.WorkshopId);
        modelBuilder.Entity<RepairTeam>().ToTable("repair_teams").HasKey(t => t.TeamId);
        modelBuilder.Entity<TeamWorkshop>().ToTable("team_workshops").HasKey(tw => new { tw.TeamId, tw.WorkshopId });
        modelBuilder.Entity<User>().ToTable("users").HasKey(u => u.UserId);
        modelBuilder.Entity<TeamMember>().ToTable("team_members").HasKey(tm => new { tm.TeamId, tm.UserId });
        modelBuilder.Entity<EquipmentStatus>().ToTable("equipment_statuses").HasKey(s => s.StatusId);
        modelBuilder.Entity<Manufacturer>().ToTable("manufacturers").HasKey(m => m.ManufacturerId);
        modelBuilder.Entity<EquipmentModel>().ToTable("equipment_models").HasKey(em => em.ModelId);
        modelBuilder.Entity<Equipment>().ToTable("equipment").HasKey(e => e.EquipmentId);
        modelBuilder.Entity<EquipmentStatusHistory>().ToTable("equipment_status_history").HasKey(h => h.HistoryId);
        modelBuilder.Entity<MaintenanceType>().ToTable("maintenance_types").HasKey(m => m.MaintenanceTypeId);
        modelBuilder.Entity<MaintenanceSchedule>().ToTable("maintenance_schedules").HasKey(ms => ms.ScheduleId);
        modelBuilder.Entity<WorkOrderStatus>().ToTable("work_order_statuses").HasKey(os => os.OrderStatusId);
        modelBuilder.Entity<WorkOrderPriority>().ToTable("work_order_priorities").HasKey(p => p.PriorityId);
        modelBuilder.Entity<WorkOrder>().ToTable("work_orders").HasKey(wo => wo.WorkOrderId);
        modelBuilder.Entity<Warehouse>().ToTable("warehouses").HasKey(wh => wh.WarehouseId);
        modelBuilder.Entity<Supplier>().ToTable("suppliers").HasKey(sp => sp.SupplierId);
        modelBuilder.Entity<Material>().ToTable("materials").HasKey(m => m.MaterialId);
        modelBuilder.Entity<WarehouseMaterial>().ToTable("warehouse_materials").HasKey(wm => new { wm.WarehouseId, wm.MaterialId });
        modelBuilder.Entity<WorkOrderMaterial>().ToTable("work_order_materials").HasKey(wom => new { wom.WorkOrderId, wom.WarehouseId, wom.MaterialId });
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
        modelBuilder.Entity<TeamWorkshop>()
            .HasOne(tw => tw.RepairTeam)
            .WithMany(t => t.TeamWorkshops)
            .HasForeignKey(tw => tw.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeamWorkshop>()
            .HasOne(tw => tw.Workshop)
            .WithMany(w => w.TeamWorkshops)
            .HasForeignKey(tw => tw.WorkshopId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeamMember>()
            .HasOne(tm => tm.RepairTeam)
            .WithMany(t => t.TeamMembers)
            .HasForeignKey(tm => tm.TeamId);

        modelBuilder.Entity<TeamMember>()
            .HasOne(tm => tm.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.UserId);

        modelBuilder.Entity<EquipmentModel>()
            .HasOne(em => em.Manufacturer)
            .WithMany(m => m.Models)
            .HasForeignKey(em => em.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.Model)
            .WithMany(em => em.EquipmentList)
            .HasForeignKey(e => e.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Material>()
            .HasOne(m => m.Supplier)
            .WithMany(s => s.Materials)
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WarehouseMaterial>()
            .HasOne(wm => wm.Warehouse)
            .WithMany(w => w.WarehouseMaterials)
            .HasForeignKey(wm => wm.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WarehouseMaterial>()
            .HasOne(wm => wm.Material)
            .WithMany(m => m.WarehouseMaterials)
            .HasForeignKey(wm => wm.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkOrderMaterial>()
            .HasOne(wom => wom.WorkOrder)
            .WithMany(wo => wo.WorkOrderMaterials)
            .HasForeignKey(wom => wom.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkOrderMaterial>()
            .HasOne(wom => wom.WarehouseMaterial)
            .WithMany(wm => wm.WorkOrderMaterials)
            .HasForeignKey(wom => new { wom.WarehouseId, wom.MaterialId })
            .OnDelete(DeleteBehavior.Restrict);

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

