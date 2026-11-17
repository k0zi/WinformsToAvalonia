namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One entry a vendor control's collection holds by reference rather than as a child:
/// <c>new LinkPersistInfo(this.barButtonItem1, true)</c> in <c>bar1.LinksPersistInfo</c>, or
/// <c>radListDataItem1</c> in <c>radDropDownList1.Items</c>.
/// </summary>
/// <param name="Owner">The model key of the control whose collection it is.</param>
/// <param name="Path">The collection, as written below the owner - <c>LinksPersistInfo</c>, <c>ItemLinks</c>.</param>
/// <param name="Item">The model key of the linked item.</param>
/// <param name="BeginGroup">The designer asked for a separator before it.</param>
public sealed record VendorLink(string Owner, string Path, string Item, bool BeginGroup);
