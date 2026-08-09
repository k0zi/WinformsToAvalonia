namespace WarehouseApp.Data.Models;

public class AppSettings
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int? DefaultWarehouseId { get; set; }
    public int LowStockThresholdPercent { get; set; } = 20;
    public bool ThemeDarkMode { get; set; }
    public string AccentColorHex { get; set; } = "#2D6CDF";
    public string? BackupFolderPath { get; set; }
    public string UIFontName { get; set; } = "Segoe UI";
    public float UIFontSize { get; set; } = 9f;
}
