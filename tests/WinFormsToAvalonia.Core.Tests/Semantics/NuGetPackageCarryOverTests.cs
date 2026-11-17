using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Semantics;
using WinFormsToAvalonia.Core.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Semantics;

/// <summary>
/// The original project's NuGet packages: which ones join the semantic compilation, and which come
/// across into the generated csproj. Every package here is built on the spot into a folder laid out
/// like NuGet's global packages folder, so nothing is downloaded.
/// </summary>
public class NuGetPackageCarryOverTests
{
    private const string MathSource = """
        namespace Acme.Math
        {
            public static class Calculator
            {
                public static int Twice(int value) => value * 2;
            }
        }
        """;

    private static string Csproj(string packageItems) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net9.0-windows</TargetFramework>
            <UseWindowsForms>true</UseWindowsForms>
          </PropertyGroup>
          <ItemGroup>
        {packageItems}
          </ItemGroup>
        </Project>
        """;

    private const string UsingFile = """
        namespace App.Services
        {
            public class Doubler
            {
                public int Run(int value) => Acme.Math.Calculator.Twice(value);
            }
        }
        """;

    private static SemanticProjectContext Build(TempProjectFixture fixture, string packageItems, string packagesRoot)
    {
        fixture.WriteFile("App/App.csproj", Csproj(packageItems));
        fixture.WriteFile("App/Services/Doubler.cs", UsingFile);
        var project = new WinFormsProjectLoader().Load(fixture.PathTo("App/App.csproj"));
        return SemanticProjectContext.Build(
            project, "Out", new HashSet<string>(StringComparer.Ordinal), NuGetPackageStore.FromFolders(packagesRoot));
    }

    [Fact]
    public void UsedPackage_JoinsTheCompilation_AndIsCarried_WithTheFileThatUsesIt()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "netstandard2.0", MathSource);

        var context = Build(fixture, """<PackageReference Include="Acme.Math" Version="1.2.0" />""", packages);

        Assert.Equal([new CarriedPackage("Acme.Math", "1.2.0")], context.CarriedPackages);
        Assert.Contains(context.CarriedFiles, f => f.RelativePath == "Services/Doubler.cs");
        Assert.Empty(context.Warnings);
        Assert.Empty(context.NotCarriedPackages);
    }

    [Fact]
    public void PackageNothingUses_IsNotCarried()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "netstandard2.0", MathSource);
        FakePackage.Write(packages, "Acme.Unused", "2.0.0", "net8.0", "namespace Acme.Unused { public class Nothing { } }");

        var context = Build(
            fixture,
            """
            <PackageReference Include="Acme.Math" Version="1.2.0" />
            <PackageReference Include="Acme.Unused" Version="2.0.0" />
            """,
            packages);

        Assert.Equal(["Acme.Math"], context.CarriedPackages.Select(p => p.Id));
    }

    [Fact]
    public void PackageWithOnlyAFrameworkLibrary_IsReported_AndItsUserStaysBehind()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "net45", MathSource);

        var context = Build(fixture, """<PackageReference Include="Acme.Math" Version="1.2.0" />""", packages);

        Assert.Empty(context.CarriedPackages);
        Assert.DoesNotContain(context.CarriedFiles, f => f.RelativePath == "Services/Doubler.cs");
        Assert.Contains(context.NotCarriedPackages.Select(p => p.Message), w => w.Contains("'Acme.Math'") && w.Contains("only for net45"));
    }

    [Fact]
    public void PackageBuiltOnWinForms_IsReported()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        var winForms = FakePackage.CompileAssembly(
            fixture.PathTo("stubs"), "System.Windows.Forms", "namespace System.Windows.Forms { public class Control { } }");
        FakePackage.Write(
            packages, "Acme.Math", "1.2.0", "net8.0",
            "namespace Acme.Math { public class Calculator : System.Windows.Forms.Control { public static int Twice(int v) => v * 2; } }",
            [winForms]);

        var context = Build(fixture, """<PackageReference Include="Acme.Math" Version="1.2.0" />""", packages);

        Assert.Empty(context.CarriedPackages);
        Assert.Contains(context.NotCarriedPackages.Select(p => p.Message), w => w.Contains("'Acme.Math'") && w.Contains("built on System.Windows.Forms"));
    }

    [Fact]
    public void MetaPackage_StandsForItsDependencies()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "netstandard2.0", MathSource);
        FakePackage.WriteMeta(packages, "Acme.All", "3.0.0", ("Acme.Math", "[1.2.0, )"));

        var context = Build(fixture, """<PackageReference Include="Acme.All" Version="3.0.0" />""", packages);

        Assert.Equal([new CarriedPackage("Acme.Math", "1.2.0")], context.CarriedPackages);
        Assert.Empty(context.Warnings);
        Assert.Empty(context.NotCarriedPackages);
    }

    [Fact]
    public void FrameworkPolyfillsAndDevelopmentOnlyPackages_AreDroppedSilently()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "netstandard2.0", MathSource);

        var context = Build(
            fixture,
            """
            <PackageReference Include="Acme.Math" Version="1.2.0" />
            <PackageReference Include="Microsoft.CSharp" Version="4.7.0" />
            <PackageReference Include="Some.Analyzers" Version="1.0.0" PrivateAssets="all" />
            """,
            packages);

        Assert.Equal(["Acme.Math"], context.CarriedPackages.Select(p => p.Id));
        Assert.Empty(context.Warnings);
        Assert.Empty(context.NotCarriedPackages);
    }

    [Theory]
    [InlineData("1.*", "a range or floating")]
    [InlineData("[1.0,2.0)", "a range or floating")]
    [InlineData("9.9.9", "not in the local NuGet cache")]
    public void UnresolvablePackage_IsReported(string version, string reason)
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "netstandard2.0", MathSource);

        var context = Build(fixture, $"""<PackageReference Include="Acme.Math" Version="{version}" />""", packages);

        Assert.Empty(context.CarriedPackages);
        Assert.Contains(context.NotCarriedPackages.Select(p => p.Message), w => w.Contains("'Acme.Math'") && w.Contains(reason));
    }

    [Fact]
    public void NonNormalizedVersion_FindsTheNormalizedFolder()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "netstandard2.0", MathSource);

        var context = Build(fixture, """<PackageReference Include="Acme.Math" Version="1.2" />""", packages);

        Assert.Equal([new CarriedPackage("Acme.Math", "1.2")], context.CarriedPackages);
    }

    [Fact]
    public void BestFrameworkFolder_IsTheOneCompiledAgainst()
    {
        using var fixture = TempProjectFixture.Create();
        var packages = fixture.PathTo("packages");
        // Only the net8.0 build has Twice; netstandard2.0 and a -windows build that would win on
        // version alone do not.
        FakePackage.Write(packages, "Acme.Math", "1.2.0", "netstandard2.0", "namespace Acme.Math { public static class Calculator { } }");
        FakePackage.AddFramework(packages, "Acme.Math", "1.2.0", "net8.0", MathSource);
        FakePackage.AddFramework(packages, "Acme.Math", "1.2.0", "net9.0-windows7.0", "namespace Acme.Math { public static class Calculator { } }");

        var context = Build(fixture, """<PackageReference Include="Acme.Math" Version="1.2.0" />""", packages);

        Assert.Contains(context.CarriedFiles, f => f.RelativePath == "Services/Doubler.cs");
    }

    [Fact]
    public void Loader_ReadsPackageReferences_CentralVersions_AndPackagesConfig()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Directory.Packages.props", """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Central.Package" Version="5.0.0" />
              </ItemGroup>
            </Project>
            """);
        fixture.WriteFile("App/App.csproj", Csproj("""
            <PackageReference Include="Attribute.Package" Version="1.0.0" />
            <PackageReference Include="Element.Package"><Version>2.0.0</Version></PackageReference>
            <PackageReference Include="Central.Package" />
            <PackageReference Include="Tool.Package" Version="3.0.0"><PrivateAssets>all</PrivateAssets></PackageReference>
            """));
        fixture.WriteFile("App/packages.config", """
            <?xml version="1.0" encoding="utf-8"?>
            <packages>
              <package id="Legacy.Package" version="4.0.0" targetFramework="net48" />
            </packages>
            """);

        var project = new WinFormsProjectLoader().Load(fixture.PathTo("App/App.csproj"));

        Assert.Equal(
            [
                ("Attribute.Package", "1.0.0", false),
                ("Element.Package", "2.0.0", false),
                ("Central.Package", "5.0.0", false),
                ("Tool.Package", "3.0.0", true),
                ("Legacy.Package", "4.0.0", false),
            ],
            project.Packages.Select(p => (p.Id, p.Version, p.IsDevelopmentOnly)));
    }

    private static class FakePackage
    {
        public static void Write(
            string root, string id, string version, string framework, string source,
            IReadOnlyList<string>? references = null)
        {
            var folder = PackageFolder(root, id, version);
            File.WriteAllText(Path.Combine(folder, id.ToLowerInvariant() + ".nuspec"), Nuspec(id, version, ""));
            AddFramework(root, id, version, framework, source, references);
        }

        public static void AddFramework(
            string root, string id, string version, string framework, string source,
            IReadOnlyList<string>? references = null)
        {
            CompileAssembly(Path.Combine(PackageFolder(root, id, version), "lib", framework), id, source, references);
        }

        public static void WriteMeta(string root, string id, string version, params (string Id, string Range)[] dependencies)
        {
            var folder = PackageFolder(root, id, version);
            var group = string.Concat(dependencies.Select(d => $"""<dependency id="{d.Id}" version="{d.Range}" />"""));
            File.WriteAllText(
                Path.Combine(folder, id.ToLowerInvariant() + ".nuspec"),
                Nuspec(id, version, $"""<dependencies><group targetFramework=".NETStandard2.0">{group}</group></dependencies>"""));
            Directory.CreateDirectory(Path.Combine(folder, "lib", "netstandard2.0"));
            File.WriteAllText(Path.Combine(folder, "lib", "netstandard2.0", "_._"), "");
        }

        public static string CompileAssembly(string directory, string assemblyName, string source, IReadOnlyList<string>? references = null)
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, assemblyName + ".dll");
            // Against the reference pack's System.Runtime, as a real package is: one built against the
            // runtime's System.Private.CoreLib would not bind in the semantic compilation at all.
            var runtimeDirectory = new DirectoryInfo(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory().TrimEnd(Path.DirectorySeparatorChar));
            var refPacks = Path.Combine(runtimeDirectory.Parent!.Parent!.Parent!.FullName, "packs", "Microsoft.NETCore.App.Ref");
            var systemRuntime = Directory.EnumerateDirectories(refPacks, "10.*")
                .Select(d => Path.Combine(d, "ref", "net10.0", "System.Runtime.dll"))
                .First(File.Exists);
            var platform = new[] { MetadataReference.CreateFromFile(systemRuntime) };
            var compilation = CSharpCompilation.Create(
                assemblyName,
                [CSharpSyntaxTree.ParseText(source)],
                [.. platform, .. (references ?? []).Select(r => MetadataReference.CreateFromFile(r))],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var result = compilation.Emit(path);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics));
            return path;
        }

        private static string PackageFolder(string root, string id, string version)
        {
            var folder = Path.Combine(root, id.ToLowerInvariant(), version.ToLowerInvariant());
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static string Nuspec(string id, string version, string dependencies) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{id}</id>
                <version>{version}</version>
                {dependencies}
              </metadata>
            </package>
            """;
    }
}
