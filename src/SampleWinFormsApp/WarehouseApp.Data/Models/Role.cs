namespace WarehouseApp.Data.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool CanManageInventory { get; set; }
    public bool CanManageOrders { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanViewReports { get; set; }

    public List<User> Users { get; set; } = [];
}
