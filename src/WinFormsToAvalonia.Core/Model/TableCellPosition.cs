namespace WinFormsToAvalonia.Core.Model;

/// <summary>A <c>TableLayoutPanel</c> cell: what <c>GetColumn</c>/<c>GetRow</c> return for the control in it.</summary>
public readonly record struct TableCellPosition(int Column, int Row);
