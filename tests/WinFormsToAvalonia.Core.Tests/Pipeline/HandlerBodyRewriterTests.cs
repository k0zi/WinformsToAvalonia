using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Pipeline;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Pipeline;

public class HandlerBodyRewriterTests
{
    private static readonly HandlerBodyRewriter Rewriter = new(new ControlMappingRegistry());

    // ---- View target: the control fields still exist -------------------------------------

    [Fact]
    public void RewriteForView_BindablePropertyAssignment_BecomesTheAvaloniaProperty()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView("this.statusLabel.Text = \"Done\";", form);

        Assert.Equal(["statusLabel.Text = \"Done\";"], result.MigratedStatements);
        Assert.True(result.IsComplete);
        Assert.Equal("", result.RemainingBody);
    }

    [Fact]
    public void RewriteForView_PropertyNamesThatDiffer_AreTranslated()
    {
        var form = FormWith(("okButton", "Button"), ("agreeCheckBox", "CheckBox"), ("panel1", "Panel"));

        var result = Rewriter.RewriteForView(
            """
            this.okButton.Text = "Go";
            this.agreeCheckBox.Checked = true;
            this.okButton.Enabled = false;
            """,
            form);

        Assert.Equal(
            [
                "okButton.Content = \"Go\";",
                "agreeCheckBox.IsChecked = true;",
                "okButton.IsEnabled = false;",
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// Reads are null-guarded, writes are not: Avalonia's string properties are `string?` while
    /// WinForms' never return null, so `?? string.Empty` is both the faithful translation and
    /// what keeps the generated project free of CS8604.
    /// </summary>
    [Fact]
    public void RewriteForView_ReadsOfControlPropertiesInsideTheExpression_AreTranslatedAndNullGuarded()
    {
        var form = FormWith(("counterLabel", "Label"));

        var result = Rewriter.RewriteForView(
            "this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();", form);

        Assert.Equal(
            ["counterLabel.Text = (int.Parse((counterLabel.Text ?? string.Empty)) + 1).ToString();"],
            result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_NonStringProperty_IsNotNullGuarded()
    {
        var form = FormWith(("agreeCheckBox", "CheckBox"), ("otherCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView("this.agreeCheckBox.Checked = this.otherCheckBox.Checked;", form);

        Assert.Equal(["agreeCheckBox.IsChecked = otherCheckBox.IsChecked;"], result.MigratedStatements);
    }

    /// <summary>
    /// Every type in BindablePropertyCatalog is a plain BCL type, so a member hanging off one is
    /// ordinary .NET rather than a WinForms API - and the receiver is still null-guarded.
    /// </summary>
    [Fact]
    public void RewriteForView_MemberOfAControlProperty_IsTranslated()
    {
        var form = FormWith(("statusLabel", "Label"), ("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView("this.statusLabel.Text = this.nameTextBox.Text.Trim();", form);

        Assert.Equal(
            ["statusLabel.Text = (nameTextBox.Text ?? string.Empty).Trim();"],
            result.MigratedStatements);
    }

    /// <summary>A property outside the catalog gives no safe receiver, so the chain stops.</summary>
    [Fact]
    public void RewriteForView_MemberOfANonCatalogProperty_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"), ("treeView1", "TreeView"));

        var result = Rewriter.RewriteForView("this.statusLabel.Text = this.treeView1.Nodes.Count.ToString();", form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_BareFieldReferenceWithoutThis_ResolvesTheSameWay()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("greetingLabel", "Label"));

        var result = Rewriter.RewriteForView("greetingLabel.Text = \"Hello, \" + nameTextBox.Text;", form);

        Assert.Equal(
            ["greetingLabel.Text = \"Hello, \" + (nameTextBox.Text ?? string.Empty);"],
            result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_InterpolatedString_TranslatesEveryHoleAndKeepsTheText()
    {
        var form = FormWith(("statusLabel", "Label"), ("amountUpDown", "NumericUpDown"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = $\"Amount: {this.amountUpDown.Value}\";", form);

        Assert.Equal(["statusLabel.Text = $\"Amount: {amountUpDown.Value}\";"], result.MigratedStatements);
    }

    /// <summary>Alignment and format clauses are plain .NET and must survive untouched.</summary>
    [Fact]
    public void RewriteForView_InterpolationWithAlignmentAndFormat_KeepsBothClauses()
    {
        var form = FormWith(("statusLabel", "Label"), ("amountUpDown", "NumericUpDown"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = $\"[{this.amountUpDown.Value,8:N2}]\";", form);

        Assert.Equal(["statusLabel.Text = $\"[{amountUpDown.Value,8:N2}]\";"], result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_InterpolatedStringReadingAStringProperty_NullGuardsTheHole()
    {
        var form = FormWith(("statusLabel", "Label"), ("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = $\"Hi {this.nameTextBox.Text}\";", form);

        Assert.Equal(
            ["statusLabel.Text = $\"Hi {(nameTextBox.Text ?? string.Empty)}\";"],
            result.MigratedStatements);
    }

    /// <summary>One un-translatable hole rejects the whole string - a half-converted message is worse than none.</summary>
    [Fact]
    public void RewriteForView_InterpolationWithAnUntranslatableHole_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"), ("treeView1", "TreeView"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = $\"Nodes: {this.treeView1.Nodes.Count}\";", form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_InterpolatedStringWithEscapedBraces_KeepsThemVerbatim()
    {
        var form = FormWith(("statusLabel", "Label"), ("amountUpDown", "NumericUpDown"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = $\"{{literal}} {this.amountUpDown.Value}\";", form);

        Assert.Equal(["statusLabel.Text = $\"{{literal}} {amountUpDown.Value}\";"], result.MigratedStatements);
    }

    [Theory]
    [InlineData("this.Close();", "Close();")]
    [InlineData("Close();", "Close();")]
    [InlineData("this.Hide();", "IsVisible = false;")]
    [InlineData("this.Show();", "Show();")]
    public void RewriteForView_FormLifetimeCalls_BecomeTheirWindowEquivalents(string original, string expected)
    {
        var result = Rewriter.RewriteForView(original, FormWith());

        Assert.Equal([expected], result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_ControlFocus_IsTheSameCallOnTheSameField()
    {
        var form = FormWith(("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView("this.nameTextBox.Focus();", form);

        Assert.Equal(["nameTextBox.Focus();"], result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_ApplicationExit_GoesThroughTheDesktopLifetime()
    {
        var result = Rewriter.RewriteForView("Application.Exit();", FormWith());

        Assert.Equal(
            ["(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();"],
            result.MigratedStatements);
        Assert.Contains("Avalonia.Controls.ApplicationLifetimes", result.RequiredUsings);
    }

    [Fact]
    public void RewriteForView_MessageBoxShow_BecomesAnAwaitedFallbackDialog()
    {
        var result = Rewriter.RewriteForView("MessageBox.Show(\"Saved\", \"Info\");", FormWith());

        Assert.Equal(["await MessageBoxFallback.ShowAsync(this, \"Saved\", \"Info\");"], result.MigratedStatements);
        Assert.True(result.RequiresAsync);
        Assert.Contains("MessageBoxFallback", result.RequiredFallbackKeys);
    }

    [Fact]
    public void RewriteForView_MessageBoxShowWithOnlyText_DefaultsTheCaption()
    {
        var result = Rewriter.RewriteForView("MessageBox.Show(\"Saved\");", FormWith());

        Assert.Equal(["await MessageBoxFallback.ShowAsync(this, \"Saved\", \"\");"], result.MigratedStatements);
    }

    /// <summary>
    /// The button/icon overloads return a DialogResult the caller normally inspects; translating
    /// only the call would silently change what the handler does.
    /// </summary>
    [Fact]
    public void RewriteForView_MessageBoxShowWithButtonsOverload_IsLeftAlone()
    {
        var result = Rewriter.RewriteForView(
            "MessageBox.Show(\"Sure?\", \"Confirm\", MessageBoxButtons.YesNo);", FormWith());

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Window navigation ---------------------------------------------------------------

    [Fact]
    public void RewriteForView_ShowDialogOnAnotherForm_OpensTheGeneratedViewModally()
    {
        var result = Rewriter.RewriteForView("new SettingsForm().ShowDialog();", FormWith(), Navigation());

        Assert.Equal(["await new SettingsView().ShowDialog(this);"], result.MigratedStatements);
        Assert.True(result.RequiresAsync);
        Assert.Contains("Demo.Views.Dialogs", result.RequiredUsings);
    }

    /// <summary>WinForms' ShowDialog(owner) - the translated call supplies the owner itself.</summary>
    [Fact]
    public void RewriteForView_ShowDialogWithAnExplicitOwner_IsStillTranslated()
    {
        var result = Rewriter.RewriteForView("new SettingsForm().ShowDialog(this);", FormWith(), Navigation());

        Assert.Equal(["await new SettingsView().ShowDialog(this);"], result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_ShowOnAnotherForm_OpensTheGeneratedViewModelessly()
    {
        var result = Rewriter.RewriteForView("new SettingsForm().Show();", FormWith(), Navigation());

        Assert.Equal(["new SettingsView().Show();"], result.MigratedStatements);
        Assert.False(result.RequiresAsync);
    }

    [Fact]
    public void RewriteForView_FormThatIsNotPartOfThisConversion_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("new ThirdPartyForm().ShowDialog();", FormWith(), Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>The generated View constructor takes no arguments, so a Form built with any is left alone.</summary>
    [Fact]
    public void RewriteForView_FormConstructedWithArguments_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("new SettingsForm(userId).ShowDialog();", FormWith(), Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// The shape most WinForms code actually uses. Avalonia's ShowDialog returns a Task whose
    /// result is whatever the dialog passed to Close(), so translating the call without the
    /// branch around it would silently change the control flow.
    /// </summary>
    [Fact]
    public void RewriteForView_ShowDialogInsideAnIfCondition_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "if (new SettingsForm().ShowDialog() == DialogResult.OK) { }", FormWith(), Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A converted UserControl is not a Window, so it cannot own a modal dialog.</summary>
    [Fact]
    public void RewriteForView_ShowDialogFromAUserControlView_IsNotMigratedButShowStillIs()
    {
        var inUserControl = Navigation(hostIsWindow: false);

        Assert.Empty(Rewriter.RewriteForView("new SettingsForm().ShowDialog();", FormWith(), inUserControl).MigratedStatements);
        Assert.Equal(
            ["new SettingsView().Show();"],
            Rewriter.RewriteForView("new SettingsForm().Show();", FormWith(), inUserControl).MigratedStatements);
    }

    [Fact]
    public void RewriteForViewModel_FormNavigation_IsNeverMigrated()
    {
        var result = Rewriter.RewriteForViewModel("new SettingsForm().Show();", FormWith(), []);

        Assert.Empty(result.MigratedStatements);
    }

    private static ViewNavigationContext Navigation(bool hostIsWindow = true) =>
        new(
            new Dictionary<string, FormViewInfo>(StringComparer.Ordinal)
            {
                ["SettingsForm"] = new("SettingsForm", "SettingsView", "Demo.Views.Dialogs"),
            },
            hostIsWindow);

    // ---- What must NOT be migrated -------------------------------------------------------

    [Fact]
    public void RewriteForView_PropertyOutsideTheBindableCatalog_IsNotMigrated()
    {
        var form = FormWith(("treeView1", "TreeView"));

        var result = Rewriter.RewriteForView("this.treeView1.Nodes.Add(\"x\");", form);

        Assert.Empty(result.MigratedStatements);
        Assert.Equal("this.treeView1.Nodes.Add(\"x\");", result.RemainingBody);
    }

    /// <summary>
    /// A fallback control is one of this tool's own templates and does not necessarily expose
    /// the property the catalog names - the same reasoning that stops it being event-wired.
    /// </summary>
    [Fact]
    public void RewriteForView_PropertyOnAFallbackMappedControl_IsNotMigrated()
    {
        var form = FormWith(("statusStrip1", "StatusStrip"));

        var result = Rewriter.RewriteForView("this.statusStrip1.Visible = false;", form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_CallToAHelperMethod_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("SetBusy(true);", FormWith());

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_LocalVariableDeclaration_IsNotMigrated()
    {
        var form = FormWith(("label1", "Label"));

        var result = Rewriter.RewriteForView("var text = label1.Text;", form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_UnknownStaticReceiver_IsNotMigrated()
    {
        var form = FormWith(("label1", "Label"));

        var result = Rewriter.RewriteForView("label1.Text = Clipboard.GetText();", form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_UnparseableBody_IsLeftEntirelyAlone()
    {
        var result = Rewriter.RewriteForView("this.label1.Text = ;;;", FormWith(("label1", "Label")));

        Assert.Empty(result.MigratedStatements);
        Assert.Equal("this.label1.Text = ;;;", result.RemainingBody);
    }

    // ---- The prefix rule -----------------------------------------------------------------

    /// <summary>
    /// Migrating statements 1 and 3 while dropping 2 would produce a method that looks migrated
    /// but silently skips work, so migration stops at the first statement it cannot translate.
    /// </summary>
    [Fact]
    public void RewriteForView_UnmigratableStatementInTheMiddle_StopsThereAndKeepsTheRestVerbatim()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            this.statusLabel.Text = "Working";
            DoTheWork();
            this.statusLabel.Text = "Done";
            """,
            form);

        Assert.Equal(["statusLabel.Text = \"Working\";"], result.MigratedStatements);
        Assert.False(result.IsComplete);
        Assert.Equal(
            """
            DoTheWork();
            this.statusLabel.Text = "Done";
            """,
            result.RemainingBody);
        Assert.Equal(3, result.TotalStatementCount);
    }

    [Fact]
    public void RewriteForView_EmptyBody_MigratesNothing()
    {
        var result = Rewriter.RewriteForView("", FormWith());

        Assert.Empty(result.MigratedStatements);
        Assert.False(result.IsComplete);
    }

    // ---- ViewModel target: no controls, only [ObservableProperty] ------------------------

    [Fact]
    public void RewriteForViewModel_ControlPropertyAccess_BecomesTheBoundViewModelProperty()
    {
        var form = FormWith(("counterLabel", "Label"));
        var bound = new[] { new BoundPropertyPlan("counterLabel", "Text", "CounterLabelText", "string", "") };

        var result = Rewriter.RewriteForViewModel(
            "this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();", form, bound);

        Assert.Equal(["CounterLabelText = (int.Parse(CounterLabelText) + 1).ToString();"], result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    /// <summary>A ViewModel has no Window to close and no owner for a dialog.</summary>
    [Theory]
    [InlineData("this.Close();")]
    [InlineData("MessageBox.Show(\"hi\");")]
    public void RewriteForViewModel_WindowAndDialogApis_AreNotMigrated(string body)
    {
        var result = Rewriter.RewriteForViewModel(body, FormWith(), []);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForViewModel_PropertyWithNoBoundPlan_IsNotMigrated()
    {
        var form = FormWith(("label1", "Label"));

        var result = Rewriter.RewriteForViewModel("label1.Text = \"x\";", form, []);

        Assert.Empty(result.MigratedStatements);
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
}
