using System.Drawing.Drawing2D;

namespace WarehouseApp.Controls;

public class LoadingSpinnerControl : Control
{
    private readonly System.Windows.Forms.Timer _timer;
    private int _angle;
    private bool _spinning;

    public bool Spinning
    {
        get => _spinning;
        set
        {
            _spinning = value;
            Visible = value;
            if (value)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }
    }

    public Color SpinnerColor { get; set; } = Color.SteelBlue;

    public LoadingSpinnerControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(32, 32);
        BackColor = Color.Transparent;
        _timer = new System.Windows.Forms.Timer { Interval = 40 };
        _timer.Tick += (_, _) =>
        {
            _angle = (_angle + 30) % 360;
            Invalidate();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor == Color.Transparent ? Parent?.BackColor ?? SystemColors.Control : BackColor);

        var rect = new Rectangle(2, 2, Width - 4, Height - 4);
        for (var i = 0; i < 8; i++)
        {
            var alpha = 255 - i * 28;
            if (alpha < 30)
            {
                alpha = 30;
            }
            using var pen = new Pen(Color.FromArgb(alpha, SpinnerColor), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(pen, rect, _angle - i * 20, 16);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
