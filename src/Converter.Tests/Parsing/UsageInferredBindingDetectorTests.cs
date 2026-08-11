using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class UsageInferredBindingDetectorTests
{
    private static readonly IReadOnlyDictionary<(string, string), string> NoExistingBindings =
        new Dictionary<(string, string), string>();

    [Fact]
    public void DetectInferredBindings_PropertyReferencedFromTwoDistinctMembers_IsPromoted()
    {
        var memberBodies = new[]
        {
            "private void LoadFromEntity() { skuTextBox.Text = entity.Sku; }",
            "private void SaveToEntity() { entity.Sku = skuTextBox.Text; }"
        };

        var result = UsageInferredBindingDetector.DetectInferredBindings(
            memberBodies, new HashSet<string> { "skuTextBox" }, NoExistingBindings);

        Assert.Equal("Sku", Assert.Contains(("skuTextBox", "Text"), result));
    }

    [Fact]
    public void DetectInferredBindings_PropertyReferencedFromOnlyOneMember_IsNotPromoted()
    {
        var memberBodies = new[]
        {
            "private void ClearForm() { skuTextBox.Text = string.Empty; }"
        };

        var result = UsageInferredBindingDetector.DetectInferredBindings(
            memberBodies, new HashSet<string> { "skuTextBox" }, NoExistingBindings);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectInferredBindings_AlreadyBoundPair_IsExcludedEvenWhenReferencedTwice()
    {
        var memberBodies = new[]
        {
            "private void LoadFromEntity() { skuTextBox.Text = entity.Sku; }",
            "private void SaveToEntity() { entity.Sku = skuTextBox.Text; }"
        };
        var alreadyBound = new Dictionary<(string, string), string> { [("skuTextBox", "Text")] = "Sku" };

        var result = UsageInferredBindingDetector.DetectInferredBindings(
            memberBodies, new HashSet<string> { "skuTextBox" }, alreadyBound);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectInferredBindings_IdentifierNotInControlNames_IsIgnored()
    {
        var memberBodies = new[]
        {
            "private void LoadFromEntity() { entity.Sku = \"X\"; }",
            "private void SaveToEntity() { entity.Sku = \"Y\"; }"
        };

        var result = UsageInferredBindingDetector.DetectInferredBindings(
            memberBodies, new HashSet<string> { "skuTextBox" }, NoExistingBindings);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("skuTextBox", "Sku")]
    [InlineData("quantityNumericUpDown", "Quantity")]
    [InlineData("activeCheckBox", "Active")]
    [InlineData("statusComboBox", "Status")]
    [InlineData("nameLabel", "Name")]
    [InlineData("saveButton", "Save")]
    // No recognized WinForms control-type suffix - the whole name is PascalCased as-is.
    [InlineData("total", "Total")]
    public void DerivePropertyName_StripsControlSuffix(string controlName, string expected)
    {
        Assert.Equal(expected, UsageInferredBindingDetector.DerivePropertyName(controlName));
    }

    [Fact]
    public void DerivePropertyName_LongestSuffixMatchesFirst()
    {
        // "MaskedTextBox" ends with "TextBox" too - must match the longer, more specific suffix.
        Assert.Equal("Phone", UsageInferredBindingDetector.DerivePropertyName("phoneMaskedTextBox"));
    }
}
