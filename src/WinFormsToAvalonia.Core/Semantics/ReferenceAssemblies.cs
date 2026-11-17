using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace WinFormsToAvalonia.Core.Semantics;

/// <summary>
/// The .NET base class library the generated project compiles against, as Roslyn metadata
/// references - and, deliberately, nothing else.
/// </summary>
/// <remarks>
/// <para>
/// WinForms is <em>not</em> here, and that absence is the whole mechanism. Every WinForms type a
/// source file names binds to an error type, so "does this code touch WinForms?" becomes "does
/// anything in it fail to bind?" - a question the compiler answers exactly, rather than one a list
/// of names answers approximately. See <see cref="UiNeutrality"/>.
/// </para>
/// <para>
/// The <em>reference pack</em> is preferred over the runtime's implementation assemblies: it is what
/// <c>dotnet build</c> of the generated <c>net10.0</c> project really compiles against, and it carries
/// the <c>[SupportedOSPlatform]</c> annotations the platform analyser reads (a Windows-only API that
/// came across would be a CA1416 warning in a project that has to build warning-free). The runtime
/// directory is the fallback when no SDK is installed beside the runtime - binding still works, it
/// is just answered for whatever runtime this tool happens to run on.
/// </para>
/// </remarks>
internal static class ReferenceAssemblies
{
    private static readonly Lazy<IReadOnlyList<MetadataReference>> Loaded = new(Load, isThreadSafe: true);

    /// <summary>The target framework the generated project declares - see <c>AvaloniaProjectScaffolder</c>.</summary>
    internal const string TargetFramework = "net10.0";

    public static IReadOnlyList<MetadataReference> All => Loaded.Value;

    /// <summary>
    /// The simple names of the assemblies the framework itself provides. A package shipping one of
    /// them (<c>Microsoft.CSharp</c>, <c>System.ValueTuple</c>, ...) is a polyfill .NET 10 no longer needs.
    /// </summary>
    public static IReadOnlySet<string> AssemblyNames => LoadedNames.Value;

    private static readonly Lazy<IReadOnlySet<string>> LoadedNames = new(
        () => All.OfType<PortableExecutableReference>()
            .Select(r => Path.GetFileNameWithoutExtension(r.FilePath!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase),
        isThreadSafe: true);

    private static IReadOnlyList<MetadataReference> Load()
    {
        var directory = FindReferencePack() ?? RuntimeEnvironment.GetRuntimeDirectory();
        var references = new List<MetadataReference>();

        foreach (var path in Directory.EnumerateFiles(directory, "*.dll").Order(StringComparer.Ordinal))
        {
            if (!HasMetadata(path))
            {
                continue;
            }

            references.Add(MetadataReference.CreateFromFile(path));
        }

        return references;
    }

    /// <summary>
    /// <c>{dotnet root}/packs/Microsoft.NETCore.App.Ref/{10.x}/ref/net10.0</c>, the highest 10.x
    /// installed. The dotnet root is found from the running runtime's own directory,
    /// <c>{dotnet root}/shared/Microsoft.NETCore.App/{version}/</c>.
    /// </summary>
    private static string? FindReferencePack()
    {
        var runtimeDirectory = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory().TrimEnd(Path.DirectorySeparatorChar));
        var dotnetRoot = runtimeDirectory.Parent?.Parent?.Parent;
        var packs = dotnetRoot is null ? null : Path.Combine(dotnetRoot.FullName, "packs", "Microsoft.NETCore.App.Ref");

        if (packs is null || !Directory.Exists(packs))
        {
            return null;
        }

        return Directory.EnumerateDirectories(packs)
            .Select(d => (Directory: d, Version: Version.TryParse(Path.GetFileName(d).Split('-')[0], out var v) ? v : null))
            .Where(d => d.Version is { Major: 10 })
            .OrderByDescending(d => d.Version)
            .Select(d => Path.Combine(d.Directory, "ref", TargetFramework))
            .FirstOrDefault(Directory.Exists);
    }

    private static bool HasMetadata(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new PEReader(stream);
            return reader.HasMetadata;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
