using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToirProf.Api.DTOs;
using ToirProf.Domain;

namespace ToirProf.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
[Route("api/[controller]")]
public class WorkOrdersController : ControllerBase
{
    private readonly ToirDbContext _db;

    public WorkOrdersController(ToirDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetWorkOrders(
        [FromQuery] int? statusId, [FromQuery] int? priorityId, [FromQuery] int? workshopId,
        [FromQuery] int? teamId, [FromQuery] string? search, [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.WorkOrders
            .Include(wo => wo.Equipment).ThenInclude(e => e.Workshop)
            .Include(wo => wo.Status)
            .Include(wo => wo.Priority)
            .Include(wo => wo.MasterUser)
            .Include(wo => wo.AssignedTeam)
            .AsQueryable();

        if (statusId.HasValue && statusId.Value > 0) query = query.Where(wo => wo.OrderStatusId == statusId.Value);
        if (priorityId.HasValue && priorityId.Value > 0) query = query.Where(wo => wo.PriorityId == priorityId.Value);
        if (workshopId.HasValue && workshopId.Value > 0) query = query.Where(wo => wo.Equipment.WorkshopId == workshopId.Value);
        if (teamId.HasValue && teamId.Value > 0) query = query.Where(wo => wo.AssignedTeamId == teamId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(wo => wo.OrderNumber.ToLower().Contains(s) ||
                                      wo.Equipment.EquipmentName.ToLower().Contains(s) ||
                                      wo.Equipment.InventoryNumber.ToLower().Contains(s) ||
                                      wo.Description.ToLower().Contains(s));
        }

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("ordernumber", false) => query.OrderBy(wo => wo.OrderNumber),
            ("ordernumber", true) => query.OrderByDescending(wo => wo.OrderNumber),
            ("priority", false) => query.OrderBy(wo => wo.PriorityId),
            ("priority", true) => query.OrderByDescending(wo => wo.PriorityId),
            ("status", false) => query.OrderBy(wo => wo.OrderStatusId),
            ("status", true) => query.OrderByDescending(wo => wo.OrderStatusId),
            ("plannedstart", false) => query.OrderBy(wo => wo.PlannedStartDate),
            ("plannedstart", true) => query.OrderByDescending(wo => wo.PlannedStartDate),
            (_, false) => query.OrderBy(wo => wo.CreatedAt),
            _ => query.OrderByDescending(wo => wo.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(wo => new WorkOrderItemDto
        {
            WorkOrderId = wo.WorkOrderId, OrderNumber = wo.OrderNumber, OrderType = wo.OrderType,
            PriorityId = wo.PriorityId, PriorityCode = wo.Priority.PriorityCode, PriorityName = wo.Priority.PriorityName,
            OrderStatusId = wo.OrderStatusId, StatusCode = wo.Status.StatusCode, StatusName = wo.Status.StatusName,
            EquipmentId = wo.EquipmentId, EquipmentName = wo.Equipment.EquipmentName, InventoryNumber = wo.Equipment.InventoryNumber,
            WorkshopId = wo.Equipment.WorkshopId, WorkshopName = wo.Equipment.Workshop.WorkshopName,
            MasterUserId = wo.MasterUserId, MasterName = $"{wo.MasterUser.LastName} {wo.MasterUser.FirstName}".Trim(),
            AssignedTeamId = wo.AssignedTeamId, TeamName = wo.AssignedTeam != null ? wo.AssignedTeam.TeamName : null,
            ScheduleId = wo.ScheduleId, PlannedStartDate = wo.PlannedStartDate, PlannedEndDate = wo.PlannedEndDate,
            ActualStartDate = wo.ActualStartDate, ActualEndDate = wo.ActualEndDate,
            DowntimeHours = wo.DowntimeHours, CreatedAt = wo.CreatedAt
        }).ToListAsync();

        return Ok(new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize), items });
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<List<WorkOrderStatusDto>>> GetStatuses()
    {
        return Ok(await _db.WorkOrderStatuses.Select(s => new WorkOrderStatusDto
        {
            OrderStatusId = s.OrderStatusId, StatusCode = s.StatusCode, StatusName = s.StatusName
        }).ToListAsync());
    }

    [HttpGet("priorities")]
    public async Task<ActionResult<List<WorkOrderPriorityDto>>> GetPriorities()
    {
        return Ok(await _db.WorkOrderPriorities.Select(p => new WorkOrderPriorityDto
        {
            PriorityId = p.PriorityId, PriorityCode = p.PriorityCode, PriorityName = p.PriorityName
        }).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkOrderDetailDto>> GetWorkOrderById(int id)
    {
        var wo = await _db.WorkOrders
            .Include(x => x.Equipment).ThenInclude(e => e.Workshop)
            .Include(x => x.Status).Include(x => x.Priority)
            .Include(x => x.MasterUser).Include(x => x.AssignedTeam)
            .Include(x => x.WorkOrderMaterials).ThenInclude(m => m.WarehouseMaterial).ThenInclude(wm => wm.Material)
            .Include(x => x.WorkOrderMaterials).ThenInclude(m => m.WarehouseMaterial).ThenInclude(wm => wm.Warehouse)
            .FirstOrDefaultAsync(x => x.WorkOrderId == id);

        if (wo == null) return NotFound(new { message = $"Заявка с ID {id} не найдена" });

        return Ok(new WorkOrderDetailDto
        {
            WorkOrderId = wo.WorkOrderId, OrderNumber = wo.OrderNumber, OrderType = wo.OrderType,
            PriorityId = wo.PriorityId, PriorityCode = wo.Priority.PriorityCode, PriorityName = wo.Priority.PriorityName,
            OrderStatusId = wo.OrderStatusId, StatusCode = wo.Status.StatusCode, StatusName = wo.Status.StatusName,
            EquipmentId = wo.EquipmentId, EquipmentName = wo.Equipment.EquipmentName, InventoryNumber = wo.Equipment.InventoryNumber,
            WorkshopId = wo.Equipment.WorkshopId, WorkshopName = wo.Equipment.Workshop.WorkshopName,
            MasterUserId = wo.MasterUserId, MasterName = $"{wo.MasterUser.LastName} {wo.MasterUser.FirstName}".Trim(),
            AssignedTeamId = wo.AssignedTeamId, TeamName = wo.AssignedTeam?.TeamName,
            ScheduleId = wo.ScheduleId, Description = wo.Description, FailureReason = wo.FailureReason, ReportText = wo.ReportText,
            PlannedStartDate = wo.PlannedStartDate, PlannedEndDate = wo.PlannedEndDate,
            ActualStartDate = wo.ActualStartDate, ActualEndDate = wo.ActualEndDate,
            DowntimeHours = wo.DowntimeHours, CreatedAt = wo.CreatedAt, UpdatedAt = wo.UpdatedAt,
            Materials = wo.WorkOrderMaterials.Select(m => new WorkOrderMaterialDto
            {
                WarehouseId = m.WarehouseId, WarehouseName = m.WarehouseMaterial.Warehouse.WarehouseName,
                MaterialId = m.MaterialId, MaterialCode = m.WarehouseMaterial.Material.MaterialCode, MaterialName = m.WarehouseMaterial.Material.MaterialName,
                UnitOfMeasure = m.WarehouseMaterial.Material.UnitOfMeasure, QuantityUsed = m.QuantityUsed,
                HistoricalUnitPrice = m.HistoricalUnitPrice, UsedAt = m.UsedAt, CommentText = m.CommentText
            }).ToList()
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<WorkOrderItemDto>> CreateWorkOrder([FromBody] CreateWorkOrderRequest req)
    {
        if (req.EquipmentId <= 0 || req.PriorityId <= 0 || string.IsNullOrWhiteSpace(req.Description))
            return BadRequest(new { message = "Оборудование, приоритет и описание дефекта обязательны" });

        if (req.OrderType.Equals("emergency", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(req.FailureReason))
            return BadRequest(new { message = "Для аварийной заявки необходимо указать причину отказа" });

        if (req.PlannedEndDate < req.PlannedStartDate)
            return BadRequest(new { message = "Плановая дата окончания не может быть раньше даты начала" });

        var equipment = await _db.Equipment.Include(e => e.Workshop).FirstOrDefaultAsync(e => e.EquipmentId == req.EquipmentId);
        if (equipment == null) return NotFound(new { message = "Указанное оборудование не найдено" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int currentUserId = int.TryParse(userIdClaim, out var uid) ? uid : 1;

        var year = DateTime.UtcNow.Year;
        var maxId = await _db.WorkOrders.MaxAsync(w => (int?)w.WorkOrderId) ?? 0;
        var orderNumber = $"Z-{year}-{(maxId + 1):D4}";

        var wo = new WorkOrder
        {
            OrderNumber = orderNumber, OrderType = req.OrderType, PriorityId = req.PriorityId,
            OrderStatusId = 1, EquipmentId = req.EquipmentId, MasterUserId = currentUserId,
            ScheduleId = req.ScheduleId, Description = req.Description.Trim(), FailureReason = req.FailureReason?.Trim(),
            PlannedStartDate = req.PlannedStartDate == default ? DateTime.UtcNow : req.PlannedStartDate,
            PlannedEndDate = req.PlannedEndDate == default ? DateTime.UtcNow.AddHours(4) : req.PlannedEndDate,
            DowntimeHours = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        if (req.OrderType.ToLower() == "emergency")
        {
            equipment.StatusId = 3;
            _db.EquipmentStatusHistories.Add(new EquipmentStatusHistory
            {
                EquipmentId = equipment.EquipmentId, StatusId = 3,
                Reason = $"Автоматический аварийный простой по заявке {orderNumber}",
                ChangedByUserId = currentUserId, ChangedAt = DateTime.UtcNow
            });
        }

        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWorkOrderById), new { id = wo.WorkOrderId }, new WorkOrderItemDto
        {
            WorkOrderId = wo.WorkOrderId, OrderNumber = wo.OrderNumber, OrderType = wo.OrderType,
            PriorityId = wo.PriorityId, OrderStatusId = wo.OrderStatusId, EquipmentId = wo.EquipmentId,
            EquipmentName = equipment.EquipmentName, InventoryNumber = equipment.InventoryNumber,
            WorkshopId = equipment.WorkshopId, WorkshopName = equipment.Workshop.WorkshopName,
            MasterUserId = wo.MasterUserId, PlannedStartDate = wo.PlannedStartDate,
            PlannedEndDate = wo.PlannedEndDate, CreatedAt = wo.CreatedAt
        });
    }

    [HttpPut("{id:int}/assign-team")]
    [Authorize]
    public async Task<ActionResult> AssignTeam(int id, [FromBody] AssignTeamRequest req)
    {
        var wo = await _db.WorkOrders.FindAsync(id);
        if (wo == null) return NotFound(new { message = $"Заявка с ID {id} не найдена" });

        var team = await _db.RepairTeams.FindAsync(req.TeamId);
        if (team == null) return NotFound(new { message = $"Бригада с ID {req.TeamId} не найдена" });

        var conflict = await _db.WorkOrders
            .Where(w => w.WorkOrderId != id
                     && w.AssignedTeamId == req.TeamId
                     && (w.OrderStatusId == 1 || w.OrderStatusId == 2 || w.OrderStatusId == 3 || w.OrderStatusId == 4)
                     && w.PlannedStartDate < wo.PlannedEndDate
                     && w.PlannedEndDate > wo.PlannedStartDate)
            .Select(w => new { w.OrderNumber, w.PlannedStartDate, w.PlannedEndDate })
            .FirstOrDefaultAsync();

        if (conflict != null)
        {
            return Conflict(new
            {
                message = $"Конфликт занятости: бригада '{team.TeamName}' уже назначена на заявку {conflict.OrderNumber} ({conflict.PlannedStartDate:dd.MM HH:mm} — {conflict.PlannedEndDate:dd.MM HH:mm})"
            });
        }

        wo.AssignedTeamId = req.TeamId;
        if (wo.OrderStatusId == 1) wo.OrderStatusId = 2;
        wo.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Бригада '{team.TeamName}' назначена на заявку {wo.OrderNumber}" });
    }

    [HttpPut("{id:int}/status")]
    [Authorize]
    public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var wo = await _db.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.WorkOrderMaterials).ThenInclude(wom => wom.WarehouseMaterial).ThenInclude(wm => wm.Material)
            .FirstOrDefaultAsync(w => w.WorkOrderId == id);

        if (wo == null) return NotFound(new { message = $"Заявка с ID {id} не найдена" });

        if ((req.OrderStatusId == 5 || req.OrderStatusId == 6) && wo.OrderStatusId != 5 && wo.OrderStatusId != 6)
        {
            foreach (var item in wo.WorkOrderMaterials)
            {
                if (item.WarehouseMaterial != null && item.WarehouseMaterial.CurrentStock < item.QuantityUsed)
                {
                    return BadRequest(new
                    {
                        message = $"Недостаточно материала '{item.WarehouseMaterial.Material.MaterialName}' на складе: доступно {item.WarehouseMaterial.CurrentStock}, требуется {item.QuantityUsed}"
                    });
                }
            }
        }

        var prevStatus = wo.OrderStatusId;
        wo.OrderStatusId = req.OrderStatusId;
        if (req.ActualStartDate.HasValue) wo.ActualStartDate = req.ActualStartDate.Value;
        if (req.ActualEndDate.HasValue) wo.ActualEndDate = req.ActualEndDate.Value;
        if (!string.IsNullOrWhiteSpace(req.ReportText)) wo.ReportText = req.ReportText.Trim();
        if (!string.IsNullOrWhiteSpace(req.FailureReason)) wo.FailureReason = req.FailureReason.Trim();

        if (req.OrderStatusId == 5 || req.OrderStatusId == 6)
        {
            if (!wo.ActualEndDate.HasValue) wo.ActualEndDate = DateTime.UtcNow;
            if (!wo.ActualStartDate.HasValue) wo.ActualStartDate = wo.PlannedStartDate;

            if (req.DowntimeHours.HasValue && req.DowntimeHours.Value > 0)
                wo.DowntimeHours = req.DowntimeHours.Value;
            else
            {
                var diffHours = (decimal)(wo.ActualEndDate.Value - wo.ActualStartDate.Value).TotalHours;
                wo.DowntimeHours = Math.Max(0, Math.Round(diffHours, 2));
            }

            if (wo.Equipment != null && (wo.Equipment.StatusId == 2 || wo.Equipment.StatusId == 3))
            {
                wo.Equipment.StatusId = 1;
                _db.EquipmentStatusHistories.Add(new EquipmentStatusHistory
                {
                    EquipmentId = wo.Equipment.EquipmentId, StatusId = 1,
                    Reason = $"Автоматический возврат в работу по заявке {wo.OrderNumber}",
                    ChangedAt = DateTime.UtcNow
                });
            }

            if (prevStatus != 5 && prevStatus != 6)
            {
                foreach (var item in wo.WorkOrderMaterials)
                {
                    if (item.WarehouseMaterial != null)
                    {
                        item.WarehouseMaterial.CurrentStock -= item.QuantityUsed;
                        item.WarehouseMaterial.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        wo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Статус заявки {wo.OrderNumber} обновлен", downtimeHours = wo.DowntimeHours });
    }

    [HttpPost("{id:int}/materials")]
    [Authorize]
    public async Task<ActionResult> AddMaterial(int id, [FromBody] AddWorkOrderMaterialRequest req)
    {
        var wo = await _db.WorkOrders.FindAsync(id);
        if (wo == null) return NotFound(new { message = $"Заявка с ID {id} не найдена" });

        if (req.QuantityUsed <= 0) return BadRequest(new { message = "Количество должно быть больше нуля" });

        WarehouseMaterial? wm;
        if (req.WarehouseId.HasValue && req.WarehouseId.Value > 0)
        {
            wm = await _db.WarehouseMaterials.Include(x => x.Material)
                .FirstOrDefaultAsync(x => x.MaterialId == req.MaterialId && x.WarehouseId == req.WarehouseId.Value);
        }
        else
        {
            wm = await _db.WarehouseMaterials.Include(x => x.Material)
                .Where(x => x.MaterialId == req.MaterialId)
                .OrderByDescending(x => x.CurrentStock)
                .FirstOrDefaultAsync();
        }

        if (wm == null) return NotFound(new { message = $"Материал с ID {req.MaterialId} на складах не найден" });

        var existing = await _db.WorkOrderMaterials.FirstOrDefaultAsync(m =>
            m.WorkOrderId == id && m.WarehouseId == wm.WarehouseId && m.MaterialId == req.MaterialId);
        if (existing != null)
        {
            existing.QuantityUsed += req.QuantityUsed;
            existing.CommentText = req.CommentText ?? existing.CommentText;
            existing.UsedAt = DateTime.UtcNow;
        }
        else
        {
            _db.WorkOrderMaterials.Add(new WorkOrderMaterial
            {
                WorkOrderId = id, WarehouseId = wm.WarehouseId, MaterialId = req.MaterialId,
                QuantityUsed = req.QuantityUsed, HistoricalUnitPrice = wm.Material.UnitPrice,
                CommentText = req.CommentText, UsedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Материал '{wm.Material.MaterialName}' добавлен к заявке" });
    }
}
