namespace WarehouseApp.Data.Models;

public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public int QuantityOrdered { get; set; }
    public int QuantityShipped { get; set; }
    public decimal UnitPrice { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
