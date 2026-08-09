using System.Drawing.Drawing2D;

namespace WarehouseApp.Controls;

public class ToggleSwitchControl : Control
{
    private bool _checked;
    private double _thumbPosition;
    private readonly System.Windows.Forms.Timer _animationTimer;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }
            _checked = value;
            _animationTimer.Start();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string OnText { get; set; } = "ON";
    public string OffText { get; set; } = "OFF";
    public Color OnColor { get; set; } = Color.MediumSeaGreen;
    public Color OffColor { get; set; } = Color.Gainsboro;

    public ToggleSwitchControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(60, 26);
        Cursor = Cursors.Hand;

        _animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _animationTimer.Tick += AnimationTimer_Tick;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        var target = _checked ? 1.0 : 0.0;
        var step = 0.15;
        if (Math.Abs(_thumbPosition - target) < step)
        {
            _thumbPosition = target;
            _animationTimer.Stop();
        }
        else
        {
            _thumbPosition += target > _thumbPosition ? step : -step;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var trackRect = new Rectangle(0, 0, Width - 1, Height - 1);
        var trackColor = BlendColor(OffColor, OnColor, _thumbPosition);
        using (var trackBrush = new SolidBrush(trackColor))
        using (var path = RoundedRect(trackRect, Height / 2))
        {
            g.FillPath(trackBrush, path);
        }

        var diameter = Height - 6;
        var minX = 3;
        var maxX = Width - diameter - 3;
        var thumbX = minX + (int)((maxX - minX) * _thumbPosition);
        using (var thumbBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(thumbBrush, thumbX, 3, diameter, diameter);
        }

        var text = _checked ? OnText : OffText;
        using var font = new Font(Font.FontFamily, 7.5f, FontStyle.Bold);
        var textSize = g.MeasureString(text, font);
        var textX = _checked ? (Width - diameter - 6 - textSize.Width) / 2 + 2 : (Width + diameter) / 2f - textSize.Width / 2f - 2;
        g.DrawString(text, font, Brushes.White, textX, (Height - textSize.Height) / 2f);
    }

    private static Color BlendColor(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
