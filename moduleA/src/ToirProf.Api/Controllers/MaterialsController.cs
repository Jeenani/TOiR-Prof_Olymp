using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToirProf.Api.DTOs;
using ToirProf.Domain;

namespace ToirProf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialsController : ControllerBase
{
    private readonly ToirDbContext _db;

    public MaterialsController(ToirDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MaterialItemDto>>> GetMaterials([FromQuery] int? warehouseId, [FromQuery] bool? onlyLowStock)
    {
        var query = _db.WarehouseMaterials
            .Include(wm => wm.Warehouse)
            .Include(wm => wm.Material).ThenInclude(m => m.Supplier)
            .AsQueryable();

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(wm => wm.WarehouseId == warehouseId.Value);

        if (onlyLowStock == true)
            query = query.Where(wm => wm.CurrentStock <= wm.MinimumStock);

        var items = await query
            .OrderBy(wm => wm.Material.MaterialName)
            .Select(wm => new MaterialItemDto
            {
                MaterialId = wm.MaterialId,
                WarehouseId = wm.WarehouseId,
                WarehouseName = wm.Warehouse.WarehouseName,
                MaterialCode = wm.Material.MaterialCode,
                MaterialName = wm.Material.MaterialName,
                UnitOfMeasure = wm.Material.UnitOfMeasure,
                CurrentStock = wm.CurrentStock,
                MinimumStock = wm.MinimumStock,
                StorageCell = wm.StorageCell,
                UnitPrice = wm.Material.UnitPrice,
                SupplierName = wm.Material.Supplier != null ? wm.Material.Supplier.SupplierName : null
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("warehouses")]
    public async Task<ActionResult<List<WarehouseDto>>> GetWarehouses()
    {
        var list = await _db.Warehouses
            .Select(w => new WarehouseDto
            {
                WarehouseId = w.WarehouseId,
                WarehouseCode = w.WarehouseCode,
                WarehouseName = w.WarehouseName,
                LocationDescription = w.LocationDescription
            }).ToListAsync();

        return Ok(list);
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult> GetSuppliers()
    {
        var list = await _db.Suppliers
            .Select(s => new
            {
                s.SupplierId,
                s.SupplierCode,
                s.SupplierName,
                s.Inn,
                s.ContactPhone,
                s.ContactEmail
            }).ToListAsync();

        return Ok(list);
    }

    [HttpPost("{id:int}/adjust")]
    [Authorize]
    public async Task<ActionResult> AdjustStock(int id, [FromBody] AdjustStockRequest req)
    {
        WarehouseMaterial? wm;
        if (req.WarehouseId.HasValue && req.WarehouseId.Value > 0)
        {
            wm = await _db.WarehouseMaterials
                .Include(x => x.Material)
                .FirstOrDefaultAsync(x => x.MaterialId == id && x.WarehouseId == req.WarehouseId.Value);
        }
        else
        {
            wm = await _db.WarehouseMaterials
                .Include(x => x.Material)
                .FirstOrDefaultAsync(x => x.MaterialId == id);
        }

        if (wm == null) return NotFound(new { message = $"Остаток материала ID {id} на складе не найден" });

        var newStock = wm.CurrentStock + req.QuantityDelta;
        if (newStock < 0) return BadRequest(new { message = "Остаток на складе не может стать отрицательным" });

        wm.CurrentStock = newStock;
        wm.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Остаток материала '{wm.Material.MaterialName}' скорректирован", currentStock = wm.CurrentStock });
    }
}
