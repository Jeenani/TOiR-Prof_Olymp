using Microsoft.EntityFrameworkCore;
using ToirProf.Domain;

namespace ToirProf.Api.Endpoints;

public static partial class OperationsEndpoints
{
    private static void MapMaterialAndAnalytics(WebApplication app)
    {
        var mat = app.MapGroup("/api/materials").WithTags("Materials");

        mat.MapGet("/", async (int? warehouseId, bool? onlyLowStock, ToirDbContext db) =>
        {
            var q = db.WarehouseMaterials.Include(wm => wm.Warehouse).Include(wm => wm.Material).ThenInclude(m => m.Supplier).AsQueryable();
            if (warehouseId > 0) q = q.Where(wm => wm.WarehouseId == warehouseId);
            if (onlyLowStock == true) q = q.Where(wm => wm.CurrentStock <= wm.MinimumStock);
            return Results.Ok(await q.OrderBy(wm => wm.Material.MaterialName).Select(wm => new MaterialItemDto(
                wm.MaterialId, wm.WarehouseId, wm.Warehouse.WarehouseName, wm.Material.MaterialCode, wm.Material.MaterialName,
                wm.Material.UnitOfMeasure, wm.CurrentStock, wm.MinimumStock, wm.StorageCell, wm.Material.UnitPrice,
                wm.Material.Supplier != null ? wm.Material.Supplier.SupplierName : null)).ToListAsync());
        });

        mat.MapGet("/warehouses", async (ToirDbContext db) => Results.Ok(await db.Warehouses.Select(w => new { w.WarehouseId, w.WarehouseCode, w.WarehouseName, w.LocationDescription }).ToListAsync()));
        mat.MapGet("/suppliers", async (ToirDbContext db) => Results.Ok(await db.Suppliers.Select(s => new { s.SupplierId, s.SupplierCode, s.SupplierName, s.Inn, s.ContactPhone, s.ContactEmail }).ToListAsync()));

        mat.MapPost("/{id:int}/adjust", async (int id, AdjustStockReq req, ToirDbContext db) =>
        {
            var wm = req.WarehouseId > 0
                ? await db.WarehouseMaterials.Include(x => x.Material).FirstOrDefaultAsync(x => x.MaterialId == id && x.WarehouseId == req.WarehouseId)
                : await db.WarehouseMaterials.Include(x => x.Material).FirstOrDefaultAsync(x => x.MaterialId == id);
            if (wm == null) return Results.NotFound(new { message = $"Остаток материала {id} не найден" });
            if (wm.CurrentStock + req.QuantityDelta < 0) return Results.BadRequest(new { message = "Остаток не может стать отрицательным" });
            wm.CurrentStock += req.QuantityDelta; wm.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"Остаток '{wm.Material.MaterialName}' скорректирован", currentStock = wm.CurrentStock });
        }).RequireAuthorization();

        MapAnalyticsGroup(app);
    }
}
