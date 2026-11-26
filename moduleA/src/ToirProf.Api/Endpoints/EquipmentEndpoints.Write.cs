using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class EquipmentEndpoints
{
    private static void MapWriteEndpoints(RouteGroupBuilder g)
    {
        g.MapPost("/", async (CreateEquipmentReq req, ClaimsPrincipal user, ToirDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.InventoryNumber) || string.IsNullOrWhiteSpace(req.EquipmentName))
                return Results.BadRequest(new { message = "Инвентарный номер и наименование обязательны" });
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var comm = req.CommissioningDate == default ? today : req.CommissioningDate;
            var op = req.OperationDate == default ? today : req.OperationDate;
            if (op < comm || op > today) return Results.BadRequest(new { message = "Некорректная хронология дат ввода оборудования" });
            if (await db.Equipment.AnyAsync(x => x.InventoryNumber.ToLower() == req.InventoryNumber.Trim().ToLower()))
                return Results.Conflict(new { message = $"Оборудование '{req.InventoryNumber}' уже существует" });

            var uid = JwtHelper.GetUserId(user);
            var model = await FindOrCreateModelAsync(db, req.ModelName, req.Manufacturer);
            var item = new Equipment { WorkshopId = req.WorkshopId, StatusId = req.StatusId > 0 ? req.StatusId : 1, ModelId = model.ModelId, InventoryNumber = req.InventoryNumber.Trim(), EquipmentName = req.EquipmentName.Trim(), SerialNumber = req.SerialNumber?.Trim(), CommissioningDate = comm, OperationDate = op, WarrantyUntil = req.WarrantyUntil, PassportUrl = req.PassportUrl?.Trim(), CreatedByUserId = uid };
            db.Equipment.Add(item);
            await db.SaveChangesAsync();
            db.AuditLogs.Add(new AuditLog { UserId = uid, EntityName = "equipment", EntityId = item.EquipmentId, ActionType = "CREATE", NewValue = $"{{\"inventory_number\":\"{item.InventoryNumber}\"}}" });
            await db.SaveChangesAsync();
            return Results.Created($"/api/equipment/{item.EquipmentId}", new { item.EquipmentId, item.WorkshopId, item.StatusId, item.InventoryNumber, item.EquipmentName, model.ModelName, Manufacturer = model.Manufacturer.ManufacturerName, item.CommissioningDate, item.OperationDate, item.CreatedAt });
        }).RequireAuthorization();

        g.MapPut("/{id:int}", async (int id, UpdateEquipmentReq req, ClaimsPrincipal user, ToirDbContext db) =>
        {
            var e = await db.Equipment.FindAsync(id);
            if (e == null) return Results.NotFound(new { message = $"Оборудование с ID {id} не найдено" });
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (req.OperationDate < req.CommissioningDate || req.OperationDate > today)
                return Results.BadRequest(new { message = "Некорректная хронология дат ввода оборудования" });

            var uid = JwtHelper.GetUserId(user);
            var model = await FindOrCreateModelAsync(db, req.ModelName, req.Manufacturer);
            e.WorkshopId = req.WorkshopId; e.EquipmentName = req.EquipmentName.Trim(); e.ModelId = model.ModelId;
            e.SerialNumber = req.SerialNumber?.Trim(); e.CommissioningDate = req.CommissioningDate; e.OperationDate = req.OperationDate;
            e.WarrantyUntil = req.WarrantyUntil; e.PassportUrl = req.PassportUrl?.Trim(); e.UpdatedByUserId = uid; e.UpdatedAt = DateTime.UtcNow;
            if (req.StatusId > 0 && e.StatusId != req.StatusId)
            {
                e.StatusId = req.StatusId;
                db.EquipmentStatusHistories.Add(new EquipmentStatusHistory { EquipmentId = id, StatusId = req.StatusId, Reason = req.StatusChangeReason ?? "Обновление статуса", ChangedByUserId = uid });
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Данные оборудования обновлены" });
        }).RequireAuthorization();

        g.MapDelete("/{id:int}", async (int id, ToirDbContext db) =>
        {
            var e = await db.Equipment.Include(x => x.WorkOrders).Include(x => x.MaintenanceSchedules).Include(x => x.StatusHistory).FirstOrDefaultAsync(x => x.EquipmentId == id);
            if (e == null) return Results.NotFound(new { message = $"Оборудование с ID {id} не найдено" });
            if (e.WorkOrders.Any() || e.MaintenanceSchedules.Any())
                return Results.Conflict(new { message = $"Невозможно удалить '{e.InventoryNumber}': имеются связанные заявки или записи ТО" });
            db.EquipmentStatusHistories.RemoveRange(e.StatusHistory);
            db.Equipment.Remove(e);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"Оборудование '{e.InventoryNumber}' удалено" });
        }).RequireAuthorization();
    }

    private static async Task<EquipmentModel> FindOrCreateModelAsync(ToirDbContext db, string modelName, string? manufacturerName)
    {
        var mName = string.IsNullOrWhiteSpace(modelName) ? "Универсальная модель" : modelName.Trim();
        var mfName = string.IsNullOrWhiteSpace(manufacturerName) ? "Отечественный производитель" : manufacturerName.Trim();
        var mf = await db.Manufacturers.FirstOrDefaultAsync(x => x.ManufacturerName.ToLower() == mfName.ToLower());
        if (mf == null) { mf = new Manufacturer { ManufacturerName = mfName }; db.Manufacturers.Add(mf); await db.SaveChangesAsync(); }
        var model = await db.EquipmentModels.Include(m => m.Manufacturer).FirstOrDefaultAsync(x => x.ManufacturerId == mf.ManufacturerId && x.ModelName.ToLower() == mName.ToLower());
        if (model == null) { model = new EquipmentModel { ManufacturerId = mf.ManufacturerId, ModelName = mName, Manufacturer = mf }; db.EquipmentModels.Add(model); await db.SaveChangesAsync(); }
        return model;
    }
}
