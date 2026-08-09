namespace WarehouseApp.Data.Models;

public enum UnitOfMeasure
{
    Each,
    Box,
    Pallet,
    Pack
}

public enum LocationType
{
    Zone,
    Shelf
}

public enum MovementType
{
    In,
    Out,
    TransferIn,
    TransferOut,
    Adjustment
}

public enum ReferenceType
{
    PurchaseOrder,
    SalesOrder,
    Transfer,
    Adjustment,
    Manual
}

public enum PurchaseOrderStatus
{
    Draft,
    Sent,
    PartiallyReceived,
    Received,
    Cancelled
}

public enum SalesOrderStatus
{
    New,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

public enum AuditAction
{
    Create,
    Update,
    Delete
}
