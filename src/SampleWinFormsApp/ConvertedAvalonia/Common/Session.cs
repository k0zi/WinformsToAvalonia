using WarehouseApp.Data.Models;

namespace WarehouseApp.Common;

public static class Session
{
    public static User? CurrentUser { get; set; }
}
