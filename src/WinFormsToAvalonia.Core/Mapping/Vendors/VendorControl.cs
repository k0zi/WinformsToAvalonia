using Microsoft.CodeAnalysis.CSharp;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Mapping.Vendors;

/// <summary>What a vendor control's collection holds, and so what the walker records for an entry in it.</summary>
public enum VendorCollectionKind
{
    /// <summary>Real children, exactly like <c>Controls.Add</c>: a tab control's pages, a menu item's items.</summary>
    Children,

    /// <summary>
    /// The entries of a list control: string literals become <see cref="ControlModel.LiteralItems"/>,
    /// and an item object (<c>radListDataItem1</c>) is linked for the normalizer, which reads its text.
    /// </summary>
    Items,

    /// <summary>
    /// Entries built inline - <c>new RadioGroupItem(null, "Small")</c> - whose caption is one of the
    /// constructor arguments (<see cref="VendorCollection.CaptionArgument"/>).
    /// </summary>
    ConstructedItems,

    /// <summary>
    /// References to items that live elsewhere: a DevExpress bar's <c>LinksPersistInfo</c>, a ribbon
    /// group's <c>ItemLinks</c>. What they become depends on where they are linked, which is the
    /// normalizer's decision.
    /// </summary>
    Links,

    /// <summary>A list that only says the object exists (<c>barManager1.Items</c>) - nothing to record.</summary>
    Registration,
}

/// <param name="Path">The collection below the owner, as written: <c>Properties.Items</c>, <c>TabPages</c>.</param>
/// <param name="CaptionArgument">For <see cref="VendorCollectionKind.ConstructedItems"/>: which constructor argument is the caption.</param>
public sealed record VendorCollection(string Path, VendorCollectionKind Kind, int CaptionArgument = 0);

/// <summary>
/// One vendor designer property and the WinForms property it is, with a conversion when the two
/// spell the value differently. A conversion that returns null declines: the property keeps its
/// vendor name and is reported among the untranslated ones.
/// </summary>
public sealed record VendorProperty(string From, string To, Func<PropertyValue, PropertyValue?>? Convert = null);

/// <summary>
/// One built-in vendor control: what it is converted as, and how its designer vocabulary is
/// renamed into that control's. Every name here is checked against the vendor's real assemblies by
/// <c>WinFormsToAvalonia.Vendors.Tests</c>.
/// </summary>
public sealed record VendorControl
{
    private static readonly IReadOnlyDictionary<string, string> NoNames = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>The type's full name, the key a designer's <c>new DevExpress.XtraEditors.TextEdit()</c> is matched on.</summary>
    public required string FullName { get; init; }

    /// <summary>"DevExpress" or "Telerik" - what the report calls the support it was converted by.</summary>
    public required string Vendor { get; init; }

    /// <summary>
    /// The WinForms control it is converted as - one the registry maps. Null for plumbing the
    /// normalizer removes (a GridView, a BarManager) and for items whose control depends on where
    /// they are placed (a BarButtonItem is a menu item in a menu and a button in a toolbar).
    /// </summary>
    public string? As { get; init; }

    /// <summary>
    /// For an item whose control depends on where it is placed (<see cref="As"/> is null): the WinForms
    /// control it becomes in a menu, a toolbar and a status bar, keyed by those three names. What
    /// <c>VendorDesignerNormalizer</c> builds a bar's items from - and every one of them is checked to
    /// carry this entry's renamed properties, events and members.
    /// </summary>
    public IReadOnlyDictionary<string, string> PlacedAs { get; init; } = NoNames;

    public IReadOnlyList<VendorProperty> Properties { get; init; } = [];

    /// <summary>Vendor event → the WinForms event of <see cref="As"/>.</summary>
    public IReadOnlyDictionary<string, string> Events { get; init; } = NoNames;

    /// <summary>
    /// Vendor member → WinForms member a handler body may use, each proved to have the same type
    /// on both sides. Nothing else on the control is translatable from code.
    /// </summary>
    public IReadOnlyDictionary<string, string> Members { get; init; } = NoNames;

    /// <summary>
    /// Designer properties the vendor control implies without the designer writing them - a
    /// <c>MemoEdit</c> is a multi-line box. C# source, evaluated like a designer value; only added
    /// when the key is not already set.
    /// </summary>
    public IReadOnlyList<(string Key, string Expression)> Synthesized { get; init; } = [];

    public IReadOnlyList<VendorCollection> Collections { get; init; } = [];

    /// <summary>
    /// Designer keys that carry nothing in Avalonia - skins, theme names, appearance switches.
    /// An entry ending in '.' covers every key below it.
    /// </summary>
    public IReadOnlyList<string> Ignored { get; init; } = [];

    /// <summary>
    /// The designer creates this as a local variable inside InitializeComponent (Telerik does, for
    /// grid columns and list items) rather than as a field.
    /// </summary>
    public bool AllowedAsLocal { get; init; }

    /// <summary>Which constructor argument, if any, is the item's text: <c>new RadTreeNode("Root")</c>.</summary>
    public int? ConstructorTextArgument { get; init; }

    public string SimpleName => FullName[(FullName.LastIndexOf('.') + 1)..];

    public string Namespace => FullName[..FullName.LastIndexOf('.')];

    public bool IsIgnored(string key) =>
        Ignored.Any(i => i.EndsWith('.') ? key.StartsWith(i, StringComparison.Ordinal) : string.Equals(i, key, StringComparison.Ordinal));

    public VendorCollection? CollectionAt(string path) =>
        Collections.FirstOrDefault(c => string.Equals(c.Path, path, StringComparison.Ordinal));

    /// <summary>
    /// Converts <paramref name="control"/> into <paramref name="target"/>: renamed properties and
    /// events, the implied ones, the ignorable ones dropped, and the members a body may use. The
    /// written name is kept for the report, as for the project's own subclasses.
    /// </summary>
    public void Retype(ControlModel control, string target)
    {
        foreach (var key in control.Properties.Keys.Where(IsIgnored).ToList())
        {
            control.Properties.Remove(key, out _);
        }

        foreach (var property in Properties)
        {
            if (!control.Properties.TryGetValue(property.From, out var value))
            {
                continue;
            }

            var converted = property.Convert is null ? value : property.Convert(value);
            if (converted is null)
            {
                continue;
            }

            control.Properties.Remove(property.From, out _);

            // A later spelling of the same thing never overrides an earlier one the designer wrote
            // under the WinForms name itself (`Text` beside `EditValue`).
            control.Properties.TryAdd(property.To, converted);
        }

        foreach (var (key, expression) in Synthesized)
        {
            if (!control.Properties.ContainsKey(key))
            {
                control.Properties[key] = ExpressionEvaluator.Evaluate(SyntaxFactory.ParseExpression(expression));
            }
        }

        for (var i = 0; i < control.Events.Count; i++)
        {
            if (Events.TryGetValue(control.Events[i].EventName, out var renamed))
            {
                control.Events[i] = control.Events[i] with { EventName = renamed };
            }
        }

        control.DeclaredClrTypeName ??= control.ClrTypeName;
        control.ClrTypeName = target;
        control.VendorTypeName = FullName;
        control.VendorMembers = Members;
    }
}

/// <summary>A vendor's own Form or UserControl base class, and what it is converted as.</summary>
/// <param name="Ignored">Form-level keys it adds that carry nothing (<c>LookAndFeel.SkinName</c>).</param>
public sealed record VendorBaseType(string FullName, string Vendor, string As, IReadOnlyList<string> Ignored)
{
    public string SimpleName => FullName[(FullName.LastIndexOf('.') + 1)..];

    public bool IsIgnored(string key) =>
        Ignored.Any(i => i.EndsWith('.') ? key.StartsWith(i, StringComparison.Ordinal) : string.Equals(i, key, StringComparison.Ordinal));
}
