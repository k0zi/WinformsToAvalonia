using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class MessageBoxTranspilerTests
{
    [Fact]
    public void Transpile_FiveArgOverload_RewritesToAwaitDialogsShowAsync()
    {
        var body = """
            {
                MessageBox.Show(this, "Customer name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            """;

        var result = MessageBoxTranspiler.Transpile(body, "SampleApp");

        Assert.True(result.AddedAwait);
        Assert.Contains("await SampleApp.Common.Dialogs.ShowAsync(", result.TransformedBody);
        Assert.Contains("\"Customer name is required.\"", result.TransformedBody);
        Assert.Contains("\"Validation\"", result.TransformedBody);
        Assert.Contains("SampleApp.Common.MessageBoxButtons.OK", result.TransformedBody);
        Assert.Contains("SampleApp.Common.MessageBoxIcon.Warning", result.TransformedBody);
        Assert.DoesNotContain("MessageBox.Show", result.TransformedBody);
    }

    [Fact]
    public void Transpile_OwnerArgument_IsDropped()
    {
        var body = """
            {
                MessageBox.Show(this, "Text", "Caption", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            """;

        var result = MessageBoxTranspiler.Transpile(body, "SampleApp");

        // "this" (the owner) must not survive into the rewritten call - a ViewModel body has no
        // View reference to pass, and Dialogs.ShowAsync resolves its own parent window.
        Assert.DoesNotContain("this,", result.TransformedBody);
    }

    [Fact]
    public void Transpile_StandaloneDialogResultReference_IsAlsoQualified()
    {
        var body = """
            {
                var confirm = MessageBox.Show(this, "Delete it?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                {
                    return;
                }
            }
            """;

        var result = MessageBoxTranspiler.Transpile(body, "SampleApp");

        Assert.Contains("SampleApp.Common.DialogResult.Yes", result.TransformedBody);
    }

    [Fact]
    public void Transpile_NonMessageBoxCode_IsUnaffectedAndAwaitNotAdded()
    {
        var body = """
            {
                var x = 1 + 2;
                DoSomethingElse();
            }
            """;

        var result = MessageBoxTranspiler.Transpile(body, "SampleApp");

        Assert.False(result.AddedAwait);
        Assert.Equal(body, result.TransformedBody);
    }

    [Fact]
    public void Transpile_UnsupportedOverload_LeavesCallUntouched()
    {
        // A 2-arg overload isn't recognized - left exactly as-is, falling through to the
        // existing WinFormsTypeUsageDetector-based manual step instead of a wrong rewrite.
        var body = """
            {
                MessageBox.Show("Just text");
            }
            """;

        var result = MessageBoxTranspiler.Transpile(body, "SampleApp");

        Assert.False(result.AddedAwait);
        Assert.Contains("MessageBox.Show(\"Just text\")", result.TransformedBody);
    }

    [Fact]
    public void TranspileMethod_FullMethodText_RewritesBodyAndPreservesSignature()
    {
        // Transpile (body-only) silently fails to parse when fed a *full* method (the wrapper
        // becomes "void __M() private void Foo() { ... }" - invalid). TranspileMethod is the
        // full-method counterpart ViewModelGenerator's helper-method loop must use instead,
        // since a helper method's original signature is migrated verbatim, not reconstructed.
        var fullMethod = """
            private async Task SaveCustomerAsync()
            {
                MessageBox.Show(this, "Customer name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            """;

        var result = MessageBoxTranspiler.TranspileMethod(fullMethod, "SampleApp");

        Assert.True(result.AddedAwait);
        Assert.Contains("private async Task SaveCustomerAsync()", result.TransformedBody);
        Assert.Contains("await SampleApp.Common.Dialogs.ShowAsync(", result.TransformedBody);
        Assert.DoesNotContain("MessageBox.Show", result.TransformedBody);
    }

    [Fact]
    public void TranspileMethod_NonTaskReturnType_LeavesMethodUntouchedInsteadOfInvalidAsync()
    {
        // "async" is only legal on void/Task/Task<T> (or an already-async method) - a
        // bool-returning helper method (e.g. ValidateInput()) must not become "async bool",
        // which doesn't compile. Found via a real build against WarehouseApp.
        var fullMethod = """
            internal bool ValidateInput()
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    MessageBox.Show(this, "Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            """;

        var result = MessageBoxTranspiler.TranspileMethod(fullMethod, "SampleApp");

        Assert.False(result.AddedAwait);
        Assert.Equal(fullMethod, result.TransformedBody);
    }

    [Fact]
    public void TranspileMethod_AlreadyAsyncNonTaskLikeIsImpossible_TaskReturnType_IsRewritten()
    {
        // Sanity check for the boundary case right next to the bool-return regression above:
        // a Task-returning method is legitimately safe to rewrite.
        var fullMethod = """
            internal Task SaveAsync()
            {
                MessageBox.Show(this, "Saved.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return Task.CompletedTask;
            }
            """;

        var result = MessageBoxTranspiler.TranspileMethod(fullMethod, "SampleApp");

        Assert.True(result.AddedAwait);
        Assert.Contains("await SampleApp.Common.Dialogs.ShowAsync(", result.TransformedBody);
    }

    [Fact]
    public void TranspileMethod_NoMessageBoxCall_LeavesMethodUnchanged()
    {
        var fullMethod = """
            private void DoSomething()
            {
                var x = 1;
            }
            """;

        var result = MessageBoxTranspiler.TranspileMethod(fullMethod, "SampleApp");

        Assert.False(result.AddedAwait);
        Assert.Equal(fullMethod, result.TransformedBody);
    }

    [Fact]
    public void Transpile_MultipleCallsInSameBody_AllRewritten()
    {
        var body = """
            {
                MessageBox.Show(this, "First", "Caption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MessageBox.Show(this, "Second", "Caption", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            """;

        var result = MessageBoxTranspiler.Transpile(body, "SampleApp");

        Assert.Equal(2, result.TransformedBody.Split("await SampleApp.Common.Dialogs.ShowAsync").Length - 1);
    }
}
