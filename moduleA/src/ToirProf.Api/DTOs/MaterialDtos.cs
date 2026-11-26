namespace ToirProf.Api.DTOs;

public class MaterialItemDto
{
    public int MaterialId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string? StorageCell { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SupplierName { get; set; }
    public bool IsLowStock => CurrentStock <= MinimumStock;
}

public class WarehouseDto
{
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? LocationDescription { get; set; }
}

public class AdjustStockRequest
{
    public int? WarehouseId { get; set; }
    public decimal QuantityDelta { get; set; }
    public string? Reason { get; set; }
}
