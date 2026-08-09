using System.Drawing.Drawing2D;

namespace WarehouseApp.Controls;

public class CardTileControl : Control
{
    private bool _hovering;
    private string _title = "Title";
    private string _subtitle = "Subtitle";
    private int? _badgeCount;

    public event EventHandler? TileClicked;

    public string Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    public string Subtitle
    {
        get => _subtitle;
        set { _subtitle = value; Invalidate(); }
    }

    public int? BadgeCount
    {
        get => _badgeCount;
        set { _badgeCount = value; Invalidate(); }
    }

    public Color AccentColor { get; set; } = Color.SteelBlue;
    public char Glyph { get; set; } = '▣';

    public CardTileControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(150, 100);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovering = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovering = false;
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        TileClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? SystemColors.Control);

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        var elevation = _hovering ? 3 : 1;
        var shadowRect = new Rectangle(rect.X + elevation, rect.Y + elevation, rect.Width, rect.Height);

        using (var shadowPath = RoundedRect(shadowRect, 10))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
        {
            g.FillPath(shadowBrush, shadowPath);
        }

        using (var path = RoundedRect(rect, 10))
        {
            using var backBrush = new SolidBrush(_hovering ? Color.FromArgb(245, 249, 255) : Color.White);
            g.FillPath(backBrush, path);
            using var borderPen = new Pen(_hovering ? AccentColor : Color.LightGray, 1.5f);
            g.DrawPath(borderPen, path);
        }

        using (var glyphBrush = new SolidBrush(AccentColor))
        using (var glyphFont = new Font("Segoe UI", 16f))
        {
            g.DrawString(Glyph.ToString(), glyphFont, glyphBrush, rect.X + 12, rect.Y + 10);
        }

        using (var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold))
        {
            g.DrawString(_title, titleFont, Brushes.Black, rect.X + 12, rect.Bottom - 42);
        }
        using (var subFont = new Font("Segoe UI", 8f))
        {
            g.DrawString(_subtitle, subFont, Brushes.Gray, rect.X + 12, rect.Bottom - 24, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
        }

        if (_badgeCount is > 0)
        {
            var badgeText = _badgeCount > 99 ? "99+" : _badgeCount.ToString()!;
            using var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            var badgeSize = g.MeasureString(badgeText, badgeFont);
            var badgeRect = new RectangleF(rect.Right - badgeSize.Width - 14, rect.Y + 8, badgeSize.Width + 8, badgeSize.Height + 4);
            using var badgePath = RoundedRect(Rectangle.Round(badgeRect), (int)(badgeRect.Height / 2));
            using var badgeBrush = new SolidBrush(Color.IndianRed);
            g.FillPath(badgeBrush, badgePath);
            g.DrawString(badgeText, badgeFont, Brushes.White, badgeRect.X + 4, badgeRect.Y + 2);
        }
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
