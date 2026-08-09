namespace WarehouseApp.Data.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string? Details { get; set; }
    public int UserId { get; set; }
    public DateTime Timestamp { get; set; }

    public User User { get; set; } = null!;
}
