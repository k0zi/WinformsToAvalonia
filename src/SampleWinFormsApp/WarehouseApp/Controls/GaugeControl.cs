using System.Drawing.Drawing2D;

namespace WarehouseApp.Controls;

public class GaugeControl : Control
{
    private double _minimum;
    private double _maximum = 100;
    private double _value;
    private string _unit = string.Empty;

    public event EventHandler? ValueChanged;

    public double Minimum
    {
        get => _minimum;
        set { _minimum = value; Invalidate(); }
    }

    public double Maximum
    {
        get => _maximum;
        set { _maximum = value; Invalidate(); }
    }

    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (Math.Abs(clamped - _value) < double.Epsilon)
            {
                return;
            }
            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Unit
    {
        get => _unit;
        set { _unit = value; Invalidate(); }
    }

    public double WarningThreshold { get; set; } = 0.6;
    public double DangerThreshold { get; set; } = 0.85;

    public GaugeControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(160, 140);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var rect = new Rectangle(10, 10, Width - 20, Width - 20);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        const float startAngle = 180f;
        const float sweepTotal = 180f;

        using (var pen = new Pen(Color.Gainsboro, 14))
        {
            g.DrawArc(pen, rect, startAngle, sweepTotal);
        }

        var range = _maximum - _minimum;
        if (range > 0)
        {
            var warningSweep = sweepTotal * (float)WarningThreshold;
            var dangerSweep = sweepTotal * (float)(DangerThreshold - WarningThreshold);
            var okSweep = warningSweep;
            using var okPen = new Pen(Color.MediumSeaGreen, 14);
            using var warnPen = new Pen(Color.Goldenrod, 14);
            using var dangerPen = new Pen(Color.IndianRed, 14);
            g.DrawArc(okPen, rect, startAngle, okSweep);
            g.DrawArc(warnPen, rect, startAngle + okSweep, dangerSweep);
            g.DrawArc(dangerPen, rect, startAngle + okSweep + dangerSweep, sweepTotal - okSweep - dangerSweep);

            var fraction = (_value - _minimum) / range;
            var needleAngleDeg = startAngle + sweepTotal * fraction;
            var needleAngleRad = needleAngleDeg * Math.PI / 180.0;
            var center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            var needleLength = rect.Width / 2f - 6;
            var tip = new PointF(
                center.X + (float)(needleLength * Math.Cos(needleAngleRad)),
                center.Y + (float)(needleLength * Math.Sin(needleAngleRad)));

            using var needlePen = new Pen(Color.Black, 2.5f);
            g.DrawLine(needlePen, center, tip);
            using var hubBrush = new SolidBrush(Color.Black);
            g.FillEllipse(hubBrush, center.X - 4, center.Y - 4, 8, 8);
        }

        var valueText = $"{_value:0.#}{(string.IsNullOrEmpty(_unit) ? "" : " " + _unit)}";
        using var font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
        var textSize = g.MeasureString(valueText, font);
        g.DrawString(valueText, font, Brushes.Black, rect.X + rect.Width / 2f - textSize.Width / 2f, rect.Bottom - 18);
    }
}
