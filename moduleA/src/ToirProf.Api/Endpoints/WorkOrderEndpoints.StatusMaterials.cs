using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class WorkOrderEndpoints
{
    private static void MapStatusAndMaterialEndpoints(RouteGroupBuilder g)
    {
        g.MapPut("/{id:int}/status", async (int id, UpdateOrderStatusReq req, ToirDbContext db) =>
        {
            var wo = await db.WorkOrders.Include(w => w.Equipment).Include(w => w.WorkOrderMaterials).ThenInclude(m => m.WarehouseMaterial).ThenInclude(wm => wm.Material).FirstOrDefaultAsync(w => w.WorkOrderId == id);
            if (wo == null) return Results.NotFound(new { message = $"Заявка с ID {id} не найдена" });

            var completing = (req.OrderStatusId == 5 || req.OrderStatusId == 6) && wo.OrderStatusId != 5 && wo.OrderStatusId != 6;
            if (completing)
            {
                var low = wo.WorkOrderMaterials.FirstOrDefault(m => m.WarehouseMaterial.CurrentStock < m.QuantityUsed);
                if (low != null) return Results.BadRequest(new { message = $"Недостаточно '{low.WarehouseMaterial.Material.MaterialName}': доступно {low.WarehouseMaterial.CurrentStock}, нужно {low.QuantityUsed}" });
            }

            wo.OrderStatusId = req.OrderStatusId;
            if (req.ActualStartDate.HasValue) wo.ActualStartDate = req.ActualStartDate;
            if (req.ActualEndDate.HasValue) wo.ActualEndDate = req.ActualEndDate;
            if (!string.IsNullOrWhiteSpace(req.ReportText)) wo.ReportText = req.ReportText.Trim();
            if (!string.IsNullOrWhiteSpace(req.FailureReason)) wo.FailureReason = req.FailureReason.Trim();

            if (req.OrderStatusId is 5 or 6)
            {
                wo.ActualEndDate ??= DateTime.UtcNow;
                wo.ActualStartDate ??= wo.PlannedStartDate;
                wo.DowntimeHours = req.DowntimeHours > 0 ? req.DowntimeHours.Value
                    : wo.ActualEndDate >= wo.ActualStartDate ? ToirCalculations.CalculateDowntimeHours(wo.ActualStartDate, wo.ActualEndDate) : 0m;
                if (wo.Equipment != null && wo.Equipment.StatusId is 2 or 3)
                {
                    wo.Equipment.StatusId = 1;
                    db.EquipmentStatusHistories.Add(new EquipmentStatusHistory { EquipmentId = wo.Equipment.EquipmentId, StatusId = 1, Reason = $"Возврат в работу по {wo.OrderNumber}" });
                }
                if (completing)
                    foreach (var m in wo.WorkOrderMaterials) { m.WarehouseMaterial.CurrentStock -= m.QuantityUsed; m.WarehouseMaterial.UpdatedAt = DateTime.UtcNow; }
            }
            wo.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"Статус заявки {wo.OrderNumber} обновлен", downtimeHours = wo.DowntimeHours });
        }).RequireAuthorization();

        g.MapPost("/{id:int}/materials", async (int id, AddWorkOrderMaterialReq req, ToirDbContext db) =>
        {
            if (await db.WorkOrders.FindAsync(id) == null) return Results.NotFound(new { message = $"Заявка {id} не найдена" });
            if (req.QuantityUsed <= 0) return Results.BadRequest(new { message = "Количество должно быть больше нуля" });

            var wm = req.WarehouseId > 0
                ? await db.WarehouseMaterials.Include(x => x.Material).FirstOrDefaultAsync(x => x.MaterialId == req.MaterialId && x.WarehouseId == req.WarehouseId)
                : await db.WarehouseMaterials.Include(x => x.Material).Where(x => x.MaterialId == req.MaterialId).OrderByDescending(x => x.CurrentStock).FirstOrDefaultAsync();
            if (wm == null) return Results.NotFound(new { message = $"Материал {req.MaterialId} на складах не найден" });

            var ex = await db.WorkOrderMaterials.FirstOrDefaultAsync(m => m.WorkOrderId == id && m.WarehouseId == wm.WarehouseId && m.MaterialId == req.MaterialId);
            if (ex != null) { ex.QuantityUsed += req.QuantityUsed; ex.CommentText = req.CommentText ?? ex.CommentText; ex.UsedAt = DateTime.UtcNow; }
            else db.WorkOrderMaterials.Add(new WorkOrderMaterial { WorkOrderId = id, WarehouseId = wm.WarehouseId, MaterialId = req.MaterialId, QuantityUsed = req.QuantityUsed, HistoricalUnitPrice = wm.Material.UnitPrice, CommentText = req.CommentText });

            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"Материал '{wm.Material.MaterialName}' добавлен к заявке" });
        }).RequireAuthorization();
    }
}
