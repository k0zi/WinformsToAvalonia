using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Emission;

public class ViewCodeBehindEmitterTests
{
    [Fact]
    public void EmitViewCodeBehind_CodeBehindHandler_EmitsAMethodWithTheAvaloniaSignatureAndThePreservedBody()
    {
        var formModel = FormWith(("treeView1", "TreeView"));
        formModel.Controls["treeView1"].Events.Add(new EventHandlerBinding("DragOver", "treeView1_DragOver", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void treeView1_DragOver(object sender, DragEventArgs e)
                    {
                        this.treeView1.Nodes.Add("dropped");
                    }
                }
            }
            """);

        var classDecl = SingleClass(source);
        var method = Assert.Single(classDecl.Members.OfType<MethodDeclarationSyntax>());
        Assert.Equal("treeView1_DragOver", method.Identifier.ValueText);
        Assert.Equal("void", method.ReturnType.ToString());
        Assert.Equal(["object?", "DragEventArgs"], method.ParameterList.Parameters.Select(p => p.Type!.ToString()));

        // The original body is preserved inside the method, but never as compiling code.
        Assert.Contains("this.treeView1.Nodes.Add(\"dropped\");", source);
        Assert.DoesNotContain("Nodes.Add(\"dropped\");\n        MigrationTodo", source.Replace("\r\n", "\n"));

        // Reported, not thrown: Avalonia raises these from the framework - including during XAML
        // initialization - so a throwing stub took the generated app down before it was visible.
        Assert.Contains("MigrationTodo.NotMigrated(nameof(treeView1_DragOver), \"treeView1_DragOver\");", source);
        Assert.DoesNotContain("throw new NotImplementedException", source);
    }

    [Fact]
    public void EmitViewCodeBehind_AsyncOriginal_EmitsANonAsyncMethodAndRecordsTheModifier()
    {
        // `async void` with no await would compile with CS1998; the modifier is noted in the
        // TODO instead so the generated project stays warning-free.
        var formModel = FormWith(("loginButton", "Button"));
        formModel.Controls["loginButton"].Events.Add(new EventHandlerBinding("Click", "loginButton_Click", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private async void loginButton_Click(object? sender, EventArgs e)
                    {
                        await AuthenticateAsync();
                        this.Close();
                    }
                }
            }
            """);

        var method = Assert.Single(SingleClass(source).Members.OfType<MethodDeclarationSyntax>());
        Assert.DoesNotContain(method.Modifiers, m => m.ValueText == "async");
        Assert.Contains("ORIGINAL WINFORMS BODY of async 'loginButton_Click'", source);
    }

    /// <summary>
    /// A body HandlerBodyRewriter fully translates leaves no TODO behind: the method is the
    /// migration, so there is nothing left to report at run time.
    /// </summary>
    [Fact]
    public void EmitViewCodeBehind_FullyMigratedBody_EmitsRealCodeAndNoMigrationMarker()
    {
        var formModel = FormWith(("closeButton", "Button"), ("statusLabel", "Label"));
        formModel.Controls["closeButton"].Events.Add(new EventHandlerBinding("Click", "closeButton_Click", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void closeButton_Click(object sender, EventArgs e)
                    {
                        this.statusLabel.Text = "Bye";
                        this.Close();
                    }
                }
            }
            """);

        Assert.Contains("statusLabel.Text = \"Bye\";", source);
        Assert.Contains("Close();", source);
        Assert.DoesNotContain("ORIGINAL WINFORMS BODY", source);
        Assert.DoesNotContain("MigrationTodo.NotMigrated(nameof(closeButton_Click)", source);
    }

    /// <summary>
    /// A partly translated body keeps both halves: the migrated prefix as code, the rest as the
    /// same TODO comment as before - plus the marker, because the handler is still unfinished.
    /// </summary>
    [Fact]
    public void EmitViewCodeBehind_PartiallyMigratedBody_EmitsCodeThenTheRemainingTodo()
    {
        var formModel = FormWith(("saveButton", "Button"), ("statusLabel", "Label"));
        formModel.Controls["saveButton"].Events.Add(new EventHandlerBinding("Click", "saveButton_Click", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void saveButton_Click(object sender, EventArgs e)
                    {
                        this.statusLabel.Text = "Saving";
                        PersistEverything();
                        this.Close();
                    }
                }
            }
            """);

        Assert.Contains("statusLabel.Text = \"Saving\";", source);
        Assert.Contains("REMAINING WINFORMS BODY", source);
        Assert.Contains("PersistEverything();", source);
        Assert.Contains("MigrationTodo.NotMigrated(nameof(saveButton_Click)", source);
    }

    [Fact]
    public void EmitViewCodeBehind_MessageBoxCall_MakesTheHandlerAsyncAndImportsTheFallback()
    {
        var formModel = FormWith(("infoButton", "Button"));
        formModel.Controls["infoButton"].Events.Add(new EventHandlerBinding("Click", "infoButton_Click", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void infoButton_Click(object sender, EventArgs e)
                    {
                        MessageBox.Show("Saved", "Info");
                    }
                }
            }
            """);

        var method = Assert.Single(SingleClass(source).Members.OfType<MethodDeclarationSyntax>());
        Assert.Contains(method.Modifiers, m => m.ValueText == "async");
        Assert.Contains("await MessageBoxFallback.ShowAsync(this, \"Saved\", \"Info\");", source);
        Assert.Contains("using Demo.Controls;", source);
    }

    [Fact]
    public void EmitViewCodeBehind_Always_SetsTheDataContextSoBindingsResolve()
    {
        var source = new ViewCodeBehindEmitter().EmitViewCodeBehind(
            "Demo", "", "Form1View", "Form1ViewModel", FormMigrationPlan.Empty, rawCodeBehind: null);

        Assert.Contains("DataContext = new Form1ViewModel();", source);
        Assert.Contains("namespace Demo.Views;", source);
        ParseAndAssertNoErrors(source);
    }

    [Fact]
    public void EmitViewCodeBehind_TimerWithTickHandler_CreatesAndSubscribesADispatcherTimer()
    {
        var formModel = FormWith(("refreshTimer", "Timer"));
        formModel.Controls["refreshTimer"].Properties["Interval"] = new PropertyValue.Literal(250);
        formModel.Controls["refreshTimer"].Properties["Enabled"] = new PropertyValue.Literal(true);
        formModel.Controls["refreshTimer"].Events.Add(new EventHandlerBinding("Tick", "refreshTimer_Tick", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void refreshTimer_Tick(object sender, EventArgs e)
                    {
                    }
                }
            }
            """);

        Assert.Contains("private readonly DispatcherTimer refreshTimer;", source);
        Assert.Contains("refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };", source);
        Assert.Contains("refreshTimer.Tick += refreshTimer_Tick;", source);
        Assert.Contains("refreshTimer.Start();", source);
        Assert.Contains("using Avalonia.Threading;", source);
        ParseAndAssertNoErrors(source);
    }

    [Fact]
    public void EmitViewCodeBehind_FileDialogComponent_EmitsAStorageProviderMethodOnTheView()
    {
        var source = EmitFor(FormWith(("openFileDialog1", "OpenFileDialog")), "namespace Demo { public partial class Form1 : Form { } }");

        Assert.Contains("private async Task ShowOpenFileDialog1Async()", source);
        Assert.Contains("await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions());", source);
        Assert.Contains("using Avalonia.Platform.Storage;", source);
        Assert.DoesNotContain("Application.Current", source);
        ParseAndAssertNoErrors(source);
    }

    [Fact]
    public void EmitViewCodeBehind_OriginalSourceContainingLiteralBlockCommentClose_IsEscapedAndStillParses()
    {
        var original = new RawCodeBehind("Form1.cs", "// a comment terminator: */ in the middle\nclass X {}");

        var source = new ViewCodeBehindEmitter().EmitViewCodeBehind(
            "Demo", "", "Form1View", "Form1ViewModel", FormMigrationPlan.Empty, original);

        var classDecl = SingleClass(source);
        Assert.Equal("Form1View", classDecl.Identifier.ValueText);
        Assert.DoesNotContain("*/ in the middle", source);
        Assert.Contains("* / in the middle", source);
    }

    [Fact]
    public void EmitViewCodeBehind_NoRawCodeBehind_OmitsTheTrailingCommentBlock()
    {
        var source = new ViewCodeBehindEmitter().EmitViewCodeBehind(
            "Demo", "Admin", "UserFormView", "UserFormViewModel", FormMigrationPlan.Empty, rawCodeBehind: null);

        Assert.DoesNotContain("PRESERVED FOR REFERENCE", source);
        Assert.Contains("namespace Demo.Views.Admin;", source);
        ParseAndAssertNoErrors(source);
    }

    private static string EmitFor(FormModel formModel, string codeBehindSource)
    {
        var codeBehind = new CodeBehindAnalyzer().Analyze(codeBehindSource, "Form1.cs", formModel);
        var plan = new FormMigrationPlanner(new ControlMappingRegistry(), new EventMappingRegistry()).Plan(formModel, codeBehind);
        return new ViewCodeBehindEmitter().EmitViewCodeBehind("Demo", "", "Form1View", "Form1ViewModel", plan, rawCodeBehind: null);
    }

    private static FormModel FormWith(params (string FieldName, string TypeName)[] controls)
    {
        var formModel = new FormModel { ClassName = "Form1" };
        foreach (var (fieldName, typeName) in controls)
        {
            var control = new ControlModel { FieldName = fieldName, ClrTypeName = typeName };
            formModel.Controls[fieldName] = control;
            formModel.RootControls.Add(control);
        }

        return formModel;
    }

    private static ClassDeclarationSyntax SingleClass(string source) =>
        Assert.Single(ParseAndAssertNoErrors(source).DescendantNodes().OfType<ClassDeclarationSyntax>());

    private static CompilationUnitSyntax ParseAndAssertNoErrors(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "Parse errors: " + string.Join("; ", errors));
        return (CompilationUnitSyntax)tree.GetRoot();
    }
}
