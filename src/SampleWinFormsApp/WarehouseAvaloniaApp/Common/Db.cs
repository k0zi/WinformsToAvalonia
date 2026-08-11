using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data.Data;

namespace WarehouseApp.Common;

public static class Db
{
    private static readonly string ConnectionString;

    static Db()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WarehouseApp");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "warehouse.db");
        ConnectionString = $"Data Source={dbPath}";
    }

    public static WarehouseDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseSqlite(ConnectionString)
            .Options;
        return new WarehouseDbContext(options);
    }
}
