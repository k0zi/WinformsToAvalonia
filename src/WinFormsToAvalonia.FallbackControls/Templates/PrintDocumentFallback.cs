using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// The Avalonia counterpart of WinForms' <c>PrintPageEventArgs</c>.
/// </summary>
/// <remarks>
/// <c>Context</c> stands in for <c>Graphics</c>, and the two rectangles for the properties of the
/// same name. <c>HasMorePages</c> is settable and read back after the handler returns, exactly as
/// WinForms did - so a handler that paginates still says what it meant, even though this
/// implementation renders only the first page.
/// </remarks>
public sealed class PrintPageSurfaceEventArgs : EventArgs
{
    public PrintPageSurfaceEventArgs(DrawingContext context, Rect pageBounds, Rect marginBounds)
    {
        Context = context;
        PageBounds = pageBounds;
        MarginBounds = marginBounds;
    }

    /// <summary>Where to draw - the WinForms PrintPageEventArgs.Graphics equivalent.</summary>
    public DrawingContext Context { get; }

    /// <summary>The whole page.</summary>
    public Rect PageBounds { get; }

    /// <summary>The page inside its margins, which is where WinForms laid content out.</summary>
    public Rect MarginBounds { get; }

    /// <summary>Set by the handler when it has another page to draw.</summary>
    public bool HasMorePages { get; set; }
}

/// <summary>
/// Fallback for WinForms' <c>PrintDocument</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This does not print, and cannot.</b> Avalonia has no printing API of any kind - measured
/// against its reference assemblies there is not one <c>Print*</c> type. What it does have is
/// <c>RenderTargetBitmap</c>, so what a <c>PrintDocument</c> becomes here is the half that *is*
/// expressible: the page is really drawn, by the handler the original wrote, and then written to
/// a file the user picks. Sending that page to a printer is the part you still have to add, and
/// which library does it is your choice.
/// </para>
/// <para>
/// The reason this is worth having at all is that <c>PrintPage</c> handlers are drawing code, and
/// drawing code translates: <c>e.Graphics.DrawString(...)</c> becomes a real
/// <c>DrawingContext</c> call. Without a surface to raise the event on, that translated body had
/// nowhere to run and was left as a comment.
/// </para>
/// <para>
/// A plain class rather than a control: a PrintDocument is not in the visual tree, and neither is
/// this. It renders off-screen, which is also why the page size is its own property rather than a
/// layout result.
/// </para>
/// </remarks>
public class PrintDocumentFallback
{
    /// <summary>Used as the suggested file name when the page is exported.</summary>
    public string DocumentName { get; set; } = "document";

    /// <summary>US Letter at 96 dpi, which is what WinForms' default page works out to.</summary>
    public Size PageSize { get; set; } = new(816, 1056);

    /// <summary>One inch on every side, WinForms' default.</summary>
    public Thickness Margins { get; set; } = new(96);

    /// <summary>Raised once per rendered page - the WinForms PrintPage equivalent.</summary>
    public event EventHandler<PrintPageSurfaceEventArgs>? PrintPage;

    /// <summary>Draws the first page onto a bitmap.</summary>
    /// <remarks>
    /// Only the first. WinForms looped while <c>HasMorePages</c> stayed true, and reproducing that
    /// would mean deciding how several pages become one file - a decision about your document, not
    /// about the conversion. The flag is still handed to the handler and can be read back.
    /// </remarks>
    public RenderTargetBitmap RenderFirstPage()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize((int)PageSize.Width, (int)PageSize.Height));

        using (var context = bitmap.CreateDrawingContext())
        {
            var page = new Rect(0, 0, PageSize.Width, PageSize.Height);

            // Paper is white; a RenderTargetBitmap starts transparent.
            context.FillRectangle(Brushes.White, page);

            PrintPage?.Invoke(this, new PrintPageSurfaceEventArgs(context, page, MarginBounds(page)));
        }

        return bitmap;
    }

    /// <summary>Renders the page and writes it to a file the user picks.</summary>
    public async Task PrintAsync(Visual owner)
    {
        if (TopLevel.GetTopLevel(owner)?.StorageProvider is not { } storage)
        {
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export page",
            SuggestedFileName = DocumentName + ".png",
            DefaultExtension = "png",
        });

        if (file is null)
        {
            return;
        }

        using var bitmap = RenderFirstPage();
        await using var stream = await file.OpenWriteAsync();

        // The options overload, not Save(stream, quality) - that one is [Obsolete], and a
        // generated project has to compile without warnings.
        bitmap.Save(stream, new PngBitmapEncoderOptions());
    }

    private Rect MarginBounds(Rect page) => new(
        page.X + Margins.Left,
        page.Y + Margins.Top,
        Math.Max(0, page.Width - Margins.Left - Margins.Right),
        Math.Max(0, page.Height - Margins.Top - Margins.Bottom));
}
