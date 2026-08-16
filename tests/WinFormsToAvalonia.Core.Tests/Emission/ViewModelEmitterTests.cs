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

public class ViewModelEmitterTests
{
    [Fact]
    public void EmitViewModel_PromotedClickHandler_GeneratesACommandAndItsBoundProperties()
    {
        var formModel = FormWith(("okButton", "Button"), ("nameTextBox", "TextBox"));
        formModel.Controls["nameTextBox"].Properties["Text"] = new PropertyValue.Literal("Ada");
        formModel.Controls["okButton"].Events.Add(new EventHandlerBinding("Click", "okButton_Click", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        nameTextBox.Text = "done";
                    }
                }
            }
            """);

        var classDecl = SingleClass(source);
        Assert.Equal("Form1ViewModel", classDecl.Identifier.ValueText);
        Assert.Contains(classDecl.Modifiers, m => m.ValueText == "partial");

        var property = Assert.Single(classDecl.Members.OfType<PropertyDeclarationSyntax>());
        Assert.Equal("NameTextBoxText", property.Identifier.ValueText);
        Assert.Equal("string", property.Type.ToString());
        Assert.Contains(property.AttributeLists.SelectMany(a => a.Attributes), a => a.Name.ToString() == "ObservableProperty");
        // The designer literal moves here, because the AXAML attribute is now a {Binding}.
        Assert.Contains("= \"Ada\";", source);

        var method = Assert.Single(classDecl.Members.OfType<MethodDeclarationSyntax>());
        Assert.Equal("OkButton", method.Identifier.ValueText);
        Assert.Contains(method.AttributeLists.SelectMany(a => a.Attributes), a => a.Name.ToString() == "RelayCommand");
        Assert.Contains("nameTextBox.Text = \"done\";", source);
        Assert.Contains("MigrationTodo.NotMigrated(nameof(OkButton), \"okButton_Click\");", source);
        Assert.DoesNotContain("throw new NotImplementedException", source);
    }

    [Fact]
    public void EmitViewModel_HandlerThatCannotBePromoted_ProducesAnEmptyViewModel()
    {
        // The samples/WinForms/WinForms-Control-Click shape: casting `sender` keeps the handler
        // in code-behind, so nothing is left for the ViewModel to hold.
        var formModel = FormWith(("button1", "Button"));
        formModel.Controls["button1"].Events.Add(new EventHandlerBinding("Click", "controlClick", null));

        var source = EmitFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void controlClick(object sender, EventArgs e)
                    {
                        var control = (Control)sender;
                    }
                }
            }
            """);

        Assert.Empty(SingleClass(source).Members);
        Assert.DoesNotContain("RelayCommand", source);
        Assert.DoesNotContain("ObservableProperty", source);
    }

    [Fact]
    public void EmitViewModel_BindableControlsWithNoPromotedHandler_GenerateNoSpeculativeProperties()
    {
        // A TextBox/CheckBox/NumericUpDown/ProgressBar on the form is not, by itself, evidence
        // that anything needs a ViewModel property - the AXAML would never bind it.
        var formModel = FormWith(("nameTextBox", "TextBox"), ("agreeCheckBox", "CheckBox"), ("countUpDown", "NumericUpDown"));
        formModel.Controls["nameTextBox"].Properties["Text"] = new PropertyValue.Literal("");
        formModel.Controls["agreeCheckBox"].Properties["Checked"] = new PropertyValue.Literal(false);

        var source = EmitFor(formModel, "namespace Demo { public partial class Form1 : Form { } }");

        Assert.Empty(SingleClass(source).Members);
    }

    [Fact]
    public void EmitViewModel_TimerAndFileDialogComponents_StayOutOfTheViewModel()
    {
        // Both need something only the View has: a DispatcherTimer is not a control, and
        // StorageProvider hangs off the TopLevel - so they are emitted in code-behind instead.
        var formModel = FormWith(("refreshTimer", "Timer"), ("openFileDialog1", "OpenFileDialog"));
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

        Assert.Empty(SingleClass(source).Members);
        Assert.DoesNotContain("DispatcherTimer", source);
        Assert.DoesNotContain("StorageProvider", source);
    }

    [Fact]
    public void EmitViewModel_RelativeFolder_ProducesNestedNamespace()
    {
        var source = new ViewModelEmitter().EmitViewModel(FormMigrationPlan.Empty, "Demo", "Admin/Users", "UserViewModel");

        Assert.Contains("namespace Demo.ViewModels.Admin.Users;", source);
        ParseAndAssertNoErrors(source);
    }

    private static string EmitFor(FormModel formModel, string codeBehindSource)
    {
        var codeBehind = new CodeBehindAnalyzer().Analyze(codeBehindSource, "Form1.cs", formModel);
        var plan = new FormMigrationPlanner(new ControlMappingRegistry(), new EventMappingRegistry()).Plan(formModel, codeBehind);
        return new ViewModelEmitter().EmitViewModel(plan, "Demo", "", "Form1ViewModel");
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
