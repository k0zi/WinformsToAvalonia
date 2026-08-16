namespace All_In_One_WinForms.Models;

/// <summary>
/// One row of the Lists tab's file grid. The WinForms original was a <c>ListViewItem</c> built
/// from a string array; a DataGrid (the control a details-mode ListView maps to) binds to a
/// model instead, one property per column.
/// </summary>
public sealed record FileEntry(string Name, string Size);
