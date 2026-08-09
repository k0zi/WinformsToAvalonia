using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(WarehouseDbContext ctx)
    {
        if (await ctx.Users.AnyAsync())
        {
            return;
        }

        var roles = new List<Role>
        {
            new() { Name = "Admin", CanManageInventory = true, CanManageOrders = true, CanManageUsers = true, CanViewReports = true },
            new() { Name = "Manager", CanManageInventory = true, CanManageOrders = true, CanManageUsers = false, CanViewReports = true },
            new() { Name = "Clerk", CanManageInventory = true, CanManageOrders = false, CanManageUsers = false, CanViewReports = false }
        };
        ctx.Roles.AddRange(roles);
        ctx.SaveChanges();

        var users = new List<User>
        {
            new() { Username = "admin", PasswordHash = PasswordHasher.Hash("admin123"), DisplayName = "Alice Admin", RoleId = roles[0].Id, IsActive = true },
            new() { Username = "manager", PasswordHash = PasswordHasher.Hash("manager123"), DisplayName = "Mark Manager", RoleId = roles[1].Id, IsActive = true },
            new() { Username = "clerk", PasswordHash = PasswordHasher.Hash("clerk123"), DisplayName = "Cara Clerk", RoleId = roles[2].Id, IsActive = true }
        };
        ctx.Users.AddRange(users);
        ctx.SaveChanges();
        var adminUser = users[0];

        var categories = new List<Category>
        {
            new() { Name = "Power Tools", Description = "Corded and cordless power tools" },
            new() { Name = "Hand Tools", Description = "Manual tools and equipment" },
            new() { Name = "Fasteners", Description = "Screws, bolts, nails, anchors" },
            new() { Name = "Safety Equipment", Description = "PPE and safety gear" },
            new() { Name = "Electrical", Description = "Wiring, outlets, breakers" },
            new() { Name = "Plumbing", Description = "Pipes, fittings, valves" }
        };
        ctx.Categories.AddRange(categories);
        ctx.SaveChanges();

        var suppliers = new List<Supplier>
        {
            new() { Name = "Northfield Tool Co.", ContactName = "Derek Holt", Phone = "555-0101", Email = "sales@northfieldtool.example", Address = "12 Industrial Way, Northfield", Rating = 5, IsActive = true },
            new() { Name = "Ironclad Supply", ContactName = "Priya Nair", Phone = "555-0102", Email = "orders@ironcladsupply.example", Address = "88 Foundry Rd, Millbrook", Rating = 4, IsActive = true },
            new() { Name = "BrightSpark Electrical", ContactName = "Tomas Reyes", Phone = "555-0103", Email = "info@brightspark.example", Address = "4 Volt Ave, Riverside", Rating = 4, IsActive = true },
            new() { Name = "AquaFlow Distributors", ContactName = "Nina Petrova", Phone = "555-0104", Email = "contact@aquaflow.example", Address = "77 Pipeline Dr, Harborview", Rating = 3, IsActive = true },
            new() { Name = "SafeGuard Gear", ContactName = "Owen Blake", Phone = "555-0105", Email = "sales@safeguardgear.example", Address = "21 Shield St, Northfield", Rating = 5, IsActive = true }
        };
        ctx.Suppliers.AddRange(suppliers);
        ctx.SaveChanges();

        var customers = new List<Customer>
        {
            new() { Name = "Meridian Construction", ContactName = "Sam Ortiz", Phone = "555-0201", Email = "sam@meridianconstruction.example", Address = "300 Beam St, Uptown", Notes = "Preferred customer, net-30 terms.", IsActive = true },
            new() { Name = "Coastal Renovations", ContactName = "Liu Chen", Phone = "555-0202", Email = "liu@coastalreno.example", Address = "18 Harbor Ln, Harborview", Notes = "Frequent bulk fastener orders.", IsActive = true },
            new() { Name = "Summit Electric", ContactName = "Rae Johnson", Phone = "555-0203", Email = "rae@summitelectric.example", Address = "9 Peak Rd, Northfield", IsActive = true },
            new() { Name = "GreenLeaf Landscaping", ContactName = "Ben Ashworth", Phone = "555-0204", Email = "ben@greenleaf.example", Address = "55 Garden Ct, Millbrook", IsActive = true },
            new() { Name = "Union Plumbing Co.", ContactName = "Tara Singh", Phone = "555-0205", Email = "tara@unionplumbing.example", Address = "6 Drain Ave, Riverside", IsActive = true },
            new() { Name = "Ashford Property Group", ContactName = "Ivan Kowalski", Phone = "555-0206", Email = "ivan@ashfordpg.example", Address = "200 Estate Blvd, Uptown", IsActive = true },
            new() { Name = "Riverside Maintenance", ContactName = "Dana Wu", Phone = "555-0207", Email = "dana@riversidemaint.example", Address = "40 River Rd, Riverside", IsActive = true },
            new() { Name = "Foundation Builders", ContactName = "Marcus Lee", Phone = "555-0208", Email = "marcus@foundationbuilders.example", Address = "70 Concrete Way, Millbrook", Notes = "New account, needs credit check.", IsActive = true }
        };
        ctx.Customers.AddRange(customers);
        ctx.SaveChanges();

        var warehouses = new List<Warehouse>
        {
            new() { Code = "WH-N", Name = "Northfield Distribution Center", Address = "1 Logistics Pkwy, Northfield", CapacityUnits = 20000 },
            new() { Code = "WH-M", Name = "Millbrook Regional Warehouse", Address = "5 Storage Ln, Millbrook", CapacityUnits = 15000 },
            new() { Code = "WH-R", Name = "Riverside Overflow Depot", Address = "9 Dock St, Riverside", CapacityUnits = 8000 }
        };
        ctx.Warehouses.AddRange(warehouses);
        ctx.SaveChanges();

        var locations = new List<Location>();
        foreach (var wh in warehouses)
        {
            for (var z = 1; z <= 2; z++)
            {
                var zone = new Location
                {
                    WarehouseId = wh.Id,
                    Code = $"{wh.Code}-Z{z}",
                    Name = $"Zone {z}",
                    LocationType = LocationType.Zone,
                    CapacityUnits = wh.CapacityUnits / 2
                };
                locations.Add(zone);
            }
        }
        ctx.Locations.AddRange(locations);
        ctx.SaveChanges();

        var shelves = new List<Location>();
        foreach (var zone in locations)
        {
            for (var s = 1; s <= 2; s++)
            {
                shelves.Add(new Location
                {
                    WarehouseId = zone.WarehouseId,
                    ParentLocationId = zone.Id,
                    Code = $"{zone.Code}-S{s}",
                    Name = $"Shelf {s}",
                    LocationType = LocationType.Shelf,
                    CapacityUnits = zone.CapacityUnits / 2
                });
            }
        }
        ctx.Locations.AddRange(shelves);
        ctx.SaveChanges();

        var productSeeds = new (string Sku, string Name, int Category, int Supplier, UnitOfMeasure Uom, decimal Price, int Reorder)[]
        {
            ("PT-1001", "18V Cordless Drill/Driver", 0, 0, UnitOfMeasure.Each, 89.99m, 15),
            ("PT-1002", "Cordless Circular Saw", 0, 0, UnitOfMeasure.Each, 129.99m, 10),
            ("PT-1003", "Angle Grinder 4.5\"", 0, 1, UnitOfMeasure.Each, 54.50m, 12),
            ("PT-1004", "Reciprocating Saw", 0, 0, UnitOfMeasure.Each, 99.00m, 8),
            ("PT-1005", "Cordless Impact Driver", 0, 1, UnitOfMeasure.Each, 79.99m, 10),
            ("HT-2001", "Claw Hammer 16oz", 1, 1, UnitOfMeasure.Each, 14.25m, 30),
            ("HT-2002", "Adjustable Wrench Set", 1, 1, UnitOfMeasure.Pack, 32.00m, 20),
            ("HT-2003", "Screwdriver Set 20pc", 1, 0, UnitOfMeasure.Pack, 24.99m, 25),
            ("HT-2004", "Tape Measure 25ft", 1, 1, UnitOfMeasure.Each, 9.75m, 40),
            ("HT-2005", "Utility Knife", 1, 0, UnitOfMeasure.Each, 6.50m, 50),
            ("FA-3001", "Wood Screws #8 x 2in", 2, 1, UnitOfMeasure.Box, 11.20m, 60),
            ("FA-3002", "Hex Bolts M8", 2, 1, UnitOfMeasure.Box, 15.40m, 45),
            ("FA-3003", "Concrete Anchors", 2, 1, UnitOfMeasure.Box, 18.60m, 35),
            ("FA-3004", "Common Nails 3in", 2, 1, UnitOfMeasure.Box, 9.90m, 70),
            ("FA-3005", "Machine Screws Assorted", 2, 1, UnitOfMeasure.Pack, 13.30m, 40),
            ("SE-4001", "Safety Glasses", 3, 4, UnitOfMeasure.Each, 5.99m, 60),
            ("SE-4002", "Hard Hat", 3, 4, UnitOfMeasure.Each, 21.00m, 30),
            ("SE-4003", "Work Gloves (pair)", 3, 4, UnitOfMeasure.Pack, 8.50m, 50),
            ("SE-4004", "High-Vis Vest", 3, 4, UnitOfMeasure.Each, 12.75m, 35),
            ("EL-5001", "Romex Wire 12/2 250ft", 4, 2, UnitOfMeasure.Each, 145.00m, 6),
            ("EL-5002", "Duplex Outlet", 4, 2, UnitOfMeasure.Each, 3.25m, 80),
            ("EL-5003", "Circuit Breaker 20A", 4, 2, UnitOfMeasure.Each, 17.90m, 25),
            ("EL-5004", "LED Shop Light", 4, 2, UnitOfMeasure.Each, 34.99m, 15),
            ("PL-6001", "PVC Pipe 1in 10ft", 5, 3, UnitOfMeasure.Each, 12.10m, 40),
            ("PL-6002", "Ball Valve 3/4in", 5, 3, UnitOfMeasure.Each, 9.40m, 30),
            ("PL-6003", "Pipe Fittings Assorted", 5, 3, UnitOfMeasure.Pack, 22.00m, 20)
        };

        var products = productSeeds.Select(p => new Product
        {
            Sku = p.Sku,
            Name = p.Name,
            Description = $"{p.Name} — standard stock item.",
            CategoryId = categories[p.Category].Id,
            SupplierId = suppliers[p.Supplier].Id,
            UnitOfMeasure = p.Uom,
            UnitPrice = p.Price,
            ReorderLevel = p.Reorder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 400))
        }).ToList();
        ctx.Products.AddRange(products);
        ctx.SaveChanges();

        var rng = new Random(42);
        foreach (var product in products)
        {
            var warehouseCount = rng.Next(1, 3);
            var chosenWarehouses = warehouses.OrderBy(_ => rng.Next()).Take(warehouseCount);
            foreach (var wh in chosenWarehouses)
            {
                ctx.StockLevels.Add(new StockLevel
                {
                    ProductId = product.Id,
                    WarehouseId = wh.Id,
                    QuantityOnHand = rng.Next(0, product.ReorderLevel * 4 + 5),
                    QuantityReserved = rng.Next(0, 5),
                    LastCountedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 60))
                });
            }
        }
        ctx.SaveChanges();

        var po1 = new PurchaseOrder
        {
            OrderNumber = "PO-00001",
            SupplierId = suppliers[0].Id,
            OrderDate = DateTime.UtcNow.AddDays(-10),
            ExpectedDate = DateTime.UtcNow.AddDays(-3),
            Status = PurchaseOrderStatus.Received,
            Notes = "Standard restock order.",
            CreatedByUserId = adminUser.Id,
            Lines =
            [
                new PurchaseOrderLine { ProductId = products[0].Id, QuantityOrdered = 20, QuantityReceived = 20, UnitPrice = products[0].UnitPrice },
                new PurchaseOrderLine { ProductId = products[1].Id, QuantityOrdered = 15, QuantityReceived = 15, UnitPrice = products[1].UnitPrice }
            ]
        };
        var po2 = new PurchaseOrder
        {
            OrderNumber = "PO-00002",
            SupplierId = suppliers[3].Id,
            OrderDate = DateTime.UtcNow.AddDays(-2),
            ExpectedDate = DateTime.UtcNow.AddDays(5),
            Status = PurchaseOrderStatus.Sent,
            Notes = "Plumbing restock for Riverside depot.",
            CreatedByUserId = users[1].Id,
            Lines =
            [
                new PurchaseOrderLine { ProductId = products[23].Id, QuantityOrdered = 30, QuantityReceived = 0, UnitPrice = products[23].UnitPrice },
                new PurchaseOrderLine { ProductId = products[24].Id, QuantityOrdered = 25, QuantityReceived = 0, UnitPrice = products[24].UnitPrice }
            ]
        };
        ctx.PurchaseOrders.AddRange(po1, po2);
        ctx.SaveChanges();

        var so1 = new SalesOrder
        {
            OrderNumber = "SO-00001",
            CustomerId = customers[0].Id,
            WarehouseId = warehouses[0].Id,
            OrderDate = DateTime.UtcNow.AddDays(-5),
            RequiredDate = DateTime.UtcNow.AddDays(-1),
            Status = SalesOrderStatus.Delivered,
            SatisfactionRating = 5,
            Notes = "Delivered on schedule.",
            CreatedByUserId = adminUser.Id,
            Lines =
            [
                new SalesOrderLine { ProductId = products[0].Id, QuantityOrdered = 4, QuantityShipped = 4, UnitPrice = products[0].UnitPrice },
                new SalesOrderLine { ProductId = products[10].Id, QuantityOrdered = 10, QuantityShipped = 10, UnitPrice = products[10].UnitPrice }
            ]
        };
        var so2 = new SalesOrder
        {
            OrderNumber = "SO-00002",
            CustomerId = customers[2].Id,
            WarehouseId = warehouses[1].Id,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            RequiredDate = DateTime.UtcNow.AddDays(4),
            Status = SalesOrderStatus.Confirmed,
            Notes = "Awaiting shipment.",
            CreatedByUserId = users[1].Id,
            Lines =
            [
                new SalesOrderLine { ProductId = products[19].Id, QuantityOrdered = 2, QuantityShipped = 0, UnitPrice = products[19].UnitPrice },
                new SalesOrderLine { ProductId = products[21].Id, QuantityOrdered = 6, QuantityShipped = 0, UnitPrice = products[21].UnitPrice }
            ]
        };
        ctx.SalesOrders.AddRange(so1, so2);
        ctx.SaveChanges();

        ctx.AppSettings.Add(new AppSettings
        {
            Id = 1,
            CompanyName = "Northfield Warehouse Supply",
            DefaultWarehouseId = warehouses[0].Id,
            LowStockThresholdPercent = 20,
            ThemeDarkMode = false,
            AccentColorHex = "#2D6CDF",
            BackupFolderPath = null,
            UIFontName = "Segoe UI",
            UIFontSize = 9f
        });
        ctx.SaveChanges();

        ctx.AuditLogs.AddRange(
            new AuditLog { EntityName = "PurchaseOrder", EntityId = po1.Id, Action = AuditAction.Create, Details = $"Created {po1.OrderNumber}", UserId = adminUser.Id, Timestamp = DateTime.UtcNow.AddDays(-10) },
            new AuditLog { EntityName = "PurchaseOrder", EntityId = po1.Id, Action = AuditAction.Update, Details = $"{po1.OrderNumber} marked Received", UserId = adminUser.Id, Timestamp = DateTime.UtcNow.AddDays(-3) },
            new AuditLog { EntityName = "SalesOrder", EntityId = so1.Id, Action = AuditAction.Create, Details = $"Created {so1.OrderNumber}", UserId = adminUser.Id, Timestamp = DateTime.UtcNow.AddDays(-5) },
            new AuditLog { EntityName = "SalesOrder", EntityId = so1.Id, Action = AuditAction.Update, Details = $"{so1.OrderNumber} marked Delivered", UserId = users[1].Id, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new AuditLog { EntityName = "Product", EntityId = products[0].Id, Action = AuditAction.Create, Details = $"Created {products[0].Sku}", UserId = adminUser.Id, Timestamp = DateTime.UtcNow.AddDays(-30) }
        );
        ctx.SaveChanges();
    }
}
