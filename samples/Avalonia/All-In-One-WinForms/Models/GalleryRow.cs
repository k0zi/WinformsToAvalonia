using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace All_In_One_WinForms.Models;

/// <summary>
/// One row of the Data tab's grid - the migrated form of the <c>GalleryRow</c> class that used
/// to be a private nested type of <c>MainForm</c> and was fed to the grid through a
/// <c>BindingSource</c>. Avalonia has no BindingSource: the DataGrid binds straight to an
/// <c>ObservableCollection&lt;GalleryRow&gt;</c> on the ViewModel, so the row itself has to be
/// observable for the cell editors to round-trip.
/// </summary>
public sealed partial class GalleryRow : ObservableObject
{
    /// <summary>Choices of the "Category" column - the WinForms DataGridViewComboBoxColumn items.</summary>
    public static IReadOnlyList<string> Categories { get; } = ["Alpha", "Beta", "Gamma"];

    private static readonly DrawingImage ActiveIcon = CreateDot(Brushes.SeaGreen);

    private static readonly DrawingImage InactiveIcon = CreateDot(Brushes.Silver);

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Active { get; set; }

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty;

    /// <summary>Text of the link column cell (WinForms DataGridViewLinkColumn).</summary>
    [ObservableProperty]
    public partial string Details { get; set; } = "Details";

    /// <summary>Image column cell (WinForms DataGridViewImageColumn).</summary>
    public IImage Icon => Active ? ActiveIcon : InactiveIcon;

    /// <summary>Button column cell (WinForms DataGridViewButtonColumn) - it had no handler in
    /// the original, so it just stamps the row to show the binding works.</summary>
    [RelayCommand]
    private void Run() =>
        Details = $"Ran at {DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture)}";

    partial void OnActiveChanged(bool value) => OnPropertyChanged(nameof(Icon));

    private static DrawingImage CreateDot(IBrush brush) => new()
    {
        Drawing = new GeometryDrawing
        {
            Brush = brush,
            Geometry = new EllipseGeometry(new Rect(0, 0, 16, 16)),
        },
    };
}
