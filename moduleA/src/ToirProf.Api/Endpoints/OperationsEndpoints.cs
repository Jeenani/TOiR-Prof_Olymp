using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class OperationsEndpoints
{
    public static void MapOperationsEndpoints(this WebApplication app)
    {
        var sch = app.MapGroup("/api/schedules").WithTags("Schedules");

        sch.MapGet("/", async (int? workshopId, int? equipmentId, int? month, int? year, string? status, ToirDbContext db) =>
        {
            var q = db.MaintenanceSchedules.Include(ms => ms.Equipment).ThenInclude(e => e.Workshop).Include(ms => ms.MaintenanceType).Include(ms => ms.AssignedTeam).AsQueryable();
            if (workshopId > 0) q = q.Where(ms => ms.Equipment.WorkshopId == workshopId);
            if (equipmentId > 0) q = q.Where(ms => ms.EquipmentId == equipmentId);
            if (month is >= 1 and <= 12) q = q.Where(ms => ms.PlannedDate.Month == month);
            if (year > 2000) q = q.Where(ms => ms.PlannedDate.Year == year);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(ms => ms.ScheduleStatus.ToLower() == status.Trim().ToLower());
            return Results.Ok(await q.OrderBy(ms => ms.PlannedDate).Select(ms => new
            {
                ms.ScheduleId, ms.EquipmentId, ms.Equipment.EquipmentName, ms.Equipment.InventoryNumber, ms.Equipment.Workshop.WorkshopName,
                ms.MaintenanceTypeId, ms.MaintenanceType.TypeCode, ms.MaintenanceType.TypeName, ms.AssignedTeamId,
                TeamName = ms.AssignedTeam != null ? ms.AssignedTeam.TeamName : null, ms.PlannedDate, ms.ScheduleStatus, ms.CreatedAt
            }).ToListAsync());
        });

        sch.MapGet("/types", async (ToirDbContext db) =>
            Results.Ok(await db.MaintenanceTypes.Where(m => m.IsActive).Select(m => new { m.MaintenanceTypeId, m.TypeCode, m.TypeName, m.PeriodicityDays, m.StandardDurationHours, m.RequiresStop, m.Description }).ToListAsync()));

        sch.MapPost("/", async (CreateScheduleReq req, ToirDbContext db) =>
        {
            var eq = await db.Equipment.Include(e => e.Workshop).FirstOrDefaultAsync(e => e.EquipmentId == req.EquipmentId);
            var mt = await db.MaintenanceTypes.FindAsync(req.MaintenanceTypeId);
            if (eq == null || mt == null) return Results.NotFound(new { message = "Оборудование или вид ТО не найден" });
            var item = new MaintenanceSchedule { EquipmentId = req.EquipmentId, MaintenanceTypeId = req.MaintenanceTypeId, AssignedTeamId = req.AssignedTeamId, PlannedDate = req.PlannedDate == default ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)) : req.PlannedDate };
            db.MaintenanceSchedules.Add(item);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.ScheduleId, eq.EquipmentId, eq.EquipmentName, eq.InventoryNumber, eq.Workshop.WorkshopName, mt.MaintenanceTypeId, mt.TypeCode, mt.TypeName, item.AssignedTeamId, item.PlannedDate, item.ScheduleStatus, item.CreatedAt });
        }).RequireAuthorization();

        sch.MapPost("/{id:int}/generate-order", async (int id, GenerateOrderReq req, ClaimsPrincipal user, ToirDbContext db) =>
        {
            var s = await db.MaintenanceSchedules.Include(x => x.Equipment).Include(x => x.MaintenanceType).FirstOrDefaultAsync(x => x.ScheduleId == id);
            if (s == null) return Results.NotFound(new { message = $"Запись графика {id} не найдена" });
            var maxId = await db.WorkOrders.MaxAsync(w => (int?)w.WorkOrderId) ?? 0;
            var start = s.PlannedDate.ToDateTime(new TimeOnly(8, 0));
            var hrs = (double)(s.MaintenanceType.StandardDurationHours > 0 ? s.MaintenanceType.StandardDurationHours : 4m);
            var wo = new WorkOrder
            {
                OrderNumber = $"Z-{DateTime.UtcNow.Year}-{(maxId + 1):D4}", OrderType = "planned", PriorityId = req.PriorityId > 0 ? req.PriorityId : 2,
                OrderStatusId = s.AssignedTeamId.HasValue ? 2 : 1, EquipmentId = s.EquipmentId, MasterUserId = JwtHelper.GetUserId(user) ?? 1,
                AssignedTeamId = s.AssignedTeamId, ScheduleId = s.ScheduleId,
                Description = req.Description ?? $"Плановое ТО {s.MaintenanceType.TypeCode} ({s.MaintenanceType.TypeName})",
                PlannedStartDate = start, PlannedEndDate = start.AddHours(hrs)
            };
            s.ScheduleStatus = "in_progress";
            db.WorkOrders.Add(wo);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"Заявка {wo.OrderNumber} сформирована", workOrderId = wo.WorkOrderId, orderNumber = wo.OrderNumber });
        }).RequireAuthorization();

        MapMaterialAndAnalytics(app);
    }
}
