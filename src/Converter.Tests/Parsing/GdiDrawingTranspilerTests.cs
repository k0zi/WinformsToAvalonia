using Converter.Core.Parsing;
using Microsoft.CodeAnalysis.CSharp;

namespace Converter.Tests.Parsing;

public class GdiDrawingTranspilerTests
{
    // Verbatim copy of the real WarehouseApp sample's Common/AppIcons.cs - the concrete case
    // that motivated this transpiler (silently copied and produced build errors with zero
    // warning before this feature).
    private const string AppIconsSource = """
        using System.Drawing.Drawing2D;

        namespace WarehouseApp.Common;

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
        """;

    [Fact]
    public void TryTranspile_RealAppIconsSource_Succeeds()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);

        Assert.True(result.Success, result.FailureReason);
        Assert.NotNull(result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_RealAppIconsSource_ProducesSyntacticallyValidCSharp()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        var diagnostics = CSharpSyntaxTree.ParseText(result.TransformedSource!).GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void TryTranspile_RealAppIconsSource_RemovesAllSystemDrawingReferences()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.DoesNotContain("System.Drawing", result.TransformedSource);
        Assert.DoesNotContain("Graphics.FromImage", result.TransformedSource);
        Assert.DoesNotContain("SolidBrush", result.TransformedSource);
        Assert.DoesNotContain("SmoothingMode", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_RealAppIconsSource_UsesAvaloniaRenderApi()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.Contains("using Avalonia.Media.Imaging;", result.TransformedSource);
        Assert.Contains("new RenderTargetBitmap(new PixelSize(", result.TransformedSource);
        Assert.Contains("CreateDrawingContext()", result.TransformedSource);
        Assert.Contains("new SolidColorBrush(", result.TransformedSource);
        Assert.Contains("new FormattedText(", result.TransformedSource);
        Assert.Contains("DrawText(", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_ConditionalClear_TranslatesToFillRectangleInsideIf()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.Contains("if (backColor is { } bc)", result.TransformedSource);
        Assert.Contains("FillRectangle(new SolidColorBrush(bc)", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_ThreeArgFromArgb_TranslatesToFromRgb()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.Contains("Color.FromRgb((byte)(45), (byte)(108), (byte)(223))", result.TransformedSource);
        Assert.DoesNotContain("Color.FromArgb(45", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_NamedColor_TranslatesToColorsClass()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.Contains("Colors.White", result.TransformedSource);
        Assert.Contains("Colors.WhiteSmoke", result.TransformedSource);
        Assert.Contains("Colors.Silver", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_BoldFont_TranslatesToFontWeightBold()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.Contains("FontWeight.Bold", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_BrushesGrayStaticReference_PassesThroughUnchanged()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.Contains("Brushes.Gray", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_MethodSignaturesPreservedVerbatim()
    {
        var result = GdiDrawingTranspiler.TryTranspile(AppIconsSource);
        Assert.True(result.Success, result.FailureReason);

        Assert.Contains("public static Bitmap CreateGlyph(string glyph, Color color, int size = 16, Color? backColor = null)", result.TransformedSource);
        Assert.Contains("public static Bitmap CreateLogo(int size = 64)", result.TransformedSource);
        Assert.Contains("public static Bitmap CreatePlaceholderProductImage(int width = 120, int height = 120)", result.TransformedSource);
    }

    [Fact]
    public void TryTranspile_UnrecognizedGdiApi_FailsWithoutThrowing()
    {
        const string source = """
            namespace WarehouseApp.Common;

            public static class Weird
            {
                public static Bitmap DrawSomething(Bitmap source)
                {
                    var bmp = new Bitmap(10, 10);
                    using var g = Graphics.FromImage(bmp);
                    g.DrawImage(source, 0, 0);
                    return bmp;
                }
            }
            """;

        var result = GdiDrawingTranspiler.TryTranspile(source);

        Assert.False(result.Success);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void TryTranspile_NoGdiApiAtAll_StillHandledGracefully()
    {
        const string source = """
            namespace WarehouseApp.Common;

            public static class Db
            {
                public static int Add(int a, int b)
                {
                    return a + b;
                }
            }
            """;

        var result = GdiDrawingTranspiler.TryTranspile(source);

        // Nothing GDI+-specific to recognize, but nothing unrecognized either - the method
        // body only contains a plain return statement, which is a recognized shape.
        Assert.True(result.Success, result.FailureReason);
    }

    [Fact]
    public void HasGdiDrawingApiUsage_FileWithGraphicsAndBitmap_ReturnsTrue()
    {
        var root = CSharpSyntaxTree.ParseText(AppIconsSource).GetRoot();

        Assert.True(GdiDrawingTranspiler.HasGdiDrawingApiUsage(root));
    }

    [Fact]
    public void HasGdiDrawingApiUsage_PlainFile_ReturnsFalse()
    {
        var root = CSharpSyntaxTree.ParseText("namespace X; public static class Y { public static int Z() => 1; }").GetRoot();

        Assert.False(GdiDrawingTranspiler.HasGdiDrawingApiUsage(root));
    }

    [Fact]
    public void TryRewriteColorOnly_NamedColor_RewritesToColorsClass()
    {
        const string source = """
            using System.Drawing;

            namespace WarehouseApp.Common;

            public static class Palette
            {
                public static Color Highlight => Color.CornflowerBlue;
            }
            """;

        var result = GdiDrawingTranspiler.TryRewriteColorOnly(source);

        Assert.NotNull(result);
        Assert.Contains("Colors.CornflowerBlue", result);
        Assert.DoesNotContain("using System.Drawing;", result);
        Assert.Contains("using Avalonia.Media;", result);
    }

    [Fact]
    public void TryRewriteColorOnly_ThreeArgFromArgb_RewritesToFromRgb()
    {
        const string source = """
            namespace WarehouseApp.Common;

            public static class Palette
            {
                public static Color Brand => Color.FromArgb(45, 108, 223);
            }
            """;

        var result = GdiDrawingTranspiler.TryRewriteColorOnly(source);

        Assert.NotNull(result);
        Assert.Contains("Color.FromRgb((byte)(45), (byte)(108), (byte)(223))", result);
    }

    [Fact]
    public void TryRewriteColorOnly_ResultIsSyntacticallyValid()
    {
        const string source = """
            namespace WarehouseApp.Common;

            public static class Palette
            {
                public static Color Highlight => Color.CornflowerBlue;
                public static Color Brand => Color.FromArgb(45, 108, 223);
            }
            """;

        var result = GdiDrawingTranspiler.TryRewriteColorOnly(source);
        Assert.NotNull(result);

        var diagnostics = CSharpSyntaxTree.ParseText(result!).GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void TryRewriteColorOnly_NoColorReferences_ReturnsNull()
    {
        const string source = """
            namespace WarehouseApp.Common;

            public static class Db
            {
                public static int Add(int a, int b) => a + b;
            }
            """;

        var result = GdiDrawingTranspiler.TryRewriteColorOnly(source);

        Assert.Null(result);
    }
}
