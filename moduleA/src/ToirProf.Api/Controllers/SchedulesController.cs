using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToirProf.Api.DTOs;
using ToirProf.Domain;

namespace ToirProf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly ToirDbContext _db;

    public SchedulesController(ToirDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ScheduleItemDto>>> GetSchedules(
        [FromQuery] int? workshopId,
        [FromQuery] int? equipmentId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] string? status)
    {
        var query = _db.MaintenanceSchedules
            .Include(ms => ms.Equipment).ThenInclude(e => e.Workshop)
            .Include(ms => ms.MaintenanceType)
            .Include(ms => ms.AssignedTeam)
            .AsQueryable();

        if (workshopId.HasValue && workshopId.Value > 0)
            query = query.Where(ms => ms.Equipment.WorkshopId == workshopId.Value);

        if (equipmentId.HasValue && equipmentId.Value > 0)
            query = query.Where(ms => ms.EquipmentId == equipmentId.Value);

        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            query = query.Where(ms => ms.PlannedDate.Month == month.Value);

        if (year.HasValue && year.Value > 2000)
            query = query.Where(ms => ms.PlannedDate.Year == year.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(ms => ms.ScheduleStatus.ToLower() == status.Trim().ToLower());

        var items = await query
            .OrderBy(ms => ms.PlannedDate)
            .Select(ms => new ScheduleItemDto
            {
                ScheduleId = ms.ScheduleId,
                EquipmentId = ms.EquipmentId,
                EquipmentName = ms.Equipment.EquipmentName,
                InventoryNumber = ms.Equipment.InventoryNumber,
                WorkshopName = ms.Equipment.Workshop.WorkshopName,
                MaintenanceTypeId = ms.MaintenanceTypeId,
                TypeCode = ms.MaintenanceType.TypeCode,
                TypeName = ms.MaintenanceType.TypeName,
                AssignedTeamId = ms.AssignedTeamId,
                TeamName = ms.AssignedTeam != null ? ms.AssignedTeam.TeamName : null,
                PlannedDate = ms.PlannedDate,
                ScheduleStatus = ms.ScheduleStatus,
                CreatedAt = ms.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ScheduleItemDto>> CreateSchedule([FromBody] CreateScheduleRequest req)
    {
        var equipment = await _db.Equipment.Include(e => e.Workshop).FirstOrDefaultAsync(e => e.EquipmentId == req.EquipmentId);
        if (equipment == null) return NotFound(new { message = "Оборудование не найдено" });

        var mType = await _db.MaintenanceTypes.FindAsync(req.MaintenanceTypeId);
        if (mType == null) return NotFound(new { message = "Тип ТО не найден" });

        var schedule = new MaintenanceSchedule
        {
            EquipmentId = req.EquipmentId,
            MaintenanceTypeId = req.MaintenanceTypeId,
            AssignedTeamId = req.AssignedTeamId,
            PlannedDate = req.PlannedDate == default ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)) : req.PlannedDate,
            ScheduleStatus = "planned",
            CreatedAt = DateTime.UtcNow
        };

        _db.MaintenanceSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        return Ok(new ScheduleItemDto
        {
            ScheduleId = schedule.ScheduleId,
            EquipmentId = equipment.EquipmentId,
            EquipmentName = equipment.EquipmentName,
            InventoryNumber = equipment.InventoryNumber,
            WorkshopName = equipment.Workshop.WorkshopName,
            MaintenanceTypeId = mType.MaintenanceTypeId,
            TypeCode = mType.TypeCode,
            TypeName = mType.TypeName,
            AssignedTeamId = schedule.AssignedTeamId,
            PlannedDate = schedule.PlannedDate,
            ScheduleStatus = schedule.ScheduleStatus,
            CreatedAt = schedule.CreatedAt
        });
    }

    [HttpPost("{id:int}/generate-order")]
    [Authorize]
    public async Task<ActionResult> GenerateOrderFromSchedule(int id, [FromBody] GenerateOrderFromScheduleRequest req)
    {
        var schedule = await _db.MaintenanceSchedules
            .Include(s => s.Equipment).ThenInclude(e => e.Workshop)
            .Include(s => s.MaintenanceType)
            .FirstOrDefaultAsync(s => s.ScheduleId == id);

        if (schedule == null) return NotFound(new { message = $"Запись графика ТО с ID {id} не найдена" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int currentUserId = int.TryParse(userIdClaim, out var uid) ? uid : 1;

        var year = DateTime.UtcNow.Year;
        var maxId = await _db.WorkOrders.MaxAsync(w => (int?)w.WorkOrderId) ?? 0;
        var orderNumber = $"Z-{year}-{(maxId + 1):D4}";

        var plannedStart = schedule.PlannedDate.ToDateTime(new TimeOnly(8, 0));
        var durationHours = (double)(schedule.MaintenanceType.StandardDurationHours > 0 ? schedule.MaintenanceType.StandardDurationHours : 4m);
        var plannedEnd = plannedStart.AddHours(durationHours);

        var wo = new WorkOrder
        {
            OrderNumber = orderNumber,
            OrderType = "planned",
            PriorityId = req.PriorityId > 0 ? req.PriorityId : 2,
            OrderStatusId = schedule.AssignedTeamId.HasValue ? 2 : 1,
            EquipmentId = schedule.EquipmentId,
            MasterUserId = currentUserId,
            AssignedTeamId = schedule.AssignedTeamId,
            ScheduleId = schedule.ScheduleId,
            Description = req.Description ?? $"Плановое обслуживание {schedule.MaintenanceType.TypeCode} ({schedule.MaintenanceType.TypeName})",
            PlannedStartDate = plannedStart,
            PlannedEndDate = plannedEnd,
            DowntimeHours = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        schedule.ScheduleStatus = "in_progress";
        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Заявка {wo.OrderNumber} успешно сформирована из графика ТО",
            workOrderId = wo.WorkOrderId,
            orderNumber = wo.OrderNumber
        });
    }

    [HttpGet("types")]
    public async Task<ActionResult<List<MaintenanceTypeDto>>> GetMaintenanceTypes()
    {
        var list = await _db.MaintenanceTypes
            .Where(m => m.IsActive)
            .Select(m => new MaintenanceTypeDto
            {
                MaintenanceTypeId = m.MaintenanceTypeId,
                TypeCode = m.TypeCode,
                TypeName = m.TypeName,
                PeriodicityDays = m.PeriodicityDays,
                StandardDurationHours = m.StandardDurationHours,
                RequiresStop = m.RequiresStop,
                Description = m.Description
            }).ToListAsync();

        return Ok(list);
    }
}
