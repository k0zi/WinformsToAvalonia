using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Semantics;

/// <summary>A package whose assemblies a <c>net10.0</c> project can compile against, with no UI in them.</summary>
/// <param name="CompileAssemblies">The assemblies NuGet would hand the compiler for <c>net10.0</c>.</param>
internal sealed record ResolvedPackage(string Id, string Version, IReadOnlyList<string> CompileAssemblies);

/// <summary>A package the project declares that the generated project does not reference, and why.</summary>
public sealed record NotCarriedPackage(string Id, string Version, string Reason)
{
    public string Message =>
        $"The NuGet package '{Id}' ({Version}) was not carried over: {Reason}. Code that uses it stays un-migrated.";
}

/// <summary>
/// Turns the project's declared packages into the ones that may come across into the generated
/// project, following dependencies the way a restore would - and refusing every package a
/// <c>net10.0</c> Avalonia project could not use as it stands.
/// </summary>
/// <remarks>
/// <para>A package is refused, and the reason reported, when it:</para>
/// <list type="bullet">
///   <item>has a library only for .NET Framework (or for <c>-windows</c>): restoring it into
///     <c>net10.0</c> is an NU1701 warning at best, and a project that has to build warning-free;</item>
///   <item>references WinForms or WPF - a control suite is converted through <c>--mappings</c>, not
///     by referencing it;</item>
///   <item>is platform-specific (<c>[assembly: SupportedOSPlatform("windows")]</c>), a CA1416 at every call.</item>
/// </list>
/// <para>
/// A package whose assembly the framework already ships (<c>Microsoft.CSharp</c>,
/// <c>System.Data.DataSetExtensions</c>) is dropped silently: it is a polyfill .NET 10 does not need,
/// and referencing it is an NU1510 warning. A meta-package (no library, only dependencies, like
/// <c>Microsoft.Windows.Compatibility</c>) is replaced by its dependencies, each decided on its own.
/// </para>
/// <para>
/// Only declared packages are ever reported: a dependency that fails is simply not available, and
/// code that used it stays un-migrated, as it would have anyway.
/// </para>
/// </remarks>
internal static partial class NuGetPackageResolver
{
    private const int MaxDependencyDepth = 4;

    private static readonly string[] UiAssemblyPrefixes =
        ["System.Windows.Forms", "PresentationFramework", "PresentationCore", "WindowsBase", "System.Xaml"];

    public static (IReadOnlyList<ResolvedPackage> Packages, IReadOnlyList<NotCarriedPackage> Refused) Resolve(
        IEnumerable<(NuGetPackageReference Package, string ProjectDirectory)> declared,
        NuGetPackageStore store,
        IReadOnlySet<string> frameworkAssemblyNames)
    {
        var resolved = new List<ResolvedPackage>();
        var warnings = new List<NotCarriedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<(string Id, string Version, string ProjectDirectory, bool Declared, int Depth)>();

        foreach (var (package, directory) in declared)
        {
            if (!package.IsDevelopmentOnly)
            {
                pending.Enqueue((package.Id, package.Version, directory, true, 0));
            }
        }

        while (pending.TryDequeue(out var item))
        {
            if (!seen.Add(item.Id) || frameworkAssemblyNames.Contains(item.Id))
            {
                continue;
            }

            void Refuse(string reason)
            {
                if (item.Declared)
                {
                    warnings.Add(new NotCarriedPackage(item.Id, item.Version, reason));
                }
            }

            if (NuGetPackageStore.NormalizeVersion(item.Version) is null)
            {
                Refuse("only an exact version can be looked up, and this one is a range or floating");
                continue;
            }

            // A WinForms control suite is refused on sight, before the store is asked: every one of
            // them references WinForms, so the check below would refuse it anyway - after
            // downloading hundreds of megabytes from nuget.org, where both of these now live. Their
            // controls are converted by the built-in vendor support instead.
            if (IsWinFormsControlSuite(item.Id))
            {
                Refuse("it is a WinForms control suite - its controls are converted by the built-in vendor support, and none of it can run in the generated project");
                continue;
            }

            if (store.Locate(item.Id, item.Version, item.ProjectDirectory) is not { } folder)
            {
                Refuse("it is not in the local NuGet cache and could not be downloaded (run a restore of the original project, or convert without --offline)");
                continue;
            }

            var assets = SelectCompileAssets(folder);
            if (assets.IncompatibleFolders is { Count: > 0 } incompatible)
            {
                Refuse($"it has no library for .NET 10, only for {string.Join(", ", incompatible)}");
                continue;
            }

            var assemblies = new List<string>();
            string? problem = null;
            var inBox = false;
            foreach (var path in assets.Assemblies)
            {
                switch (Inspect(path, frameworkAssemblyNames, out var uiAssembly))
                {
                    case AssemblyVerdict.Usable:
                        assemblies.Add(path);
                        break;
                    case AssemblyVerdict.InBox:
                        inBox = true;
                        break;
                    case AssemblyVerdict.NoMetadata:
                        break;
                    case AssemblyVerdict.UsesUi:
                        problem = $"its {Path.GetFileName(path)} is built on {uiAssembly}";
                        break;
                    case AssemblyVerdict.PlatformSpecific:
                        problem = $"its {Path.GetFileName(path)} is platform-specific ([assembly: SupportedOSPlatform])";
                        break;
                }

                if (problem is not null)
                {
                    break;
                }
            }

            if (problem is not null)
            {
                Refuse(problem);
                continue;
            }

            if (inBox)
            {
                continue;
            }

            if (assemblies.Count > 0)
            {
                resolved.Add(new ResolvedPackage(item.Id, item.Version, assemblies));
            }

            if (item.Depth < MaxDependencyDepth)
            {
                foreach (var (id, version) in ReadDependencies(folder))
                {
                    pending.Enqueue((id, version, item.ProjectDirectory, false, item.Depth + 1));
                }
            }
        }

        return (resolved, warnings);
    }

    /// <summary>
    /// The assemblies of the best framework folder for <c>net10.0</c> - <c>ref/</c> if the package has
    /// one, as NuGet prefers it for compiling - or the folders that exist when none fits.
    /// </summary>
    /// <summary>
    /// DevExpress' and Telerik's WinForms packages. Deliberately not every <c>DevExpress.*</c>: XPO and
    /// DevExpress.Data are UI-neutral, and code calling into them can come across.
    /// </summary>
    internal static bool IsWinFormsControlSuite(string packageId) =>
        packageId.StartsWith("DevExpress.Win", StringComparison.OrdinalIgnoreCase)
        || packageId.StartsWith("DevExpress.Utils", StringComparison.OrdinalIgnoreCase)
        || packageId.StartsWith("Telerik.UI.for.WinForms", StringComparison.OrdinalIgnoreCase)
        || packageId.StartsWith("UI.for.WinForms", StringComparison.OrdinalIgnoreCase);

    internal static (IReadOnlyList<string> Assemblies, IReadOnlyList<string>? IncompatibleFolders) SelectCompileAssets(string packageFolder)
    {
        foreach (var kind in new[] { "ref", "lib" })
        {
            var root = Path.Combine(packageFolder, kind);
            if (!Directory.Exists(root))
            {
                continue;
            }

            var folders = Directory.EnumerateDirectories(root).Select(Path.GetFileName).OfType<string>().ToList();
            var looseDlls = Directory.EnumerateFiles(root, "*.dll").ToList();
            if (folders.Count == 0 && looseDlls.Count == 0)
            {
                continue;
            }

            // A lib/ with dlls straight in it is the old "any framework" layout - .NET Framework, in practice.
            var best = folders
                .Select(f => (Folder: f, Rank: FrameworkRank(f)))
                .Where(f => f.Rank > 0)
                .OrderByDescending(f => f.Rank)
                .FirstOrDefault();

            if (best.Folder is null)
            {
                return ([], folders.Count > 0 ? folders.Order(StringComparer.Ordinal).ToList() : ["(no framework folder)"]);
            }

            var assemblies = Directory.EnumerateFiles(Path.Combine(root, best.Folder), "*.dll")
                .Order(StringComparer.Ordinal)
                .ToList();
            return (assemblies, null);
        }

        return ([], null);
    }

    /// <summary>
    /// How well a framework folder suits <c>net10.0</c>, the higher the better; 0 when it does not at
    /// all. Same order as NuGet's: .NET 5-10, then .NET Core, then .NET Standard.
    /// </summary>
    internal static int FrameworkRank(string framework)
    {
        var name = framework.ToLowerInvariant();
        var match = FrameworkName().Match(name);
        if (!match.Success)
        {
            return 0;
        }

        var major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
        var minor = match.Groups["minor"].Success ? int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture) : 0;
        return match.Groups["family"].Value switch
        {
            "net" when major is >= 5 and <= 10 => 3000 + (major * 10) + minor,
            "netcoreapp" => 2000 + (major * 10) + minor,
            "netstandard" when major * 10 + minor <= 21 => 1000 + (major * 10) + minor,
            _ => 0,
        };
    }

    /// <summary>The dependencies of the nuspec group that best fits <c>net10.0</c>, at their lowest allowed version.</summary>
    private static IEnumerable<(string Id, string Version)> ReadDependencies(string packageFolder)
    {
        var nuspec = Directory.EnumerateFiles(packageFolder, "*.nuspec").FirstOrDefault();
        if (nuspec is null)
        {
            return [];
        }

        XElement? dependencies;
        try
        {
            dependencies = XDocument.Load(nuspec).Descendants().FirstOrDefault(e => e.Name.LocalName == "dependencies");
        }
        catch (System.Xml.XmlException)
        {
            return [];
        }

        if (dependencies is null)
        {
            return [];
        }

        var groups = dependencies.Elements().Where(e => e.Name.LocalName == "group").ToList();
        IEnumerable<XElement> chosen;
        if (groups.Count == 0)
        {
            chosen = dependencies.Elements().Where(e => e.Name.LocalName == "dependency");
        }
        else
        {
            var best = groups
                .Select(g => (Group: g, Rank: g.Attribute("targetFramework")?.Value is { Length: > 0 } tfm ? FrameworkRank(ShortFrameworkName(tfm)) : 1))
                .Where(g => g.Rank > 0)
                .OrderByDescending(g => g.Rank)
                .FirstOrDefault();
            chosen = best.Group?.Elements().Where(e => e.Name.LocalName == "dependency") ?? [];
        }

        return chosen
            .Select(d => (Id: d.Attribute("id")?.Value, Version: LowestVersion(d.Attribute("version")?.Value)))
            .Where(d => d.Id is { Length: > 0 } && d.Version is not null)
            .Select(d => (d.Id!, d.Version!))
            .ToList();
    }

    /// <summary><c>.NETStandard2.0</c> → <c>netstandard2.0</c>, <c>.NETCoreApp3.1</c> → <c>netcoreapp3.1</c>.</summary>
    private static string ShortFrameworkName(string framework) =>
        framework.Trim() switch
        {
            var f when f.StartsWith(".NETStandard", StringComparison.OrdinalIgnoreCase) => "netstandard" + f[".NETStandard".Length..],
            var f when f.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase) => "netcoreapp" + f[".NETCoreApp".Length..],
            var f when f.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase) => "netframework",
            var f => f,
        };

    /// <summary>A dependency's version range at its lower bound: <c>[4.5.0, )</c> and <c>4.5.0</c> are both 4.5.0.</summary>
    private static string? LowestVersion(string? range)
    {
        if (string.IsNullOrWhiteSpace(range))
        {
            return null;
        }

        var lower = range.Trim().TrimStart('[', '(').Split(',')[0].TrimEnd(']', ')').Trim();
        return NuGetPackageStore.NormalizeVersion(lower) is not null ? lower : null;
    }

    private enum AssemblyVerdict
    {
        Usable,
        InBox,
        NoMetadata,
        UsesUi,
        PlatformSpecific,
    }

    private static AssemblyVerdict Inspect(string path, IReadOnlySet<string> frameworkAssemblyNames, out string? uiAssembly)
    {
        uiAssembly = null;
        try
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
            {
                return AssemblyVerdict.NoMetadata;
            }

            var reader = pe.GetMetadataReader();
            if (!reader.IsAssembly)
            {
                return AssemblyVerdict.NoMetadata;
            }

            var name = reader.GetString(reader.GetAssemblyDefinition().Name);
            if (frameworkAssemblyNames.Contains(name))
            {
                return AssemblyVerdict.InBox;
            }

            foreach (var handle in reader.AssemblyReferences)
            {
                var referenced = reader.GetString(reader.GetAssemblyReference(handle).Name);
                if (UiAssemblyPrefixes.Any(ui => referenced == ui || referenced.StartsWith(ui + ".", StringComparison.Ordinal)))
                {
                    uiAssembly = referenced;
                    return AssemblyVerdict.UsesUi;
                }
            }

            foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
            {
                if (AttributeTypeName(reader, reader.GetCustomAttribute(handle)) == "SupportedOSPlatformAttribute")
                {
                    return AssemblyVerdict.PlatformSpecific;
                }
            }

            return AssemblyVerdict.Usable;
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            return AssemblyVerdict.NoMetadata;
        }
    }

    private static string? AttributeTypeName(MetadataReader reader, CustomAttribute attribute)
    {
        EntityHandle type = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => default,
        };

        return type.Kind switch
        {
            HandleKind.TypeReference => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)type).Name),
            HandleKind.TypeDefinition => reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)type).Name),
            _ => null,
        };
    }

    [GeneratedRegex(@"^(?<family>netstandard|netcoreapp|net)(?<major>\d+)(\.(?<minor>\d+))?$")]
    private static partial Regex FrameworkName();
}
