using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToirProf.Api.DTOs;
using ToirProf.Domain;

namespace ToirProf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly ToirDbContext _db;

    public EquipmentController(ToirDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetEquipment(
        [FromQuery] int? workshopId, [FromQuery] int? statusId, [FromQuery] string? model,
        [FromQuery] string? search, [FromQuery] string? sortBy = "inventoryNumber",
        [FromQuery] bool sortDesc = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Equipment
            .Include(e => e.Workshop)
            .Include(e => e.Status)
            .Include(e => e.Model).ThenInclude(m => m.Manufacturer)
            .AsQueryable();
        if (workshopId.HasValue && workshopId.Value > 0) query = query.Where(e => e.WorkshopId == workshopId.Value);
        if (statusId.HasValue && statusId.Value > 0) query = query.Where(e => e.StatusId == statusId.Value);
        if (!string.IsNullOrWhiteSpace(model)) query = query.Where(e => e.Model.ModelName.ToLower().Contains(model.Trim().ToLower()));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.InventoryNumber.ToLower().Contains(s) ||
                                     e.EquipmentName.ToLower().Contains(s) ||
                                     e.Model.ModelName.ToLower().Contains(s));
        }

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("name", false) => query.OrderBy(e => e.EquipmentName),
            ("name", true) => query.OrderByDescending(e => e.EquipmentName),
            ("status", false) => query.OrderBy(e => e.Status.StatusName),
            ("status", true) => query.OrderByDescending(e => e.Status.StatusName),
            ("workshop", false) => query.OrderBy(e => e.Workshop.WorkshopName),
            ("workshop", true) => query.OrderByDescending(e => e.Workshop.WorkshopName),
            (_, true) => query.OrderByDescending(e => e.InventoryNumber),
            _ => query.OrderBy(e => e.InventoryNumber)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(e => new EquipmentItemDto
        {
            EquipmentId = e.EquipmentId, WorkshopId = e.WorkshopId, WorkshopName = e.Workshop.WorkshopName,
            StatusId = e.StatusId, StatusCode = e.Status.StatusCode, StatusName = e.Status.StatusName,
            InventoryNumber = e.InventoryNumber, EquipmentName = e.EquipmentName, ModelName = e.Model.ModelName,
            SerialNumber = e.SerialNumber, Manufacturer = e.Model.Manufacturer.ManufacturerName, WarrantyUntil = e.WarrantyUntil,
            PassportUrl = e.PassportUrl, CommissioningDate = e.CommissioningDate, OperationDate = e.OperationDate,
            DecommissioningDate = e.DecommissioningDate, CreatedAt = e.CreatedAt
        }).ToListAsync();

        return Ok(new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize), items });
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<List<EquipmentStatusDto>>> GetStatuses()
    {
        return Ok(await _db.EquipmentStatuses.Select(s => new EquipmentStatusDto
        {
            StatusId = s.StatusId, StatusCode = s.StatusCode, StatusName = s.StatusName, Description = s.Description
        }).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentDetailDto>> GetEquipmentById(int id)
    {
        var e = await _db.Equipment
            .Include(x => x.Workshop).Include(x => x.Status)
            .Include(x => x.Model).ThenInclude(m => m.Manufacturer)
            .Include(x => x.StatusHistory).ThenInclude(h => h.Status)
            .Include(x => x.MaintenanceSchedules).ThenInclude(ms => ms.MaintenanceType)
            .Include(x => x.MaintenanceSchedules).ThenInclude(ms => ms.AssignedTeam)
            .Include(x => x.WorkOrders).ThenInclude(wo => wo.Status)
            .Include(x => x.WorkOrders).ThenInclude(wo => wo.Priority)
            .Include(x => x.WorkOrders).ThenInclude(wo => wo.MasterUser)
            .FirstOrDefaultAsync(x => x.EquipmentId == id);

        if (e == null) return NotFound(new { message = $"Оборудование с ID {id} не найдено" });

        return Ok(new EquipmentDetailDto
        {
            EquipmentId = e.EquipmentId, WorkshopId = e.WorkshopId, WorkshopName = e.Workshop.WorkshopName,
            StatusId = e.StatusId, StatusCode = e.Status.StatusCode, StatusName = e.Status.StatusName,
            InventoryNumber = e.InventoryNumber, EquipmentName = e.EquipmentName, ModelName = e.Model.ModelName,
            SerialNumber = e.SerialNumber, Manufacturer = e.Model.Manufacturer.ManufacturerName, WarrantyUntil = e.WarrantyUntil,
            PassportUrl = e.PassportUrl, CommissioningDate = e.CommissioningDate, OperationDate = e.OperationDate,
            DecommissioningDate = e.DecommissioningDate, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            StatusHistory = e.StatusHistory.OrderByDescending(h => h.ChangedAt).Select(h => new EquipmentStatusHistoryDto
            {
                HistoryId = h.HistoryId, StatusId = h.StatusId, StatusName = h.Status.StatusName, Reason = h.Reason, ChangedAt = h.ChangedAt
            }).ToList(),
            Schedules = e.MaintenanceSchedules.OrderBy(s => s.PlannedDate).Select(s => new ScheduleItemDto
            {
                ScheduleId = s.ScheduleId, EquipmentId = s.EquipmentId, EquipmentName = e.EquipmentName,
                InventoryNumber = e.InventoryNumber, WorkshopName = e.Workshop.WorkshopName,
                MaintenanceTypeId = s.MaintenanceTypeId, TypeCode = s.MaintenanceType.TypeCode,
                TypeName = s.MaintenanceType.TypeName, AssignedTeamId = s.AssignedTeamId,
                TeamName = s.AssignedTeam != null ? s.AssignedTeam.TeamName : null,
                PlannedDate = s.PlannedDate, ScheduleStatus = s.ScheduleStatus, CreatedAt = s.CreatedAt
            }).ToList(),
            RecentWorkOrders = e.WorkOrders.OrderByDescending(wo => wo.CreatedAt).Take(5).Select(wo => new WorkOrderItemDto
            {
                WorkOrderId = wo.WorkOrderId, OrderNumber = wo.OrderNumber, OrderType = wo.OrderType,
                PriorityId = wo.PriorityId, PriorityCode = wo.Priority.PriorityCode, PriorityName = wo.Priority.PriorityName,
                OrderStatusId = wo.OrderStatusId, StatusCode = wo.Status.StatusCode, StatusName = wo.Status.StatusName,
                EquipmentId = wo.EquipmentId, EquipmentName = e.EquipmentName, InventoryNumber = e.InventoryNumber,
                WorkshopId = e.WorkshopId, WorkshopName = e.Workshop.WorkshopName, MasterUserId = wo.MasterUserId,
                MasterName = $"{wo.MasterUser.LastName} {wo.MasterUser.FirstName}",
                PlannedStartDate = wo.PlannedStartDate, PlannedEndDate = wo.PlannedEndDate,
                ActualStartDate = wo.ActualStartDate, ActualEndDate = wo.ActualEndDate,
                DowntimeHours = wo.DowntimeHours, CreatedAt = wo.CreatedAt
            }).ToList()
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<EquipmentItemDto>> CreateEquipment([FromBody] CreateEquipmentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.InventoryNumber) || string.IsNullOrWhiteSpace(req.EquipmentName))
            return BadRequest(new { message = "Инвентарный номер и наименование обязательны" });

        var exists = await _db.Equipment.AnyAsync(e => e.InventoryNumber.ToLower() == req.InventoryNumber.Trim().ToLower());
        if (exists) return Conflict(new { message = $"Оборудование с номером '{req.InventoryNumber}' уже существует" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? currentUserId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        var modelEntity = await FindOrCreateModelAsync(req.ModelName, req.Manufacturer);

        var equipment = new Equipment
        {
            WorkshopId = req.WorkshopId, StatusId = req.StatusId > 0 ? req.StatusId : 1,
            ModelId = modelEntity.ModelId,
            InventoryNumber = req.InventoryNumber.Trim(), EquipmentName = req.EquipmentName.Trim(),
            SerialNumber = req.SerialNumber?.Trim(),
            CommissioningDate = req.CommissioningDate == default ? DateOnly.FromDateTime(DateTime.UtcNow) : req.CommissioningDate,
            OperationDate = req.OperationDate == default ? DateOnly.FromDateTime(DateTime.UtcNow) : req.OperationDate,
            WarrantyUntil = req.WarrantyUntil, PassportUrl = req.PassportUrl?.Trim(),
            CreatedByUserId = currentUserId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = currentUserId, EntityName = "equipment", EntityId = equipment.EquipmentId,
            ActionType = "CREATE", NewValue = $"Создано '{equipment.EquipmentName}' ({equipment.InventoryNumber})",
            LoggedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEquipmentById), new { id = equipment.EquipmentId }, new EquipmentItemDto
        {
            EquipmentId = equipment.EquipmentId, WorkshopId = equipment.WorkshopId, StatusId = equipment.StatusId,
            InventoryNumber = equipment.InventoryNumber, EquipmentName = equipment.EquipmentName,
            ModelName = modelEntity.ModelName, Manufacturer = modelEntity.Manufacturer.ManufacturerName,
            CommissioningDate = equipment.CommissioningDate, OperationDate = equipment.OperationDate, CreatedAt = equipment.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult> UpdateEquipment(int id, [FromBody] UpdateEquipmentRequest req)
    {
        var e = await _db.Equipment.FindAsync(id);
        if (e == null) return NotFound(new { message = $"Оборудование с ID {id} не найдено" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? currentUserId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        var modelEntity = await FindOrCreateModelAsync(req.ModelName, req.Manufacturer);
        var statusChanged = e.StatusId != req.StatusId && req.StatusId > 0;
        e.WorkshopId = req.WorkshopId; e.EquipmentName = req.EquipmentName.Trim();
        e.ModelId = modelEntity.ModelId; e.SerialNumber = req.SerialNumber?.Trim();
        e.CommissioningDate = req.CommissioningDate;
        e.OperationDate = req.OperationDate; e.WarrantyUntil = req.WarrantyUntil;
        e.PassportUrl = req.PassportUrl?.Trim(); e.UpdatedByUserId = currentUserId;
        e.UpdatedAt = DateTime.UtcNow;

        if (statusChanged)
        {
            e.StatusId = req.StatusId;
            _db.EquipmentStatusHistories.Add(new EquipmentStatusHistory
            {
                EquipmentId = e.EquipmentId, StatusId = req.StatusId,
                Reason = req.StatusChangeReason ?? "Обновление статуса",
                ChangedByUserId = currentUserId, ChangedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Данные оборудования успешно обновлены" });
    }

    private async Task<EquipmentModel> FindOrCreateModelAsync(string modelName, string? manufacturerName)
    {
        var mName = string.IsNullOrWhiteSpace(modelName) ? "Универсальная модель" : modelName.Trim();
        var mfName = string.IsNullOrWhiteSpace(manufacturerName) ? "Отечественный производитель" : manufacturerName.Trim();

        var manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(x => x.ManufacturerName.ToLower() == mfName.ToLower());
        if (manufacturer == null)
        {
            manufacturer = new Manufacturer { ManufacturerName = mfName, Country = "Россия" };
            _db.Manufacturers.Add(manufacturer);
            await _db.SaveChangesAsync();
        }

        var model = await _db.EquipmentModels.Include(m => m.Manufacturer)
            .FirstOrDefaultAsync(x => x.ManufacturerId == manufacturer.ManufacturerId && x.ModelName.ToLower() == mName.ToLower());
        if (model == null)
        {
            model = new EquipmentModel { ManufacturerId = manufacturer.ManufacturerId, ModelName = mName, Manufacturer = manufacturer };
            _db.EquipmentModels.Add(model);
            await _db.SaveChangesAsync();
        }

        return model;
    }
}
