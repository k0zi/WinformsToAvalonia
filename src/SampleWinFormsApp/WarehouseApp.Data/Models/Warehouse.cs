namespace WarehouseApp.Data.Models;

public class Warehouse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int CapacityUnits { get; set; }

    public List<Location> Locations { get; set; } = [];
    public List<StockLevel> StockLevels { get; set; } = [];
    public List<StockMovement> StockMovements { get; set; } = [];
    public List<SalesOrder> SalesOrders { get; set; } = [];
}
