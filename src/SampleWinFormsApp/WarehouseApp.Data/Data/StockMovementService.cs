using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Data.Data;

/// <summary>
/// Single place that mutates StockLevel + writes the matching StockMovement audit row,
/// shared by every screen that moves stock (receipt, issue, transfer, adjustment).
/// </summary>
public class StockMovementService(WarehouseDbContext ctx)
{
    public async Task PostGoodsReceiptAsync(int productId, int warehouseId, int quantity, int userId, int? referenceId = null, string? notes = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        await using var tx = await ctx.Database.BeginTransactionAsync();

        var stockLevel = await GetOrCreateStockLevelAsync(productId, warehouseId, null);
        stockLevel.QuantityOnHand += quantity;

        ctx.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = MovementType.In,
            Quantity = quantity,
            ReferenceType = referenceId.HasValue ? ReferenceType.PurchaseOrder : ReferenceType.Manual,
            ReferenceId = referenceId,
            MovementDate = DateTime.UtcNow,
            Notes = notes,
            CreatedByUserId = userId
        });

        await ctx.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task PostGoodsIssueAsync(int productId, int warehouseId, int quantity, int userId, int? referenceId = null, string? notes = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        await using var tx = await ctx.Database.BeginTransactionAsync();

        var stockLevel = await GetOrCreateStockLevelAsync(productId, warehouseId, null);
        if (stockLevel.QuantityOnHand < quantity)
        {
            throw new InvalidOperationException($"Insufficient stock: only {stockLevel.QuantityOnHand} on hand.");
        }
        stockLevel.QuantityOnHand -= quantity;

        ctx.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = MovementType.Out,
            Quantity = quantity,
            ReferenceType = referenceId.HasValue ? ReferenceType.SalesOrder : ReferenceType.Manual,
            ReferenceId = referenceId,
            MovementDate = DateTime.UtcNow,
            Notes = notes,
            CreatedByUserId = userId
        });

        await ctx.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task PostTransferAsync(int productId, int fromWarehouseId, int toWarehouseId, int quantity, int userId, string? notes = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
        if (fromWarehouseId == toWarehouseId)
        {
            throw new InvalidOperationException("Source and destination warehouses must differ.");
        }

        await using var tx = await ctx.Database.BeginTransactionAsync();

        var fromStock = await GetOrCreateStockLevelAsync(productId, fromWarehouseId, null);
        if (fromStock.QuantityOnHand < quantity)
        {
            throw new InvalidOperationException($"Insufficient stock at source warehouse: only {fromStock.QuantityOnHand} on hand.");
        }
        fromStock.QuantityOnHand -= quantity;

        var toStock = await GetOrCreateStockLevelAsync(productId, toWarehouseId, null);
        toStock.QuantityOnHand += quantity;

        var now = DateTime.UtcNow;
        ctx.StockMovements.Add(new StockMovement
        {
            ProductId = productId, WarehouseId = fromWarehouseId, MovementType = MovementType.TransferOut,
            Quantity = quantity, ReferenceType = ReferenceType.Transfer, MovementDate = now, Notes = notes, CreatedByUserId = userId
        });
        ctx.StockMovements.Add(new StockMovement
        {
            ProductId = productId, WarehouseId = toWarehouseId, MovementType = MovementType.TransferIn,
            Quantity = quantity, ReferenceType = ReferenceType.Transfer, MovementDate = now, Notes = notes, CreatedByUserId = userId
        });

        await ctx.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task PostAdjustmentAsync(int productId, int warehouseId, int newQuantityOnHand, int userId, string? notes = null)
    {
        if (newQuantityOnHand < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newQuantityOnHand), "Quantity cannot be negative.");
        }

        await using var tx = await ctx.Database.BeginTransactionAsync();

        var stockLevel = await GetOrCreateStockLevelAsync(productId, warehouseId, null);
        var delta = newQuantityOnHand - stockLevel.QuantityOnHand;
        stockLevel.QuantityOnHand = newQuantityOnHand;
        stockLevel.LastCountedAt = DateTime.UtcNow;

        if (delta != 0)
        {
            ctx.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                MovementType = MovementType.Adjustment,
                Quantity = delta,
                ReferenceType = ReferenceType.Adjustment,
                MovementDate = DateTime.UtcNow,
                Notes = notes,
                CreatedByUserId = userId
            });
        }

        await ctx.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task<StockLevel> GetOrCreateStockLevelAsync(int productId, int warehouseId, int? locationId)
    {
        var existing = await ctx.StockLevels
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId && s.LocationId == locationId);
        if (existing is not null)
        {
            return existing;
        }

        var created = new StockLevel { ProductId = productId, WarehouseId = warehouseId, LocationId = locationId, QuantityOnHand = 0, QuantityReserved = 0 };
        ctx.StockLevels.Add(created);
        return created;
    }
}
