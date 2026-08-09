namespace WarehouseApp.Data.Models;

public class Location
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public int? ParentLocationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public int CapacityUnits { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Location? ParentLocation { get; set; }
    public List<Location> Children { get; set; } = [];
    public List<StockLevel> StockLevels { get; set; } = [];
}
