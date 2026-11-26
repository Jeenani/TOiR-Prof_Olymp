using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class WorkOrderEndpoints
{
    public static void MapWorkOrderEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/work-orders").WithTags("WorkOrders");

        g.MapGet("/", async (int? statusId, int? priorityId, int? workshopId, int? teamId, string? search, string? sortBy, bool? sortDesc, int? page, int? pageSize, ToirDbContext db) =>
        {
            int p = Math.Max(1, page ?? 1), ps = Math.Clamp(pageSize ?? 25, 1, 100);
            var q = db.WorkOrders.Include(w => w.Equipment).ThenInclude(e => e.Workshop).Include(w => w.Status).Include(w => w.Priority).Include(w => w.MasterUser).Include(w => w.AssignedTeam).AsQueryable();
            if (statusId > 0) q = q.Where(w => w.OrderStatusId == statusId);
            if (priorityId > 0) q = q.Where(w => w.PriorityId == priorityId);
            if (workshopId > 0) q = q.Where(w => w.Equipment.WorkshopId == workshopId);
            if (teamId > 0) q = q.Where(w => w.AssignedTeamId == teamId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(w => w.OrderNumber.ToLower().Contains(s) || w.Equipment.EquipmentName.ToLower().Contains(s) || w.Equipment.InventoryNumber.ToLower().Contains(s) || w.Description.ToLower().Contains(s));
            }
            q = (sortBy?.ToLower(), sortDesc ?? true) switch
            {
                ("ordernumber", false) => q.OrderBy(w => w.OrderNumber), ("ordernumber", true) => q.OrderByDescending(w => w.OrderNumber),
                ("priority", false) => q.OrderBy(w => w.PriorityId), ("priority", true) => q.OrderByDescending(w => w.PriorityId),
                ("status", false) => q.OrderBy(w => w.OrderStatusId), ("status", true) => q.OrderByDescending(w => w.OrderStatusId),
                (_, false) => q.OrderBy(w => w.CreatedAt), _ => q.OrderByDescending(w => w.CreatedAt)
            };
            var total = await q.CountAsync();
            var items = await q.Skip((p - 1) * ps).Take(ps).Select(w => new
            {
                w.WorkOrderId, w.OrderNumber, w.OrderType, w.PriorityId, w.Priority.PriorityCode, w.Priority.PriorityName,
                w.OrderStatusId, w.Status.StatusCode, w.Status.StatusName, w.EquipmentId, w.Equipment.EquipmentName, w.Equipment.InventoryNumber,
                w.Equipment.WorkshopId, w.Equipment.Workshop.WorkshopName, w.MasterUserId, MasterName = (w.MasterUser.LastName + " " + w.MasterUser.FirstName).Trim(),
                w.AssignedTeamId, TeamName = w.AssignedTeam != null ? w.AssignedTeam.TeamName : null, w.ScheduleId,
                w.PlannedStartDate, w.PlannedEndDate, w.ActualStartDate, w.ActualEndDate, w.DowntimeHours, w.CreatedAt
            }).ToListAsync();
            return Results.Ok(new { total, page = p, pageSize = ps, totalPages = (int)Math.Ceiling((double)total / ps), items });
        });

        g.MapGet("/statuses", async (ToirDbContext db) => Results.Ok(await db.WorkOrderStatuses.Select(s => new { s.OrderStatusId, s.StatusCode, s.StatusName }).ToListAsync()));
        g.MapGet("/priorities", async (ToirDbContext db) => Results.Ok(await db.WorkOrderPriorities.Select(p => new { p.PriorityId, p.PriorityCode, p.PriorityName }).ToListAsync()));

        g.MapGet("/{id:int}", async (int id, ToirDbContext db) =>
        {
            var wo = await db.WorkOrders.Include(x => x.Equipment).ThenInclude(e => e.Workshop).Include(x => x.Status).Include(x => x.Priority).Include(x => x.MasterUser).Include(x => x.AssignedTeam)
                .Include(x => x.WorkOrderMaterials).ThenInclude(m => m.WarehouseMaterial).ThenInclude(wm => wm.Material)
                .Include(x => x.WorkOrderMaterials).ThenInclude(m => m.WarehouseMaterial).ThenInclude(wm => wm.Warehouse)
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);
            if (wo == null) return Results.NotFound(new { message = $"Заявка с ID {id} не найдена" });

            return Results.Ok(new
            {
                wo.WorkOrderId, wo.OrderNumber, wo.OrderType, wo.PriorityId, wo.Priority.PriorityCode, wo.Priority.PriorityName,
                wo.OrderStatusId, wo.Status.StatusCode, wo.Status.StatusName, wo.EquipmentId, wo.Equipment.EquipmentName, wo.Equipment.InventoryNumber,
                wo.Equipment.WorkshopId, wo.Equipment.Workshop.WorkshopName, wo.MasterUserId, MasterName = (wo.MasterUser.LastName + " " + wo.MasterUser.FirstName).Trim(),
                wo.AssignedTeamId, TeamName = wo.AssignedTeam?.TeamName, wo.ScheduleId, wo.Description, wo.FailureReason, wo.ReportText,
                wo.PlannedStartDate, wo.PlannedEndDate, wo.ActualStartDate, wo.ActualEndDate, wo.DowntimeHours, wo.CreatedAt, wo.UpdatedAt,
                Materials = wo.WorkOrderMaterials.Select(m => new WorkOrderMaterialDto(m.WarehouseId, m.WarehouseMaterial.Warehouse.WarehouseName, m.MaterialId, m.WarehouseMaterial.Material.MaterialCode, m.WarehouseMaterial.Material.MaterialName, m.WarehouseMaterial.Material.UnitOfMeasure, m.QuantityUsed, m.HistoricalUnitPrice, m.UsedAt, m.CommentText)).ToList()
            });
        });

        MapWriteEndpoints(g);
    }
}
