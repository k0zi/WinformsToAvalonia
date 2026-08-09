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
    public Color Color { get; set; } = Color.SteelBlue;
    public List<(string Label, double Value)> Points { get; set; } = [];
}

public class ChartPointClickedEventArgs(string seriesName, string label, double value) : EventArgs
{
    public string SeriesName { get; } = seriesName;
    public string Label { get; } = label;
    public double Value { get; } = value;
}

public class ChartControl : Control
{
    private readonly List<ChartSeries> _series = [];
    private readonly List<(RectangleF Bounds, string SeriesName, string Label, double Value)> _hitRegions = [];

    public string Title { get; set; } = string.Empty;
    public bool ShowLegend { get; set; } = true;
    public ChartType Type { get; set; } = ChartType.Bar;
    public Font TitleFont { get; set; } = new("Segoe UI", 10f, FontStyle.Bold);

    public IReadOnlyList<ChartSeries> Series => _series;

    public event EventHandler<ChartPointClickedEventArgs>? PointClicked;

    public ChartControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(400, 260);
        BackColor = Color.White;
    }

    public void SetData(IEnumerable<ChartSeries> series)
    {
        _series.Clear();
        _series.AddRange(series);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        _hitRegions.Clear();

        var top = 10f;
        if (!string.IsNullOrEmpty(Title))
        {
            g.DrawString(Title, TitleFont, Brushes.Black, 10, top);
            top += TitleFont.Height + 6;
        }

        var legendHeight = ShowLegend && _series.Count > 0 ? 20f : 0f;
        var plotRect = new RectangleF(50, top, Width - 70, Height - top - 30 - legendHeight);
        if (plotRect.Width <= 0 || plotRect.Height <= 0 || _series.Count == 0)
        {
            return;
        }

        var allPoints = _series.SelectMany(s => s.Points).ToList();
        if (allPoints.Count == 0)
        {
            return;
        }

        var maxValue = Math.Max(allPoints.Max(p => p.Value), 0.0001);
        var labels = _series[0].Points.Select(p => p.Label).ToList();

        using (var axisPen = new Pen(Color.Gray, 1))
        {
            g.DrawLine(axisPen, plotRect.Left, plotRect.Top, plotRect.Left, plotRect.Bottom);
            g.DrawLine(axisPen, plotRect.Left, plotRect.Bottom, plotRect.Right, plotRect.Bottom);
        }

        using var gridPen = new Pen(Color.WhiteSmoke, 1);
        using var axisFont = new Font("Segoe UI", 7.5f);
        for (var i = 0; i <= 4; i++)
        {
            var y = plotRect.Bottom - plotRect.Height * i / 4f;
            g.DrawLine(gridPen, plotRect.Left, y, plotRect.Right, y);
            var label = (maxValue * i / 4).ToString("0.#");
            var labelSize = g.MeasureString(label, axisFont);
            g.DrawString(label, axisFont, Brushes.Gray, plotRect.Left - labelSize.Width - 4, y - labelSize.Height / 2);
        }

        if (Type == ChartType.Bar)
        {
            DrawBars(g, plotRect, labels, maxValue);
        }
        else
        {
            DrawLines(g, plotRect, labels, maxValue);
        }

        for (var i = 0; i < labels.Count; i++)
        {
            var x = plotRect.Left + plotRect.Width * (i + 0.5f) / labels.Count;
            var labelSize = g.MeasureString(labels[i], axisFont);
            g.DrawString(labels[i], axisFont, Brushes.Black, x - labelSize.Width / 2, plotRect.Bottom + 4);
        }

        if (ShowLegend)
        {
            var lx = 10f;
            var ly = Height - legendHeight + 2;
            using var legendFont = new Font("Segoe UI", 7.5f);
            foreach (var s in _series)
            {
                using var brush = new SolidBrush(s.Color);
                g.FillRectangle(brush, lx, ly + 2, 10, 10);
                g.DrawString(s.Name, legendFont, Brushes.Black, lx + 14, ly);
                lx += g.MeasureString(s.Name, legendFont).Width + 30;
            }
        }
    }

    private void DrawBars(Graphics g, RectangleF plotRect, List<string> labels, double maxValue)
    {
        var groupWidth = plotRect.Width / labels.Count;
        var barWidth = groupWidth / (_series.Count + 1);

        for (var s = 0; s < _series.Count; s++)
        {
            using var brush = new SolidBrush(_series[s].Color);
            for (var i = 0; i < _series[s].Points.Count && i < labels.Count; i++)
            {
                var (label, value) = _series[s].Points[i];
                var barHeight = (float)(value / maxValue) * plotRect.Height;
                var x = plotRect.Left + groupWidth * i + barWidth * (s + 0.5f);
                var rect = new RectangleF(x, plotRect.Bottom - barHeight, barWidth * 0.85f, barHeight);
                g.FillRectangle(brush, rect);
                _hitRegions.Add((rect, _series[s].Name, label, value));
            }
        }
    }

    private void DrawLines(Graphics g, RectangleF plotRect, List<string> labels, double maxValue)
    {
        foreach (var series in _series)
        {
            if (series.Points.Count == 0)
            {
                continue;
            }

            using var pen = new Pen(series.Color, 2f);
            using var pointBrush = new SolidBrush(series.Color);
            var points = new PointF[series.Points.Count];
            for (var i = 0; i < series.Points.Count; i++)
            {
                var (label, value) = series.Points[i];
                var x = plotRect.Left + plotRect.Width * (i + 0.5f) / labels.Count;
                var y = plotRect.Bottom - (float)(value / maxValue) * plotRect.Height;
                points[i] = new PointF(x, y);
                var hitRect = new RectangleF(x - 4, y - 4, 8, 8);
                g.FillEllipse(pointBrush, hitRect);
                _hitRegions.Add((hitRect, series.Name, label, value));
            }
            if (points.Length > 1)
            {
                g.DrawLines(pen, points);
            }
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        foreach (var region in _hitRegions)
        {
            if (region.Bounds.Contains(e.Location))
            {
                PointClicked?.Invoke(this, new ChartPointClickedEventArgs(region.SeriesName, region.Label, region.Value));
                break;
            }
        }
    }
}
