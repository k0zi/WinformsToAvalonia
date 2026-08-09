using System.Drawing.Drawing2D;

namespace WarehouseApp.Common;

/// <summary>
/// Generates small glyph-based bitmaps at runtime so the app needs no embedded
/// binary image assets — every icon/logo/product picture is drawn with GDI+.
/// </summary>
public static class AppIcons
{
    public static Bitmap CreateGlyph(string glyph, Color color, int size = 16, Color? backColor = null)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        if (backColor is { } bc)
        {
            g.Clear(bc);
        }
        using var font = new Font("Segoe UI", size * 0.6f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        var textSize = g.MeasureString(glyph, font);
        g.DrawString(glyph, font, brush, (size - textSize.Width) / 2f, (size - textSize.Height) / 2f);
        return bmp;
    }

    public static Bitmap CreateLogo(int size = 64)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bgBrush = new SolidBrush(Color.FromArgb(45, 108, 223));
        g.FillEllipse(bgBrush, 0, 0, size, size);
        using var font = new Font("Segoe UI", size * 0.4f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        var text = "W";
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, textBrush, (size - textSize.Width) / 2f, (size - textSize.Height) / 2f);
        return bmp;
    }

    public static Bitmap CreatePlaceholderProductImage(int width = 120, int height = 120)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.WhiteSmoke);
        using var pen = new Pen(Color.Silver, 2) { DashStyle = DashStyle.Dash };
        g.DrawRectangle(pen, 1, 1, width - 3, height - 3);
        using var font = new Font("Segoe UI", 9f);
        var text = "No Image";
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, Brushes.Gray, (width - textSize.Width) / 2f, (height - textSize.Height) / 2f);
        return bmp;
    }
}
