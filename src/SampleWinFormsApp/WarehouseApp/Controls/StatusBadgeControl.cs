using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;

namespace WarehouseApp.Controls;

public enum BadgeStyle
{
    Success,
    Warning,
    Danger,
    Info,
    Neutral
}

public class StatusBadgeControl : Control
{
    private string _text = "Status";
    private BadgeStyle _style = BadgeStyle.Neutral;

    [AllowNull]
    public override string Text
    {
        get => _text;
        set { _text = value ?? string.Empty; UpdateSize(); Invalidate(); }
    }

    public BadgeStyle BadgeStyle
    {
        get => _style;
        set { _style = value; Invalidate(); }
    }

    public StatusBadgeControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        UpdateSize();
    }

    private void UpdateSize()
    {
        using var g = CreateGraphics();
        var size = g.MeasureString(_text, Font);
        Size = new Size((int)size.Width + 24, (int)size.Height + 8);
    }

    private (Color Back, Color Fore) GetColors() => _style switch
    {
        BadgeStyle.Success => (Color.FromArgb(220, 245, 225), Color.FromArgb(30, 120, 60)),
        BadgeStyle.Warning => (Color.FromArgb(255, 244, 214), Color.FromArgb(150, 105, 10)),
        BadgeStyle.Danger => (Color.FromArgb(252, 224, 224), Color.FromArgb(170, 40, 40)),
        BadgeStyle.Info => (Color.FromArgb(220, 235, 252), Color.FromArgb(30, 90, 160)),
        _ => (Color.FromArgb(232, 232, 232), Color.FromArgb(90, 90, 90))
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? SystemColors.Control);

        var (back, fore) = GetColors();
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, Height / 2);
        using var backBrush = new SolidBrush(back);
        g.FillPath(backBrush, path);

        using var foreBrush = new SolidBrush(fore);
        var textSize = g.MeasureString(_text, Font);
        g.DrawString(_text, Font, foreBrush, (Width - textSize.Width) / 2f, (Height - textSize.Height) / 2f);
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
}
