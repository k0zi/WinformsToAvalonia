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
            ResourceFiles: []);

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
            ResourceFiles: [fixture.PathTo("Form1.resx")]);

        var results = new DesignerFileLocator().Locate(project);

        var form1 = Assert.Single(results);
        Assert.Equal(fixture.PathTo("Form1.resx"), form1.ResxFilePath);
    }
}
