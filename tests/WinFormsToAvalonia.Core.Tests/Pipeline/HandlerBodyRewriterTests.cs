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

    /// <summary>Zero-argument control methods with an exact Avalonia equivalent.</summary>
    [Theory]
    [InlineData("TextBox", "Focus", "nameTextBox.Focus();")]
    [InlineData("TextBox", "Select", "nameTextBox.Focus();")]
    [InlineData("TextBox", "Clear", "nameTextBox.Clear();")]
    [InlineData("TextBox", "SelectAll", "nameTextBox.SelectAll();")]
    [InlineData("TextBox", "Invalidate", "nameTextBox.InvalidateVisual();")]
    [InlineData("TextBox", "Refresh", "nameTextBox.InvalidateVisual();")]
    [InlineData("Label", "Hide", "nameTextBox.IsVisible = false;")]
    public void RewriteForView_ControlMethodWithAnExactEquivalent_IsTranslated(
        string typeName, string methodName, string expected)
    {
        var form = FormWith(("nameTextBox", typeName));

        var result = Rewriter.RewriteForView($"this.nameTextBox.{methodName}();", form);

        Assert.Equal([expected], result.MigratedStatements);
    }

    /// <summary>`Clear` is a TextBox method; a Label has no such thing.</summary>
    [Fact]
    public void RewriteForView_TypeSpecificMethodOnTheWrongControl_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView("this.statusLabel.Clear();", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A method with arguments is outside this table, which is zero-argument only.</summary>
    [Fact]
    public void RewriteForView_ControlMethodWithArguments_IsNotMigrated()
    {
        var form = FormWith(("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView("this.nameTextBox.Select(0, 3);", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// RichTextBoxFallback derives from Avalonia's TextBox, so Clear() is a known fact - the same
    /// rule that lets its Text be written.
    /// </summary>
    [Fact]
    public void RewriteForView_MethodOnAFallbackThatExposesIt_IsTranslated()
    {
        var form = FormWith(("notesRichTextBox", "RichTextBox"));

        var result = Rewriter.RewriteForView("this.notesRichTextBox.Clear();", form);

        Assert.Equal(["notesRichTextBox.Clear();"], result.MigratedStatements);
    }

    /// <summary>A component is not a control; nothing about it carries over.</summary>
    [Fact]
    public void RewriteForView_MethodOnANonVisualComponent_IsNotMigrated()
    {
        var form = FormWith(("clockTimer", "Timer"));

        var result = Rewriter.RewriteForView("this.clockTimer.Refresh();", form);

        Assert.Empty(result.MigratedStatements);
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

    // ---- Control flow --------------------------------------------------------------------

    [Fact]
    public void RewriteForView_IfWithATranslatableConditionAndBody_BecomesRealCode()
    {
        var form = FormWith(("statusLabel", "Label"), ("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(
            """
            if (string.IsNullOrWhiteSpace(this.nameTextBox.Text))
            {
                this.statusLabel.Text = "Name is required";
            }
            """,
            form);

        Assert.Equal(
            [
                """
                if (string.IsNullOrWhiteSpace((nameTextBox.Text ?? string.Empty)))
                {
                    statusLabel.Text = "Name is required";
                }
                """,
            ],
            result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void RewriteForView_IfElse_TranslatesBothBranches()
    {
        var form = FormWith(("statusLabel", "Label"), ("agreeCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView(
            """
            if (this.agreeCheckBox.Checked)
            {
                this.statusLabel.Text = "yes";
            }
            else
            {
                this.statusLabel.Text = "no";
            }
            """,
            form);

        Assert.Equal(
            [
                """
                if (agreeCheckBox.IsChecked)
                {
                    statusLabel.Text = "yes";
                }
                else
                {
                    statusLabel.Text = "no";
                }
                """,
            ],
            result.MigratedStatements);
    }

    /// <summary>`else if` stays an `else if` rather than becoming a nested braced block.</summary>
    [Fact]
    public void RewriteForView_ElseIfChain_KeepsItsShape()
    {
        var form = FormWith(("statusLabel", "Label"), ("a", "CheckBox"), ("b", "CheckBox"));

        var result = Rewriter.RewriteForView(
            """
            if (this.a.Checked)
            {
                this.statusLabel.Text = "a";
            }
            else if (this.b.Checked)
            {
                this.statusLabel.Text = "b";
            }
            """,
            form);

        Assert.Contains("else if (b.IsChecked)", Assert.Single(result.MigratedStatements));
    }

    /// <summary>Braces are added even where the original had none, so `else` cannot re-bind.</summary>
    [Fact]
    public void RewriteForView_BracelessIf_GetsBraces()
    {
        var form = FormWith(("statusLabel", "Label"), ("agreeCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView(
            "if (this.agreeCheckBox.Checked) this.statusLabel.Text = \"yes\";", form);

        Assert.Equal(
            [
                """
                if (agreeCheckBox.IsChecked)
                {
                    statusLabel.Text = "yes";
                }
                """,
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// The prefix rule stops at the top level only. Inside a branch it is all-or-nothing: the
    /// un-migrated remainder is emitted *after* the whole statement, so a partly translated
    /// branch would silently drop its own tail with nothing at that spot to say so.
    /// </summary>
    [Fact]
    public void RewriteForView_IfWhoseBranchIsOnlyPartlyTranslatable_IsNotMigratedAtAll()
    {
        var form = FormWith(("statusLabel", "Label"), ("agreeCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView(
            """
            if (this.agreeCheckBox.Checked)
            {
                this.statusLabel.Text = "yes";
                PersistEverything();
            }
            """,
            form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_IfWithAnUntranslatableCondition_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"), ("treeView1", "TreeView"));

        var result = Rewriter.RewriteForView(
            """
            if (this.treeView1.Nodes.Count > 0)
            {
                this.statusLabel.Text = "has nodes";
            }
            """,
            form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_EarlyReturnInsideAnIf_IsTranslated()
    {
        var form = FormWith(("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(
            """
            if (string.IsNullOrEmpty(this.nameTextBox.Text))
            {
                return;
            }
            """,
            form);

        Assert.Contains("return;", Assert.Single(result.MigratedStatements));
    }

    /// <summary>Anything a branch needs propagates out to the whole handler.</summary>
    [Fact]
    public void RewriteForView_MessageBoxInsideAnIf_MakesTheWholeHandlerAsync()
    {
        var form = FormWith(("agreeCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView(
            """
            if (this.agreeCheckBox.Checked)
            {
                MessageBox.Show("Thanks");
            }
            """,
            form);

        Assert.True(result.RequiresAsync);
        Assert.Contains("MessageBoxFallback", result.RequiredFallbackKeys);
        Assert.Contains("await MessageBoxFallback.ShowAsync", Assert.Single(result.MigratedStatements));
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
    /// The shape most WinForms code actually uses. A converted dialog closes with a bool (see
    /// FormMigrationPlanner.PlanDialogResultButtons), so the whole comparison collapses into the
    /// awaited call.
    /// </summary>
    [Fact]
    public void RewriteForView_ShowDialogComparedToOk_BecomesTheAwaitedCall()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (new SettingsForm().ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = "accepted";
            }
            """,
            form,
            Navigation());

        Assert.Contains("if (await new SettingsView().ShowDialog<bool>(this))", Assert.Single(result.MigratedStatements));
        Assert.True(result.RequiresAsync);
        Assert.Contains("Demo.Views.Dialogs", result.RequiredUsings);
    }

    /// <summary>`== OK` and `!= Cancel` both mean "accepted"; the other two mean the opposite.</summary>
    [Theory]
    [InlineData("== DialogResult.OK", "await")]
    [InlineData("!= DialogResult.Cancel", "await")]
    [InlineData("== DialogResult.Cancel", "!await")]
    [InlineData("!= DialogResult.OK", "!await")]
    public void RewriteForView_DialogResultComparisons_AreNegatedCorrectly(string comparison, string expectedPrefix)
    {
        var result = Rewriter.RewriteForView(
            $"if (new SettingsForm().ShowDialog(this) {comparison}) {{ this.Close(); }}",
            FormWith(),
            Navigation());

        Assert.Contains($"if ({expectedPrefix} new SettingsView()", Assert.Single(result.MigratedStatements));
    }

    [Fact]
    public void RewriteForView_DialogResultComparisonOnALocal_IsTranslated()
    {
        var result = Rewriter.RewriteForView(
            """
            var dialog = new SettingsForm();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                this.Close();
            }
            """,
            FormWith(),
            Navigation());

        Assert.Equal(2, result.MigratedStatements.Count);
        Assert.Contains("if (await dialog.ShowDialog<bool>(this))", result.MigratedStatements[1]);
    }

    /// <summary>
    /// A three-way dialog cannot be expressed as a bool, and inventing a wider result type would
    /// change what the converted dialog returns.
    /// </summary>
    [Fact]
    public void RewriteForView_DialogResultComparedToYes_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "if (new SettingsForm().ShowDialog(this) == DialogResult.Yes) { }", FormWith(), Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Component file dialogs ----------------------------------------------------------

    /// <summary>
    /// The one translation that changes an expression's shape: Avalonia has no dialog object to
    /// ask afterwards, so the picker's own return value becomes the selection - bound inline by a
    /// list pattern so nothing has to be inserted before the `if`.
    /// </summary>
    [Fact]
    public void RewriteForView_OpenFileDialogBranch_BecomesAnInlinedPicker()
    {
        var form = FormWith(("openFileDialog1", "OpenFileDialog"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = this.openFileDialog1.FileName;
            }
            """,
            form);

        Assert.Equal(
            [
                """
                if (await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()) is [var openFileDialog1File, ..])
                {
                    statusLabel.Text = openFileDialog1File.Path.LocalPath;
                }
                """,
            ],
            result.MigratedStatements);
        Assert.True(result.RequiresAsync);
        Assert.Contains("Avalonia.Platform.Storage", result.RequiredUsings);
        Assert.Contains("openFileDialog1", result.InlinedDialogFields);
    }

    /// <summary>The save picker returns a single nullable file, so it needs a different pattern.</summary>
    [Fact]
    public void RewriteForView_SaveFileDialogBranch_UsesTheSingleFilePattern()
    {
        var form = FormWith(("saveFileDialog1", "SaveFileDialog"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = this.saveFileDialog1.FileName;
            }
            """,
            form);

        Assert.Contains(
            "if (await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()) is { } saveFileDialog1File)",
            Assert.Single(result.MigratedStatements));
    }

    [Fact]
    public void RewriteForView_FolderBrowserBranch_TranslatesSelectedPath()
    {
        var form = FormWith(("folderBrowserDialog1", "FolderBrowserDialog"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = this.folderBrowserDialog1.SelectedPath;
            }
            """,
            form);

        var statement = Assert.Single(result.MigratedStatements);
        Assert.Contains("OpenFolderPickerAsync(new FolderPickerOpenOptions()) is [var folderBrowserDialog1Folder, ..]", statement);
        Assert.Contains("statusLabel.Text = folderBrowserDialog1Folder.Path.LocalPath;", statement);
    }

    /// <summary>The selection is a pattern variable scoped to the branch, not a lasting object.</summary>
    [Fact]
    public void RewriteForView_DialogPropertyReadAfterTheBranch_IsNotMigrated()
    {
        var form = FormWith(("openFileDialog1", "OpenFileDialog"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = "picked";
            }
            this.statusLabel.Text = this.openFileDialog1.FileName;
            """,
            form);

        Assert.Single(result.MigratedStatements);
        Assert.Contains("openFileDialog1.FileName", result.RemainingBody);
    }

    /// <summary>An `else` runs when the user cancelled; it cannot see the selection.</summary>
    [Fact]
    public void RewriteForView_DialogBranchWithElse_TranslatesBothAndScopesTheSelection()
    {
        var form = FormWith(("openFileDialog1", "OpenFileDialog"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = this.openFileDialog1.FileName;
            }
            else
            {
                this.statusLabel.Text = "cancelled";
            }
            """,
            form);

        var statement = Assert.Single(result.MigratedStatements);
        Assert.Contains("openFileDialog1File.Path.LocalPath", statement);
        Assert.Contains("statusLabel.Text = \"cancelled\";", statement);
    }

    /// <summary>ColorDialog and friends have no Avalonia equivalent at all.</summary>
    [Fact]
    public void RewriteForView_DialogWithNoAvaloniaEquivalent_IsNotMigrated()
    {
        var form = FormWith(("colorDialog1", "ColorDialog"), ("panel1", "Panel"));

        var result = Rewriter.RewriteForView(
            "if (this.colorDialog1.ShowDialog(this) == DialogResult.OK) { this.panel1.Visible = true; }",
            form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>Multi-select has no single path to bind, so the whole branch is left alone.</summary>
    [Fact]
    public void RewriteForView_DialogFileNamesCollection_IsNotMigrated()
    {
        var form = FormWith(("openFileDialog1", "OpenFileDialog"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = this.openFileDialog1.FileNames[0];
            }
            """,
            form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A failed statement must not leave the method marked async: the requirements an expression
    /// records are rolled back with it.
    /// </summary>
    [Fact]
    public void RewriteForView_DialogComparisonInsideAnUntranslatableStatement_LeavesNoAsyncBehind()
    {
        var result = Rewriter.RewriteForView(
            "var accepted = new SettingsForm().ShowDialog(this) == DialogResult.Yes;",
            FormWith(),
            Navigation());

        Assert.Empty(result.MigratedStatements);
        Assert.False(result.RequiresAsync);
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
    /// A fallback control only exposes what its bundled template demonstrably has -
    /// StatusStripFallback is a StackPanel, so a catalog property on it stays un-migrated.
    /// </summary>
    [Fact]
    public void RewriteForView_PropertyOnAFallbackThatDoesNotExposeIt_IsNotMigrated()
    {
        var form = FormWith(("statusStrip1", "StatusStrip"));

        var result = Rewriter.RewriteForView("this.statusStrip1.Visible = false;", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// RichTextBoxFallback derives from Avalonia's TextBox, so its Text is a known fact rather
    /// than a guess - and these templates ship in this repo, which is what makes that checkable.
    /// </summary>
    [Fact]
    public void RewriteForView_PropertyOnAFallbackThatExposesIt_IsMigrated()
    {
        var form = FormWith(("notesRichTextBox", "RichTextBox"), ("maskedTextBox1", "MaskedTextBox"));

        var result = Rewriter.RewriteForView(
            """
            this.notesRichTextBox.Text = "hello";
            this.maskedTextBox1.Text = this.notesRichTextBox.Text;
            """,
            form);

        Assert.Equal(
            [
                "notesRichTextBox.Text = \"hello\";",
                "maskedTextBox1.Text = (notesRichTextBox.Text ?? string.Empty);",
            ],
            result.MigratedStatements);
    }

    /// <summary>The ToolStrip items are Direct-mapped, so their values were always reachable in principle.</summary>
    [Theory]
    [InlineData("ToolStripStatusLabel", "Text = \"busy\"", "field1.Text = \"busy\";")]
    [InlineData("ToolStripButton", "Text = \"Go\"", "field1.Content = \"Go\";")]
    [InlineData("ToolStripMenuItem", "Text = \"File\"", "field1.Header = \"File\";")]
    [InlineData("ToolStripMenuItem", "Checked = true", "field1.IsChecked = true;")]
    [InlineData("ToolStripProgressBar", "Value = 40", "field1.Value = 40;")]
    public void RewriteForView_ToolStripItemProperties_AreTranslated(string typeName, string assignment, string expected)
    {
        var form = FormWith(("field1", typeName));

        var result = Rewriter.RewriteForView($"this.field1.{assignment};", form);

        Assert.Equal([expected], result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_CallToAHelperMethod_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("SetBusy(true);", FormWith());

        Assert.Empty(result.MigratedStatements);
    }

    // ---- EventArgs -----------------------------------------------------------------------

    /// <summary>Avalonia spells these exactly as WinForms did, so the member passes through.</summary>
    [Theory]
    [InlineData("ScrollEventArgs", "NewValue")]
    [InlineData("RangeBaseValueChangedEventArgs", "NewValue")]
    public void RewriteForView_EventArgsMemberAvaloniaSpellsTheSame_PassesThrough(string argsType, string member)
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            $"this.statusLabel.Text = e.{member}.ToString();",
            form,
            navigation: null,
            new HandlerSignature("e", argsType, "trackBar1"));

        Assert.Equal([$"statusLabel.Text = e.{member}.ToString();"], result.MigratedStatements);
    }

    /// <summary>
    /// WinForms' e.X/e.Y are relative to the control that raised the event - which is exactly
    /// what Avalonia's GetPosition takes.
    /// </summary>
    [Fact]
    public void RewriteForView_PointerCoordinates_BecomeGetPositionOnTheRaisingControl()
    {
        var form = FormWith(("statusLabel", "Label"), ("canvas", "Panel"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = $\"{e.X},{e.Y}\";",
            form,
            navigation: null,
            new HandlerSignature("e", "PointerPressedEventArgs", "canvas"));

        Assert.Equal(
            ["statusLabel.Text = $\"{e.GetPosition(canvas).X},{e.GetPosition(canvas).Y}\";"],
            result.MigratedStatements);
    }

    /// <summary>A shared handler has no single raising control, so there is no exact answer.</summary>
    [Fact]
    public void RewriteForView_PointerCoordinatesOnASharedHandler_AreNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = e.X.ToString();",
            form,
            navigation: null,
            new HandlerSignature("e", "PointerPressedEventArgs", SourceControlFieldName: null));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>An args type that is plain .NET reached the generated project untouched.</summary>
    [Fact]
    public void RewriteForView_MemberOfAnUnchangedArgsType_PassesThrough()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = e.FullPath;",
            form,
            navigation: null,
            new HandlerSignature("e", "FileSystemEventArgs", "watcher1"));

        Assert.Equal(["statusLabel.Text = e.FullPath;"], result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_AssignmentToAnEventArgsMember_IsTranslated()
    {
        var result = Rewriter.RewriteForView(
            "e.Cancel = true;",
            FormWith(),
            navigation: null,
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldName: null));

        Assert.Equal(["e.Cancel = true;"], result.MigratedStatements);
    }

    /// <summary>A computed translation is a read; there is nothing to assign to.</summary>
    [Fact]
    public void RewriteForView_AssignmentToAComputedEventArgsMember_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "e.X = 5;",
            FormWith(("canvas", "Panel")),
            navigation: null,
            new HandlerSignature("e", "PointerPressedEventArgs", "canvas"));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// Avalonia reports a grid cell through an object rather than an index pair, so there is no
    /// exact answer and the member is left for a human.
    /// </summary>
    [Fact]
    public void RewriteForView_EventArgsMemberWithNoExactEquivalent_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"), ("grid1", "DataGridView"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = e.RowIndex.ToString();",
            form,
            navigation: null,
            new HandlerSignature("e", "DataGridCellPointerPressedEventArgs", "grid1"));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// Plain <c>EventArgs</c> is the fallback the planner uses when an event has no Avalonia
    /// equivalent, so it means "unknown type" - while the original body is reaching for members
    /// of the richer WinForms args type it was written against. Treating it as pass-through
    /// emitted `e.ProgressPercentage` on a parameter declared `EventArgs`, which does not compile.
    /// </summary>
    [Fact]
    public void RewriteForView_MemberOnPlainEventArgs_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = e.ProgressPercentage.ToString();",
            form,
            navigation: null,
            new HandlerSignature("e", "EventArgs", "worker1"));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>Without signature information nothing about `e` can be resolved.</summary>
    [Fact]
    public void RewriteForView_WithoutAHandlerSignature_EventArgsMembersAreNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView("this.statusLabel.Text = e.FullPath;", form);

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Loops ---------------------------------------------------------------------------

    /// <summary>
    /// The loop variable is a plain value for the same reason any other local is: the collection
    /// expression had to translate, so it is a BCL value and so are its elements.
    /// </summary>
    [Fact]
    public void RewriteForView_ForEach_TranslatesCollectionAndBody()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            foreach (var letter in this.nameTextBox.Text)
            {
                this.statusLabel.Text = letter.ToString();
            }
            """,
            form);

        Assert.Equal(
            [
                """
                foreach (var letter in (nameTextBox.Text ?? string.Empty))
                {
                    statusLabel.Text = letter.ToString();
                }
                """,
            ],
            result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_ForLoop_TranslatesHeaderAndBody()
    {
        var form = FormWith(("progressBar1", "ProgressBar"));

        var result = Rewriter.RewriteForView(
            """
            for (var i = 0; i <= 100; i += 10)
            {
                this.progressBar1.Value = i;
            }
            """,
            form);

        Assert.Equal(
            [
                """
                for (var i = 0; i <= 100; i += 10)
                {
                    progressBar1.Value = i;
                }
                """,
            ],
            result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_ForLoopWithPostfixIncrement_IsTranslated()
    {
        var form = FormWith(("progressBar1", "ProgressBar"));

        var result = Rewriter.RewriteForView(
            """
            for (int i = 0; i < 10; i++)
            {
                this.progressBar1.Value = i;
            }
            """,
            form);

        Assert.Contains("for (int i = 0; i < 10; i++)", Assert.Single(result.MigratedStatements));
    }

    [Fact]
    public void RewriteForView_While_TranslatesConditionAndBody()
    {
        var form = FormWith(("progressBar1", "ProgressBar"));

        var result = Rewriter.RewriteForView(
            """
            var i = 0;
            while (i < 10)
            {
                this.progressBar1.Value = i;
                i++;
            }
            """,
            form);

        Assert.Equal(2, result.MigratedStatements.Count);
        Assert.Contains("while (i < 10)", result.MigratedStatements[1]);
        Assert.Contains("i++;", result.MigratedStatements[1]);
    }

    /// <summary>All-or-nothing inside the body, exactly as in an `if` branch.</summary>
    [Fact]
    public void RewriteForView_LoopWhoseBodyIsOnlyPartlyTranslatable_IsNotMigratedAtAll()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            foreach (var letter in this.nameTextBox.Text)
            {
                this.statusLabel.Text = letter.ToString();
                PersistEverything();
            }
            """,
            form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_LoopVariable_DoesNotLeakPastTheLoop()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            foreach (var letter in this.nameTextBox.Text)
            {
                this.statusLabel.Text = letter.ToString();
            }
            this.statusLabel.Text = letter.ToString();
            """,
            form);

        Assert.Single(result.MigratedStatements);
        Assert.Contains("letter.ToString()", result.RemainingBody);
    }

    [Fact]
    public void RewriteForView_ForEachOverAnUntranslatableCollection_IsNotMigrated()
    {
        var form = FormWith(("treeView1", "TreeView"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            foreach (var node in this.treeView1.Nodes)
            {
                this.statusLabel.Text = "x";
            }
            """,
            form);

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Local variables -----------------------------------------------------------------

    [Fact]
    public void RewriteForView_LocalDeclarationAndUse_AreBothTranslated()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            var name = this.nameTextBox.Text;
            this.statusLabel.Text = name;
            """,
            form);

        Assert.Equal(
            ["var name = (nameTextBox.Text ?? string.Empty);", "statusLabel.Text = name;"],
            result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    /// <summary>
    /// A translatable initializer can only produce a plain .NET value, so members of the local
    /// are plain .NET too - the same argument that allows members of a control property.
    /// </summary>
    [Fact]
    public void RewriteForView_MemberOfALocal_IsTranslated()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            var name = this.nameTextBox.Text;
            this.statusLabel.Text = name.Trim().ToUpper();
            """,
            form);

        Assert.Equal("statusLabel.Text = name.Trim().ToUpper();", result.MigratedStatements[1]);
    }

    [Fact]
    public void RewriteForView_ExplicitKeywordType_IsKept()
    {
        var form = FormWith(("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView("string name = this.nameTextBox.Text;", form);

        Assert.Equal(["string name = (nameTextBox.Text ?? string.Empty);"], result.MigratedStatements);
    }

    /// <summary>A named type could be a WinForms type whose translation is a different type entirely.</summary>
    [Fact]
    public void RewriteForView_ExplicitNamedType_IsNotMigrated()
    {
        var form = FormWith(("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView("StringBuilder name = this.nameTextBox.Text;", form);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_LocalWithAnUntranslatableInitializer_IsNotMigrated()
    {
        var form = FormWith(("treeView1", "TreeView"));

        var result = Rewriter.RewriteForView("var count = this.treeView1.Nodes.Count;", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// The statements that would have used it are exactly the ones that did not translate, so a
    /// local stranded at the end of a partial prefix is dead - and a constant one would be CS0219.
    /// </summary>
    [Fact]
    public void RewriteForView_LocalStrandedAtTheEndOfAPartialPrefix_IsDropped()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            this.statusLabel.Text = "Working";
            var attempts = 0;
            PersistEverything();
            """,
            form);

        Assert.Equal(["statusLabel.Text = \"Working\";"], result.MigratedStatements);
        Assert.Contains("var attempts = 0;", result.RemainingBody);
    }

    [Fact]
    public void RewriteForView_LocalDeclaredInsideABranch_DoesNotLeakPastIt()
    {
        var form = FormWith(("agreeCheckBox", "CheckBox"), ("statusLabel", "Label"), ("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(
            """
            if (this.agreeCheckBox.Checked)
            {
                var name = this.nameTextBox.Text;
                this.statusLabel.Text = name;
            }
            this.statusLabel.Text = name;
            """,
            form);

        // The `if` translates; the trailing use of the now-out-of-scope local does not.
        Assert.Single(result.MigratedStatements);
        Assert.Contains("this.statusLabel.Text = name;", result.RemainingBody);
    }

    [Fact]
    public void RewriteForView_ConstLocal_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("const int limit = 5;", FormWith());

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_LocalHoldingAConvertedForm_OpensItThroughTheView()
    {
        var result = Rewriter.RewriteForView(
            """
            var dialog = new SettingsForm();
            dialog.ShowDialog(this);
            """,
            FormWith(),
            Navigation());

        Assert.Equal(
            ["var dialog = new SettingsView();", "await dialog.ShowDialog(this);"],
            result.MigratedStatements);
        Assert.True(result.RequiresAsync);
        Assert.Contains("Demo.Views.Dialogs", result.RequiredUsings);
    }

    /// <summary>An Avalonia Window is not IDisposable, so there is no disposal to preserve.</summary>
    [Fact]
    public void RewriteForView_UsingVarHoldingAConvertedForm_DropsTheUsing()
    {
        var result = Rewriter.RewriteForView(
            """
            using var dialog = new SettingsForm();
            dialog.ShowDialog(this);
            """,
            FormWith(),
            Navigation());

        Assert.Equal("var dialog = new SettingsView();", result.MigratedStatements[0]);
    }

    /// <summary>On anything else a `using` would be discarding a real Dispose call.</summary>
    [Fact]
    public void RewriteForView_UsingVarOnANonFormInitializer_IsNotMigrated()
    {
        var form = FormWith(("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView("using var name = this.nameTextBox.Text;", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A View is something you open, not a value to read members off - so the read stops the
    /// prefix, and the declaration above it is then stranded and dropped with it.
    /// </summary>
    [Fact]
    /// <summary>
    /// A dialog's *own* public property is not something the conversion can name: the generated
    /// View carries the controls and the handlers, not the Form's public surface, so
    /// <c>dialog.EnteredText</c> would compile against a member that does not exist.
    /// </summary>
    public void RewriteForView_FormViewLocalUsedAsAValue_StopsThePrefixAtTheDeclaration()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            this.statusLabel.Text = "before";
            var dialog = new SettingsForm();
            this.statusLabel.Text = dialog.EnteredText;
            """,
            form,
            Navigation());

        Assert.Equal(["statusLabel.Text = \"before\";"], result.MigratedStatements);
        Assert.Contains("var dialog = new SettingsForm();", result.RemainingBody);
        Assert.Contains("dialog.EnteredText", result.RemainingBody);
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

    // ---- The dialog-result contract, hand-written side -----------------------------------

    /// <summary>
    /// `DialogResult = ...; Close();` is *one* act, and has to be translated as one: taken a
    /// statement at a time, the trailing bare `Close()` would close the window with
    /// `default(bool)` and overwrite the result the line above just set.
    /// </summary>
    [Fact]
    public void RewriteForView_DialogResultThenClose_CollapsesIntoASingleCloseWithTheResult()
    {
        var form = FormWith(("okButton", "Button"));

        var result = Rewriter.RewriteForView(
            """
            DialogResult = DialogResult.OK;
            Close();
            """,
            form,
            Navigation());

        Assert.Equal(["Close(true);"], result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void RewriteForView_DialogResultAlone_StillCloses()
    {
        var result = Rewriter.RewriteForView("this.DialogResult = DialogResult.Cancel;", FormWith(), Navigation());

        Assert.Equal(["Close(false);"], result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void RewriteForView_StatementsBeforeTheDialogResult_AreKeptInOrder()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            this.statusLabel.Text = "Saving";
            DialogResult = DialogResult.Yes;
            Close();
            """,
            form,
            Navigation());

        Assert.Equal(["statusLabel.Text = \"Saving\";", "Close(true);"], result.MigratedStatements);
    }

    /// <summary>
    /// In WinForms the handler keeps running after the assignment; in Avalonia the Close is the
    /// end. Where the original still had work to do, the two are not the same thing.
    /// </summary>
    [Fact]
    public void RewriteForView_WorkAfterTheDialogResult_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            DialogResult = DialogResult.OK;
            this.statusLabel.Text = "after";
            """,
            form,
            Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A three-way dialog cannot round-trip through a bool, and widening the result type would
    /// change what every converted dialog returns.
    /// </summary>
    [Fact]
    public void RewriteForView_DialogResultWithNoBoolAnswer_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("DialogResult = DialogResult.Retry;", FormWith(), Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_DialogResultInAUserControl_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "DialogResult = DialogResult.OK;", FormWith(), Navigation(hostIsWindow: false));

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Window properties ---------------------------------------------------------------

    [Fact]
    public void RewriteForView_FormTextAndWindowState_BecomeTheirWindowCounterparts()
    {
        var result = Rewriter.RewriteForView(
            """
            this.Text = "Report";
            WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            """,
            FormWith(),
            Navigation());

        Assert.Equal(
            [
                "Title = \"Report\";",
                "WindowState = WindowState.Maximized;",
                "Topmost = true;",
            ],
            result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_ReadingTheFormTitle_IsNullGuardedLikeAnyOtherStringRead()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView("this.statusLabel.Text = this.Text;", form, Navigation());

        Assert.Equal(["statusLabel.Text = (Title ?? string.Empty);"], result.MigratedStatements);
    }

    /// <summary>A converted UserControl has no Title, so the same statement must not translate.</summary>
    [Fact]
    public void RewriteForView_WindowPropertyInAUserControl_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "this.Text = \"Report\";", FormWith(), Navigation(hostIsWindow: false));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>The window property a *dialog* carries, reached through the local that holds it.</summary>
    [Fact]
    public void RewriteForView_WindowPropertyOnAFormViewLocal_IsTranslated()
    {
        var result = Rewriter.RewriteForView(
            """
            var dialog = new SettingsForm();
            dialog.Text = "About";
            dialog.ShowDialog(this);
            """,
            FormWith(),
            Navigation());

        Assert.Equal(
            [
                "var dialog = new SettingsView();",
                "dialog.Title = \"About\";",
                "await dialog.ShowDialog(this);",
            ],
            result.MigratedStatements);
        Assert.True(result.RequiresAsync);
    }

    /// <summary>A local of the same name shadows the form's own property, exactly as in C#.</summary>
    [Fact]
    public void RewriteForView_LocalNamedLikeAWindowProperty_ShadowsIt()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            var Text = "local";
            this.statusLabel.Text = Text;
            """,
            form,
            Navigation());

        Assert.Equal(
            ["var Text = \"local\";", "statusLabel.Text = Text;"],
            result.MigratedStatements);
    }

    /// <summary>
    /// The size properties are deliberately absent: WinForms measures the outer frame and
    /// Avalonia does not, so there is no fixed conversion between them.
    /// </summary>
    [Fact]
    public void RewriteForView_FormSize_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("this.Width = 400;", FormWith(), Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    // ---- The DispatcherTimer this conversion creates itself -------------------------------

    [Fact]
    public void RewriteForView_TimerMembers_AreTranslatedAgainstTheGeneratedDispatcherTimer()
    {
        var form = FormWith(("clockTimer", "Timer"));
        var timers = new HashSet<string>(StringComparer.Ordinal) { "clockTimer" };

        var result = Rewriter.RewriteForView(
            """
            this.clockTimer.Enabled = !this.clockTimer.Enabled;
            this.clockTimer.Interval = 250;
            this.clockTimer.Stop();
            """,
            form,
            Navigation(),
            dispatcherTimerFields: timers);

        Assert.Equal(
            [
                "clockTimer.IsEnabled = !clockTimer.IsEnabled;",
                "clockTimer.Interval = TimeSpan.FromMilliseconds(250);",
                "clockTimer.Stop();",
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// WinForms counts Interval in int milliseconds, Avalonia holds a TimeSpan. A write can be
    /// wrapped faithfully; a read would compile and quietly mean something else.
    /// </summary>
    [Fact]
    public void RewriteForView_ReadingTimerInterval_IsNotMigrated()
    {
        var form = FormWith(("clockTimer", "Timer"), ("statusLabel", "Label"));
        var timers = new HashSet<string>(StringComparer.Ordinal) { "clockTimer" };

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = this.clockTimer.Interval.ToString();",
            form,
            Navigation(),
            dispatcherTimerFields: timers);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A Timer with no Tick handler never becomes a field, so nothing may name it.</summary>
    [Fact]
    public void RewriteForView_TimerThatWasNeverPlanned_IsNotMigrated()
    {
        var form = FormWith(("clockTimer", "Timer"));

        var result = Rewriter.RewriteForView("this.clockTimer.Stop();", form, Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    // ---- sender, the clipboard, and drag payloads -----------------------------------------

    /// <summary>
    /// In a handler wired to exactly one control, `sender` provably *is* that control - so the
    /// local becomes another name for its field and the cast disappears rather than being
    /// translated. Casting to the Avalonia element type would need the semantic model this
    /// converter deliberately does without.
    /// </summary>
    [Fact]
    public void RewriteForView_SenderCastOnASingleControlHandler_BecomesAnAliasForThatControl()
    {
        var form = FormWith(("okButton", "Button"));

        var result = Rewriter.RewriteForView(
            """
            var button = (Button)sender!;
            button.Text = "Clicked";
            button.Enabled = false;
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", "okButton"));

        Assert.Equal(
            ["okButton.Content = \"Clicked\";", "okButton.IsEnabled = false;"],
            result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    /// <summary>A handler shared by two controls has no single answer for what `sender` is.</summary>
    [Fact]
    public void RewriteForView_SenderCastOnASharedHandler_IsNotMigrated()
    {
        var form = FormWith(("okButton", "Button"));

        var result = Rewriter.RewriteForView(
            """
            var button = (Button)sender!;
            button.Text = "Clicked";
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", SourceControlFieldName: null));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A base-type cast is refused rather than widened: what the body does with the local is
    /// checked against the actual control either way, so accepting it would only let the
    /// translated code claim something the original did not.
    /// </summary>
    [Fact]
    public void RewriteForView_SenderCastToADifferentType_IsNotMigrated()
    {
        var form = FormWith(("okButton", "Button"));

        var result = Rewriter.RewriteForView(
            """
            var control = (Control)sender!;
            control.Text = "Clicked";
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", "okButton"));

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_ClipboardSetText_GoesThroughTheTopLevelAndTurnsTheHandlerAsync()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView("Clipboard.SetText(this.statusLabel.Text);", form);

        Assert.Equal(
            ["await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync((statusLabel.Text ?? string.Empty));"],
            result.MigratedStatements);
        Assert.True(result.RequiresAsync);
    }

    [Fact]
    public void RewriteForView_DragEffectAndPayloadQuery_AreTranslated()
    {
        var form = FormWith(("dropPanel", "Panel"));

        var result = Rewriter.RewriteForView(
            """
            e.Effect = e.Data!.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "DragEventArgs", "dropPanel"));

        Assert.Equal(
            ["e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;"],
            result.MigratedStatements);
    }

    /// <summary>
    /// Reading the payload is a change of shape, not of spelling: Avalonia hands back storage
    /// items rather than the `string[]` the original casts to.
    /// </summary>
    [Fact]
    public void RewriteForView_ReadingTheDragPayload_IsNotMigrated()
    {
        var form = FormWith(("dropPanel", "Panel"));

        var result = Rewriter.RewriteForView(
            "var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;",
            form,
            Navigation(),
            new HandlerSignature("e", "DragEventArgs", "dropPanel"));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// WinForms' DragDropEffects has members Avalonia's does not, and emitting one would be a
    /// compile error in the generated project.
    /// </summary>
    [Fact]
    public void RewriteForView_DragEffectAvaloniaDoesNotHave_IsNotMigrated()
    {
        var form = FormWith(("dropPanel", "Panel"));

        var result = Rewriter.RewriteForView(
            "e.Effect = DragDropEffects.Scroll;",
            form,
            Navigation(),
            new HandlerSignature("e", "DragEventArgs", "dropPanel"));

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
