using System;
using System.Collections.Generic;
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
/// WinForms did: the document keeps raising <c>PrintPage</c> while it stays true.
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

    /// <summary>
    /// A cap on <see cref="RenderPages"/>, so a handler that never clears <c>HasMorePages</c> ends
    /// in a finished export instead of filling the memory. WinForms had none, and such a handler
    /// printed forever.
    /// </summary>
    public int MaxPages { get; set; } = 1000;

    /// <summary>Draws the first page onto a bitmap - whatever the handler says about the rest.</summary>
    public RenderTargetBitmap RenderFirstPage() => RenderPage(out _);

    /// <summary>
    /// Every page, as WinForms printed them: <c>PrintPage</c> is raised once per page, with fresh
    /// arguments, for as long as the handler sets <c>HasMorePages</c>.
    /// </summary>
    /// <remarks>
    /// The handler's own state is what pages through the content - a row counter on the Form,
    /// typically - so rendering twice (a preview, then an export) continues from wherever the first
    /// run left it, exactly as printing twice did in WinForms. Reset that state before each run, as
    /// the original's BeginPrint handler did.
    /// </remarks>
    public IReadOnlyList<RenderTargetBitmap> RenderPages()
    {
        var pages = new List<RenderTargetBitmap>();
        bool hasMorePages;
        do
        {
            pages.Add(RenderPage(out hasMorePages));
        }
        while (hasMorePages && pages.Count < MaxPages);

        return pages;
    }

    /// <summary>
    /// Renders every page and writes them to files the user picks: the chosen file is the first
    /// page, and the rest go beside it as <c>name-2.png</c>, <c>name-3.png</c>, ... - or, where the
    /// storage gives no access to the chosen file's folder (a browser, a sandbox), stacked top to
    /// bottom in the one file.
    /// </summary>
    public async Task PrintAsync(Visual owner)
    {
        if (TopLevel.GetTopLevel(owner)?.StorageProvider is not { } storage)
        {
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export pages",
            SuggestedFileName = DocumentName + ".png",
            DefaultExtension = "png",
        });

        if (file is null)
        {
            return;
        }

        var pages = RenderPages();
        try
        {
            var folder = pages.Count > 1 ? await file.GetParentAsync() : null;
            if (pages.Count > 1 && folder is null)
            {
                using var stacked = Stack(pages);
                await WriteAsync(file, stacked);
                return;
            }

            await WriteAsync(file, pages[0]);

            var baseName = System.IO.Path.GetFileNameWithoutExtension(file.Name);
            for (var index = 1; index < pages.Count; index++)
            {
                if (await folder!.CreateFileAsync($"{baseName}-{index + 1}.png") is { } pageFile)
                {
                    await WriteAsync(pageFile, pages[index]);
                }
            }
        }
        finally
        {
            foreach (var page in pages)
            {
                page.Dispose();
            }
        }
    }

    private RenderTargetBitmap RenderPage(out bool hasMorePages)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize((int)PageSize.Width, (int)PageSize.Height));
        var page = new Rect(0, 0, PageSize.Width, PageSize.Height);

        using (var context = bitmap.CreateDrawingContext())
        {
            // Paper is white; a RenderTargetBitmap starts transparent.
            context.FillRectangle(Brushes.White, page);

            var args = new PrintPageSurfaceEventArgs(context, page, MarginBounds(page));
            PrintPage?.Invoke(this, args);
            hasMorePages = args.HasMorePages;
        }

        return bitmap;
    }

    private static RenderTargetBitmap Stack(IReadOnlyList<RenderTargetBitmap> pages)
    {
        var width = pages[0].PixelSize.Width;
        var height = pages[0].PixelSize.Height;
        var sheet = new RenderTargetBitmap(new PixelSize(width, height * pages.Count));
        using var context = sheet.CreateDrawingContext();
        for (var index = 0; index < pages.Count; index++)
        {
            context.DrawImage(pages[index], new Rect(0, index * height, width, height));
        }

        return sheet;
    }

    private static async Task WriteAsync(IStorageFile file, RenderTargetBitmap bitmap)
    {
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
