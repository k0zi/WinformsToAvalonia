using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class WinFormsProjectLoaderTests
{
    [Fact]
    public void Load_LegacyStyleProject_ReadsExplicitCompileItemsOnlyInDeclaredOrder()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Form1.cs", "class Form1 {}");
        fixture.WriteFile("Form1.Designer.cs", "partial class Form1 {}");
        // Deliberately NOT referenced by <Compile Include> - must be excluded for legacy projects.
        fixture.WriteFile("NotIncluded.cs", "class NotIncluded {}");
        fixture.WriteFile("Project.csproj", """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup>
                <RootNamespace>MyLegacyApp</RootNamespace>
                <AssemblyName>MyLegacyApp</AssemblyName>
                <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="Form1.cs" />
                <Compile Include="Form1.Designer.cs" />
              </ItemGroup>
            </Project>
            """);

        var model = new WinFormsProjectLoader().Load(fixture.PathTo("Project.csproj"));

        Assert.True(model.IsLegacyStyle);
        Assert.Equal("MyLegacyApp", model.RootNamespace);
        Assert.Equal(["v4.8"], model.TargetFrameworks);
        Assert.Equal(2, model.CompileFiles.Count);
        Assert.Contains(fixture.PathTo("Form1.cs"), model.CompileFiles);
        Assert.Contains(fixture.PathTo("Form1.Designer.cs"), model.CompileFiles);
        Assert.DoesNotContain(fixture.PathTo("NotIncluded.cs"), model.CompileFiles);
    }

    [Fact]
    public void Load_SdkStyleProject_GlobsCsFilesAndHonorsCompileRemove()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Form1.cs", "class Form1 {}");
        fixture.WriteFile("Excluded.cs", "class Excluded {}");
        fixture.WriteFile(Path.Combine("obj", "Generated.cs"), "class Generated {}");
        fixture.WriteFile("Project.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0-windows</TargetFramework>
                <UseWindowsForms>true</UseWindowsForms>
              </PropertyGroup>
              <ItemGroup>
                <Compile Remove="Excluded.cs" />
              </ItemGroup>
            </Project>
            """);

        var model = new WinFormsProjectLoader().Load(fixture.PathTo("Project.csproj"));

        Assert.False(model.IsLegacyStyle);
        Assert.Equal(["net9.0-windows"], model.TargetFrameworks);
        Assert.Contains(fixture.PathTo("Form1.cs"), model.CompileFiles);
        Assert.DoesNotContain(fixture.PathTo("Excluded.cs"), model.CompileFiles);
        Assert.DoesNotContain(fixture.PathTo(Path.Combine("obj", "Generated.cs")), model.CompileFiles);
    }

    /// <summary>
    /// The reference graph is what a solution-wide conversion uses to decide which *other*
    /// projects' UserControls a Form here may host, so the paths have to come out absolute and
    /// with Windows separators normalised - a WinForms csproj always writes them with backslashes.
    /// </summary>
    [Fact]
    public void Load_ProjectWithReferences_ResolvesThemToAbsolutePaths()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Project.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0-windows</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Widgets\Widgets.csproj" />
                <ProjectReference Include="..\Shared\Shared.csproj" />
              </ItemGroup>
            </Project>
            """);

        var model = new WinFormsProjectLoader().Load(fixture.PathTo("Project.csproj"));

        Assert.Equal(
            [
                Path.GetFullPath(Path.Combine(fixture.PathTo("."), "..", "Shared", "Shared.csproj")),
                Path.GetFullPath(Path.Combine(fixture.PathTo("."), "..", "Widgets", "Widgets.csproj")),
            ],
            model.ProjectReferences.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void Load_ProjectWithoutReferences_HasNone()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Project.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0-windows</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        Assert.Empty(new WinFormsProjectLoader().Load(fixture.PathTo("Project.csproj")).ProjectReferences);
    }
}
