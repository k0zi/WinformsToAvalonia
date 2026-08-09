namespace WarehouseApp.Data.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? LocationId { get; set; }
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public ReferenceType ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime MovementDate { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Location? Location { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
