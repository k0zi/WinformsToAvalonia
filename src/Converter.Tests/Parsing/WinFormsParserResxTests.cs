using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class WinFormsParserResxTests
{
    private const string DesignerWithResxReferences = """
        namespace SampleApp
        {
            partial class ResxForm
            {
                private System.ComponentModel.ComponentResourceManager resources;
                private System.Windows.Forms.Button button1;
                private System.Windows.Forms.PictureBox pictureBox1;

                private void InitializeComponent()
                {
                    resources = new System.ComponentModel.ComponentResourceManager(typeof(ResxForm));
                    this.button1 = new System.Windows.Forms.Button();
                    this.pictureBox1 = new System.Windows.Forms.PictureBox();
                    this.SuspendLayout();
                    this.button1.Text = ((string)(resources.GetObject("button1.Text")));
                    this.button1.Name = "button1";
                    this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
                    this.pictureBox1.Name = "pictureBox1";
                    resources.ApplyResources(this, "$this");
                    this.Controls.Add(this.button1);
                    this.Controls.Add(this.pictureBox1);
                    this.Name = "ResxForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private static async Task<string> WriteFixtureAsync(string content)
    {
        var dir = Directory.CreateTempSubdirectory("resx-parser-test-").FullName;
        var path = Path.Combine(dir, "ResxForm.Designer.cs");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Fact]
    public async Task ParseDesignerFileAsync_WithResources_ResolvesGetObjectStringValue()
    {
        var path = await WriteFixtureAsync(DesignerWithResxReferences);
        try
        {
            var resources = new Dictionary<string, ResxEntry>
            {
                ["button1.Text"] = new ResxEntry { Name = "button1.Text", StringValue = "Click Me" }
            };

            var result = await new WinFormsParser().ParseDesignerFileAsync(path, resources);

            var button1 = result.AllControls.Single(c => c.Name == "button1");
            var textProperty = button1.Properties["Text"];

            Assert.Equal("Click Me", textProperty.Value);
            Assert.True(textProperty.IsResource);
            Assert.Equal("button1.Text", textProperty.ResourceKey);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ParseDesignerFileAsync_WithResources_ResolvesGetObjectBinaryEntryAsResourceBinary()
    {
        var path = await WriteFixtureAsync(DesignerWithResxReferences);
        try
        {
            var resources = new Dictionary<string, ResxEntry>
            {
                ["pictureBox1.Image"] = new ResxEntry
                {
                    Name = "pictureBox1.Image",
                    BinaryValue = [0x89, 0x50, 0x4E, 0x47]
                }
            };

            var result = await new WinFormsParser().ParseDesignerFileAsync(path, resources);

            var pictureBox1 = result.AllControls.Single(c => c.Name == "pictureBox1");
            var imageProperty = pictureBox1.Properties["Image"];

            Assert.Null(imageProperty.Value);
            Assert.Equal("resource-binary", imageProperty.Type);
            Assert.True(imageProperty.IsResource);
            Assert.Equal("pictureBox1.Image", imageProperty.ResourceKey);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ParseDesignerFileAsync_WithResources_ResolvesApplyResourcesPrefixedEntries()
    {
        var path = await WriteFixtureAsync(DesignerWithResxReferences);
        try
        {
            var resources = new Dictionary<string, ResxEntry>
            {
                ["$this.Text"] = new ResxEntry { Name = "$this.Text", StringValue = "Resx Form Title" },
                ["$this.AutoScaleDimensions"] = new ResxEntry
                {
                    Name = "$this.AutoScaleDimensions", StringValue = "6, 13"
                }
            };

            var result = await new WinFormsParser().ParseDesignerFileAsync(path, resources);

            Assert.Equal("Resx Form Title", result.RootControl!.Properties["Text"].Value);
            Assert.Equal("6, 13", result.RootControl.Properties["AutoScaleDimensions"].Value);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ParseDesignerFileAsync_WithoutResources_LeavesRawTextBehaviorUnchanged()
    {
        var path = await WriteFixtureAsync(DesignerWithResxReferences);
        try
        {
            // resources: null (default) - the pre-existing behavior for every caller that
            // doesn't pass a resx dictionary must be completely unaffected.
            var result = await new WinFormsParser().ParseDesignerFileAsync(path);

            var button1 = result.AllControls.Single(c => c.Name == "button1");
            var textProperty = button1.Properties["Text"];

            Assert.False(textProperty.IsResource);
            Assert.Null(textProperty.ResourceKey);
            Assert.Contains("resources.GetObject", textProperty.Value?.ToString());
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ParseDesignerFileAsync_ResxKeyNotFound_FallsBackToRawText()
    {
        var path = await WriteFixtureAsync(DesignerWithResxReferences);
        try
        {
            // An empty resx dictionary means the lookup misses for every key.
            var result = await new WinFormsParser().ParseDesignerFileAsync(
                path, new Dictionary<string, ResxEntry>());

            var button1 = result.AllControls.Single(c => c.Name == "button1");
            var textProperty = button1.Properties["Text"];

            Assert.False(textProperty.IsResource);
            Assert.Contains("resources.GetObject", textProperty.Value?.ToString());
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
