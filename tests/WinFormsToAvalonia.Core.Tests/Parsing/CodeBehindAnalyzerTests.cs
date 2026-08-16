using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class CodeBehindAnalyzerTests
{
    [Fact]
    public void Analyze_SharedSenderCastingHandler_RecordsSenderUsage()
    {
        // The real samples/WinForms/WinForms-Control-Click shape: one handler wired to six
        // controls, casting sender - the canonical "can never be a RelayCommand" case.
        var formModel = FormWithControls(("button1", "Button"), ("textBox1", "TextBox"));
        const string source = """
            using System;
            using System.Windows.Forms;

            namespace Demo
            {
                public partial class Form1 : Form
                {
                    public Form1()
                    {
                        InitializeComponent();
                    }

                    private void controlClick(object sender, EventArgs e)
                    {
                        var control = (Control)sender;
                        MessageBox.Show("Clicked:" + control.Name);
                    }
                }
            }
            """;
        formModel.Controls["button1"].Events.Add(new EventHandlerBinding("Click", "controlClick", null));

        var model = new CodeBehindAnalyzer().Analyze(source, "Form1.cs", formModel);

        var handler = Assert.Single(model.HandlerMethods);
        Assert.Equal("controlClick", handler.MethodName);
        Assert.True(handler.UsesSender);
        Assert.False(handler.UsesEventArgs);
        Assert.False(handler.IsAsync);
        Assert.Equal("EventArgs", handler.EventArgsTypeName);
        Assert.Contains("MessageBox.Show", handler.BodyText);
        Assert.StartsWith("var control = (Control)sender;", handler.BodyText);
    }

    [Fact]
    public void Analyze_DragOverHandler_RecordsEventArgsUsageAndControlMemberAccesses()
    {
        var formModel = FormWithControls(("treeView1", "TreeView"));
        const string source = """
            using System.Windows.Forms;

            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private void treeView_DragOver(object sender, DragEventArgs e)
                    {
                        var tree = (TreeView)sender;
                        e.Effect = DragDropEffects.None;
                        this.treeView1.Nodes.Add("x");
                    }
                }
            }
            """;
        formModel.Controls["treeView1"].Events.Add(new EventHandlerBinding("DragOver", "treeView_DragOver", null));

        var handler = Assert.Single(new CodeBehindAnalyzer().Analyze(source, "Form1.cs", formModel).HandlerMethods);

        Assert.True(handler.UsesSender);
        Assert.True(handler.UsesEventArgs);
        Assert.Equal("DragEventArgs", handler.EventArgsTypeName);
        Assert.Contains("treeView1", handler.ReferencedControlFields);
        Assert.Equal(["Nodes"], handler.ControlMemberAccesses["treeView1"]);
    }

    [Fact]
    public void Analyze_PureValuePropertyHandler_HasNoPromotionBlockers()
    {
        var formModel = FormWithControls(("nameTextBox", "TextBox"), ("greetingLabel", "Label"));
        const string source = """
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
            """;
        formModel.Controls["nameTextBox"].Events.Add(new EventHandlerBinding("Click", "okButton_Click", null));

        var handler = Assert.Single(new CodeBehindAnalyzer().Analyze(source, "Form1.cs", formModel).HandlerMethods);

        Assert.False(handler.UsesSender);
        Assert.False(handler.UsesEventArgs);
        Assert.False(handler.CreatesOtherForms);
        Assert.Empty(handler.TouchedFormMembers);
        Assert.Equal(["Text"], handler.ControlMemberAccesses["nameTextBox"]);
        Assert.Equal(["Text"], handler.ControlMemberAccesses["greetingLabel"]);
    }

    [Fact]
    public void Analyze_AsyncHandlerUsingFormMembersAndOtherForms_RecordsBothBlockers()
    {
        // The samples/WinForms/WarehouseApp LoginForm shape.
        var formModel = FormWithControls(("loginButton", "Button"), ("usernameTextBox", "TextBox"));
        const string source = """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private async void loginButton_Click(object? sender, EventArgs e)
                    {
                        SetBusy(true);
                        var name = usernameTextBox.Text.Trim();
                        Hide();
                        using var dashboard = new DashboardForm();
                        dashboard.ShowDialog();
                        Close();
                    }

                    private void SetBusy(bool busy)
                    {
                        loginButton.Enabled = !busy;
                    }
                }
            }
            """;
        formModel.Controls["loginButton"].Events.Add(new EventHandlerBinding("Click", "loginButton_Click", null));

        var model = new CodeBehindAnalyzer().Analyze(source, "Form1.cs", formModel);
        var handler = Assert.Single(model.HandlerMethods);

        Assert.True(handler.IsAsync);
        Assert.True(handler.CreatesOtherForms);
        Assert.Contains("Hide", handler.TouchedFormMembers);
        Assert.Contains("Close", handler.TouchedFormMembers);
        Assert.Equal(["SetBusy"], handler.CalledHelperMethods);

        var helper = Assert.Single(model.HelperMembers);
        Assert.Equal("SetBusy", helper.Name);
        Assert.Equal(HelperMemberKind.Method, helper.Kind);
        Assert.StartsWith("private void SetBusy(bool busy)", helper.SourceText);
    }

    [Fact]
    public void Analyze_RuntimeSubscription_PromotesTargetMethodToHandlerAndKeepsFieldAsHelper()
    {
        // samples/WinForms/WinForms-User-Control-Progress-Bar: the Tick handler is wired at
        // runtime from Form1_Load, so it never appears in InitializeComponent().
        var formModel = FormWithControls(("buttonStart", "Button"));
        const string source = """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    private Timer timer;

                    private void Form1_Load(object sender, EventArgs e)
                    {
                        this.timer = new Timer {Interval = 1000};
                        this.timer.Tick += this.Timer_Tick;
                    }

                    private void Timer_Tick(object sender, EventArgs e)
                    {
                        this.timer.Enabled = false;
                    }
                }
            }
            """;
        formModel.FormEvents.Add(new EventHandlerBinding("Load", "Form1_Load", null));

        var model = new CodeBehindAnalyzer().Analyze(source, "Form1.cs", formModel);

        var subscription = Assert.Single(model.RuntimeEventSubscriptions);
        Assert.Equal("timer", subscription.TargetFieldName);
        Assert.Equal("Tick", subscription.EventName);
        Assert.Equal("Timer_Tick", subscription.HandlerMethodName);

        Assert.Equal(["Form1_Load", "Timer_Tick"], model.HandlerMethods.Select(h => h.MethodName).Order());
        Assert.Equal(["timer"], model.HelperMembers.Select(h => h.Name));
    }

    [Fact]
    public void Analyze_ConstructorStatementsBeyondInitializeComponent_ArePreserved()
    {
        var formModel = FormWithControls(("button1", "Button"));
        const string source = """
            namespace Demo
            {
                public partial class Form1 : Form
                {
                    public Form1()
                    {
                        InitializeComponent();
                        this.Text = "Loaded";
                        LoadSettings();
                    }

                    private void LoadSettings()
                    {
                    }
                }
            }
            """;

        var model = new CodeBehindAnalyzer().Analyze(source, "Form1.cs", formModel);

        Assert.Equal(["this.Text = \"Loaded\";", "LoadSettings();"], model.ConstructorExtraStatements);
    }

    [Fact]
    public void Analyze_MissingFile_ReturnsEmptyModel()
    {
        var model = new CodeBehindAnalyzer().Analyze((string?)null, new FormModel { ClassName = "Form1" });

        Assert.Empty(model.HandlerMethods);
        Assert.Empty(model.HelperMembers);
        Assert.Empty(model.ConstructorExtraStatements);
        Assert.Empty(model.RuntimeEventSubscriptions);
    }

    private static FormModel FormWithControls(params (string FieldName, string TypeName)[] controls)
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
}
