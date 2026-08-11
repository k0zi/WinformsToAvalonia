using Avalonia.Media;
using System.Drawing.Drawing2D;

namespace WarehouseApp.Controls;

public enum ChartType
{
    Bar,
    Line
}

public class ChartSeries
{
    public string Name { get; set; } = string.Empty;
    public Color Color { get; set; } = Colors.SteelBlue;
    public List<(string Label, double Value)> Points { get; set; } = [];
}

public class ChartPointClickedEventArgs(string seriesName, string label, double value) : EventArgs
{
    public string SeriesName { get; } = seriesName;
    public string Label { get; } = label;
    public double Value { get; } = value;
}
