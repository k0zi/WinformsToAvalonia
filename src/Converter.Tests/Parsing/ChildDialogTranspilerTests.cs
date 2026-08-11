using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class ChildDialogTranspilerTests
{
    private static readonly IReadOnlySet<(string FormName, string DialogResultValue)> PickerFormHasOkButton =
        new HashSet<(string, string)> { ("PickerForm", "OK") };

    [Fact]
    public void Transpile_ParameterlessConstructorWithMatchingButton_RewritesToShowChildAsync()
    {
        var body = """
            {
                using var form = new PickerForm();
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    Reload();
                }
            }
            """;

        var result = ChildDialogTranspiler.Transpile(body, "SampleApp", PickerFormHasOkButton);

        Assert.True(result.AddedAwait);
        Assert.Contains(
            "await SampleApp.Common.Dialogs.ShowChildAsync<SampleApp.Views.PickerForm, SampleApp.ViewModels.PickerFormViewModel>()",
            result.TransformedBody);
        Assert.Contains("== SampleApp.Common.DialogResult.OK", result.TransformedBody);
        Assert.Contains("Reload();", result.TransformedBody);
        Assert.DoesNotContain("ShowDialog", result.TransformedBody);
    }

    [Fact]
    public void Transpile_PlainVarWithoutUsing_AlsoRewrites()
    {
        var body = """
            {
                var form = new PickerForm();
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    Reload();
                }
            }
            """;

        var result = ChildDialogTranspiler.Transpile(body, "SampleApp", PickerFormHasOkButton);

        Assert.True(result.AddedAwait);
        Assert.Contains("ShowChildAsync<SampleApp.Views.PickerForm, SampleApp.ViewModels.PickerFormViewModel>()", result.TransformedBody);
    }

    [Fact]
    public void Transpile_ConstructorWithArguments_IsLeftUntouched()
    {
        // The "edit" flow (new OtherForm(entity)) is deliberately out of scope - threading an
        // arbitrary constructor argument into a generically-generated ViewModel without
        // knowing its load contract would be an unsafe guess.
        var body = """
            {
                using var form = new PickerForm(existingEntity);
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    Reload();
                }
            }
            """;

        var result = ChildDialogTranspiler.Transpile(body, "SampleApp", PickerFormHasOkButton);

        Assert.False(result.AddedAwait);
        Assert.Equal(body, result.TransformedBody);
    }

    [Fact]
    public void Transpile_FormNotInDialogResultButtonSet_IsLeftUntouched()
    {
        // No button on "OtherForm" is known to close it with a result - rewriting the caller
        // would risk a dialog that silently never closes.
        var body = """
            {
                using var form = new OtherForm();
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    Reload();
                }
            }
            """;

        var result = ChildDialogTranspiler.Transpile(body, "SampleApp", PickerFormHasOkButton);

        Assert.False(result.AddedAwait);
        Assert.Equal(body, result.TransformedBody);
    }

    [Fact]
    public void Transpile_MismatchedDialogResultValue_IsLeftUntouched()
    {
        // PickerForm only has an OK button in the fixture set - comparing against Cancel has
        // no matching close path.
        var body = """
            {
                using var form = new PickerForm();
                if (form.ShowDialog(this) == DialogResult.Cancel)
                {
                    Reload();
                }
            }
            """;

        var result = ChildDialogTranspiler.Transpile(body, "SampleApp", PickerFormHasOkButton);

        Assert.False(result.AddedAwait);
        Assert.Equal(body, result.TransformedBody);
    }

    [Fact]
    public void Transpile_NonMatchingCode_IsUnaffected()
    {
        var body = """
            {
                var x = 1 + 2;
                DoSomethingElse();
            }
            """;

        var result = ChildDialogTranspiler.Transpile(body, "SampleApp", PickerFormHasOkButton);

        Assert.False(result.AddedAwait);
        Assert.Equal(body, result.TransformedBody);
    }

    [Fact]
    public void TranspileMethod_FullMethodText_RewritesBodyAndPreservesSignature()
    {
        var fullMethod = """
            private void AddNew()
            {
                using var form = new PickerForm();
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    Reload();
                }
            }
            """;

        var result = ChildDialogTranspiler.TranspileMethod(fullMethod, "SampleApp", PickerFormHasOkButton);

        Assert.True(result.AddedAwait);
        Assert.Contains("private void AddNew()", result.TransformedBody);
        Assert.Contains("ShowChildAsync<SampleApp.Views.PickerForm, SampleApp.ViewModels.PickerFormViewModel>()", result.TransformedBody);
    }

    [Fact]
    public void TranspileMethod_NonTaskReturnType_LeavesMethodUntouchedInsteadOfInvalidAsync()
    {
        var fullMethod = """
            internal bool AddNew()
            {
                using var form = new PickerForm();
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    return true;
                }
                return false;
            }
            """;

        var result = ChildDialogTranspiler.TranspileMethod(fullMethod, "SampleApp", PickerFormHasOkButton);

        Assert.False(result.AddedAwait);
        Assert.Equal(fullMethod, result.TransformedBody);
    }

    [Fact]
    public void TranspileMethod_NoMatchingPattern_LeavesMethodUnchanged()
    {
        var fullMethod = """
            private void DoSomething()
            {
                var x = 1;
            }
            """;

        var result = ChildDialogTranspiler.TranspileMethod(fullMethod, "SampleApp", PickerFormHasOkButton);

        Assert.False(result.AddedAwait);
        Assert.Equal(fullMethod, result.TransformedBody);
    }
}
