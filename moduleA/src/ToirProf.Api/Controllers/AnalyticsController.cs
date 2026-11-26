using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToirProf.Api.DTOs;
using ToirProf.Domain;

namespace ToirProf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly ToirDbContext _db;

    public AnalyticsController(ToirDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardKpiDto>> GetDashboardKpi([FromQuery] int? workshopId)
    {
        var eqQuery = _db.Equipment.Include(e => e.Status).Include(e => e.Workshop).AsQueryable();
        var woQuery = _db.WorkOrders
            .Include(w => w.Equipment).ThenInclude(e => e.Workshop)
            .Include(w => w.Status).Include(w => w.Priority)
            .Include(w => w.MasterUser).Include(w => w.AssignedTeam)
            .AsQueryable();
        var schedQuery = _db.MaintenanceSchedules.AsQueryable();

        if (workshopId.HasValue && workshopId.Value > 0)
        {
            eqQuery = eqQuery.Where(e => e.WorkshopId == workshopId.Value);
            woQuery = woQuery.Where(w => w.Equipment.WorkshopId == workshopId.Value);
            schedQuery = schedQuery.Where(s => s.Equipment.WorkshopId == workshopId.Value);
        }

        var totalEq = await eqQuery.CountAsync();
        var activeOrders = await woQuery.CountAsync(w => w.OrderStatusId == 1 || w.OrderStatusId == 2 || w.OrderStatusId == 3);
        var completedOrders = await woQuery.CountAsync(w => w.OrderStatusId == 5 || w.OrderStatusId == 6);
        var totalDowntime = await woQuery.SumAsync(w => (decimal?)w.DowntimeHours) ?? 0m;

        var completedDurations = await woQuery
            .Where(w => (w.OrderStatusId == 5 || w.OrderStatusId == 6) && w.ActualStartDate.HasValue && w.ActualEndDate.HasValue)
            .Select(w => (decimal)(w.ActualEndDate!.Value - w.ActualStartDate!.Value).TotalHours)
            .ToListAsync();
        var avgRepairHours = completedDurations.Any() ? Math.Round(completedDurations.Average(), 1) : 0m;

        var totalSched = await schedQuery.CountAsync();
        var doneSched = await schedQuery.CountAsync(s => s.ScheduleStatus == "completed");
        var complianceRate = totalSched > 0 ? Math.Round((decimal)doneSched / totalSched * 100m, 1) : 100m;

        var statusStats = await eqQuery
            .GroupBy(e => new { e.Status.StatusCode, e.Status.StatusName })
            .Select(g => new EquipmentStatusStatDto
            {
                StatusCode = g.Key.StatusCode, StatusName = g.Key.StatusName, Count = g.Count()
            }).ToListAsync();

        var workshopStats = await _db.Workshops
            .Select(ws => new WorkshopWorkOrderStatDto
            {
                WorkshopId = ws.WorkshopId, WorkshopName = ws.WorkshopName,
                ActiveOrders = ws.EquipmentList.SelectMany(e => e.WorkOrders).Count(w => w.OrderStatusId == 1 || w.OrderStatusId == 2 || w.OrderStatusId == 3),
                CompletedOrders = ws.EquipmentList.SelectMany(e => e.WorkOrders).Count(w => w.OrderStatusId == 5 || w.OrderStatusId == 6)
            }).ToListAsync();

        var top5Downtime = await _db.Equipment
            .Include(e => e.Workshop)
            .Select(e => new TopDowntimeEquipmentDto
            {
                EquipmentId = e.EquipmentId, InventoryNumber = e.InventoryNumber,
                EquipmentName = e.EquipmentName, WorkshopName = e.Workshop.WorkshopName,
                DowntimeHours = e.WorkOrders.Sum(w => (decimal?)w.DowntimeHours) ?? 0m
            })
            .OrderByDescending(x => x.DowntimeHours).Take(5).ToListAsync();

        var recentOrders = await woQuery
            .OrderByDescending(w => w.CreatedAt).Take(10)
            .Select(w => new WorkOrderItemDto
            {
                WorkOrderId = w.WorkOrderId, OrderNumber = w.OrderNumber, OrderType = w.OrderType,
                PriorityId = w.PriorityId, PriorityCode = w.Priority.PriorityCode, PriorityName = w.Priority.PriorityName,
                OrderStatusId = w.OrderStatusId, StatusCode = w.Status.StatusCode, StatusName = w.Status.StatusName,
                EquipmentId = w.EquipmentId, EquipmentName = w.Equipment.EquipmentName, InventoryNumber = w.Equipment.InventoryNumber,
                WorkshopId = w.Equipment.WorkshopId, WorkshopName = w.Equipment.Workshop.WorkshopName,
                MasterUserId = w.MasterUserId, MasterName = $"{w.MasterUser.LastName} {w.MasterUser.FirstName}".Trim(),
                AssignedTeamId = w.AssignedTeamId, TeamName = w.AssignedTeam != null ? w.AssignedTeam.TeamName : null,
                PlannedStartDate = w.PlannedStartDate, PlannedEndDate = w.PlannedEndDate,
                ActualStartDate = w.ActualStartDate, ActualEndDate = w.ActualEndDate,
                DowntimeHours = w.DowntimeHours, CreatedAt = w.CreatedAt
            }).ToListAsync();

        var violations = new List<ViolationAlertDto>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueSchedules = await _db.MaintenanceSchedules
            .Include(s => s.Equipment).Include(s => s.MaintenanceType)
            .Where(s => s.ScheduleStatus == "planned" && s.PlannedDate < today)
            .Take(5).ToListAsync();

        foreach (var os in overdueSchedules)
        {
            violations.Add(new ViolationAlertDto
            {
                ViolationType = "OverdueSchedule",
                Title = $"Просрочено ТО: {os.Equipment.InventoryNumber}",
                Description = $"Плановая дата {os.PlannedDate:dd.MM.yyyy} по регламенту {os.MaintenanceType.TypeCode}",
                Severity = "danger"
            });
        }

        var lowStock = await _db.WarehouseMaterials
            .Include(wm => wm.Material)
            .Include(wm => wm.Warehouse)
            .Where(wm => wm.CurrentStock <= wm.MinimumStock)
            .Take(5).ToListAsync();
        foreach (var ls in lowStock)
        {
            violations.Add(new ViolationAlertDto
            {
                ViolationType = "LowStock",
                Title = $"Критический остаток: {ls.Material.MaterialName} ({ls.Warehouse.WarehouseCode})",
                Description = $"Остаток {ls.CurrentStock} {ls.Material.UnitOfMeasure} (норма: {ls.MinimumStock})",
                Severity = "warning"
            });
        }

        return Ok(new DashboardKpiDto
        {
            TotalEquipment = totalEq, ActiveWorkOrders = activeOrders, CompletedWorkOrders = completedOrders,
            TotalDowntimeHours = totalDowntime, AverageRepairHours = avgRepairHours,
            MaintenanceComplianceRate = complianceRate, EquipmentByStatus = statusStats,
            OrdersByWorkshop = workshopStats, Top5Downtime = top5Downtime,
            RecentWorkOrders = recentOrders, Violations = violations
        });
    }

    [HttpGet("workshop-summary/{workshopId:int}")]
    public async Task<ActionResult<List<WorkshopSummaryReportDto>>> GetWorkshopSummary(
        int workshopId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var end = endDate ?? DateTime.UtcNow;

        var result = new List<WorkshopSummaryReportDto>();
        var totalEq = await _db.Equipment.CountAsync(e => e.WorkshopId == workshopId);
        var activeEq = await _db.Equipment.CountAsync(e => e.WorkshopId == workshopId && e.StatusId == 1);
        var ordersInPeriod = await _db.WorkOrders
            .Where(w => w.Equipment.WorkshopId == workshopId && w.CreatedAt >= start && w.CreatedAt <= end).CountAsync();
        var downtimeSum = await _db.WorkOrders
            .Where(w => w.Equipment.WorkshopId == workshopId && w.CreatedAt >= start && w.CreatedAt <= end)
            .SumAsync(w => (decimal?)w.DowntimeHours) ?? 0m;

        result.Add(new WorkshopSummaryReportDto { MetricName = "Всего единиц оборудования", MetricValue = totalEq, Unit = "шт." });
        result.Add(new WorkshopSummaryReportDto { MetricName = "Оборудование в работе", MetricValue = activeEq, Unit = "шт." });
        result.Add(new WorkshopSummaryReportDto { MetricName = "Заявок на ремонт за период", MetricValue = ordersInPeriod, Unit = "ед." });
        result.Add(new WorkshopSummaryReportDto { MetricName = "Суммарный простой оборудования", MetricValue = downtimeSum, Unit = "час." });

        return Ok(result);
    }
}
