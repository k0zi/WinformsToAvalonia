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

    /// <summary>
    /// WinForms' <c>CheckBox.Checked</c> is a <c>bool</c>; Avalonia's <c>IsChecked</c> is a
    /// <c>bool?</c>. A read has to come out as the type the original expression had, or the
    /// generated project does not compile - <c>if (checkBox1.IsChecked)</c> is a CS0266. Writing
    /// needs no such thing, since a <c>bool</c> goes into a <c>bool?</c> unaided.
    /// </summary>
    [Fact]
    public void RewriteForView_NullableAvaloniaProperty_IsCoalescedOnReadButNotOnWrite()
    {
        var form = FormWith(("agreeCheckBox", "CheckBox"), ("otherCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView("this.agreeCheckBox.Checked = this.otherCheckBox.Checked;", form);

        Assert.Equal(["agreeCheckBox.IsChecked = (otherCheckBox.IsChecked ?? false);"], result.MigratedStatements);
    }

    /// <summary>
    /// A three-state CheckBox reports Indeterminate as <c>Checked == true</c>, and no coalescing
    /// of Avalonia's <c>bool?</c> says that - so the read is refused rather than quietly inverted.
    /// </summary>
    [Fact]
    public void RewriteForView_ThreeStateCheckBox_RefusesTheRead()
    {
        var form = FormWith(("agreeCheckBox", "CheckBox"), ("statusLabel", "Label"));
        form.Controls["agreeCheckBox"].Properties["ThreeState"] = new PropertyValue.Literal(true);

        var result = Rewriter.RewriteForView(
            "if (this.agreeCheckBox.Checked) { this.statusLabel.Text = \"on\"; }", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A Button's <c>Text</c> becomes <c>Content</c>, which is an <c>object?</c> - reading one
    /// into a string is a CS0266 in the generated project unless the read says how.
    /// </summary>
    [Fact]
    public void RewriteForView_ObjectTypedAvaloniaProperty_IsReadAsAString()
    {
        var form = FormWith(("goButton", "Button"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView("this.statusLabel.Text = this.goButton.Text;", form);

        Assert.Equal(
            ["statusLabel.Text = (goButton.Content as string ?? string.Empty);"],
            result.MigratedStatements);
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
                if ((agreeCheckBox.IsChecked ?? false))
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

        Assert.Contains("else if ((b.IsChecked ?? false))", Assert.Single(result.MigratedStatements));
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
                if ((agreeCheckBox.IsChecked ?? false))
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

    /// <summary>
    /// The print dialogs have no Avalonia equivalent at all - Avalonia has no printing API, so
    /// unlike the colour and font dialogs there is nothing to wrap in a bundled window either.
    /// </summary>
    [Fact]
    public void RewriteForView_DialogWithNoAvaloniaEquivalent_IsNotMigrated()
    {
        var form = FormWith(("printDialog1", "PrintDialog"), ("panel1", "Panel"));

        var result = Rewriter.RewriteForView(
            "if (this.printDialog1.ShowDialog(this) == DialogResult.OK) { this.panel1.Visible = true; }",
            form,
            Navigation());

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

        var result = Rewriter.RewriteForView("this.treeView1.ExpandAll();", form);

        Assert.Empty(result.MigratedStatements);
        Assert.Equal("this.treeView1.ExpandAll();", result.RemainingBody);
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
            new HandlerSignature("e", argsType, ["trackBar1"]));

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
            new HandlerSignature("e", "PointerPressedEventArgs", ["canvas"]));

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
            new HandlerSignature("e", "PointerPressedEventArgs", SourceControlFieldNames: []));

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
            new HandlerSignature("e", "FileSystemEventArgs", ["watcher1"]));

        Assert.Equal(["statusLabel.Text = e.FullPath;"], result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_AssignmentToAnEventArgsMember_IsTranslated()
    {
        var result = Rewriter.RewriteForView(
            "e.Cancel = true;",
            FormWith(),
            navigation: null,
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldNames: []));

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
            new HandlerSignature("e", "PointerPressedEventArgs", ["canvas"]));

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
            new HandlerSignature("e", "DataGridCellPointerPressedEventArgs", ["grid1"]));

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
            new HandlerSignature("e", "EventArgs", ["worker1"]));

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
            new HandlerSignature("e", "RoutedEventArgs", ["okButton"]));

        Assert.Equal(
            ["okButton.Content = \"Clicked\";", "okButton.IsEnabled = false;"],
            result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    /// <summary>
    /// One handler on N buttons - how WinForms shares a handler at all. There is no single field
    /// to alias, so the cast survives against the *Avalonia* element type, and the local stands
    /// for a control of that type: everything the body says about it is checked against the one
    /// type all the wired controls share.
    /// </summary>
    [Fact]
    public void RewriteForView_SenderCastOnAHandlerSharedBySameTypedControls_CastsToTheAvaloniaElement()
    {
        var form = FormWith(("okButton", "Button"), ("cancelButton", "Button"));

        var result = Rewriter.RewriteForView(
            """
            var button = (Button)sender!;
            button.Text = "Clicked";
            button.Enabled = false;
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", ["okButton", "cancelButton"]));

        Assert.Equal(
            [
                "var button = (Button)sender!;",
                "button.Content = \"Clicked\";",
                "button.IsEnabled = false;",
            ],
            result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    /// <summary>
    /// Mixed types stay refused - telling them apart is the whole reason such a handler reads
    /// `sender`, and no single cast is valid for both.
    /// </summary>
    [Fact]
    public void RewriteForView_SenderCastOnAHandlerSharedByDifferentTypes_IsNotMigrated()
    {
        var form = FormWith(("okButton", "Button"), ("agreeCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView(
            """
            var button = (Button)sender!;
            button.Text = "Clicked";
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", ["okButton", "agreeCheckBox"]));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A Form-level event has no raising control at all, so there is nothing to cast to.</summary>
    [Fact]
    public void RewriteForView_SenderCastOnAFormLevelHandler_IsNotMigrated()
    {
        var form = FormWith(("okButton", "Button"));

        var result = Rewriter.RewriteForView(
            """
            var button = (Button)sender!;
            button.Text = "Clicked";
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", SourceControlFieldNames: []));

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
            new HandlerSignature("e", "RoutedEventArgs", ["okButton"]));

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
            new HandlerSignature("e", "DragEventArgs", ["dropPanel"]));

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
            new HandlerSignature("e", "DragEventArgs", ["dropPanel"]));

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
            new HandlerSignature("e", "DragEventArgs", ["dropPanel"]));

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Non-visual components emitted as real fields -------------------------------------

    /// <summary>
    /// These components are the same .NET type they were in WinForms, so anything hanging off one
    /// is ordinary .NET - which is why there is no per-member catalog for them, and why a nested
    /// path like `process1.StartInfo.FileName` works for exactly the same reason.
    /// </summary>
    [Fact]
    public void RewriteForView_ComponentMembers_PassThroughUnchanged()
    {
        var form = FormWith(("worker1", "BackgroundWorker"), ("process1", "Process"), ("statusLabel", "Label"));
        var components = new HashSet<string>(StringComparer.Ordinal) { "worker1", "process1" };

        var result = Rewriter.RewriteForView(
            """
            this.process1.StartInfo.FileName = "dotnet";
            this.worker1.RunWorkerAsync();
            this.statusLabel.Text = this.worker1.IsBusy.ToString();
            """,
            form,
            Navigation(),
            componentFields: components);

        Assert.Equal(
            [
                "process1.StartInfo.FileName = \"dotnet\";",
                "worker1.RunWorkerAsync();",
                "statusLabel.Text = worker1.IsBusy.ToString();",
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// A component with no field is a component the plan decided not to emit - naming it would
    /// produce code referring to something that does not exist.
    /// </summary>
    [Fact]
    public void RewriteForView_ComponentWithNoField_IsNotMigrated()
    {
        var form = FormWith(("worker1", "BackgroundWorker"));

        var result = Rewriter.RewriteForView("this.worker1.RunWorkerAsync();", form, Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// The arguments of a component call still go through the ordinary expression path, so one
    /// that reaches for a WinForms API stops the translation like anywhere else.
    /// </summary>
    [Fact]
    public void RewriteForView_ComponentCallWithAnUntranslatableArgument_IsNotMigrated()
    {
        var form = FormWith(("worker1", "BackgroundWorker"), ("treeView1", "TreeView"));
        var components = new HashSet<string>(StringComparer.Ordinal) { "worker1" };

        var result = Rewriter.RewriteForView(
            "this.worker1.ReportProgress(this.treeView1.Nodes.Count);",
            form,
            Navigation(),
            componentFields: components);

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Helper methods --------------------------------------------------------------------

    /// <summary>
    /// A helper is callable once the plan has promoted it, and not before - which is what the
    /// planner's fixed point turns into "not until its own body translated".
    /// </summary>
    [Fact]
    public void RewriteForView_CallToAPromotedHelper_IsTranslated()
    {
        var form = FormWith(("statusLabel", "Label"));
        var helpers = new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal)
        {
            ["SetBusy"] = new(ParameterCount: 1, IsAsync: false),
            ["Describe"] = new(ParameterCount: 1, IsAsync: false),
        };

        var result = Rewriter.RewriteForView(
            """
            SetBusy(true);
            this.statusLabel.Text = Describe(1);
            """,
            form,
            Navigation(),
            promotedHelpers: helpers);

        Assert.Equal(
            ["SetBusy(true);", "statusLabel.Text = Describe(1);"],
            result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_CallToAHelperThatWasNotPromoted_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("SetBusy(true);", FormWith(), Navigation());

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>An arity mismatch means this is not the helper the plan promoted.</summary>
    [Fact]
    public void RewriteForView_HelperCalledWithTheWrongNumberOfArguments_IsNotMigrated()
    {
        var helpers = new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal)
        {
            ["SetBusy"] = new(ParameterCount: 1, IsAsync: false),
        };

        var result = Rewriter.RewriteForView(
            "SetBusy(true, false);", FormWith(), Navigation(), promotedHelpers: helpers);

        Assert.Empty(result.MigratedStatements);
    }

    [Fact]
    public void RewriteForView_CallToAnAsyncHelper_IsAwaitedAndTurnsTheCallerAsync()
    {
        var helpers = new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal)
        {
            ["WarnAndReset"] = new(ParameterCount: 0, IsAsync: true),
        };

        var result = Rewriter.RewriteForView(
            "this.WarnAndReset();", FormWith(), Navigation(), promotedHelpers: helpers);

        Assert.Equal(["await WarnAndReset();"], result.MigratedStatements);
        Assert.True(result.RequiresAsync);
    }

    /// <summary>
    /// Awaiting inside a larger expression needs precedence handling this rewriter deliberately
    /// avoids, so an async helper is callable as a statement and nowhere else.
    /// </summary>
    [Fact]
    public void RewriteForView_AsyncHelperUsedAsAValue_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"));
        var helpers = new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal)
        {
            ["Describe"] = new(ParameterCount: 0, IsAsync: true),
        };

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = Describe();", form, Navigation(), promotedHelpers: helpers);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A helper body sees its parameters as ordinary locals, and nothing else.</summary>
    [Fact]
    public void RewriteForHelper_ParametersAreInScope()
    {
        var form = FormWith(("startButton", "Button"));

        var result = Rewriter.RewriteForHelper(
            new HelperMethodSignature("void", "(bool busy)", ["busy"], "this.startButton.Enabled = !busy;", IsAsync: false),
            form,
            ViewNavigationContext.None,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));

        Assert.Equal(["startButton.IsEnabled = !busy;"], result.MigratedStatements);
        Assert.True(result.IsComplete);
    }

    /// <summary>A helper has no sender or EventArgs, so a body reaching for one cannot translate.</summary>
    [Fact]
    public void RewriteForHelper_BodyUsingEventArgs_IsNotMigrated()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForHelper(
            new HelperMethodSignature("void", "()", [], "this.statusLabel.Text = e.ProgressPercentage.ToString();", IsAsync: false),
            form,
            ViewNavigationContext.None,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Control methods that take an argument ---------------------------------------------

    /// <summary>
    /// Avalonia has no AppendText; appending to Text is what it does to the contents. The
    /// argument goes through the ordinary expression path like any other.
    /// </summary>
    [Fact]
    public void RewriteForView_AppendText_BecomesAnAppendToTheTextProperty()
    {
        var form = FormWith(("logTextBox", "TextBox"), ("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(
            "this.logTextBox.AppendText(this.nameTextBox.Text + Environment.NewLine);", form);

        Assert.Equal(
            ["logTextBox.Text += (nameTextBox.Text ?? string.Empty) + Environment.NewLine;"],
            result.MigratedStatements);
    }

    /// <summary>
    /// A fallback control carries the call when its template exposes the member the *translation*
    /// touches - `Text` - even though the template has no AppendText of its own.
    /// </summary>
    [Fact]
    public void RewriteForView_AppendTextOnAFallbackControl_UsesTheTemplatesTextProperty()
    {
        var form = FormWith(("notesRichTextBox", "RichTextBox"));

        var result = Rewriter.RewriteForView("this.notesRichTextBox.AppendText(\"done\");", form);

        Assert.Equal(["notesRichTextBox.Text += \"done\";"], result.MigratedStatements);
    }

    /// <summary>An overload with a different arity is a different method.</summary>
    [Fact]
    public void RewriteForView_ControlMethodCalledWithTheWrongArity_IsNotMigrated()
    {
        var form = FormWith(("logTextBox", "TextBox"));

        var result = Rewriter.RewriteForView("this.logTextBox.Clear(true);", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>The argument is translated, so one reaching a WinForms API refuses the call.</summary>
    [Fact]
    public void RewriteForView_AppendTextWithAnUntranslatableArgument_IsNotMigrated()
    {
        var form = FormWith(("logTextBox", "TextBox"), ("treeView1", "TreeView"));

        var result = Rewriter.RewriteForView(
            "this.logTextBox.AppendText(this.treeView1.Nodes.Count.ToString());", form);

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Form lifetime, message boxes, and blocking calls -----------------------------------

    [Fact]
    public void RewriteForView_Activate_IsTranslatedOnAWindow()
    {
        var result = Rewriter.RewriteForView("this.Activate();", FormWith(), Navigation());

        Assert.Equal(["Activate();"], result.MigratedStatements);
    }

    /// <summary>
    /// Avalonia's UserControl has no Close/Show/Activate at all, so emitting them into a converted
    /// UserControl would not compile - which is what used to happen.
    /// </summary>
    [Theory]
    [InlineData("this.Close();")]
    [InlineData("this.Activate();")]
    public void RewriteForView_WindowOnlyLifetimeCall_IsNotMigratedInAUserControl(string body)
    {
        var result = Rewriter.RewriteForView(body, FormWith(), Navigation(hostIsWindow: false));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// `Show()` is the one that has an answer on both hosts: on a WinForms control it always meant
    /// `Visible = true`, which is exactly what it has to mean on a UserControl here.
    /// </summary>
    [Fact]
    public void RewriteForView_ShowInAUserControl_BecomesAVisibilityWrite()
    {
        var result = Rewriter.RewriteForView("this.Show();", FormWith(), Navigation(hostIsWindow: false));

        Assert.Equal(["IsVisible = true;"], result.MigratedStatements);
    }

    /// <summary>
    /// The owner overloads put the form first, and the translated call supplies its own owner.
    /// Stripping only a literal `this` is what keeps the arity unambiguous.
    /// </summary>
    [Fact]
    public void RewriteForView_MessageBoxWithAnOwner_DropsTheOwnerArgument()
    {
        var form = FormWith(("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(
            "MessageBox.Show(this, $\"Hello, {this.nameTextBox.Text}!\", \"Demo\");", form);

        Assert.Equal(
            ["await MessageBoxFallback.ShowAsync(this, $\"Hello, {(nameTextBox.Text ?? string.Empty)}!\", \"Demo\");"],
            result.MigratedStatements);
        Assert.True(result.RequiresAsync);
    }

    /// <summary>
    /// The buttons overloads return a DialogResult the caller branches on, and inventing an answer
    /// would change what the original did - so the same arity must still refuse without an owner.
    /// </summary>
    [Fact]
    public void RewriteForView_MessageBoxWithButtons_IsStillNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "MessageBox.Show(\"Sure?\", \"Demo\", MessageBoxButtons.YesNo);", FormWith());

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>Blocking the UI thread is what the original did; faithful is the bar, not wise.</summary>
    [Fact]
    public void RewriteForView_ThreadSleep_PassesThrough()
    {
        var result = Rewriter.RewriteForView("Thread.Sleep(100);", FormWith());

        Assert.Equal(["Thread.Sleep(100);"], result.MigratedStatements);
    }

    // ---- ErrorProvider ----------------------------------------------------------------------

    /// <summary>
    /// The one translation whose result is a *static* call on a fallback type: the WinForms
    /// component has no element at all, and its Avalonia counterpart is an attached property set
    /// from outside.
    /// </summary>
    [Fact]
    public void RewriteForView_SetError_BecomesAStaticCallOnTheBundledFallback()
    {
        var form = FormWith(("errorProvider1", "ErrorProvider"), ("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(
            "this.errorProvider1.SetError(this.nameTextBox, \"Required.\");", form);

        Assert.Equal(["ErrorProviderFallback.SetError(nameTextBox, \"Required.\");"], result.MigratedStatements);
        Assert.Contains("ErrorProviderFallback", result.RequiredFallbackKeys);
    }

    /// <summary>
    /// The first argument has to be a control the AXAML actually names - otherwise the generated
    /// View has no field to hand the fallback.
    /// </summary>
    [Fact]
    public void RewriteForView_SetErrorOnSomethingWithNoElement_IsNotMigrated()
    {
        var form = FormWith(("errorProvider1", "ErrorProvider"), ("timer1", "Timer"));

        var result = Rewriter.RewriteForView(
            "this.errorProvider1.SetError(this.timer1, \"Required.\");", form);

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Colours ----------------------------------------------------------------------------

    /// <summary>
    /// WinForms writes a Color, Avalonia wants a brush - and the colour itself goes through the
    /// very same evaluator and formatter the designer path uses, so a colour written in a handler
    /// and the same colour written in the designer cannot come out differently.
    /// </summary>
    [Fact]
    public void RewriteForView_ColorProperties_BecomeBrushes()
    {
        var form = FormWith(("statusLabel", "Label"), ("panel1", "Panel"));

        var result = Rewriter.RewriteForView(
            """
            this.statusLabel.ForeColor = Color.Red;
            this.panel1.BackColor = SystemColors.Control;
            """,
            form);

        Assert.Equal(
            [
                "statusLabel.Foreground = new SolidColorBrush(Color.Parse(\"#FFFF0000\"));",
                "panel1.Background = new SolidColorBrush(Color.Parse(\"#FFF0F0F0\"));",
            ],
            result.MigratedStatements);
        Assert.Contains("Avalonia.Media", result.RequiredUsings);
    }

    /// <summary>
    /// Gated on the element, through the same table AxamlEmitter consults: a Panel has a
    /// Background but no Foreground, and writing one that is not there is a compile error in the
    /// generated project.
    /// </summary>
    [Fact]
    public void RewriteForView_ForeColorOnAPanel_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView("this.panel1.ForeColor = Color.Red;", FormWith(("panel1", "Panel")));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A PictureBox maps to an Image, which carries no styling at all.</summary>
    [Fact]
    public void RewriteForView_ColorOnAnElementWithNoStyling_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "this.pictureBox1.BackColor = Color.Red;", FormWith(("pictureBox1", "PictureBox")));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A fallback control gets no styling anywhere in this converter - its bundled template need
    /// not expose the property - and that has to hold for a handler body too.
    /// </summary>
    [Fact]
    public void RewriteForView_ColorOnAFallbackControl_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "this.notesRichTextBox.BackColor = Color.Red;", FormWith(("notesRichTextBox", "RichTextBox")));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A colour the evaluator cannot resolve to a literal is refused, as in the AXAML.</summary>
    [Fact]
    public void RewriteForView_ComputedColor_IsNotMigrated()
    {
        var form = FormWith(("panel1", "Panel"), ("other", "Panel"));

        var result = Rewriter.RewriteForView("this.panel1.BackColor = this.other.BackColor;", form);

        Assert.Empty(result.MigratedStatements);
    }

    // ---- The dialogs Avalonia has nothing for ------------------------------------------------

    /// <summary>
    /// Same shape as the file dialogs, for the same reason: the Avalonia replacement returns the
    /// choice instead of being an object you ask afterwards. A plain `is { }` pattern, because
    /// these return one nullable value rather than a list.
    /// </summary>
    [Fact]
    public void RewriteForView_ColorDialog_IsInlinedAndItsResultBecomesABrush()
    {
        var form = FormWith(("colorDialog1", "ColorDialog"), ("panel1", "Panel"));

        var result = Rewriter.RewriteForView(
            """
            if (this.colorDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.panel1.BackColor = this.colorDialog1.Color;
            }
            """,
            form,
            Navigation());

        Assert.Equal(
            """
            if (await ColorDialogFallback.ShowAsync(this) is { } colorDialog1Color)
            {
                panel1.Background = new SolidColorBrush(colorDialog1Color);
            }
            """,
            Assert.Single(result.MigratedStatements).Replace("\r\n", "\n"));
        Assert.True(result.RequiresAsync);
        Assert.Contains("ColorDialogFallback", result.RequiredFallbackKeys);
    }

    /// <summary>
    /// One WinForms value, four Avalonia properties - a change of shape, and faithful because all
    /// four are written together so nothing observes a half-applied font.
    /// </summary>
    [Fact]
    public void RewriteForView_FontDialog_SetsAllFourFontProperties()
    {
        var form = FormWith(("fontDialog1", "FontDialog"), ("titleLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.fontDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.titleLabel.Font = this.fontDialog1.Font;
            }
            """,
            form,
            Navigation());

        var text = Assert.Single(result.MigratedStatements).Replace("\r\n", "\n");
        Assert.Contains("await FontDialogFallback.ShowAsync(this) is { } fontDialog1Font", text);
        Assert.Contains("titleLabel.FontFamily = fontDialog1Font.Family;", text);
        Assert.Contains("titleLabel.FontStyle = fontDialog1Font.Style;", text);
        Assert.Contains("FontDialogFallback", result.RequiredFallbackKeys);
    }

    /// <summary>The selection is a pattern variable, so it cannot outlive its branch.</summary>
    [Fact]
    public void RewriteForView_ColorUsedAfterTheDialogBranch_IsNotMigrated()
    {
        var form = FormWith(("colorDialog1", "ColorDialog"), ("panel1", "Panel"));

        var result = Rewriter.RewriteForView(
            """
            if (this.colorDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.panel1.BackColor = this.colorDialog1.Color;
            }

            this.panel1.BackColor = this.colorDialog1.Color;
            """,
            form,
            Navigation());

        Assert.Single(result.MigratedStatements);
        Assert.Contains("colorDialog1.Color", result.RemainingBody);
    }

    // ---- The two-button message boxes ---------------------------------------------------------

    /// <summary>
    /// Structurally the converted-dialog contract again: the whole comparison collapses into one
    /// awaited call returning a bool, because the dialog on the other end is one this repo ships.
    /// </summary>
    [Theory]
    [InlineData("MessageBoxButtons.YesNo", "DialogResult.Yes", "await MessageBoxFallback.ShowYesNoAsync(this, \"Sure?\", \"Demo\")")]
    [InlineData("MessageBoxButtons.YesNo", "DialogResult.No", "!await MessageBoxFallback.ShowYesNoAsync(this, \"Sure?\", \"Demo\")")]
    [InlineData("MessageBoxButtons.OKCancel", "DialogResult.OK", "await MessageBoxFallback.ShowOkCancelAsync(this, \"Sure?\", \"Demo\")")]
    public void RewriteForView_TwoButtonMessageBox_CollapsesIntoOneAwaitedCall(string buttons, string result, string expected)
    {
        var form = FormWith(("agreeCheckBox", "CheckBox"));

        var rewritten = Rewriter.RewriteForView(
            $"this.agreeCheckBox.Checked = MessageBox.Show(\"Sure?\", \"Demo\", {buttons}) == {result};", form);

        Assert.Equal([$"agreeCheckBox.IsChecked = {expected};"], rewritten.MigratedStatements);
        Assert.True(rewritten.RequiresAsync);
        Assert.Contains("MessageBoxFallback", rewritten.RequiredFallbackKeys);
    }

    /// <summary>
    /// A three-way answer does not fit in a bool, and widening the result would change what every
    /// bundled dialog returns.
    /// </summary>
    [Fact]
    public void RewriteForView_ThreeWayMessageBox_IsNotMigrated()
    {
        var form = FormWith(("agreeCheckBox", "CheckBox"));

        var rewritten = Rewriter.RewriteForView(
            "this.agreeCheckBox.Checked = MessageBox.Show(\"Sure?\", \"Demo\", MessageBoxButtons.YesNoCancel) == DialogResult.Yes;",
            form);

        Assert.Empty(rewritten.MigratedStatements);
    }

    /// <summary>
    /// Confirm-on-close, the canonical WinForms shape. There is no statement-level translation of
    /// it - Avalonia reads `e.Cancel` when the handler first awaits, and nothing to ask before
    /// then is synchronous - so the whole body is rewritten into the Avalonia idiom: cancel,
    /// await, and on "yes" close again from code, guarded so the second pass falls through.
    /// </summary>
    [Fact]
    public void RewriteForView_CloseConfirmation_IsRewrittenIntoTheAvaloniaIdiom()
    {
        var result = Rewriter.RewriteForView(
            "e.Cancel = MessageBox.Show(\"Sure?\", \"Demo\", MessageBoxButtons.YesNo) == DialogResult.No;",
            FormWith(),
            Navigation(),
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldNames: []));

        var body = Assert.Single(result.MigratedStatements);
        Assert.Contains("if (w2aForceClose)", body);
        Assert.Contains("e.Cancel = true;", body);
        Assert.Contains("var w2aClosing = await MessageBoxFallback.ShowYesNoAsync(this, \"Sure?\", \"Demo\");", body);
        Assert.Contains("w2aForceClose = true;", body);
        Assert.Contains("Close();", body);
        Assert.True(result.RequiresAsync);
        Assert.True(result.RequiresCloseGuard);
        Assert.True(result.IsComplete);
    }

    /// <summary>
    /// The statements around the confirmation still run exactly once per close attempt, in their
    /// original order - the guard returns immediately on the second pass, so the tail inside the
    /// branch is not a second copy of the one outside it, it is the other path through the same
    /// attempt.
    /// </summary>
    [Fact]
    public void RewriteForView_CloseConfirmationWithATail_RunsTheTailOnBothPaths()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.statusLabel.Text == "busy")
            {
                e.Cancel = MessageBox.Show("Sure?", "Demo", MessageBoxButtons.YesNo) == DialogResult.No;
            }

            this.statusLabel.Text = "bye";
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldNames: []));

        var body = Assert.Single(result.MigratedStatements);
        Assert.Equal(2, body.Split("statusLabel.Text = \"bye\";").Length - 1);
        Assert.True(result.IsComplete);
    }

    /// <summary>
    /// A tail that cannot be translated no longer takes the whole shape with it - it moves into a
    /// local function both paths call.
    /// </summary>
    /// <remarks>
    /// That indirection is the point rather than a detail. The confirmation runs the tail on two
    /// paths, so a remainder appended to the end of the method would sit on one of them only, and
    /// a human fixing it would silently leave the other broken. There is exactly one of it to edit.
    /// </remarks>
    [Fact]
    public void RewriteForView_CloseConfirmationWithAnUntranslatableTail_PutsItInALocalFunction()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            if (this.statusLabel.Text == "busy")
            {
                e.Cancel = MessageBox.Show("Sure?", "Demo", MessageBoxButtons.YesNo) == DialogResult.No;
            }

            this.statusLabel.SomethingElse();
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldNames: []));

        // Once on the path that asks, once on the path that does not - the two the original ran
        // its tail on.
        var body = Assert.Single(result.MigratedStatements);
        Assert.Equal(2, body.Split("w2aRemaining();").Length - 1);
        Assert.Equal("this.statusLabel.SomethingElse();", result.RemainingBody);
        Assert.Equal("w2aRemaining", result.Remainder?.LocalFunctionName);
        Assert.Empty(result.Remainder!.MigratedStatements);
    }

    /// <summary>
    /// The part of the tail that does translate still comes across - it goes into the same local
    /// function, above the remainder, exactly as a partly-translated handler body would.
    /// </summary>
    [Fact]
    public void RewriteForView_CloseConfirmationWithAPartlyTranslatableTail_KeepsWhatItCan()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            e.Cancel = MessageBox.Show("Sure?", "Demo", MessageBoxButtons.YesNo) == DialogResult.No;
            this.statusLabel.Text = "bye";
            this.statusLabel.SomethingElse();
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldNames: []));

        Assert.Equal(["statusLabel.Text = \"bye\";"], result.Remainder?.MigratedStatements);
        Assert.Equal("this.statusLabel.SomethingElse();", result.RemainingBody);
    }

    /// <summary>
    /// Only the shape this converter can vouch for. A closing handler that awaits something which
    /// is *not* the cancel decision keeps refusing: rewriting it would invent control flow the
    /// original never had.
    /// </summary>
    [Fact]
    public void RewriteForView_ClosingHandlerAwaitingSomethingElse_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "MessageBox.Show(\"Bye\");",
            FormWith(),
            Navigation(),
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldNames: []));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>...and the prefix before it still comes across.</summary>
    [Fact]
    public void RewriteForView_ClosingHandler_KeepsThePrefixBeforeTheAsyncStatement()
    {
        var form = FormWith(("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            this.statusLabel.Text = "closing";
            MessageBox.Show("Bye");
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "WindowClosingEventArgs", SourceControlFieldNames: []));

        Assert.Equal(["statusLabel.Text = \"closing\";"], result.MigratedStatements);
        Assert.False(result.RequiresAsync);
    }

    /// <summary>
    /// A bundled template is not a Direct mapping, but it *is* ours - so what it exposes is a
    /// known fact rather than a guess, and a RichTextBox (a TextBox-derived fallback) really does
    /// inherit the four font properties a WinForms Font becomes.
    /// </summary>
    [Fact]
    public void RewriteForView_FontOntoAFallbackControl_IsTranslated()
    {
        var form = FormWith(("notesRichTextBox", "RichTextBox"), ("fontDialog1", "FontDialog"));

        var result = Rewriter.RewriteForView(
            """
            if (this.fontDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.notesRichTextBox.Font = this.fontDialog1.Font;
            }
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", SourceControlFieldNames: []));

        var body = Assert.Single(result.MigratedStatements);
        Assert.Contains("notesRichTextBox.FontFamily = fontDialog1Font.Family;", body);
        Assert.Contains("notesRichTextBox.FontSize = fontDialog1Font.Size;", body);
        Assert.True(result.IsComplete);
    }

    /// <summary>
    /// A template that does not expose the group keeps refusing - the table is a whitelist, and
    /// a font written onto an element that has none is an error in the generated project.
    /// </summary>
    [Fact]
    public void RewriteForView_FontOntoAFallbackThatDoesNotExposeIt_IsNotMigrated()
    {
        var form = FormWith(("toolStrip1", "ToolStrip"), ("fontDialog1", "FontDialog"));

        var result = Rewriter.RewriteForView(
            """
            if (this.fontDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.toolStrip1.Font = this.fontDialog1.Font;
            }
            """,
            form,
            Navigation(),
            new HandlerSignature("e", "RoutedEventArgs", SourceControlFieldNames: []));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// The plain renames the catalog was missing. Each one is a WinForms property whose Avalonia
    /// counterpart is the same thing under another name - and every one is checked against the
    /// real TextBox in WinFormsToAvalonia.Mapping.Tests, which is what makes adding them safe.
    /// </summary>
    [Theory]
    [InlineData("this.nameTextBox.Multiline = true;", "nameTextBox.AcceptsReturn = true;")]
    [InlineData("this.nameTextBox.ReadOnly = true;", "nameTextBox.IsReadOnly = true;")]
    [InlineData("this.nameTextBox.MaxLength = 40;", "nameTextBox.MaxLength = 40;")]
    [InlineData("this.nameTextBox.SelectionStart = 0;", "nameTextBox.SelectionStart = 0;")]
    public void RewriteForView_TextBoxProperty_IsRenamed(string body, string expected)
    {
        var result = Rewriter.RewriteForView(body, FormWith(("nameTextBox", "TextBox")));

        Assert.Equal([expected], result.MigratedStatements);
    }

    /// <summary>
    /// The first entry whose *value* has to be rewritten rather than just its name: WinForms holds
    /// a bool where Avalonia holds a two-valued enum. Both directions, because a half-translated
    /// pair would read back something the write never meant.
    /// </summary>
    [Fact]
    public void RewriteForView_WordWrap_BecomesTextWrapping()
    {
        var form = FormWith(("notesTextBox", "TextBox"), ("wrapCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView(
            """
            this.notesTextBox.WordWrap = this.wrapCheckBox.Checked;
            this.wrapCheckBox.Checked = this.notesTextBox.WordWrap;
            """,
            form);

        Assert.Equal(
            [
                "notesTextBox.TextWrapping = ((wrapCheckBox.IsChecked ?? false)) ? TextWrapping.Wrap : TextWrapping.NoWrap;",
                "wrapCheckBox.IsChecked = (notesTextBox.TextWrapping == TextWrapping.Wrap);",
            ],
            result.MigratedStatements);
        Assert.Contains("Avalonia.Media", result.RequiredUsings);
    }

    /// <summary>
    /// A compound operator reads the property as well as writing it, and reading is the other half
    /// of the same conversion - which cannot be spliced into a left-hand side. So it refuses.
    /// </summary>
    [Fact]
    public void RewriteForView_CompoundAssignmentToAConvertedValue_IsNotMigrated()
    {
        var result = Rewriter.RewriteForView(
            "this.notesTextBox.WordWrap |= true;", FormWith(("notesTextBox", "TextBox")));

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// The one thing WinForms code usually asks a TabControl. Provable because the
    /// TabPage → TabItem mapping is this converter's own: a non-null SelectedItem *is* a TabItem,
    /// because the conversion made every page one.
    /// </summary>
    [Fact]
    public void RewriteForView_SelectedTabText_ReadsTheSelectedItemsHeader()
    {
        var form = FormWith(("tabControl1", "TabControl"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = this.tabControl1.SelectedTab?.Text ?? string.Empty;", form);

        Assert.Equal(
            ["statusLabel.Text = ((tabControl1.SelectedItem as TabItem)?.Header as string) ?? string.Empty;"],
            result.MigratedStatements);
    }

    /// <summary>
    /// Only the `?.` form. WinForms' SelectedTab is non-null whenever the control has pages, so
    /// `SelectedTab.Text` throws on an empty TabControl - and any translation of it would quietly
    /// return an empty string instead, which is a different program.
    /// </summary>
    [Fact]
    public void RewriteForView_SelectedTabTextWithoutTheConditional_IsNotMigrated()
    {
        var form = FormWith(("tabControl1", "TabControl"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = this.tabControl1.SelectedTab.Text;", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A `SelectedTab` on something that is not a TabControl is not this shape.</summary>
    [Fact]
    public void RewriteForView_SelectedTabOnAnotherControlType_IsNotMigrated()
    {
        var form = FormWith(("listBox1", "ListBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            "this.statusLabel.Text = this.listBox1.SelectedTab?.Text ?? string.Empty;", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A bundled template's <em>own</em> properties, which were invisible for as long as the
    /// member table existed - so a line touching one refused, not because there was nowhere to
    /// translate it, but because nobody had written the name down.
    /// </summary>
    [Theory]
    [InlineData("PropertyGrid", "propertyGrid1", "this.propertyGrid1.SelectedObject = null;", "propertyGrid1.SelectedObject = null;")]
    [InlineData("GroupBox", "groupBox1", "this.groupBox1.Text = \"Options\";", "groupBox1.Header = \"Options\";")]
    [InlineData("MaskedTextBox", "maskedTextBox1", "this.maskedTextBox1.Mask = \"000-000\";", "maskedTextBox1.Mask = \"000-000\";")]
    [InlineData("DomainUpDown", "domainUpDown1", "this.domainUpDown1.Wrap = true;", "domainUpDown1.Wrap = true;")]
    public void RewriteForView_FallbackTemplatesOwnProperty_IsTranslated(
        string winFormsType, string fieldName, string body, string expected)
    {
        var form = FormWith((fieldName, winFormsType), ("nameTextBox", "TextBox"));

        var result = Rewriter.RewriteForView(body, form);

        Assert.Equal([expected], result.MigratedStatements);
    }

    /// <summary>
    /// The table stays a whitelist: a property the template does not have is still refused, which
    /// is what keeps a fallback control from being written to at random.
    /// </summary>
    [Fact]
    public void RewriteForView_PropertyAFallbackTemplateDoesNotHave_IsNotMigrated()
    {
        var form = FormWith(("groupBox1", "GroupBox"));

        var result = Rewriter.RewriteForView("this.groupBox1.SelectedObject = null;", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A tree built at run time, which is most of what a Form_Load does to one. Avalonia's
    /// `ItemsControl.Items` is a real mutable collection and a `TreeViewItem.Header` is an
    /// `object`, so this has an exact counterpart - which is worth a test, because the converter
    /// refused it for a long time as "an application design decision".
    /// </summary>
    [Fact]
    public void RewriteForView_TreeNodesBuiltAtRunTime_BecomeTreeViewItems()
    {
        var form = FormWith(("itemsTreeView", "TreeView"));

        var result = Rewriter.RewriteForView(
            """
            this.itemsTreeView.Nodes.Clear();
            this.itemsTreeView.Nodes.Add("Documents");
            """,
            form);

        Assert.Equal(
            [
                "itemsTreeView.Items.Clear();",
                "itemsTreeView.Items.Add(new TreeViewItem { Header = \"Documents\" });",
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// WinForms hands back the node it just made, and Avalonia has no such call - so the one
    /// statement becomes the two it stood for, and the local is the same node either way.
    /// </summary>
    [Fact]
    public void RewriteForView_NodeReturnedByAdd_IsUsableAsAParent()
    {
        var form = FormWith(("itemsTreeView", "TreeView"));

        var result = Rewriter.RewriteForView(
            """
            var root = this.itemsTreeView.Nodes.Add("Reloaded");
            root.Nodes.Add("Child one");
            """,
            form);

        Assert.Equal(
            [
                "var root = new TreeViewItem { Header = \"Reloaded\" };\nitemsTreeView.Items.Add(root);",
                "root.Items.Add(new TreeViewItem { Header = \"Child one\" });",
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// Only a string header. A `TreeNode` carries an image index, a tag and children of its own,
    /// none of which a bare TreeViewItem has - so the shape is refused rather than half-translated.
    /// </summary>
    [Fact]
    public void RewriteForView_NodeBuiltFromATreeNodeObject_IsNotMigrated()
    {
        var form = FormWith(("itemsTreeView", "TreeView"));

        var result = Rewriter.RewriteForView(
            "this.itemsTreeView.Nodes.Add(new TreeNode(\"Documents\"));", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>`Nodes` on something that is not a TreeView is not this shape.</summary>
    [Fact]
    public void RewriteForView_NodesOnAnotherControlType_IsNotMigrated()
    {
        var form = FormWith(("listBox1", "ListBox"));

        var result = Rewriter.RewriteForView("this.listBox1.Nodes.Add(\"x\");", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// A ListView that became a ListBox: a single-column item has an exact answer.
    /// </summary>
    [Fact]
    public void RewriteForView_ListViewItemsOnAListBoxMapping_BecomeListBoxItems()
    {
        var form = FormWith(("itemsListView", "ListView"));

        var result = Rewriter.RewriteForView(
            """
            this.itemsListView.Items.Clear();
            this.itemsListView.Items.Add(new ListViewItem("readme.txt"));
            """,
            form);

        Assert.Equal(
            [
                "itemsListView.Items.Clear();",
                "itemsListView.Items.Add(new ListBoxItem { Content = \"readme.txt\" });",
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// The same ListView in Details mode is a DataGrid, whose rows are data objects bound through
    /// columns - turning a ListViewItem into one would mean inventing a row type, so it refuses.
    /// </summary>
    [Fact]
    public void RewriteForView_ListViewItemsOnADataGridMapping_IsNotMigrated()
    {
        var form = FormWith(("itemsListView", "ListView"));
        form.Controls["itemsListView"].Properties["View"] = new PropertyValue.EnumMembers(["Details"]);

        var result = Rewriter.RewriteForView(
            "this.itemsListView.Items.Add(new ListViewItem(\"readme.txt\"));", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>A multi-column item has no counterpart on a ListBox either.</summary>
    [Fact]
    public void RewriteForView_MultiColumnListViewItem_IsNotMigrated()
    {
        var form = FormWith(("itemsListView", "ListView"));

        var result = Rewriter.RewriteForView(
            "this.itemsListView.Items.Add(new ListViewItem(new[] { \"readme.txt\", \"2 KB\" }));", form);

        Assert.Empty(result.MigratedStatements);
    }

    // ---- Null-conditional and null-coalescing -------------------------------------------------

    /// <summary>
    /// `??` already went through the generic binary path; `?.` had no case at all. The receiver
    /// translating as an expression is what makes the rest safe - everything this rewriter can
    /// produce as a value is a plain BCL value.
    /// </summary>
    [Fact]
    public void RewriteForView_NullConditionalOnATranslatedValue_IsTranslated()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView(
            """
            var trimmed = this.nameTextBox.Text?.Trim();
            this.statusLabel.Text = trimmed ?? "empty";
            """,
            form);

        Assert.Equal(
            [
                "var trimmed = (nameTextBox.Text ?? string.Empty)?.Trim();",
                "statusLabel.Text = trimmed ?? \"empty\";",
            ],
            result.MigratedStatements);
    }

    /// <summary>
    /// A control field is not a value, so `textBox1?.Text` is refused rather than quietly
    /// reinterpreted as the property path with the null-check dropped.
    /// </summary>
    [Fact]
    public void RewriteForView_NullConditionalOnAControlField_IsNotMigrated()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("statusLabel", "Label"));

        var result = Rewriter.RewriteForView("this.statusLabel.Text = this.nameTextBox?.Text;", form);

        Assert.Empty(result.MigratedStatements);
    }

    /// <summary>
    /// An argument in the chain could name a control, and the chain is copied verbatim - so the
    /// whole thing is refused rather than half-rewritten.
    /// </summary>
    [Fact]
    public void RewriteForView_NullConditionalCallWithArguments_IsNotMigrated()
    {
        var form = FormWith(("nameTextBox", "TextBox"), ("prefixBox", "TextBox"), ("agreeCheckBox", "CheckBox"));

        var result = Rewriter.RewriteForView(
            "this.agreeCheckBox.Checked = this.nameTextBox.Text?.StartsWith(this.prefixBox.Text) == true;", form);

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
