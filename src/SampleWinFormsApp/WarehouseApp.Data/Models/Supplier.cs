namespace WarehouseApp.Data.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int Rating { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Product> Products { get; set; } = [];
    public List<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
