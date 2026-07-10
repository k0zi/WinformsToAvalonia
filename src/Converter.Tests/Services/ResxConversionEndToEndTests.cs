using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Services;

public class ResxConversionEndToEndTests
{
    // A minimal valid 1x1 PNG, base64-encoded.
    private const string OnePixelPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR42mNgAAACAAFVo+47AAAAAElFTkSuQmCC";

    private const string DesignerContent = """
        namespace SampleApp
        {
            partial class ResxE2EForm
            {
                private System.ComponentModel.ComponentResourceManager resources;
                private System.Windows.Forms.PictureBox pictureBox1;
                private System.Windows.Forms.Button button1;

                private void InitializeComponent()
                {
                    resources = new System.ComponentModel.ComponentResourceManager(typeof(ResxE2EForm));
                    this.pictureBox1 = new System.Windows.Forms.PictureBox();
                    this.button1 = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
                    this.pictureBox1.Name = "pictureBox1";
                    this.button1.Image = ((System.Drawing.Image)(resources.GetObject("button1.Image")));
                    this.button1.Name = "button1";
                    this.Controls.Add(this.pictureBox1);
                    this.Controls.Add(this.button1);
                    this.Name = "ResxE2EForm";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    private static string ResxContent(string base64) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <root>
            <data name="pictureBox1.Image" type="System.Drawing.Bitmap, System.Drawing" mimetype="application/x-microsoft.net.object.bytearray.base64">
                <value>{base64}</value>
            </data>
            <data name="button1.Image" type="System.Drawing.Bitmap, System.Drawing" mimetype="application/x-microsoft.net.object.binary.base64">
                <value>AAECAw==</value>
            </data>
        </root>
        """;

    [Fact]
    public async Task ExecuteAsync_WithSiblingResx_ExtractsImageAsset_QualifiesAxamlUri_AndFlagsUnrecoverableEntry()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "ResxE2EForm.Designer.cs"), DesignerContent);
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "ResxE2EForm.resx"), ResxContent(OnePixelPngBase64));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = true },
                ResourceConversion = new ResourceConversionConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            // The recoverable (bytearray-base64, real PNG magic bytes) entry produced an asset.
            var assetsDir = Path.Combine(outputDir, "Assets");
            Assert.True(Directory.Exists(assetsDir));
            var assetFiles = Directory.GetFiles(assetsDir, "*.png");
            Assert.Single(assetFiles);
            Assert.Contains("pictureBox1", Path.GetFileName(assetFiles[0]));

            // The AXAML references it via a fully-qualified avares:// URI, not a bare relative path.
            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "ResxE2EForm.axaml"));
            Assert.Contains($"avares://{Path.GetFileName(outputDir)}/Assets/", axamlContent);
            Assert.DoesNotContain("Source=\"Assets/", axamlContent);

            // The unrecoverable (BinaryFormatter envelope) entry produced a manual step, not a
            // fabricated/broken asset.
            var guideContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MIGRATION_GUIDE.md"));
            Assert.Contains("Unextractable Binary Resource", guideContent);
            Assert.Contains("button1.Image", guideContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ResourceConversionDisabled_PreservesRawPlaceholderBehavior()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "ResxE2EForm.Designer.cs"), DesignerContent);
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "ResxE2EForm.resx"), ResxContent(OnePixelPngBase64));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                ResourceConversion = new ResourceConversionConfig { Enabled = false }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            // No .resx was consulted at all - no Assets directory, and the Image property
            // (an opaque, unmapped raw-text value) is simply dropped, same as any other
            // RequiresConversion property whose raw value the converter can't parse.
            Assert.False(Directory.Exists(Path.Combine(outputDir, "Assets")));

            var axamlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "ResxE2EForm.axaml"));
            Assert.DoesNotContain("avares://", axamlContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
