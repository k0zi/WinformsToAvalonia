using System.Reflection;

namespace WinFormsToAvalonia.Vendors.Tests;

/// <summary>
/// DevExpress' and Telerik's WinForms assemblies, and WinForms' own, read as metadata in one
/// context - so a vendor type's members can be compared with the WinForms control it is converted as.
/// </summary>
/// <remarks>
/// The vendor catalog is a claim about three APIs this repository never compiles against, and every
/// wrong name in it fails silently: a property that is not there is simply never renamed. Reading
/// the real assemblies is what turns those claims into tests, as <c>Mapping.Tests</c> does for
/// Avalonia and WinForms.
/// </remarks>
public static class VendorMetadata
{
    private static readonly Lazy<Loaded> Context = new(Load, isThreadSafe: true);

    private sealed record Loaded(Dictionary<string, Type> VendorTypes, Assembly WindowsForms);

    public static Type? VendorType(string fullName) => Context.Value.VendorTypes.GetValueOrDefault(fullName);

    public static Type? WinFormsType(string simpleName) =>
        Context.Value.WindowsForms.GetExportedTypes().FirstOrDefault(t => t.Name == simpleName);

    /// <summary>
    /// A dotted property path (<c>Properties.ReadOnly</c>) - or an event as the last segment - on
    /// <paramref name="type"/>, with the type it ends at.
    /// </summary>
    public static bool TryResolvePath(Type type, string path, out Type resolved)
    {
        var current = type;
        resolved = type;
        foreach (var segment in path.Split('.'))
        {
            var owner = current;
            var property = owner.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name == segment)
                .OrderBy(p => p.DeclaringType == owner ? 0 : 1)
                .FirstOrDefault();
            if (property is not null)
            {
                current = property.PropertyType;
                continue;
            }

            var @event = owner.GetEvents(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(e => e.Name == segment);
            if (@event is not null)
            {
                current = @event.EventHandlerType!;
                continue;
            }

            return false;
        }

        resolved = current;
        return true;
    }

    public static bool HasEvent(Type type, string name) =>
        type.GetEvents(BindingFlags.Public | BindingFlags.Instance).Any(e => e.Name == name);

    /// <summary>A public instance property of that name, or a parameterless method - what a handler body can name.</summary>
    public static Type? MemberType(Type type, string name)
    {
        var property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name == name)
            .OrderBy(p => p.DeclaringType == type ? 0 : 1)
            .FirstOrDefault();
        if (property is not null)
        {
            return property.PropertyType;
        }

        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == 0);
        return method?.ReturnType;
    }

    public static bool DerivesFrom(Type type, string fullName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.FullName == fullName)
            {
                return true;
            }
        }

        return false;
    }

    private static Loaded Load()
    {
        var packageRoot = typeof(VendorMetadata).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "NuGetPackageRoot")?.Value;
        Assert.False(string.IsNullOrEmpty(packageRoot), "The NuGet package root was not baked into this assembly - check the csproj.");

        var vendorDirectories = Directory.GetDirectories(packageRoot!)
            .Where(d => Path.GetFileName(d).StartsWith("devexpress.", StringComparison.Ordinal)
                || Path.GetFileName(d).StartsWith("telerik.", StringComparison.Ordinal));

        // net8.0-windows first: a package that ships both keeps its Windows build, which is the one
        // a WinForms project referenced.
        var vendorAssemblies = vendorDirectories
            .SelectMany(d => Directory.GetFiles(d, "*.dll", SearchOption.AllDirectories))
            .Select(f => f.Replace('\\', '/'))
            .Where(f => (f.Contains("/lib/net8.0-windows") || f.Contains("/lib/net8.0/") || f.Contains("/lib/netstandard2.0/"))
                && !f.Contains("/Design") && !f.EndsWith(".resources.dll", StringComparison.Ordinal))
            .OrderBy(f => f.Contains("/lib/net8.0-windows") ? 0 : 1)
            .ToList();
        Assert.NotEmpty(vendorAssemblies);

        var desktopRef = Path.Combine(AppContext.BaseDirectory, "WindowsDesktopRef");
        var windowsForms = Path.Combine(desktopRef, "System.Windows.Forms.dll");
        Assert.True(File.Exists(windowsForms), "The Windows Desktop reference pack was not copied beside the test assembly.");

        var candidates = vendorAssemblies
            .Concat(Directory.GetFiles(desktopRef, "*.dll"))
            .Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var mlc = new MetadataLoadContext(new PathAssemblyResolver(candidates), "System.Runtime");
        var types = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var assembly in vendorAssemblies.GroupBy(Path.GetFileName).Select(g => g.First()))
        {
            try
            {
                foreach (var type in mlc.LoadFromAssemblyPath(assembly).GetExportedTypes())
                {
                    types.TryAdd(type.FullName!, type);
                }
            }
            catch (FileLoadException)
            {
                // Telerik.Licensing.Runtime is a mixed assembly of its own; nothing in the catalog lives there.
            }
        }

        return new Loaded(types, mlc.LoadFromAssemblyPath(windowsForms));
    }
}
