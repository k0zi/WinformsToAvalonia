namespace WarehouseApp.Data.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SupplierId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public int ReorderLevel { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public List<StockLevel> StockLevels { get; set; } = [];
    public List<StockMovement> StockMovements { get; set; } = [];
    public List<PurchaseOrderLine> PurchaseOrderLines { get; set; } = [];
    public List<SalesOrderLine> SalesOrderLines { get; set; } = [];
}
