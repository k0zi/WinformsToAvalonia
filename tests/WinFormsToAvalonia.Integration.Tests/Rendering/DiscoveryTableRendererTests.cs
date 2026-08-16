using Spectre.Console.Testing;
using WinFormsToAvalonia.Cli.Rendering;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests.Rendering;

public class DiscoveryTableRendererTests
{
    [Fact]
    public void Render_MixOfKinds_ShowsFormsAndUserControlsButSkipsOther()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var project = new WinFormsProjectModel(
            "/src/Demo.csproj", IsLegacyStyle: false, RootNamespace: "Demo", AssemblyName: "Demo",
            TargetFrameworks: ["net9.0-windows"], CompileFiles: [], ResourceFiles: []);

        var pairings = new[]
        {
            new DesignerFilePairing("Form1", "Demo", WinFormsArtifactKind.Form, "/src/Form1.cs", "/src/Form1.Designer.cs", null),
            new DesignerFilePairing("MyUserControl", "Demo.Controls", WinFormsArtifactKind.UserControl, "/src/Controls/MyUserControl.cs", "/src/Controls/MyUserControl.Designer.cs", null),
            new DesignerFilePairing("Helpers", "Demo", WinFormsArtifactKind.Other, "/src/Helpers.cs", null, null),
        };

        DiscoveryTableRenderer.Render(console, project, pairings);

        var output = console.Output;
        Assert.Contains("Form1", output);
        Assert.Contains("MyUserControl", output);
        Assert.DoesNotContain("Helpers", output);
        Assert.Contains("1 form(s), 1 user control(s), 0 component(s)", output);
        Assert.Contains("1 other class(es) skipped", output);
    }
}
