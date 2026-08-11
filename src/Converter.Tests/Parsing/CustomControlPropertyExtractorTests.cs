using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class CustomControlPropertyExtractorTests
{
    private const string CustomerCardCodeBehind = """
        namespace SampleApp
        {
            public partial class CustomerCard : System.Windows.Forms.UserControl
            {
                public int CustomerId { get; set; }

                public string CardTitle
                {
                    get => titleLabel.Text;
                    set => titleLabel.Text = value;
                }

                public object Payload { get; set; }

                public System.Collections.Generic.List<string> Tags { get; set; }

                public object? SelectedItem { get; private set; }

                public string ReadOnlyNote { get; }

                private string InternalState { get; set; }

                public static string SharedThing { get; set; }
            }
        }
        """;

    private static async Task<CustomControlPropertyExtractionResult> ExtractAsync(string content, string className = "CustomerCard")
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-customcontrolprops-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, content);
        try
        {
            return await CustomControlPropertyExtractor.ExtractAsync(path, className);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractAsync_PublicAutoPropertyOfSupportedType_IsBindable()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        var property = Assert.Single(result.Bindable, p => p.Name == "CustomerId");
        Assert.Equal("int", property.TypeName);
        Assert.Equal(CustomControlPropertyKind.PlainBindable, property.Kind);
        Assert.Null(property.BackingFieldName);
    }

    [Fact]
    public async Task ExtractAsync_PropertyDelegatingToChildControlMember_IsDelegating()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        var property = Assert.Single(result.Delegating, p => p.Name == "CardTitle");
        Assert.Equal("titleLabel", property.FieldName);
        Assert.Equal("Text", property.MemberName);
        Assert.Null(property.FallbackExpression);
        Assert.False(property.WasOverride);
        Assert.DoesNotContain(result.Skipped, p => p.Name == "CardTitle");
        Assert.DoesNotContain(result.Bindable, p => p.Name == "CardTitle");
    }

    [Fact]
    public async Task ExtractAsync_ObjectTypedProperty_IsNowBindable()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        var property = Assert.Single(result.Bindable, p => p.Name == "Payload");
        Assert.Equal("object", property.TypeName);
        Assert.Equal(CustomControlPropertyKind.PlainBindable, property.Kind);
        Assert.DoesNotContain(result.Skipped, p => p.Name == "Payload");
    }

    [Fact]
    public async Task ExtractAsync_ObjectTypedAutoPropertyWithPrivateSetter_IsBindable()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        var property = Assert.Single(result.Bindable, p => p.Name == "SelectedItem");
        Assert.Equal("object?", property.TypeName);
        Assert.Equal(CustomControlPropertyKind.PlainBindable, property.Kind);
    }

    [Fact]
    public async Task ExtractAsync_GenuinelyUnsupportedType_IsSkipped()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        var skipped = Assert.Single(result.Skipped, p => p.Name == "Tags");
        Assert.Contains("not supported", skipped.Reason);
        Assert.DoesNotContain(result.Bindable, p => p.Name == "Tags");
    }

    [Fact]
    public async Task ExtractAsync_GetterOnlyProperty_IsSkippedNotBindable()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        Assert.DoesNotContain(result.Bindable, p => p.Name == "ReadOnlyNote");
        Assert.Contains(result.Skipped, p => p.Name == "ReadOnlyNote");
    }

    [Fact]
    public async Task ExtractAsync_NonPublicProperty_IsIgnoredEntirely()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        Assert.DoesNotContain(result.Bindable, p => p.Name == "InternalState");
        Assert.DoesNotContain(result.Skipped, p => p.Name == "InternalState");
    }

    [Fact]
    public async Task ExtractAsync_StaticProperty_IsIgnoredEntirely()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind);

        Assert.DoesNotContain(result.Bindable, p => p.Name == "SharedThing");
        Assert.DoesNotContain(result.Skipped, p => p.Name == "SharedThing");
    }

    [Fact]
    public async Task ExtractAsync_ClassNameNotFound_ReturnsEmpty()
    {
        var result = await ExtractAsync(CustomerCardCodeBehind, className: "SomethingElse");

        Assert.Empty(result.Bindable);
        Assert.Empty(result.Delegating);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task ExtractAsync_MissingFile_ReturnsEmpty()
    {
        var result = await CustomControlPropertyExtractor.ExtractAsync(
            Path.Combine(Path.GetTempPath(), "wf2av-does-not-exist.cs"), "CustomerCard");

        Assert.Empty(result.Bindable);
        Assert.Empty(result.Delegating);
        Assert.Empty(result.Skipped);
    }

    private const string NumericStepperCodeBehind = """
        namespace SampleApp
        {
            public partial class NumericStepperControl : System.Windows.Forms.UserControl
            {
                private decimal _increment = 1;

                public decimal Increment
                {
                    get => _increment;
                    set => _increment = value;
                }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_TrivialExpressionBodiedFieldPassthrough_IsBindable()
    {
        var result = await ExtractAsync(NumericStepperCodeBehind, className: "NumericStepperControl");

        var property = Assert.Single(result.Bindable, p => p.Name == "Increment");
        Assert.Equal(CustomControlPropertyKind.PlainBindable, property.Kind);
        Assert.Equal("_increment", property.BackingFieldName);
    }

    private const string AutocompleteSearchBoxCodeBehind = """
        namespace SampleApp
        {
            public partial class AutocompleteSearchBox : System.Windows.Forms.UserControl
            {
                private System.Windows.Forms.TextBox _textBox;

                public string PlaceholderText
                {
                    get => _textBox.PlaceholderText;
                    set => _textBox.PlaceholderText = value;
                }

                [System.Diagnostics.CodeAnalysis.AllowNull]
                public override string Text
                {
                    get => _textBox.Text;
                    set => _textBox.Text = value ?? string.Empty;
                }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_DelegatingToSingleFieldMember_IsDelegating()
    {
        var result = await ExtractAsync(AutocompleteSearchBoxCodeBehind, className: "AutocompleteSearchBox");

        var property = Assert.Single(result.Delegating, p => p.Name == "PlaceholderText");
        Assert.Equal("_textBox", property.FieldName);
        Assert.Equal("PlaceholderText", property.MemberName);
        Assert.Null(property.FallbackExpression);
    }

    [Fact]
    public async Task ExtractAsync_OverrideDelegatingPropertyWithCoalesceFallback_StripsOverrideAndCapturesFallback()
    {
        var result = await ExtractAsync(AutocompleteSearchBoxCodeBehind, className: "AutocompleteSearchBox");

        var property = Assert.Single(result.Delegating, p => p.Name == "Text");
        Assert.Equal("_textBox", property.FieldName);
        Assert.Equal("Text", property.MemberName);
        Assert.True(property.WasOverride);
        Assert.Equal("string.Empty", property.FallbackExpression);
    }

    private const string ClampSetterCodeBehind = """
        namespace SampleApp
        {
            public partial class RangeControl : System.Windows.Forms.UserControl
            {
                private decimal _value;

                public decimal Value
                {
                    get => _value;
                    set => _value = Math.Clamp(value, Minimum, Maximum);
                }

                public decimal Minimum { get; set; }

                public decimal Maximum { get; set; } = 100;
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_SelfContainedClampSetterUsingSiblingProperty_IsCoerced()
    {
        var result = await ExtractAsync(ClampSetterCodeBehind, className: "RangeControl");

        var property = Assert.Single(result.Bindable, p => p.Name == "Value");
        Assert.Equal(CustomControlPropertyKind.Coerced, property.Kind);
        Assert.Equal("_value", property.BackingFieldName);
        Assert.Equal("CoerceValue", property.CoerceMethodName);
        Assert.NotNull(property.CoerceMethodBody);
        Assert.Contains("((RangeControl)sender).Minimum", property.CoerceMethodBody);
        Assert.Contains("((RangeControl)sender).Maximum", property.CoerceMethodBody);
        Assert.Contains("Avalonia.AvaloniaObject sender", property.CoerceMethodBody);
    }

    private const string UnsafeSetterCodeBehind = """
        namespace SampleApp
        {
            public partial class SkuControl : System.Windows.Forms.UserControl
            {
                private string _sku = string.Empty;

                public string Sku
                {
                    get => _sku;
                    set => _sku = Normalize(value);
                }

                private static string Normalize(string value) => value.Trim().ToUpperInvariant();
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_SingleStatementSetterWithUnsafeRhs_IsCoercionFallback()
    {
        var result = await ExtractAsync(UnsafeSetterCodeBehind, className: "SkuControl");

        var property = Assert.Single(result.Bindable, p => p.Name == "Sku");
        Assert.Equal(CustomControlPropertyKind.CoercionFallback, property.Kind);
        Assert.Equal("_sku", property.BackingFieldName);
        Assert.Null(property.CoerceMethodName);
        Assert.Null(property.CoerceMethodBody);
    }

    private const string MultiStatementCrossPropertyCodeBehind = """
        namespace SampleApp
        {
            public partial class NumericStepperControl : System.Windows.Forms.UserControl
            {
                private decimal _value;
                private decimal _minimum;
                private decimal _maximum = 1000;

                public decimal Value
                {
                    get => _value;
                    set => _value = value;
                }

                public decimal Minimum
                {
                    get => _minimum;
                    set { _minimum = value; Value = Math.Clamp(_value, _minimum, _maximum); }
                }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_MultiStatementSetterWithCrossPropertyWrite_StaysGenericSkipped()
    {
        var result = await ExtractAsync(MultiStatementCrossPropertyCodeBehind, className: "NumericStepperControl");

        var skipped = Assert.Single(result.Skipped, p => p.Name == "Minimum");
        Assert.Equal("has custom getter/setter logic", skipped.Reason);
        Assert.DoesNotContain(result.Bindable, p => p.Name == "Minimum");
    }

    private const string FieldReferencingSetterCodeBehind = """
        namespace SampleApp
        {
            public partial class NumericStepperControl : System.Windows.Forms.UserControl
            {
                private decimal _value;
                private decimal _minimum;
                private decimal _maximum = 1000;

                public decimal Value
                {
                    get => _value;
                    set => _value = Math.Clamp(value, _minimum, _maximum);
                }
            }
        }
        """;

    [Fact]
    public async Task ExtractAsync_SetterReadingSiblingPrivateFieldInsteadOfProperty_IsCoercionFallbackNotCoerced()
    {
        // Reading a sibling *private field* (rather than a public property) is outside the
        // safe-translatable subset (only "value"/public-property reads/literals/Math.* are
        // allowed) - same as any other unsafe RHS, this becomes a bindable-but-unvalidated
        // CoercionFallback (plus a manual-step note), never a full Coerced translation and
        // never a silent full Skip (that would be a regression vs. "bindable, no
        // validation" - the property still gets created, unlike today's baseline).
        var result = await ExtractAsync(FieldReferencingSetterCodeBehind, className: "NumericStepperControl");

        var property = Assert.Single(result.Bindable, p => p.Name == "Value");
        Assert.Equal(CustomControlPropertyKind.CoercionFallback, property.Kind);
        Assert.Null(property.CoerceMethodName);
        Assert.DoesNotContain(result.Skipped, p => p.Name == "Value");
    }
}
