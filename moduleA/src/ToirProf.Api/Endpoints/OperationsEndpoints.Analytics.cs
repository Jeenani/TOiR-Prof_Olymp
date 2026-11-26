using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class OperationsEndpoints
{
    private static void MapAnalyticsGroup(WebApplication app)
    {
        var an = app.MapGroup("/api/analytics").WithTags("Analytics");

        an.MapGet("/dashboard", async (int? workshopId, ToirDbContext db) =>
        {
            var eqQ = db.Equipment.Include(e => e.Status).Include(e => e.Workshop).AsQueryable();
            var woQ = db.WorkOrders.Include(w => w.Equipment).ThenInclude(e => e.Workshop).Include(w => w.Status).Include(w => w.Priority).Include(w => w.MasterUser).Include(w => w.AssignedTeam).AsQueryable();
            var schQ = db.MaintenanceSchedules.AsQueryable();
            if (workshopId > 0) { eqQ = eqQ.Where(e => e.WorkshopId == workshopId); woQ = woQ.Where(w => w.Equipment.WorkshopId == workshopId); schQ = schQ.Where(s => s.Equipment.WorkshopId == workshopId); }

            var durations = await woQ.Where(w => (w.OrderStatusId == 5 || w.OrderStatusId == 6) && w.ActualStartDate.HasValue && w.ActualEndDate.HasValue).Select(w => (decimal)(w.ActualEndDate!.Value - w.ActualStartDate!.Value).TotalHours).ToListAsync();
            var totalSch = await schQ.CountAsync();
            var doneSch = await schQ.CountAsync(s => s.ScheduleStatus == "completed");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var violations = (await db.MaintenanceSchedules.Include(s => s.Equipment).Include(s => s.MaintenanceType).Where(s => s.ScheduleStatus == "planned" && s.PlannedDate < today).OrderBy(s => s.PlannedDate).Take(5).ToListAsync())
                .Select(s => new { ViolationType = "OverdueSchedule", Title = $"Просрочено ТО: {s.Equipment.InventoryNumber}", Description = $"План {s.PlannedDate:dd.MM.yyyy} ({s.MaintenanceType.TypeCode})", Severity = "danger" })
                .Concat((await db.WarehouseMaterials.Include(wm => wm.Material).Include(wm => wm.Warehouse).Where(wm => wm.CurrentStock <= wm.MinimumStock).OrderBy(wm => wm.CurrentStock).Take(5).ToListAsync())
                .Select(ls => new { ViolationType = "LowStock", Title = $"Критический остаток: {ls.Material.MaterialName} ({ls.Warehouse.WarehouseCode})", Description = $"Остаток {ls.CurrentStock} {ls.Material.UnitOfMeasure} (норма: {ls.MinimumStock})", Severity = "warning" })).ToList();

            return Results.Ok(new
            {
                TotalEquipment = await eqQ.CountAsync(),
                ActiveWorkOrders = await woQ.CountAsync(w => w.OrderStatusId <= 3),
                CompletedWorkOrders = await woQ.CountAsync(w => w.OrderStatusId == 5 || w.OrderStatusId == 6),
                TotalDowntimeHours = await woQ.SumAsync(w => (decimal?)w.DowntimeHours) ?? 0m,
                AverageRepairHours = durations.Any() ? Math.Round(durations.Average(), 1) : 0m,
                MaintenanceComplianceRate = totalSch > 0 ? Math.Round((decimal)doneSch / totalSch * 100m, 1) : 100m,
                EquipmentByStatus = await eqQ.GroupBy(e => new { e.Status.StatusCode, e.Status.StatusName }).Select(g => new { g.Key.StatusCode, g.Key.StatusName, Count = g.Count() }).ToListAsync(),
                OrdersByWorkshop = await db.Workshops.Select(ws => new { ws.WorkshopId, ws.WorkshopName, ActiveOrders = ws.EquipmentList.SelectMany(e => e.WorkOrders).Count(w => w.OrderStatusId <= 3), CompletedOrders = ws.EquipmentList.SelectMany(e => e.WorkOrders).Count(w => w.OrderStatusId == 5 || w.OrderStatusId == 6) }).ToListAsync(),
                Top5Downtime = await db.Equipment.Include(e => e.Workshop).Select(e => new { e.EquipmentId, e.InventoryNumber, e.EquipmentName, e.Workshop.WorkshopName, DowntimeHours = e.WorkOrders.Sum(w => (decimal?)w.DowntimeHours) ?? 0m }).OrderByDescending(x => x.DowntimeHours).Take(5).ToListAsync(),
                RecentWorkOrders = await woQ.OrderByDescending(w => w.CreatedAt).Take(10).Select(w => new { w.WorkOrderId, w.OrderNumber, w.OrderType, w.PriorityId, w.Priority.PriorityCode, w.Priority.PriorityName, w.OrderStatusId, w.Status.StatusCode, w.Status.StatusName, w.EquipmentId, w.Equipment.EquipmentName, w.Equipment.InventoryNumber, w.Equipment.WorkshopId, w.Equipment.Workshop.WorkshopName, w.MasterUserId, MasterName = (w.MasterUser.LastName + " " + w.MasterUser.FirstName).Trim(), w.AssignedTeamId, TeamName = w.AssignedTeam != null ? w.AssignedTeam.TeamName : null, w.PlannedStartDate, w.PlannedEndDate, w.ActualStartDate, w.ActualEndDate, w.DowntimeHours, w.CreatedAt }).ToListAsync(),
                Violations = violations
            });
        });

        an.MapGet("/workshop-summary/{workshopId:int}", async (int workshopId, DateTime? startDate, DateTime? endDate, ToirDbContext db) =>
        {
            var s = startDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
            var e = endDate ?? DateTime.UtcNow;
            return Results.Ok(new[]
            {
                new { MetricName = "Всего единиц оборудования", MetricValue = (decimal)await db.Equipment.CountAsync(x => x.WorkshopId == workshopId), Unit = "шт." },
                new { MetricName = "Оборудование в работе", MetricValue = (decimal)await db.Equipment.CountAsync(x => x.WorkshopId == workshopId && x.StatusId == 1), Unit = "шт." },
                new { MetricName = "Заявок на ремонт за период", MetricValue = (decimal)await db.WorkOrders.CountAsync(w => w.Equipment.WorkshopId == workshopId && w.CreatedAt >= s && w.CreatedAt <= e), Unit = "ед." },
                new { MetricName = "Суммарный простой оборудования", MetricValue = await db.WorkOrders.Where(w => w.Equipment.WorkshopId == workshopId && w.CreatedAt >= s && w.CreatedAt <= e).SumAsync(w => (decimal?)w.DowntimeHours) ?? 0m, Unit = "час." }
            });
        });

        an.MapGet("/details", async (int? workshopId, string? orderType, ToirDbContext db) =>
        {
            var woQ = db.WorkOrders.Include(w => w.Equipment).AsQueryable();
            if (workshopId > 0) woQ = woQ.Where(w => w.Equipment.WorkshopId == workshopId);
            if (!string.IsNullOrWhiteSpace(orderType) && orderType != "all") woQ = woQ.Where(w => w.OrderType == orderType);

            var dynamics = await woQ.OrderBy(w => w.CreatedAt)
                .Select(w => new { Label = w.CreatedAt.ToString("dd.MM"), w.DowntimeHours })
                .ToListAsync();

            var topMaterials = await db.WorkOrderMaterials.Include(m => m.WarehouseMaterial).ThenInclude(wm => wm.Material)
                .GroupBy(m => new { m.WarehouseMaterial.Material.MaterialCode, m.WarehouseMaterial.Material.MaterialName, m.WarehouseMaterial.Material.UnitOfMeasure })
                .Select(g => new { Code = g.Key.MaterialCode, Name = g.Key.MaterialName, Unit = g.Key.UnitOfMeasure, Used = g.Sum(x => x.QuantityUsed), Cost = g.Sum(x => x.QuantityUsed * x.HistoricalUnitPrice) })
                .OrderByDescending(x => x.Cost).Take(10).ToListAsync();

            var teamsRating = await db.RepairTeams.Include(t => t.TeamMembers).ThenInclude(tm => tm.User).Include(t => t.WorkOrders)
                .Select(t => new
                {
                    Id = t.TeamId, Name = t.TeamName,
                    Leader = t.TeamMembers.Where(m => m.IsTeamLeader).Select(m => (m.User.LastName + " " + m.User.FirstName)).FirstOrDefault() ?? "—",
                    OrdersDone = t.WorkOrders.Count(w => w.OrderStatusId == 5 || w.OrderStatusId == 6),
                    AvgHours = t.WorkOrders.Where(w => w.DowntimeHours > 0).Select(w => (decimal?)w.DowntimeHours).Average() ?? 0m
                }).ToListAsync();

            var schByWorkshop = await db.Workshops.Select(ws => new
            {
                ws.WorkshopName,
                Planned = ws.EquipmentList.SelectMany(e => e.MaintenanceSchedules).Count(),
                Completed = ws.EquipmentList.SelectMany(e => e.MaintenanceSchedules).Count(s => s.ScheduleStatus == "completed")
            }).ToListAsync();

            var ordersByStatus = await woQ.GroupBy(w => new { w.OrderStatusId, w.Status.StatusName })
                .Select(g => new { g.Key.OrderStatusId, g.Key.StatusName, Count = g.Count(), Downtime = g.Sum(x => x.DowntimeHours) })
                .OrderBy(x => x.OrderStatusId).ToListAsync();

            var ordersByPriority = await woQ.GroupBy(w => w.Priority.PriorityName)
                .Select(g => new { PriorityName = g.Key, Count = g.Count() }).ToListAsync();

            return Results.Ok(new { dynamics, topMaterials, teamsRating, schByWorkshop, ordersByStatus, ordersByPriority });
        });

        an.MapGet("/stream", async (HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            await ctx.Response.WriteAsync($"data: {{\"type\":\"connected\",\"ts\":\"{DateTime.UtcNow:O}\"}}\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(15000, ct);
                    await ctx.Response.WriteAsync($"data: {{\"type\":\"heartbeat\",\"ts\":\"{DateTime.UtcNow:O}\"}}\n\n", ct);
                    await ctx.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
        });
    }
}
