using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class StartupFormLocatorTests
{
    private static readonly DesignerFilePairing AboutForm = Form("AboutForm", "Demo");
    private static readonly DesignerFilePairing MainForm = Form("MainForm", "Demo");
    private static readonly IReadOnlyList<DesignerFilePairing> Forms = [AboutForm, MainForm];

    private static DesignerFilePairing Form(string name, string? ns) =>
        new(name, ns, WinFormsArtifactKind.Form, $"{name}.cs", $"{name}.Designer.cs", null);

    private static StartupFormResolution Locate(string program, IReadOnlyList<DesignerFilePairing>? forms = null) =>
        new StartupFormLocator().Locate([("Program.cs", program)], forms ?? Forms);

    [Theory]
    [InlineData("Application.Run(new MainForm());")]
    [InlineData("System.Windows.Forms.Application.Run(new MainForm());")]
    [InlineData("global::System.Windows.Forms.Application.Run(new global::Demo.MainForm());")]
    [InlineData("Application.Run(new Demo.MainForm());")]
    [InlineData("var form = new MainForm(); Application.Run(form);")]
    [InlineData("MainForm form = new MainForm(); Application.Run(form);")]
    [InlineData("using var form = new MainForm(); Application.Run(form);")]
    [InlineData("Form form; form = new MainForm(); Application.Run(form);")]
    public void Locate_ReadsTheFormApplicationRunStarts(string body)
    {
        var result = Locate($$"""
            namespace Demo;
            static class Program
            {
                static void Main()
                {
                    ApplicationConfiguration.Initialize();
                    {{body}}
                }
            }
            """);

        Assert.Same(MainForm, result.Form);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void Locate_TopLevelStatements_AreReadToo()
    {
        var result = Locate("""
            using Demo;
            ApplicationConfiguration.Initialize();
            var form = new MainForm();
            Application.Run(form);
            """);

        Assert.Same(MainForm, result.Form);
    }

    [Fact]
    public void Locate_ApplicationContext_IsReportedRatherThanGuessed()
    {
        var result = Locate("""
            namespace Demo;
            static class Program
            {
                static void Main() => Application.Run(new TrayContext());
            }
            class TrayContext : ApplicationContext { }
            """);

        Assert.Null(result.Form);
        Assert.NotNull(result.Warning);
        Assert.Contains("'TrayContext' is not a Form this conversion converts", result.Warning);
        Assert.Contains("'AboutForm' - the first Form discovered", result.Warning);
    }

    [Fact]
    public void Locate_LocalAssignedTwoDifferentForms_HasNoSingleAnswer()
    {
        var result = Locate("""
            namespace Demo;
            static class Program
            {
                static void Main(string[] args)
                {
                    Form form = new MainForm();
                    if (args.Length > 0) { form = new AboutForm(); }
                    Application.Run(form);
                }
            }
            """);

        Assert.Null(result.Form);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public void Locate_TwoRunCallsNamingDifferentForms_TakesTheFirstAndSaysSo()
    {
        var result = Locate("""
            namespace Demo;
            static class Program
            {
                static void Main(string[] args)
                {
                    if (args.Length > 0) { Application.Run(new AboutForm()); }
                    else { Application.Run(new MainForm()); }
                }
            }
            """);

        Assert.Same(AboutForm, result.Form);
        Assert.NotNull(result.Warning);
        Assert.Contains("several 'Application.Run(...)' calls", result.Warning);
    }

    [Fact]
    public void Locate_NoRunCall_WarnsOnlyWhenThereIsAChoice()
    {
        const string noRun = "namespace Demo; static class Helpers { }";

        Assert.NotNull(Locate(noRun).Warning);
        Assert.Null(Locate(noRun).Form);

        var single = Locate(noRun, [MainForm]);
        Assert.Null(single.Warning);
        Assert.Null(single.Form);
    }

    [Fact]
    public void Locate_SameSimpleNameInTwoNamespaces_PrefersTheCallersNamespace()
    {
        var other = Form("MainForm", "Demo.Legacy");
        var result = Locate(
            "namespace Demo; static class Program { static void Main() => Application.Run(new MainForm()); }",
            [other, MainForm]);

        Assert.Same(MainForm, result.Form);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void Locate_UnrelatedRunMethods_AreNotMistakenForApplicationRun()
    {
        var result = Locate("""
            namespace Demo;
            static class Program
            {
                static void Main()
                {
                    Task.Run(() => new AboutForm());
                    Application.Run(new MainForm());
                }
            }
            """);

        Assert.Same(MainForm, result.Form);
        Assert.Null(result.Warning);
    }
}
