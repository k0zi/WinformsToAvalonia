using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace WarehouseApp.Common;

public static class AppIcons
{
    public static Bitmap CreateGlyph(string glyph, Color color, int size = 16, Color? backColor = null)
    {
        var bmp = new RenderTargetBitmap(new PixelSize(size, size));
        using var g = bmp.CreateDrawingContext();
        if (backColor is { } bc)
        {
            g.FillRectangle(new SolidColorBrush(bc), new Rect(0, 0, size, size));
        }
        var brush = new SolidColorBrush(color);
        var textSize = new FormattedText(glyph, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), size * 0.6f, brush);
        g.DrawText(textSize, new Point((size - textSize.Width) / 2f, (size - textSize.Height) / 2f));
        return bmp;
    }

    public static Bitmap CreateLogo(int size = 64)
    {
        var bmp = new RenderTargetBitmap(new PixelSize(size, size));
        using var g = bmp.CreateDrawingContext();
        var bgBrush = new SolidColorBrush(Color.FromRgb((byte)(45), (byte)(108), (byte)(223)));
        g.DrawEllipse(bgBrush, null, new Rect(0, 0, size, size));
        var textBrush = new SolidColorBrush(Colors.White);
        var text = "W";
        var textSize = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), size * 0.4f, textBrush);
        g.DrawText(textSize, new Point((size - textSize.Width) / 2f, (size - textSize.Height) / 2f));
        return bmp;
    }

    public static Bitmap CreatePlaceholderProductImage(int width = 120, int height = 120)
    {
        var bmp = new RenderTargetBitmap(new PixelSize(width, height));
        using var g = bmp.CreateDrawingContext();
        g.FillRectangle(new SolidColorBrush(Colors.WhiteSmoke), new Rect(0, 0, width, height));
        var pen = new Pen(new SolidColorBrush(Colors.Silver), 2, new DashStyle(new double[] { 2, 2 }, 0) /* best-effort dash pattern approximation - review visually */);
        g.DrawRectangle(null, pen, new Rect(1, 1, width - 3, height - 3));
        var text = "No Image";
        var textSize = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Normal), 9f, Brushes.Gray);
        g.DrawText(textSize, new Point((width - textSize.Width) / 2f, (height - textSize.Height) / 2f));
        return bmp;
    }

}
