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
        var query = _db.Materials
            .Include(m => m.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(m => m.WarehouseId == warehouseId.Value);

        if (onlyLowStock == true)
            query = query.Where(m => m.CurrentStock <= m.MinimumStock);

        var items = await query
            .OrderBy(m => m.MaterialName)
            .Select(m => new MaterialItemDto
            {
                MaterialId = m.MaterialId,
                WarehouseId = m.WarehouseId,
                WarehouseName = m.Warehouse.WarehouseName,
                MaterialCode = m.MaterialCode,
                MaterialName = m.MaterialName,
                UnitOfMeasure = m.UnitOfMeasure,
                CurrentStock = m.CurrentStock,
                MinimumStock = m.MinimumStock,
                StorageCell = m.StorageCell,
                UnitPrice = m.UnitPrice,
                SupplierName = m.SupplierName
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

    [HttpPost("{id:int}/adjust")]
    [Authorize]
    public async Task<ActionResult> AdjustStock(int id, [FromBody] AdjustStockRequest req)
    {
        var m = await _db.Materials.FindAsync(id);
        if (m == null) return NotFound(new { message = $"Материал с ID {id} не найден" });

        var newStock = m.CurrentStock + req.QuantityDelta;
        if (newStock < 0) return BadRequest(new { message = "Остаток на складе не может стать отрицательным" });

        m.CurrentStock = newStock;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Остаток материала '{m.MaterialName}' скорректирован", currentStock = m.CurrentStock });
    }
}
