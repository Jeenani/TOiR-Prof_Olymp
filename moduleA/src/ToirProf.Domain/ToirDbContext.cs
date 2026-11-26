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
        var b = modelBuilder;
        base.OnModelCreating(b);

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

        // Связи, составные ключи и ограничения удаления
        b.Entity<TeamWorkshop>().HasOne(x => x.RepairTeam).WithMany(t => t.TeamWorkshops).HasForeignKey(x => x.TeamId);
        b.Entity<TeamWorkshop>().HasOne(x => x.Workshop).WithMany(w => w.TeamWorkshops).HasForeignKey(x => x.WorkshopId);
        b.Entity<TeamMember>().HasOne(x => x.RepairTeam).WithMany(t => t.TeamMembers).HasForeignKey(x => x.TeamId);
        b.Entity<TeamMember>().HasOne(x => x.User).WithMany(u => u.TeamMemberships).HasForeignKey(x => x.UserId);
        b.Entity<EquipmentModel>().HasOne(x => x.Manufacturer).WithMany(m => m.Models).HasForeignKey(x => x.ManufacturerId);
        b.Entity<Equipment>().HasOne(x => x.Model).WithMany(m => m.EquipmentList).HasForeignKey(x => x.ModelId);
        b.Entity<WarehouseMaterial>().HasOne(x => x.Warehouse).WithMany(w => w.WarehouseMaterials).HasForeignKey(x => x.WarehouseId);
        b.Entity<WarehouseMaterial>().HasOne(x => x.Material).WithMany(m => m.WarehouseMaterials).HasForeignKey(x => x.MaterialId);
        b.Entity<WorkOrderMaterial>().HasOne(x => x.WorkOrder).WithMany(w => w.WorkOrderMaterials).HasForeignKey(x => x.WorkOrderId);
        b.Entity<WorkOrderMaterial>().HasOne(x => x.WarehouseMaterial).WithMany(wm => wm.WorkOrderMaterials).HasForeignKey(x => new { x.WarehouseId, x.MaterialId });
        b.Entity<WorkOrder>().HasOne(x => x.Status).WithMany(s => s.WorkOrders).HasForeignKey(x => x.OrderStatusId);
        b.Entity<WorkOrder>().HasOne(x => x.Priority).WithMany(p => p.WorkOrders).HasForeignKey(x => x.PriorityId);
        b.Entity<WorkOrder>().HasOne(x => x.MasterUser).WithMany(u => u.CreatedWorkOrders).HasForeignKey(x => x.MasterUserId);
        b.Entity<WorkOrder>().HasOne(x => x.AssignedTeam).WithMany(t => t.WorkOrders).HasForeignKey(x => x.AssignedTeamId);
    }

    private static string ToSnakeCase(string input) =>
        string.IsNullOrEmpty(input) ? input : Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
}

