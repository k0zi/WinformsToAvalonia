using System.Drawing.Drawing2D;

namespace WarehouseApp.Controls;

public class StarRatingControl : Control
{
    private int _maxStars = 5;
    private int _value;
    private int _hoverValue = -1;

    public event EventHandler? RatingChanged;

    public int MaxStars
    {
        get => _maxStars;
        set { _maxStars = Math.Max(1, value); Invalidate(); }
    }

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0, _maxStars);
            if (clamped == _value)
            {
                return;
            }
            _value = clamped;
            Invalidate();
            RatingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool ReadOnly { get; set; }
    public Color FilledColor { get; set; } = Color.Goldenrod;
    public Color EmptyColor { get; set; } = Color.LightGray;

    public StarRatingControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(120, 24);
        Cursor = Cursors.Hand;
    }

    private float StarSize => Height - 4f;

    private GraphicsPath BuildStar(float x, float y, float size)
    {
        var path = new GraphicsPath();
        const int points = 5;
        var outerRadius = size / 2f;
        var innerRadius = outerRadius * 0.4f;
        var cx = x + outerRadius;
        var cy = y + outerRadius;
        var vertices = new PointF[points * 2];
        for (var i = 0; i < points * 2; i++)
        {
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            var angle = Math.PI / points * i - Math.PI / 2;
            vertices[i] = new PointF(cx + (float)(radius * Math.Cos(angle)), cy + (float)(radius * Math.Sin(angle)));
        }
        path.AddPolygon(vertices);
        return path;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var displayValue = _hoverValue >= 0 ? _hoverValue : _value;
        for (var i = 0; i < _maxStars; i++)
        {
            using var path = BuildStar(i * StarSize, 2, StarSize);
            using var brush = new SolidBrush(i < displayValue ? FilledColor : EmptyColor);
            g.FillPath(brush, path);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (ReadOnly)
        {
            return;
        }
        var star = (int)(e.X / StarSize) + 1;
        star = Math.Clamp(star, 0, _maxStars);
        if (star != _hoverValue)
        {
            _hoverValue = star;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverValue = -1;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (ReadOnly)
        {
            return;
        }
        var star = (int)(e.X / StarSize) + 1;
        Value = Math.Clamp(star, 0, _maxStars);
    }
}
