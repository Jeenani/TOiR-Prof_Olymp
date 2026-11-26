using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class EquipmentEndpoints
{
    public static void MapEquipmentEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/equipment").WithTags("Equipment");

        g.MapGet("/", async (int? workshopId, int? statusId, string? model, string? search, string? sortBy, bool? sortDesc, int? page, int? pageSize, ToirDbContext db) =>
        {
            int p = Math.Max(1, page ?? 1), ps = Math.Clamp(pageSize ?? 25, 1, 100);
            var q = db.Equipment.Include(e => e.Workshop).Include(e => e.Status).Include(e => e.Model).ThenInclude(m => m.Manufacturer).AsQueryable();
            if (workshopId > 0) q = q.Where(e => e.WorkshopId == workshopId);
            if (statusId > 0) q = q.Where(e => e.StatusId == statusId);
            if (!string.IsNullOrWhiteSpace(model)) q = q.Where(e => e.Model.ModelName.ToLower().Contains(model.Trim().ToLower()));
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(e => e.InventoryNumber.ToLower().Contains(s) || e.EquipmentName.ToLower().Contains(s) || e.Model.ModelName.ToLower().Contains(s));
            }
            q = (sortBy?.ToLower(), sortDesc ?? false) switch
            {
                ("name", false) => q.OrderBy(e => e.EquipmentName), ("name", true) => q.OrderByDescending(e => e.EquipmentName),
                ("status", false) => q.OrderBy(e => e.Status.StatusName), ("status", true) => q.OrderByDescending(e => e.Status.StatusName),
                ("workshop", false) => q.OrderBy(e => e.Workshop.WorkshopName), ("workshop", true) => q.OrderByDescending(e => e.Workshop.WorkshopName),
                (_, true) => q.OrderByDescending(e => e.InventoryNumber), _ => q.OrderBy(e => e.InventoryNumber)
            };
            var total = await q.CountAsync();
            var items = await q.Skip((p - 1) * ps).Take(ps).Select(e => new
            {
                e.EquipmentId, e.WorkshopId, e.Workshop.WorkshopName, e.StatusId, e.Status.StatusCode, e.Status.StatusName,
                e.InventoryNumber, e.EquipmentName, e.Model.ModelName, e.SerialNumber, Manufacturer = e.Model.Manufacturer.ManufacturerName,
                e.WarrantyUntil, e.PassportUrl, e.CommissioningDate, e.OperationDate, e.DecommissioningDate, e.CreatedAt
            }).ToListAsync();
            return Results.Ok(new { total, page = p, pageSize = ps, totalPages = (int)Math.Ceiling((double)total / ps), items });
        });

        g.MapGet("/statuses", async (ToirDbContext db) =>
            Results.Ok(await db.EquipmentStatuses.Select(s => new { s.StatusId, s.StatusCode, s.StatusName, s.Description }).ToListAsync()));

        g.MapGet("/{id:int}", async (int id, ToirDbContext db) =>
        {
            var e = await db.Equipment.Include(x => x.Workshop).Include(x => x.Status).Include(x => x.Model).ThenInclude(m => m.Manufacturer)
                .Include(x => x.StatusHistory).ThenInclude(h => h.Status)
                .Include(x => x.MaintenanceSchedules).ThenInclude(ms => ms.MaintenanceType)
                .Include(x => x.MaintenanceSchedules).ThenInclude(ms => ms.AssignedTeam)
                .Include(x => x.WorkOrders).ThenInclude(wo => wo.Status).Include(x => x.WorkOrders).ThenInclude(wo => wo.Priority)
                .Include(x => x.WorkOrders).ThenInclude(wo => wo.MasterUser)
                .FirstOrDefaultAsync(x => x.EquipmentId == id);
            if (e == null) return Results.NotFound(new { message = $"Оборудование с ID {id} не найдено" });

            return Results.Ok(new
            {
                e.EquipmentId, e.WorkshopId, e.Workshop.WorkshopName, e.StatusId, e.Status.StatusCode, e.Status.StatusName,
                e.InventoryNumber, e.EquipmentName, e.Model.ModelName, e.SerialNumber, Manufacturer = e.Model.Manufacturer.ManufacturerName,
                e.WarrantyUntil, e.PassportUrl, e.CommissioningDate, e.OperationDate, e.DecommissioningDate, e.CreatedAt, e.UpdatedAt,
                StatusHistory = e.StatusHistory.OrderByDescending(h => h.ChangedAt).Select(h => new { h.HistoryId, h.StatusId, h.Status.StatusName, h.Reason, h.ChangedAt }),
                Schedules = e.MaintenanceSchedules.OrderBy(s => s.PlannedDate).Select(s => new { s.ScheduleId, s.EquipmentId, e.EquipmentName, e.InventoryNumber, e.Workshop.WorkshopName, s.MaintenanceTypeId, s.MaintenanceType.TypeCode, s.MaintenanceType.TypeName, s.AssignedTeamId, TeamName = s.AssignedTeam?.TeamName, s.PlannedDate, s.ScheduleStatus, s.CreatedAt }),
                RecentWorkOrders = e.WorkOrders.OrderByDescending(w => w.CreatedAt).Take(5).Select(w => new { w.WorkOrderId, w.OrderNumber, w.OrderType, w.PriorityId, w.Priority.PriorityCode, w.Priority.PriorityName, w.OrderStatusId, w.Status.StatusCode, w.Status.StatusName, w.EquipmentId, e.EquipmentName, e.InventoryNumber, e.WorkshopId, e.Workshop.WorkshopName, w.MasterUserId, MasterName = (w.MasterUser.LastName + " " + w.MasterUser.FirstName).Trim(), w.PlannedStartDate, w.PlannedEndDate, w.ActualStartDate, w.ActualEndDate, w.DowntimeHours, w.CreatedAt })
            });
        });

        MapWriteEndpoints(g);
    }
}
