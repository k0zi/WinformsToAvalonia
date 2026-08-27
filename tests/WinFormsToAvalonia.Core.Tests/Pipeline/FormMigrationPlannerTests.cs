using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Pipeline;

public class FormMigrationPlannerTests
{
    private static readonly FormMigrationPlanner Planner = new(new ControlMappingRegistry(), new EventMappingRegistry());

    [Fact]
    public void Plan_ClickHandlerTouchingOnlyBindableProperties_IsPromotedWithItsBindings()
    {
        var formModel = FormWith(("okButton", "Button"), ("nameTextBox", "TextBox"), ("greetingLabel", "Label"));
        Wire(formModel, "okButton", "Click", "okButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        greetingLabel.Text = "Hello, " + nameTextBox.Text;
                    }
                }
            }
            """);

        var command = Assert.Single(plan.ViewModelCommands);
        Assert.Equal("OkButton", command.CommandMethodName);
        Assert.Equal("OkButtonCommand", command.CommandPropertyName);
        Assert.Equal("okButton", command.ControlFieldName);
        Assert.Contains("greetingLabel.Text", command.OriginalBody);
        Assert.Empty(plan.CodeBehindHandlers);

        Assert.Equal("OkButtonCommand", plan.CommandPropertyFor("okButton"));
        Assert.Equal(
            [("greetingLabel", "Text", "GreetingLabelText"), ("nameTextBox", "Text", "NameTextBoxText")],
            plan.BoundProperties
                .Select(p => (p.ControlFieldName, p.AvaloniaPropertyName, p.ViewModelPropertyName))
                .OrderBy(p => p.ControlFieldName, StringComparer.Ordinal));
    }

    [Fact]
    public void Plan_ClickHandlerUsingSender_StaysInCodeBehindWithAReason()
    {
        var formModel = FormWith(("button1", "Button"));
        Wire(formModel, "button1", "Click", "button1_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void button1_Click(object sender, EventArgs e)
                    {
                        var control = (Control)sender;
                    }
                }
            }
            """);

        Assert.Empty(plan.ViewModelCommands);
        var handler = Assert.Single(plan.CodeBehindHandlers);
        Assert.Equal("button1_Click", handler.MethodName);
        Assert.Equal("RoutedEventArgs", handler.EventArgsTypeName);
        Assert.Equal([("Click", "button1_Click")], plan.XamlEventAttributesFor("button1"));
        Assert.Contains(plan.Warnings, w => w.Contains("'sender'"));
    }

    [Fact]
    public void Plan_HandlerSharedAcrossControlsWithDifferentAvaloniaEvents_IsSplitPerSignature()
    {
        // samples/WinForms/WinForms-Control-Click: one handler on a Button (real Click) and on a
        // TextBox/Label (PointerPressed) - two incompatible delegate signatures.
        var formModel = FormWith(("button1", "Button"), ("textBox1", "TextBox"), ("label1", "Label"));
        Wire(formModel, "button1", "Click", "controlClick");
        Wire(formModel, "textBox1", "Click", "controlClick");
        Wire(formModel, "label1", "Click", "controlClick");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void controlClick(object sender, EventArgs e)
                    {
                        var control = (Control)sender;
                        MessageBox.Show("Clicked:" + control.Name);
                    }
                }
            }
            """);

        Assert.Empty(plan.ViewModelCommands);
        Assert.Equal(
            ["controlClick_Click", "controlClick_PointerPressed"],
            plan.CodeBehindHandlers.Select(h => h.MethodName).Order());

        Assert.Equal([("Click", "controlClick_Click")], plan.XamlEventAttributesFor("button1"));
        Assert.Equal([("PointerPressed", "controlClick_PointerPressed")], plan.XamlEventAttributesFor("label1"));
        Assert.All(plan.CodeBehindHandlers, h => Assert.Contains("MessageBox.Show", h.OriginalBody));
        Assert.Contains(plan.Warnings, w => w.Contains("different signatures"));
    }

    [Fact]
    public void Plan_DragDropHandlers_UseAttachedDragDropAttributes()
    {
        var formModel = FormWith(("treeView1", "TreeView"));
        Wire(formModel, "treeView1", "DragDrop", "treeView_DragDrop");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void treeView_DragDrop(object sender, DragEventArgs e)
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                }
            }
            """);

        var handler = Assert.Single(plan.CodeBehindHandlers);
        Assert.Equal("DragEventArgs", handler.EventArgsTypeName);
        Assert.Equal([("DragDrop.Drop", "treeView_DragDrop")], plan.XamlEventAttributesFor("treeView1"));
    }

    [Fact]
    public void Plan_TwoWinFormsEventsCollapsingOntoOneAvaloniaEvent_SubscribesOnlyTheExactOne()
    {
        // samples/WinForms/WinForms-Event-Tracker: a PictureBox's Click and MouseDown both become
        // PointerPressed, and two identically-named XML attributes fail the Avalonia XAML parser.
        var formModel = FormWith(("pictureBoxMouse", "PictureBox"));
        Wire(formModel, "pictureBoxMouse", "Click", "pictureBoxMouse_Click");
        Wire(formModel, "pictureBoxMouse", "MouseDown", "pictureBoxMouse_MouseDown");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void pictureBoxMouse_Click(object sender, EventArgs e) { }
                    private void pictureBoxMouse_MouseDown(object sender, MouseEventArgs e) { }
                }
            }
            """);

        // MouseDown is the exact mapping; Click only falls back to PointerPressed.
        Assert.Equal([("PointerPressed", "pictureBoxMouse_MouseDown")], plan.XamlEventAttributesFor("pictureBoxMouse"));

        // Both methods still exist - only the second subscription is dropped.
        Assert.Equal(
            ["pictureBoxMouse_Click", "pictureBoxMouse_MouseDown"],
            plan.CodeBehindHandlers.Select(h => h.MethodName).Order());
        Assert.Contains(plan.Warnings, w => w.Contains("same Avalonia event 'PointerPressed'"));
    }

    [Fact]
    public void Plan_EventWithNoAvaloniaEquivalent_EmitsAnUnsubscribedMethodAndAWarning()
    {
        var formModel = FormWith(("panel1", "Panel"));
        Wire(formModel, "panel1", "Paint", "panel1_Paint");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void panel1_Paint(object sender, PaintEventArgs e)
                    {
                    }
                }
            }
            """);

        var handler = Assert.Single(plan.CodeBehindHandlers);
        Assert.Empty(handler.Subscriptions);
        Assert.Empty(plan.XamlEventAttributesFor("panel1"));
        Assert.Contains(plan.Warnings, w => w.Contains("no Avalonia equivalent"));
    }

    [Fact]
    public void Plan_FormLoadHandler_SubscribesTheWindowsLoadedEvent()
    {
        var formModel = FormWith(("button1", "Button"));
        formModel.FormEvents.Add(new EventHandlerBinding("Load", "Form1_Load", null));

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void Form1_Load(object sender, EventArgs e)
                    {
                        button1.Text = "ready";
                    }
                }
            }
            """);

        var handler = Assert.Single(plan.CodeBehindHandlers);
        Assert.Equal("RoutedEventArgs", handler.EventArgsTypeName);
        Assert.Equal([("Loaded", "Form1_Load")], plan.XamlEventAttributesFor(null));
        Assert.Empty(plan.ViewModelCommands);
    }

    [Fact]
    public void Plan_HandlerCallingACodeBehindHelper_IsNotPromotedAndTheHelperIsPreserved()
    {
        var formModel = FormWith(("loginButton", "Button"));
        Wire(formModel, "loginButton", "Click", "loginButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void loginButton_Click(object sender, EventArgs e)
                    {
                        SetBusy(true);
                    }

                    private void SetBusy(bool busy)
                    {
                        loginButton.Enabled = !busy;
                    }
                }
            }
            """);

        // Everything the helper touches is bindable, so the handler and the helper move to the
        // ViewModel together - which is what relaxing promotion condition 5 buys.
        var command = Assert.Single(plan.ViewModelCommands);
        Assert.Equal(["SetBusy(true);"], command.Rewrite!.MigratedStatements);

        var helper = Assert.Single(plan.ViewModelHelpers);
        Assert.Equal("SetBusy", helper.Name);
        Assert.Equal(["LoginButtonIsEnabled = !busy;"], helper.Rewrite.MigratedStatements);

        // The helper's own control access is what made that property bindable at all - nothing in
        // the handler itself ever names loginButton.
        Assert.Contains(plan.BoundProperties, p => p.ViewModelPropertyName == "LoginButtonIsEnabled");
        Assert.Empty(plan.CodeBehindHandlers);
    }

    /// <summary>
    /// A helper whose translation is a property write wearing a method's clothes:
    /// <c>AppendText</c> is a write to <c>Text</c>, so it survives on a ViewModel where
    /// <c>Focus()</c> would not.
    /// </summary>
    [Fact]
    public void Plan_HelperCallingAControlMethodThatIsReallyAPropertyWrite_MovesToTheViewModel()
    {
        var formModel = FormWith(("okButton", "Button"), ("logTextBox", "TextBox"));
        Wire(formModel, "okButton", "Click", "okButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        Log("clicked");
                    }

                    private void Log(string message)
                    {
                        logTextBox.AppendText(message);
                    }
                }
            }
            """);

        Assert.Single(plan.ViewModelCommands);
        var helper = Assert.Single(plan.ViewModelHelpers);
        Assert.Equal(["LogTextBoxText += message;"], helper.Rewrite.MigratedStatements);
    }

    /// <summary>
    /// The relaxed condition 5 is still a condition: a helper reaching something a ViewModel has
    /// no answer for keeps its caller in code-behind, exactly as before.
    /// </summary>
    [Fact]
    public void Plan_HelperTouchingSomethingUnbindable_StillBlocksItsCaller()
    {
        var formModel = FormWith(("okButton", "Button"), ("treeView1", "TreeView"));
        Wire(formModel, "okButton", "Click", "okButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        Refresh1();
                    }

                    private void Refresh1()
                    {
                        treeView1.Nodes.Clear();
                    }
                }
            }
            """);

        Assert.Empty(plan.ViewModelCommands);
        Assert.Empty(plan.ViewModelHelpers);
        Assert.Contains(plan.Warnings, w => w.Contains("treeView1.Nodes"));
    }

    /// <summary>
    /// A helper whose name is already taken by the generated class's base type cannot be emitted
    /// on either target - so it counts as unanalysable, and its caller stays in code-behind rather
    /// than being promoted into a call that reaches nothing.
    /// </summary>
    [Fact]
    public void Plan_HelperNamedLikeAnInheritedMember_IsNotPromotedAnywhere()
    {
        var formModel = FormWith(("okButton", "Button"), ("statusLabel", "Label"));
        Wire(formModel, "okButton", "Click", "okButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        Tag("done");
                    }

                    private void Tag(string what)
                    {
                        statusLabel.Text = what;
                    }
                }
            }
            """);

        Assert.Empty(plan.ViewModelCommands);
        Assert.Empty(plan.ViewModelHelpers);
        Assert.Empty(plan.PromotedHelpers);
        Assert.Equal(["Tag"], plan.PreservedMembers.Select(m => m.Name));
    }

    /// <summary>A helper driving the Form itself takes its caller down with it.</summary>
    [Fact]
    public void Plan_HelperDrivingTheForm_StillBlocksItsCaller()
    {
        var formModel = FormWith(("okButton", "Button"));
        Wire(formModel, "okButton", "Click", "okButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        Finish();
                    }

                    private void Finish()
                    {
                        this.Close();
                    }
                }
            }
            """);

        Assert.Empty(plan.ViewModelCommands);
        Assert.Contains(plan.Warnings, w => w.Contains("drives the Form itself"));
    }

    /// <summary>
    /// The canonical WinForms pair: a helper maintaining a private flag. Without the field the
    /// helper cannot translate, so neither can any handler that calls it - which is why the field
    /// is carried over too.
    /// </summary>
    [Fact]
    public void Plan_HelperMaintainingAPrivateField_CarriesTheFieldOverToo()
    {
        var formModel = FormWith(("loginButton", "Button"));
        Wire(formModel, "loginButton", "Click", "loginButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private bool isBusy;

                    private void loginButton_Click(object sender, EventArgs e)
                    {
                        SetBusy(true);
                    }

                    private void SetBusy(bool busy)
                    {
                        this.isBusy = busy;
                        loginButton.Enabled = !busy;
                    }
                }
            }
            """);

        Assert.Equal(["isBusy"], plan.PromotedFields.Select(f => f.Name));
        Assert.Equal(["SetBusy"], plan.PromotedHelpers.Select(h => h.Name));
        Assert.Equal(
            ["isBusy = busy;", "loginButton.IsEnabled = !busy;"],
            plan.PromotedHelpers[0].Rewrite.MigratedStatements);
        Assert.Empty(plan.PreservedMembers);
    }

    /// <summary>
    /// A field whose type is not a keyword type could be a WinForms type whose Avalonia
    /// counterpart is something else entirely, and nothing here can tell without a semantic model.
    /// </summary>
    [Fact]
    public void Plan_PrivateFieldOfANamedType_IsNotCarriedOver()
    {
        var formModel = FormWith(("loginButton", "Button"));
        Wire(formModel, "loginButton", "Click", "loginButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private Font busyFont;

                    private void loginButton_Click(object sender, EventArgs e)
                    {
                        loginButton.Enabled = false;
                    }
                }
            }
            """);

        Assert.Empty(plan.PromotedFields);
        Assert.Equal(["busyFont"], plan.PreservedMembers.Select(m => m.Name));
    }

    /// <summary>
    /// A helper is emitted as code only when its <b>whole</b> body translates. A prefix would be
    /// dishonest in a way a handler's prefix is not: at the call site there would be nothing at
    /// all to say that half the work was dropped.
    /// </summary>
    [Fact]
    public void Plan_HelperThatOnlyPartlyTranslates_StaysAComment()
    {
        var formModel = FormWith(("loginButton", "Button"), ("treeView1", "TreeView"));
        Wire(formModel, "loginButton", "Click", "loginButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void loginButton_Click(object sender, EventArgs e)
                    {
                        SetBusy(true);
                    }

                    private void SetBusy(bool busy)
                    {
                        loginButton.Enabled = !busy;
                        treeView1.Nodes.Clear();
                    }
                }
            }
            """);

        Assert.Empty(plan.PromotedHelpers);
        Assert.Equal(["SetBusy"], plan.PreservedMembers.Select(m => m.Name));

        // And the call to it therefore does not translate either.
        var handler = Assert.Single(plan.CodeBehindHandlers);
        Assert.Empty(handler.Rewrite!.MigratedStatements);
    }

    [Fact]
    public void Plan_UnsupportedControl_BlocksPromotionEvenForAnOtherwisePureBody()
    {
        // ErrorProvider has no Direct mapping, so there is no element for a {Binding} to attach to.
        var formModel = FormWith(("okButton", "Button"), ("errorProvider1", "ErrorProvider"));
        Wire(formModel, "okButton", "Click", "okButton_Click");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        errorProvider1.Enabled = false;
                    }
                }
            }
            """);

        Assert.Empty(plan.ViewModelCommands);
        Assert.Empty(plan.BoundProperties);
        Assert.Contains(plan.Warnings, w => w.Contains("errorProvider1"));
    }

    private static FormMigrationPlan PlanFor(FormModel formModel, string codeBehindSource)
    {
        var codeBehind = new CodeBehindAnalyzer().Analyze(codeBehindSource, "Form1.cs", formModel);
        return Planner.Plan(formModel, codeBehind);
    }

    /// <summary>
    /// The canonical "enable the button when the input is valid" WinForms idiom. In MVVM that is
    /// a CanExecute guard, and the handler that maintained it imperatively is redundant.
    /// </summary>
    [Fact]
    public void Plan_HandlerThatOnlyKeepsEnabledInSync_BecomesTheCommandsCanExecuteGuard()
    {
        var formModel = FormWith(("okButton", "Button"), ("nameTextBox", "TextBox"));
        Wire(formModel, "okButton", "Click", "okButton_Click");
        Wire(formModel, "nameTextBox", "TextChanged", "nameTextBox_TextChanged");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        nameTextBox.Text = "sent";
                    }

                    private void nameTextBox_TextChanged(object sender, EventArgs e)
                    {
                        okButton.Enabled = nameTextBox.Text.Length > 0;
                    }
                }
            }
            """);

        var command = Assert.Single(plan.ViewModelCommands);
        Assert.Equal("NameTextBoxText.Length > 0", command.CanExecuteExpression);
        Assert.Equal("CanOkButton", command.CanExecuteMethodName);

        // The handler is gone, and so is its subscription - the bindings and
        // NotifyCanExecuteChangedFor now do its job declaratively.
        Assert.DoesNotContain(plan.CodeBehindHandlers, h => h.OriginalMethodName == "nameTextBox_TextChanged");
        Assert.Empty(plan.XamlEventAttributesFor("nameTextBox"));

        var bound = Assert.Single(plan.BoundProperties, p => p.ViewModelPropertyName == "NameTextBoxText");
        Assert.Equal(["OkButtonCommand"], bound.NotifiesCommands);

        Assert.Contains(plan.Warnings, w => w.Contains("CanExecute guard", StringComparison.Ordinal));
    }

    /// <summary>
    /// A handler that also does something else keeps its imperative write and gets no guard:
    /// splitting such a body in two would be exactly the unprovable rewrite this converter avoids.
    /// </summary>
    [Fact]
    public void Plan_HandlerThatDoesMoreThanKeepEnabledInSync_KeepsItsHandlerAndGetsNoGuard()
    {
        var formModel = FormWith(("okButton", "Button"), ("nameTextBox", "TextBox"), ("statusLabel", "Label"));
        Wire(formModel, "okButton", "Click", "okButton_Click");
        Wire(formModel, "nameTextBox", "TextChanged", "nameTextBox_TextChanged");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void okButton_Click(object sender, EventArgs e)
                    {
                        nameTextBox.Text = "sent";
                    }

                    private void nameTextBox_TextChanged(object sender, EventArgs e)
                    {
                        okButton.Enabled = nameTextBox.Text.Length > 0;
                        statusLabel.Text = "typing";
                    }
                }
            }
            """);

        Assert.Null(Assert.Single(plan.ViewModelCommands).CanExecuteExpression);
        Assert.Contains(plan.CodeBehindHandlers, h => h.OriginalMethodName == "nameTextBox_TextChanged");
    }

    [Fact]
    public void Plan_EnabledSyncForAControlWithNoPromotedCommand_IsLeftAlone()
    {
        var formModel = FormWith(("plainButton", "Button"), ("nameTextBox", "TextBox"));
        Wire(formModel, "nameTextBox", "TextChanged", "nameTextBox_TextChanged");

        var plan = PlanFor(formModel, """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void nameTextBox_TextChanged(object sender, EventArgs e)
                    {
                        plainButton.Enabled = nameTextBox.Text.Length > 0;
                    }
                }
            }
            """);

        Assert.Empty(plan.ViewModelCommands);
        Assert.Contains(plan.CodeBehindHandlers, h => h.OriginalMethodName == "nameTextBox_TextChanged");
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

    private static void Wire(FormModel formModel, string fieldName, string eventName, string handlerMethodName) =>
        formModel.Controls[fieldName].Events.Add(new EventHandlerBinding(eventName, handlerMethodName, null));
}
