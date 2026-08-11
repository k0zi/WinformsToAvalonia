using Converter.Core.Parsing;
using Converter.Plugin.Abstractions;

namespace Converter.Tests.Parsing;

public class CodeBehindEventSubscriptionDetectorTests
{
    private static ControlNode Root(string name, params ControlNode[] children) => new()
    {
        ControlType = "Form",
        FullTypeName = "System.Windows.Forms.Form",
        Name = name,
        Children = [.. children]
    };

    private static ControlNode Child(string name, string controlType) => new()
    {
        ControlType = controlType,
        FullTypeName = $"System.Windows.Forms.{controlType}",
        Name = name
    };

    private static async Task<string> WriteTempFileAsync(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wf2av-ctorevents-{Path.GetRandomFileName()}.cs");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Fact]
    public async Task MergeConstructorEventSubscriptionsAsync_BareIdentifier_MergesIntoRootEventHandlers()
    {
        var content = """
            namespace SampleApp
            {
                partial class SampleForm
                {
                    public SampleForm()
                    {
                        InitializeComponent();
                        Load += SampleForm_Load;
                    }

                    private void SampleForm_Load(object? sender, System.EventArgs e) { }
                }
            }
            """;
        var path = await WriteTempFileAsync(content);
        var root = Root("SampleForm");

        try
        {
            await CodeBehindEventSubscriptionDetector.MergeConstructorEventSubscriptionsAsync(root, path);
            Assert.Equal("SampleForm_Load", root.EventHandlers["Load"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MergeConstructorEventSubscriptionsAsync_ExplicitThis_MergesIntoRootEventHandlers()
    {
        var content = """
            namespace SampleApp
            {
                partial class SampleForm
                {
                    public SampleForm()
                    {
                        InitializeComponent();
                        this.FormClosing += this.SampleForm_FormClosing;
                    }

                    private void SampleForm_FormClosing(object? sender, System.EventArgs e) { }
                }
            }
            """;
        var path = await WriteTempFileAsync(content);
        var root = Root("SampleForm");

        try
        {
            await CodeBehindEventSubscriptionDetector.MergeConstructorEventSubscriptionsAsync(root, path);
            Assert.Equal("SampleForm_FormClosing", root.EventHandlers["FormClosing"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MergeConstructorEventSubscriptionsAsync_NamedChildControl_MergesIntoThatControlsEventHandlers()
    {
        var content = """
            namespace SampleApp
            {
                partial class SampleForm
                {
                    public SampleForm()
                    {
                        InitializeComponent();
                        saveButton.Click += SaveButton_Click;
                    }

                    private void SaveButton_Click(object? sender, System.EventArgs e) { }
                }
            }
            """;
        var path = await WriteTempFileAsync(content);
        var button = Child("saveButton", "Button");
        var root = Root("SampleForm", button);

        try
        {
            await CodeBehindEventSubscriptionDetector.MergeConstructorEventSubscriptionsAsync(root, path);
            Assert.Equal("SaveButton_Click", button.EventHandlers["Click"]);
            Assert.Empty(root.EventHandlers);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MergeConstructorEventSubscriptionsAsync_UnrecognizedEventName_IsIgnored()
    {
        // "Total" isn't a recognized WinForms event name - this must not be mistaken for a
        // subscription (guards against false positives on ordinary "+=" arithmetic).
        var content = """
            namespace SampleApp
            {
                partial class SampleForm
                {
                    private int total;

                    public SampleForm()
                    {
                        InitializeComponent();
                        Total += Increment;
                    }

                    private int Increment => 1;
                }
            }
            """;
        var path = await WriteTempFileAsync(content);
        var root = Root("SampleForm");

        try
        {
            await CodeBehindEventSubscriptionDetector.MergeConstructorEventSubscriptionsAsync(root, path);
            Assert.Empty(root.EventHandlers);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MergeConstructorEventSubscriptionsAsync_DesignerDrivenEntryAlreadyPresent_IsNeverOverwritten()
    {
        var content = """
            namespace SampleApp
            {
                partial class SampleForm
                {
                    public SampleForm()
                    {
                        InitializeComponent();
                        Load += SomeOtherHandler;
                    }

                    private void SomeOtherHandler(object? sender, System.EventArgs e) { }
                }
            }
            """;
        var path = await WriteTempFileAsync(content);
        var root = Root("SampleForm");
        root.EventHandlers["Load"] = "SampleForm_Load"; // Designer.cs-driven wiring already present.

        try
        {
            await CodeBehindEventSubscriptionDetector.MergeConstructorEventSubscriptionsAsync(root, path);
            Assert.Equal("SampleForm_Load", root.EventHandlers["Load"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MergeConstructorEventSubscriptionsAsync_UnrelatedNestedTypeConstructor_IsIgnored()
    {
        // Only SampleForm's own constructor should be scanned - a same-named-event
        // subscription inside an unrelated nested type's constructor must not leak in.
        var content = """
            namespace SampleApp
            {
                partial class SampleForm
                {
                    public SampleForm()
                    {
                        InitializeComponent();
                    }

                    private sealed class Helper
                    {
                        public Helper()
                        {
                            Load += Helper_Load;
                        }

                        private void Helper_Load(object? sender, System.EventArgs e) { }
                    }
                }
            }
            """;
        var path = await WriteTempFileAsync(content);
        var root = Root("SampleForm");

        try
        {
            await CodeBehindEventSubscriptionDetector.MergeConstructorEventSubscriptionsAsync(root, path);
            Assert.Empty(root.EventHandlers);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
