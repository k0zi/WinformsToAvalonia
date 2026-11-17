using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

public class ProjectDiscoveryTests
{
    private static readonly string SampleAppsRoot = Path.Combine(
        AppContext.BaseDirectory, "SampleApps");

    [Fact]
    public void LegacyFrameworkApp_DiscoversForm1AndSkipsNonFormClasses()
    {
        var csproj = Path.Combine(SampleAppsRoot, "LegacyFrameworkApp", "LegacyFrameworkApp.csproj");
        var project = new WinFormsProjectLoader().Load(csproj);

        Assert.True(project.IsLegacyStyle);
        Assert.Equal(["v4.8"], project.TargetFrameworks);

        var pairings = new DesignerFileLocator().Locate(project);

        var form1 = Assert.Single(pairings, p => p.ClassName == "Form1");
        Assert.Equal(WinFormsArtifactKind.Form, form1.Kind);
        Assert.Equal("LegacyFrameworkApp", form1.Namespace);
        Assert.NotNull(form1.PrimaryFilePath);
        Assert.NotNull(form1.DesignerFilePath);
        Assert.NotNull(form1.ResxFilePath);

        Assert.DoesNotContain(pairings, p => p.ClassName is "Program" or "Helpers" && p.Kind != WinFormsArtifactKind.Other);
    }

    /// <summary>
    /// The form declares `: AppFormBase`, not `: Form`. Classifying on the immediate base list
    /// alone used to drop it from the conversion without a word; the base list is now followed
    /// through the project's own classes until it reaches a WinForms root.
    /// </summary>
    [Fact]
    public void BaseFormApp_ResolvesTheProjectsOwnBaseFormAndConvertsTheDerivedForm()
    {
        var csproj = Path.Combine(SampleAppsRoot, "BaseFormApp", "BaseFormApp.csproj");
        var project = new WinFormsProjectLoader().Load(csproj);

        var pairings = new DesignerFileLocator().Locate(project);

        var mainForm = Assert.Single(pairings, p => p.ClassName == "MainForm");
        Assert.Equal(WinFormsArtifactKind.Form, mainForm.Kind);
        Assert.Empty(mainForm.UnresolvedBaseTypes);

        // The base class itself is a Form too, but has no Designer.cs - so it is discovered
        // and then correctly left unconverted, rather than emitted as an empty second Window.
        var baseForm = Assert.Single(pairings, p => p.ClassName == "AppFormBase");
        Assert.Equal(WinFormsArtifactKind.Form, baseForm.Kind);
        Assert.Null(baseForm.DesignerFilePath);
    }

    [Fact]
    public void ModernNetApp_DiscoversMainFormAndNestedUserControl()
    {
        var csproj = Path.Combine(SampleAppsRoot, "ModernNetApp", "ModernNetApp.csproj");
        var project = new WinFormsProjectLoader().Load(csproj);

        Assert.False(project.IsLegacyStyle);
        Assert.Equal(["net9.0-windows"], project.TargetFrameworks);

        var pairings = new DesignerFileLocator().Locate(project);

        var mainForm = Assert.Single(pairings, p => p.ClassName == "MainForm");
        Assert.Equal(WinFormsArtifactKind.Form, mainForm.Kind);
        Assert.Equal("ModernNetApp", mainForm.Namespace);

        var userControl = Assert.Single(pairings, p => p.ClassName == "MyUserControl");
        Assert.Equal(WinFormsArtifactKind.UserControl, userControl.Kind);
        Assert.Equal("ModernNetApp.Controls", userControl.Namespace);
        Assert.Contains(Path.Combine("Controls", "MyUserControl.cs"), userControl.PrimaryFilePath);
    }
}
