using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class DesignerFileLocatorTests
{
    [Fact]
    public void Locate_ClassifiesFormUserControlComponentAndOther()
    {
        using var fixture = TempProjectFixture.Create();

        fixture.WriteFile("Form1.cs", """
            namespace Demo
            {
                public partial class Form1 : System.Windows.Forms.Form
                {
                    public Form1() { InitializeComponent(); }
                }
            }
            """);
        fixture.WriteFile("Form1.Designer.cs", """
            namespace Demo
            {
                partial class Form1
                {
                    private void InitializeComponent() { }
                }
            }
            """);

        fixture.WriteFile("MyUserControl.cs", """
            namespace Demo
            {
                public partial class MyUserControl : System.Windows.Forms.UserControl
                {
                    public MyUserControl() { InitializeComponent(); }
                }
            }
            """);
        fixture.WriteFile("MyUserControl.Designer.cs", """
            namespace Demo
            {
                partial class MyUserControl
                {
                    private void InitializeComponent() { }
                }
            }
            """);

        fixture.WriteFile("MyTimerComponent.cs", """
            namespace Demo
            {
                public partial class MyTimerComponent : System.ComponentModel.Component
                {
                }
            }
            """);

        fixture.WriteFile("Helpers.cs", """
            namespace Demo
            {
                internal static class Helpers
                {
                    public static string Greet(string name) => "Hello " + name;
                }
            }
            """);

        var project = new WinFormsToAvalonia.Core.Model.WinFormsProjectModel(
            ProjectFilePath: fixture.PathTo("Demo.csproj"),
            IsLegacyStyle: false,
            RootNamespace: "Demo",
            AssemblyName: "Demo",
            TargetFrameworks: ["net9.0-windows"],
            CompileFiles:
            [
                fixture.PathTo("Form1.cs"),
                fixture.PathTo("Form1.Designer.cs"),
                fixture.PathTo("MyUserControl.cs"),
                fixture.PathTo("MyUserControl.Designer.cs"),
                fixture.PathTo("MyTimerComponent.cs"),
                fixture.PathTo("Helpers.cs"),
            ],
            ResourceFiles: [],
            ProjectReferences: []);

        var results = new DesignerFileLocator().Locate(project);

        var form1 = Assert.Single(results, r => r.ClassName == "Form1");
        Assert.Equal(WinFormsArtifactKind.Form, form1.Kind);
        Assert.Equal("Demo", form1.Namespace);
        Assert.NotNull(form1.PrimaryFilePath);
        Assert.NotNull(form1.DesignerFilePath);

        var userControl = Assert.Single(results, r => r.ClassName == "MyUserControl");
        Assert.Equal(WinFormsArtifactKind.UserControl, userControl.Kind);

        var component = Assert.Single(results, r => r.ClassName == "MyTimerComponent");
        Assert.Equal(WinFormsArtifactKind.Component, component.Kind);
        Assert.Null(component.DesignerFilePath);

        var helpers = Assert.Single(results, r => r.ClassName == "Helpers");
        Assert.Equal(WinFormsArtifactKind.Other, helpers.Kind);
    }

    [Fact]
    public void Locate_FormDerivingFromAProjectBaseForm_ResolvesTransitivelyToForm()
    {
        using var fixture = TempProjectFixture.Create();

        fixture.WriteFile("BaseForms/MyBaseForm.cs", """
            namespace Demo.BaseForms
            {
                public class MyBaseForm : System.Windows.Forms.Form
                {
                }
            }
            """);
        fixture.WriteFile("DerivedForm.cs", """
            namespace Demo
            {
                public partial class DerivedForm : Demo.BaseForms.MyBaseForm
                {
                    public DerivedForm() { InitializeComponent(); }
                }
            }
            """);
        fixture.WriteFile("DerivedForm.Designer.cs", """
            namespace Demo
            {
                partial class DerivedForm
                {
                    private void InitializeComponent() { }
                }
            }
            """);

        var results = new DesignerFileLocator().Locate(ProjectOf(
            fixture,
            "BaseForms/MyBaseForm.cs",
            "DerivedForm.cs",
            "DerivedForm.Designer.cs"));

        var derived = Assert.Single(results, r => r.ClassName == "DerivedForm");
        Assert.Equal(WinFormsArtifactKind.Form, derived.Kind);
        Assert.Empty(derived.UnresolvedBaseTypes);
    }

    /// <summary>Two hops, to prove the walk is a real traversal rather than a single extra lookup.</summary>
    [Fact]
    public void Locate_UserControlBehindTwoProjectBaseClasses_StillResolves()
    {
        using var fixture = TempProjectFixture.Create();

        fixture.WriteFile("ControlBase.cs", "namespace Demo { public class ControlBase : System.Windows.Forms.UserControl { } }");
        fixture.WriteFile("ThemedControlBase.cs", "namespace Demo { public class ThemedControlBase : ControlBase { } }");
        fixture.WriteFile("MyControl.cs", "namespace Demo { public partial class MyControl : ThemedControlBase { } }");
        fixture.WriteFile("MyControl.Designer.cs", "namespace Demo { partial class MyControl { private void InitializeComponent() { } } }");

        var results = new DesignerFileLocator().Locate(ProjectOf(
            fixture, "ControlBase.cs", "ThemedControlBase.cs", "MyControl.cs", "MyControl.Designer.cs"));

        var control = Assert.Single(results, r => r.ClassName == "MyControl");
        Assert.Equal(WinFormsArtifactKind.UserControl, control.Kind);
    }

    [Fact]
    public void Locate_BaseClassOutsideTheProject_StaysOtherButReportsTheUnresolvedName()
    {
        using var fixture = TempProjectFixture.Create();

        fixture.WriteFile("ExternalDerivedForm.cs", "namespace Demo { public partial class ExternalDerivedForm : ThirdParty.RibbonForm { } }");
        fixture.WriteFile("ExternalDerivedForm.Designer.cs", "namespace Demo { partial class ExternalDerivedForm { private void InitializeComponent() { } } }");

        var results = new DesignerFileLocator().Locate(ProjectOf(
            fixture, "ExternalDerivedForm.cs", "ExternalDerivedForm.Designer.cs"));

        var form = Assert.Single(results);
        Assert.Equal(WinFormsArtifactKind.Other, form.Kind);
        Assert.Equal(["RibbonForm"], form.UnresolvedBaseTypes);
    }

    /// <summary>
    /// An Other-kind class with no designer file (Program, a helper, a model) is Other on
    /// purpose - reporting it would bury the one case that matters in noise.
    /// </summary>
    [Fact]
    public void Locate_PlainClassWithNoDesignerFile_ReportsNoUnresolvedBaseTypes()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Repository.cs", "namespace Demo { public class Repository : SomeExternal.BaseRepository { } }");

        var results = new DesignerFileLocator().Locate(ProjectOf(fixture, "Repository.cs"));

        var repository = Assert.Single(results);
        Assert.Equal(WinFormsArtifactKind.Other, repository.Kind);
        Assert.Empty(repository.UnresolvedBaseTypes);
    }

    /// <summary>Illegal in C#, but this walker runs on unresolved syntax and must still terminate.</summary>
    [Fact]
    public void Locate_CyclicBaseClasses_TerminatesAndClassifiesAsOther()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("A.cs", "namespace Demo { public partial class A : B { } }");
        fixture.WriteFile("A.Designer.cs", "namespace Demo { partial class A { private void InitializeComponent() { } } }");
        fixture.WriteFile("B.cs", "namespace Demo { public class B : A { } }");

        var results = new DesignerFileLocator().Locate(ProjectOf(fixture, "A.cs", "A.Designer.cs", "B.cs"));

        var a = Assert.Single(results, r => r.ClassName == "A");
        Assert.Equal(WinFormsArtifactKind.Other, a.Kind);
    }

    private static WinFormsProjectModel ProjectOf(TempProjectFixture fixture, params string[] relativePaths) =>
        new(
            ProjectFilePath: fixture.PathTo("Demo.csproj"),
            IsLegacyStyle: false,
            RootNamespace: "Demo",
            AssemblyName: "Demo",
            TargetFrameworks: ["net9.0-windows"],
            CompileFiles: relativePaths.Select(fixture.PathTo).ToList(),
            ResourceFiles: [],
            ProjectReferences: []);

    [Fact]
    public void Locate_PairsResxFileWhenPresent()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Form1.cs", "namespace Demo { public partial class Form1 : System.Windows.Forms.Form { } }");
        fixture.WriteFile("Form1.resx", "<root></root>");

        var project = new WinFormsProjectModel(
            fixture.PathTo("Demo.csproj"),
            IsLegacyStyle: false,
            RootNamespace: "Demo",
            AssemblyName: "Demo",
            TargetFrameworks: ["net9.0-windows"],
            CompileFiles: [fixture.PathTo("Form1.cs")],
            ResourceFiles: [fixture.PathTo("Form1.resx")],
            ProjectReferences: []);

        var results = new DesignerFileLocator().Locate(project);

        var form1 = Assert.Single(results);
        Assert.Equal(fixture.PathTo("Form1.resx"), form1.ResxFilePath);
    }
}
