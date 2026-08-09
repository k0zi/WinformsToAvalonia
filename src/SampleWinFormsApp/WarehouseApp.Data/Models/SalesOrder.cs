namespace WarehouseApp.Data.Models;

public class SalesOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public SalesOrderStatus Status { get; set; }
    public int? SatisfactionRating { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public List<SalesOrderLine> Lines { get; set; } = [];
}
