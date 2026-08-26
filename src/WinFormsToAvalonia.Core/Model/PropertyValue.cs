namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A resolved (or unresolved) right-hand-side value from a designer.cs property assignment.
/// Phase 3 understands literals and Point/Size constructors; the full finite grammar
/// (Color, Font, enum members, Padding, ...) is added in Phase 4 - anything not yet
/// understood becomes <see cref="Unresolved"/> with the raw expression text preserved.
/// </summary>
public abstract record PropertyValue
{
    private PropertyValue()
    {
    }

    public sealed record Literal(object? Value) : PropertyValue;

    public sealed record PointValue(int X, int Y) : PropertyValue;

    public sealed record SizeValue(int Width, int Height) : PropertyValue;

    public sealed record PaddingValue(int Left, int Top, int Right, int Bottom) : PropertyValue;

    /// <summary>One or more enum member names, e.g. ["Fill"] for `DockStyle.Fill`, or
    /// ["Bottom", "Left"] for `AnchorStyles.Bottom | AnchorStyles.Left`. The enclosing enum
    /// type is intentionally not tracked - the property name it's assigned to (Dock,
    /// Anchor, TextAlign, ...) is what later stages use to know how to interpret it.</summary>
    public sealed record EnumMembers(IReadOnlyList<string> MemberNames) : PropertyValue
    {
        // Record-synthesized equality does not do sequence equality for collection-typed
        // properties (List<T>/array compare by reference) - override it explicitly so two
        // EnumMembers with the same member names in the same order compare equal.
        public bool Equals(EnumMembers? other) => other is not null && MemberNames.SequenceEqual(other.MemberNames);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var member in MemberNames)
            {
                hash.Add(member);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>Either a named color (`Color.Red` / `SystemColors.Control`, NamedColor set,
    /// ARGB components null) or an explicit `Color.FromArgb(...)` call (NamedColor null,
    /// ARGB components set).</summary>
    public sealed record ColorValue(string? NamedColor, byte? A, byte? R, byte? G, byte? B) : PropertyValue;

    public sealed record FontValue(string FamilyName, float SizeInPoints, IReadOnlyList<string> StyleFlags) : PropertyValue
    {
        public bool Equals(FontValue? other) => other is not null
            && FamilyName == other.FamilyName
            && SizeInPoints.Equals(other.SizeInPoints)
            && StyleFlags.SequenceEqual(other.StyleFlags);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(FamilyName);
            hash.Add(SizeInPoints);
            foreach (var flag in StyleFlags)
            {
                hash.Add(flag);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>A `this.otherField` reference to another designer field, e.g.
    /// `this.someControl.ContextMenuStrip = this.contextMenuStrip1;`'s RHS.</summary>
    public sealed record ControlReference(string FieldName) : PropertyValue;

    /// <summary>
    /// A value the designer stored in the .resx rather than in code - either
    /// `resources.GetObject("pictureBox1.Image")` on the right of an assignment, or a base64
    /// entry pulled in by `resources.ApplyResources(...)`. <see cref="ResourceKey"/> is the
    /// full resx entry name. ConversionPipeline resolves these into copied Assets/ files;
    /// one that survives to emission means the payload could not be decoded.
    /// </summary>
    public sealed record ResourceReference(string ResourceKey) : PropertyValue;

    public sealed record Unresolved(string RawExpression) : PropertyValue;
}
