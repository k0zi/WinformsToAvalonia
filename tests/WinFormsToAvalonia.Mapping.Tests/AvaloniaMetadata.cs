using System.Reflection;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Avalonia's own API, read as metadata, so the converter's hand-maintained mapping tables can be
/// checked against it.
/// </summary>
/// <remarks>
/// <para>
/// This exists because of a gap the repo names in its own guidance: the converter emits *text*
/// and never references Avalonia, so a table entry naming a property that does not exist - or
/// giving it the wrong type - compiles perfectly here and fails in the **generated** project.
/// Three such entries were found by hand before this was written; two of them produced code that
/// did not compile at all.
/// </para>
/// <para>
/// <see cref="MetadataLoadContext"/> rather than ordinary reflection: nothing here is executed,
/// no Avalonia type is ever instantiated, and the assemblies loaded are the *reference*
/// assemblies the generated projects compile against - which is the API surface that actually
/// matters. It also means this suite can be told to read a different Avalonia version without
/// changing what the test process itself runs on.
/// </para>
/// </remarks>
public static class AvaloniaMetadata
{
    private static readonly Lazy<Loaded> Context = new(Load, isThreadSafe: true);

    private sealed record Loaded(MetadataLoadContext Mlc, IReadOnlyList<Assembly> Assemblies);

    /// <summary>
    /// Resolves an Avalonia element name as the mappers spell it (<c>"Button"</c>,
    /// <c>"DataGridTextColumn"</c>) to its type.
    /// </summary>
    /// <remarks>
    /// By simple name, because that is all a mapper records - it emits an XAML element, and
    /// Avalonia's own XAML namespace resolves those by simple name too. An ambiguous name would
    /// be a problem for the emitted XAML long before it were one for this test.
    /// </remarks>
    public static Type? FindElement(string avaloniaElementName)
    {
        foreach (var assembly in Context.Value.Assemblies)
        {
            var match = assembly.GetExportedTypes()
                .FirstOrDefault(t => string.Equals(t.Name, avaloniaElementName, StringComparison.Ordinal));

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>The property of that name on the type or any of its bases, or null.</summary>
    public static PropertyInfo? FindProperty(Type type, string propertyName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var property = current.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            if (property is not null)
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>The event of that name on the type or any of its bases, or null.</summary>
    public static EventInfo? FindEvent(Type type, string eventName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var declared = current.GetEvent(
                eventName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (declared is not null)
            {
                return declared;
            }
        }

        return null;
    }

    /// <summary>
    /// The static field of that name on the type or any of its bases, or null. Attached events
    /// and properties are declared as <c>RoutedEvent</c>/<c>AvaloniaProperty</c> fields, so this
    /// is how XAML reaches them.
    /// </summary>
    public static FieldInfo? FindField(Type type, string fieldName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var declared = current.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (declared is not null)
            {
                return declared;
            }
        }

        return null;
    }

    /// <summary>
    /// A method of that name on the type or any of its bases that can be <em>called</em> with
    /// <paramref name="argumentCount"/> arguments, or null.
    /// </summary>
    /// <remarks>
    /// Callable, not "declares exactly this many parameters": Avalonia's <c>Focus()</c> takes two
    /// optional ones, and the emitted <c>control.Focus();</c> compiles perfectly against it.
    /// </remarks>
    public static MethodInfo? FindMethod(Type type, string methodName, int argumentCount)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var declared = current
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m =>
                    string.Equals(m.Name, methodName, StringComparison.Ordinal)
                    && IsCallableWith(m, argumentCount));

            if (declared is not null)
            {
                return declared;
            }
        }

        return null;
    }

    /// <summary>
    /// A type as this repo's tables spell it: <c>"bool"</c>, <c>"string"</c>, <c>"bool?"</c>,
    /// <c>"object"</c>, <c>"DateTime?"</c>.
    /// </summary>
    /// <remarks>
    /// Comparing spellings rather than <see cref="Type"/> instances is deliberate - the tables
    /// hold text that ends up in generated C#, so the check has to be against what that text
    /// means, and a mismatch has to be readable in the failure message.
    /// </remarks>
    public static string SpellType(Type type)
    {
        // Not Nullable.GetUnderlyingType: that compares against the *runtime* Nullable<>, and a
        // type read through MetadataLoadContext belongs to a different type universe, so it never
        // matches. The open generic's name is the thing both universes agree on.
        if (UnderlyingOfNullable(type) is { } underlying)
        {
            return SpellType(underlying) + "?";
        }

        return type.FullName switch
        {
            "System.Boolean" => "bool",
            "System.String" => "string",
            "System.Object" => "object",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.Double" => "double",
            "System.Single" => "float",
            "System.Decimal" => "decimal",
            "System.Char" => "char",
            _ => type.Name,
        };
    }

    private static bool IsCallableWith(MethodInfo method, int argumentCount)
    {
        var parameters = method.GetParameters();
        var required = parameters.Count(p => !p.IsOptional);
        return argumentCount >= required && argumentCount <= parameters.Length;
    }

    /// <summary>True for <c>T?</c> where T is a value type, in either type universe.</summary>
    public static bool IsNullableValueType(Type type) => UnderlyingOfNullable(type) is not null;

    /// <summary>True for an enum, seen through <c>T?</c> as well.</summary>
    public static bool IsEnumOrNullableEnum(Type type) =>
        type.IsEnum || (UnderlyingOfNullable(type)?.IsEnum ?? false);

    private static Type? UnderlyingOfNullable(Type type) =>
        type.IsGenericType
        && !type.IsGenericTypeDefinition
        && type.GetGenericTypeDefinition().FullName == "System.Nullable`1"
            ? type.GetGenericArguments()[0]
            : null;

    /// <summary>Every Avalonia assembly the generated projects can see, for diagnostics.</summary>
    public static IReadOnlyList<string> LoadedAssemblyNames =>
        [.. Context.Value.Assemblies.Select(a => a.GetName().Name!).Order(StringComparer.Ordinal)];

    /// <summary>
    /// Loads the reference assemblies this test project itself references - so the Avalonia being
    /// checked is decided by one <c>PackageReference</c> list, not by a path guessed at runtime.
    /// </summary>
    private static Loaded Load()
    {
        // Everything the test project resolved, plus the runtime's own assemblies so the resolver
        // can follow Avalonia's references into System.*.
        var candidates = Directory
            .GetFiles(AppContext.BaseDirectory, "*.dll")
            .Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var mlc = new MetadataLoadContext(new PathAssemblyResolver(candidates));

        var avalonia = candidates
            .Where(p => Path.GetFileName(p).StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase))
            .Select(p =>
            {
                try
                {
                    return mlc.LoadFromAssemblyPath(p);
                }
                catch (BadImageFormatException)
                {
                    return null;
                }
            })
            .OfType<Assembly>()
            .ToList();

        Assert.True(
            avalonia.Count > 0,
            $"No Avalonia assemblies found beside the test assembly ({AppContext.BaseDirectory}). "
            + "This suite is meaningless without them - check the PackageReferences.");

        return new Loaded(mlc, avalonia);
    }
}
