using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class WorkOrderEndpoints
{
    private static void MapWriteEndpoints(RouteGroupBuilder g)
    {
        g.MapPost("/", async (CreateWorkOrderReq req, ClaimsPrincipal user, ToirDbContext db) =>
        {
            if (req.EquipmentId <= 0 || req.PriorityId <= 0 || string.IsNullOrWhiteSpace(req.Description))
                return Results.BadRequest(new { message = "Оборудование, приоритет и описание дефекта обязательны" });
            if (req.OrderType.Equals("emergency", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(req.FailureReason))
                return Results.BadRequest(new { message = "Для аварийной заявки необходимо указать причину отказа" });
            if (req.PlannedEndDate < req.PlannedStartDate)
                return Results.BadRequest(new { message = "Плановая дата окончания не может быть раньше даты начала" });

            var eq = await db.Equipment.Include(e => e.Workshop).FirstOrDefaultAsync(e => e.EquipmentId == req.EquipmentId);
            if (eq == null) return Results.NotFound(new { message = "Указанное оборудование не найдено" });

            var uid = JwtHelper.GetUserId(user) ?? 1;
            var maxId = await db.WorkOrders.MaxAsync(w => (int?)w.WorkOrderId) ?? 0;
            var orderNumber = $"Z-{DateTime.UtcNow.Year}-{(maxId + 1):D4}";
            var wo = new WorkOrder
            {
                OrderNumber = orderNumber, OrderType = req.OrderType, PriorityId = req.PriorityId, OrderStatusId = 1,
                EquipmentId = req.EquipmentId, MasterUserId = uid, ScheduleId = req.ScheduleId,
                Description = req.Description.Trim(), FailureReason = req.FailureReason?.Trim(),
                PlannedStartDate = req.PlannedStartDate == default ? DateTime.UtcNow : req.PlannedStartDate,
                PlannedEndDate = req.PlannedEndDate == default ? DateTime.UtcNow.AddHours(4) : req.PlannedEndDate
            };
            if (req.OrderType.Equals("emergency", StringComparison.OrdinalIgnoreCase))
            {
                eq.StatusId = 3;
                db.EquipmentStatusHistories.Add(new EquipmentStatusHistory { EquipmentId = eq.EquipmentId, StatusId = 3, Reason = $"Аварийный простой по заявке {orderNumber}", ChangedByUserId = uid });
            }
            db.WorkOrders.Add(wo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/work-orders/{wo.WorkOrderId}", new { wo.WorkOrderId, wo.OrderNumber, wo.OrderType, wo.PriorityId, wo.OrderStatusId, wo.EquipmentId, eq.EquipmentName, eq.InventoryNumber, eq.WorkshopId, eq.Workshop.WorkshopName, wo.MasterUserId, wo.PlannedStartDate, wo.PlannedEndDate, wo.CreatedAt });
        }).RequireAuthorization();

        g.MapPut("/{id:int}/assign-team", async (int id, AssignTeamReq req, ToirDbContext db) =>
        {
            var wo = await db.WorkOrders.FindAsync(id);
            if (wo == null) return Results.NotFound(new { message = $"Заявка с ID {id} не найдена" });
            var team = await db.RepairTeams.FindAsync(req.TeamId);
            if (team == null) return Results.NotFound(new { message = $"Бригада с ID {req.TeamId} не найдена" });

            var conflict = await db.WorkOrders.Where(w => w.WorkOrderId != id && w.AssignedTeamId == req.TeamId && w.OrderStatusId <= 4 && w.PlannedStartDate < wo.PlannedEndDate && w.PlannedEndDate > wo.PlannedStartDate)
                .Select(w => new { w.OrderNumber, w.PlannedStartDate, w.PlannedEndDate }).FirstOrDefaultAsync();
            if (conflict != null)
                return Results.Conflict(new { message = $"Конфликт занятости: бригада '{team.TeamName}' уже на заявке {conflict.OrderNumber} ({conflict.PlannedStartDate:dd.MM HH:mm} — {conflict.PlannedEndDate:dd.MM HH:mm})" });

            wo.AssignedTeamId = req.TeamId;
            if (wo.OrderStatusId == 1) wo.OrderStatusId = 2;
            wo.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"Бригада '{team.TeamName}' назначена на заявку {wo.OrderNumber}" });
        }).RequireAuthorization();

        MapStatusAndMaterialEndpoints(g);
    }
}
