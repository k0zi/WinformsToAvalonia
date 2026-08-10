using Converter.Core.Project;

namespace Converter.Tests.Project;

public class ProjectReferenceResolverTests
{
    private const string SourceCsprojWithOneReference = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <ProjectReference Include="..\WarehouseApp.Data\WarehouseApp.Data.csproj" />
          </ItemGroup>
          <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <TargetFramework>net8.0-windows</TargetFramework>
            <UseWindowsForms>true</UseWindowsForms>
          </PropertyGroup>
        </Project>
        """;

    private const string PlainClassLibraryCsproj = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    private const string WinFormsCsproj = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <TargetFramework>net8.0-windows</TargetFramework>
            <UseWindowsForms>true</UseWindowsForms>
          </PropertyGroup>
        </Project>
        """;

    private static string CreateTempSourceLayout()
    {
        var root = Directory.CreateTempSubdirectory("wf2av-projrefs-").FullName;
        return root;
    }

    [Fact]
    public async Task Resolve_NonWinFormsSiblingProject_IsReturnedAsReferenceable()
    {
        var root = CreateTempSourceLayout();
        try
        {
            var appDir = Directory.CreateDirectory(Path.Combine(root, "WarehouseApp")).FullName;
            var dataDir = Directory.CreateDirectory(Path.Combine(root, "WarehouseApp.Data")).FullName;
            await File.WriteAllTextAsync(Path.Combine(appDir, "WarehouseApp.csproj"), SourceCsprojWithOneReference);
            await File.WriteAllTextAsync(Path.Combine(dataDir, "WarehouseApp.Data.csproj"), PlainClassLibraryCsproj);

            var result = ProjectReferenceResolver.Resolve(appDir);

            Assert.Single(result.Referenceable);
            Assert.Equal("WarehouseApp.Data", result.Referenceable[0].ProjectName);
            Assert.Empty(result.SkippedWinFormsProjectNames);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Resolve_WinFormsSiblingProject_IsSkippedAndReported()
    {
        var root = CreateTempSourceLayout();
        try
        {
            var appDir = Directory.CreateDirectory(Path.Combine(root, "WarehouseApp")).FullName;
            var dataDir = Directory.CreateDirectory(Path.Combine(root, "WarehouseApp.Data")).FullName;
            await File.WriteAllTextAsync(Path.Combine(appDir, "WarehouseApp.csproj"), SourceCsprojWithOneReference);
            await File.WriteAllTextAsync(Path.Combine(dataDir, "WarehouseApp.Data.csproj"), WinFormsCsproj);

            var result = ProjectReferenceResolver.Resolve(appDir);

            Assert.Empty(result.Referenceable);
            Assert.Contains("WarehouseApp.Data", result.SkippedWinFormsProjectNames);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolve_NoCsprojInSourceDirectory_ReturnsEmpty()
    {
        var root = CreateTempSourceLayout();
        try
        {
            var result = ProjectReferenceResolver.Resolve(root);

            Assert.Empty(result.Referenceable);
            Assert.Empty(result.SkippedWinFormsProjectNames);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Resolve_ReferencedProjectFileMissing_SkipsItWithoutThrowing()
    {
        var root = CreateTempSourceLayout();
        try
        {
            var appDir = Directory.CreateDirectory(Path.Combine(root, "WarehouseApp")).FullName;
            await File.WriteAllTextAsync(Path.Combine(appDir, "WarehouseApp.csproj"), SourceCsprojWithOneReference);
            // WarehouseApp.Data directory/csproj deliberately not created.

            var result = ProjectReferenceResolver.Resolve(appDir);

            Assert.Empty(result.Referenceable);
            Assert.Empty(result.SkippedWinFormsProjectNames);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Resolve_MalformedSourceCsproj_ReturnsEmptyWithoutThrowing()
    {
        var root = CreateTempSourceLayout();
        try
        {
            var appDir = Directory.CreateDirectory(Path.Combine(root, "WarehouseApp")).FullName;
            await File.WriteAllTextAsync(Path.Combine(appDir, "WarehouseApp.csproj"), "not valid xml <<<");

            var result = ProjectReferenceResolver.Resolve(appDir);

            Assert.Empty(result.Referenceable);
            Assert.Empty(result.SkippedWinFormsProjectNames);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
