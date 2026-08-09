using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Data;
using WarehouseApp.Forms;

namespace WarehouseApp;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        using (var ctx = Db.CreateContext())
        {
            ctx.Database.Migrate();
            DbSeeder.SeedAsync(ctx).GetAwaiter().GetResult();
        }

        Application.Run(new LoginForm());
    }
}