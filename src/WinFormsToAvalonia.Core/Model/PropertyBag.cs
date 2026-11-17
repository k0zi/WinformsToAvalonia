using System.Diagnostics.CodeAnalysis;

namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A control's (or Form's) designer-set properties, remembering which of them anything ever
/// looked up.
/// </summary>
/// <remarks>
/// <para>
/// This is how a designer property nothing translates gets <em>reported</em> instead of vanishing.
/// No table could say which properties are carried: they are read by dozens of mappers, universal
/// passes and catalogs, and a new mapper would have to remember to declare its own. So the answer
/// is recorded where it happens - a lookup by name (<c>TryGetValue</c>, <c>ContainsKey</c>, the
/// indexer) marks the key - and what is left unmarked once an artifact has been emitted is what
/// nothing in the conversion so much as asked about.
/// </para>
/// <para>
/// Enumerating does <em>not</em> mark: the passes that sweep every property (resource resolution,
/// the MDI check) look at all of them without carrying any. A pass that sweeps and really does
/// account for each one - reproducing it or reporting it by name - says so with
/// <see cref="MarkRead"/>.
/// </para>
/// <para>
/// The interfaces are re-implemented on purpose. Helpers take these as
/// <c>IReadOnlyDictionary</c>, and an interface call would otherwise go straight to
/// <c>Dictionary</c>'s own methods, past the tracking. Re-declaring the interfaces maps them onto
/// the <c>new</c> members below instead.
/// </para>
/// <para>
/// "Looked up" is not quite "carried": a styling pass that reads <c>BackColor</c> and then finds the
/// target has no Background has looked. Those passes report their own refusals; this catches the
/// properties no pass exists for at all.
/// </para>
/// </remarks>
public sealed class PropertyBag :
    Dictionary<string, PropertyValue>,
    IDictionary<string, PropertyValue>,
    IReadOnlyDictionary<string, PropertyValue>
{
    private readonly HashSet<string> _read = new(StringComparer.Ordinal);

    public PropertyBag()
        : base(StringComparer.Ordinal)
    {
    }

    public new PropertyValue this[string key]
    {
        get
        {
            _read.Add(key);
            return base[key];
        }
        set => base[key] = value;
    }

    public new bool TryGetValue(string key, [MaybeNullWhen(false)] out PropertyValue value)
    {
        _read.Add(key);
        return base.TryGetValue(key, out value);
    }

    public new bool ContainsKey(string key)
    {
        _read.Add(key);
        return base.ContainsKey(key);
    }

    /// <summary>The value, without counting as a lookup - for the coverage report itself.</summary>
    public PropertyValue Peek(string key) => base[key];

    /// <summary>For a pass that accounts for a property it reached by enumerating.</summary>
    public void MarkRead(string key) => _read.Add(key);

    /// <summary>Forgets every lookup so far - what was read while <em>parsing</em> carries nothing.</summary>
    public void ResetReads() => _read.Clear();

    /// <summary>The properties set here that nothing has looked up, ordinal-sorted.</summary>
    public IReadOnlyList<string> UnreadKeys =>
        [.. Keys.Where(k => !_read.Contains(k)).Order(StringComparer.Ordinal)];
}
