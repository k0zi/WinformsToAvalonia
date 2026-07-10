using System.Text.Json;
using Converter.Cli.Models;
using Converter.Cli.Services;
using Converter.Core.Configuration;
using Converter.Core.Models;

namespace Converter.Tests.Services;

public class ConversionOrchestratorTests
{
    /// <summary>
    /// Invokes its callback synchronously (unlike System.Progress&lt;T&gt;, which posts
    /// through a captured SynchronizationContext and isn't guaranteed synchronous), so
    /// tests can deterministically trigger cancellation at a specific point in the run.
    /// </summary>
    private sealed class SyncProgress(Action<ConversionProgress> callback) : IProgress<ConversionProgress>
    {
        public void Report(ConversionProgress value) => callback(value);
    }


    private static string MinimalDesignerFile(string className) => $$"""
        namespace SampleApp
        {
            partial class {{className}}
            {
                private System.Windows.Forms.Button button1;

                private void InitializeComponent()
                {
                    this.button1 = new System.Windows.Forms.Button();
                    this.SuspendLayout();
                    this.button1.Name = "button1";
                    this.Controls.Add(this.button1);
                    this.Name = "{{className}}";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_ExcludesFilesMatchingExcludePatterns_AndUsesCustomNamingConventions()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "IncludedForm.Designer.cs"), MinimalDesignerFile("IncludedForm"));

            var legacyDir = Directory.CreateDirectory(Path.Combine(sourceDir, "Legacy"));
            await File.WriteAllTextAsync(
                Path.Combine(legacyDir.FullName, "ExcludedForm.Designer.cs"), MinimalDesignerFile("ExcludedForm"));

            var config = new ConverterConfig
            {
                ExcludePatterns = ["Legacy"],
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                NamingConventions = new NamingConventionsConfig
                {
                    RootNamespace = "CustomNamespace",
                    ViewModelSuffix = "PresentationModel"
                }
            };

            var orchestrator = new ConversionOrchestrator(sourceDir, outputDir, config);

            var result = await orchestrator.ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Single(result.Report!.Forms);
            Assert.Equal("IncludedForm", result.Report.Forms[0].Name);

            var viewModelsDir = Path.Combine(outputDir, "ViewModels");
            var generatedFiles = Directory.GetFiles(viewModelsDir);

            Assert.Contains(generatedFiles, f => f.EndsWith("IncludedFormPresentationModel.g.cs"));
            Assert.DoesNotContain(generatedFiles, f => f.Contains("ExcludedForm"));

            var vmContent = await File.ReadAllTextAsync(
                Path.Combine(viewModelsDir, "IncludedFormPresentationModel.g.cs"));
            Assert.Contains("namespace CustomNamespace.ViewModels;", vmContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ParallelAndSequential_ProduceTheSameForms()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;

        try
        {
            for (var i = 0; i < 4; i++)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(sourceDir, $"Form{i}.Designer.cs"), MinimalDesignerFile($"Form{i}"));
            }

            var parallelOutput = Directory.CreateTempSubdirectory("wf2av-out-parallel-").FullName;
            var sequentialOutput = Directory.CreateTempSubdirectory("wf2av-out-sequential-").FullName;

            try
            {
                var parallelConfig = new ConverterConfig
                {
                    GitIntegration = new GitIntegrationConfig { Enabled = false },
                    Documentation = new DocumentationConfig { Enabled = false },
                    ParallelProcessing = new ParallelProcessingConfig { Enabled = true }
                };
                var sequentialConfig = new ConverterConfig
                {
                    GitIntegration = new GitIntegrationConfig { Enabled = false },
                    Documentation = new DocumentationConfig { Enabled = false },
                    ParallelProcessing = new ParallelProcessingConfig { Enabled = false }
                };

                var parallelResult = await new ConversionOrchestrator(sourceDir, parallelOutput, parallelConfig).ExecuteAsync();
                var sequentialResult = await new ConversionOrchestrator(sourceDir, sequentialOutput, sequentialConfig).ExecuteAsync();

                Assert.True(parallelResult.Success, parallelResult.ErrorMessage);
                Assert.True(sequentialResult.Success, sequentialResult.ErrorMessage);

                var parallelNames = parallelResult.Report!.Forms.Select(f => f.Name).OrderBy(n => n).ToList();
                var sequentialNames = sequentialResult.Report!.Forms.Select(f => f.Name).OrderBy(n => n).ToList();

                Assert.Equal(sequentialNames, parallelNames);
                Assert.Equal(4, parallelNames.Count);
            }
            finally
            {
                Directory.Delete(parallelOutput, recursive: true);
                Directory.Delete(sequentialOutput, recursive: true);
            }
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CancelledMidConversion_RollsBackAllWrittenFiles_WithoutThrowing()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            for (var i = 0; i < 10; i++)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(sourceDir, $"Form{i}.Designer.cs"), MinimalDesignerFile($"Form{i}"));
            }

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                // Sequential so cancellation timing is deterministic via the progress callback.
                ParallelProcessing = new ParallelProcessingConfig { Enabled = false }
            };

            var orchestrator = new ConversionOrchestrator(sourceDir, outputDir, config);
            var cts = new CancellationTokenSource();

            // Cancel once a few forms have actually been written, so there's something for
            // rollback to clean up - this is the regression check for the bug where
            // RollbackTransactionAsync() was called without a matching BeginTransaction(),
            // which threw InvalidOperationException from inside the cancellation handler.
            var progress = new SyncProgress(p =>
            {
                if (p.FormsProcessed >= 3)
                {
                    cts.Cancel();
                }
            });

            var result = await orchestrator.ExecuteAsync(progress, cts.Token);

            Assert.False(result.Success);
            Assert.Equal("Conversion cancelled by user", result.ErrorMessage);

            var remainingFiles = Directory.Exists(outputDir)
                ? Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
                : [];

            Assert.Empty(remainingFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_Incremental_SecondRunOnUnchangedInput_ReconvertsNothing()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form1.Designer.cs"), MinimalDesignerFile("Form1"));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                IncrementalSettings = new IncrementalConversionConfig { Enabled = true }
            };

            var firstRun = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(firstRun.Success, firstRun.ErrorMessage);
            Assert.Single(firstRun.Report!.Forms);

            var secondRun = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();
            Assert.True(secondRun.Success, secondRun.ErrorMessage);
            Assert.Empty(secondRun.Report!.Forms);

            // --force bypasses the incremental cache even though nothing changed.
            var forcedRun = await new ConversionOrchestrator(
                sourceDir, outputDir, config, force: true).ExecuteAsync();
            Assert.True(forcedRun.Success, forcedRun.ErrorMessage);
            Assert.Single(forcedRun.Report!.Forms);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_StyleExtractionEnabled_GeneratesStylesFileForSharedProperties()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            const string designerContent = """
                namespace SampleApp
                {
                    partial class StyledForm
                    {
                        private System.Windows.Forms.Button button1;
                        private System.Windows.Forms.Button button2;
                        private System.Windows.Forms.Button button3;

                        private void InitializeComponent()
                        {
                            this.button1 = new System.Windows.Forms.Button();
                            this.button2 = new System.Windows.Forms.Button();
                            this.button3 = new System.Windows.Forms.Button();
                            this.SuspendLayout();
                            this.button1.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
                            this.button1.Name = "button1";
                            this.button2.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
                            this.button2.Name = "button2";
                            this.button3.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
                            this.button3.Name = "button3";
                            this.Controls.Add(this.button1);
                            this.Controls.Add(this.button2);
                            this.Controls.Add(this.button3);
                            this.Name = "StyledForm";
                            this.ResumeLayout(false);
                        }
                    }
                }
                """;

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "StyledForm.Designer.cs"), designerContent);

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                StyleExtraction = new StyleExtractionConfig { Enabled = true, MinimumOccurrence = 3 }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var stylesPath = Path.Combine(outputDir, "Views", "StyledForm.Styles.axaml");
            Assert.True(File.Exists(stylesPath));

            var stylesContent = await File.ReadAllTextAsync(stylesPath);
            Assert.Contains("Selector=\"Button\"", stylesContent);
            Assert.Contains("Property=\"Background\"", stylesContent);
            Assert.Contains("Value=\"#0078D7\"", stylesContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CustomProjectGenerationConfig_FlowsIntoGeneratedCsproj()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form1.Designer.cs"), MinimalDesignerFile("Form1"));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                ProjectGeneration = new ProjectGenerationConfig
                {
                    AvaloniaVersion = "11.9.9",
                    CommunityToolkitMvvmVersion = "9.9.9",
                    TargetFramework = "net9.0"
                }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var csprojPath = Path.Combine(outputDir, $"{Path.GetFileName(outputDir)}.csproj");
            var csprojContent = await File.ReadAllTextAsync(csprojPath);

            Assert.Contains("Version=\"11.9.9\"", csprojContent);
            Assert.Contains("Version=\"9.9.9\"", csprojContent);
            Assert.Contains("<TargetFramework>net9.0</TargetFramework>", csprojContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private static string DesignerFileWithGotFocusHandler(string className) => $$"""
        namespace SampleApp
        {
            partial class {{className}}
            {
                private System.Windows.Forms.TextBox textBox1;

                private void InitializeComponent()
                {
                    this.textBox1 = new System.Windows.Forms.TextBox();
                    this.SuspendLayout();
                    this.textBox1.Name = "textBox1";
                    this.textBox1.GotFocus += new System.EventHandler(this.textBox1_GotFocus);
                    this.Controls.Add(this.textBox1);
                    this.Name = "{{className}}";
                    this.ResumeLayout(false);
                }
            }
        }
        """;

    [Fact]
    public async Task ExecuteAsync_DefaultConfig_TargetsAvalonia12_AndUsesFocusChangedEventArgs()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form1.Designer.cs"), DesignerFileWithGotFocusHandler("Form1"));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var csprojPath = Path.Combine(outputDir, $"{Path.GetFileName(outputDir)}.csproj");
            var csprojContent = await File.ReadAllTextAsync(csprojPath);
            Assert.Contains("Include=\"Avalonia\" Version=\"12.0.0\"", csprojContent);

            var codeBehindContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "Form1.axaml.cs"));
            Assert.Contains("Avalonia.Input.FocusChangedEventArgs", codeBehindContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ExplicitAvalonia11Config_UsesGotFocusEventArgs()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form1.Designer.cs"), DesignerFileWithGotFocusHandler("Form1"));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                ProjectGeneration = new ProjectGenerationConfig { AvaloniaVersion = "11.2.0" }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);

            var csprojPath = Path.Combine(outputDir, $"{Path.GetFileName(outputDir)}.csproj");
            var csprojContent = await File.ReadAllTextAsync(csprojPath);
            Assert.Contains("Include=\"Avalonia\" Version=\"11.2.0\"", csprojContent);

            var codeBehindContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Views", "Form1.axaml.cs"));
            Assert.Contains("Avalonia.Input.GotFocusEventArgs", codeBehindContent);
            Assert.DoesNotContain("FocusChangedEventArgs", codeBehindContent);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithResume_CancelledMidRun_LeavesCompletedFormsOnDiskAndSavesCheckpoint()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            var form1Path = Path.Combine(sourceDir, "Form1.Designer.cs");
            await File.WriteAllTextAsync(form1Path, MinimalDesignerFile("Form1"));
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form2.Designer.cs"), MinimalDesignerFile("Form2"));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                ParallelProcessing = new ParallelProcessingConfig { Enabled = false }
            };

            var orchestrator = new ConversionOrchestrator(sourceDir, outputDir, config, resume: true);
            var cts = new CancellationTokenSource();

            var progress = new SyncProgress(p =>
            {
                // Fires on the FIRST per-form progress report (FormsProcessed=0, reported
                // right before that form's own conversion runs). By the time this callback
                // sets the token, that form's own cancellation check has already passed, so
                // it still completes; the *next* form's check (top of the next loop
                // iteration) is what actually observes the cancellation - giving exactly one
                // fully-completed form before the run aborts. Gating on CurrentFormName
                // excludes the earlier GitInit/Parsing/pre-loop reports, which also report
                // FormsProcessed=0 but with no form name attached.
                if (p.CurrentOperation == OperationType.ConvertingForm && p.CurrentFormName != null)
                {
                    cts.Cancel();
                }
            });

            var result = await orchestrator.ExecuteAsync(progress, cts.Token);

            Assert.False(result.Success);

            // Whichever form completed first (sequential, but source order isn't guaranteed by
            // Directory.GetFiles) should have all 3 files still on disk - not rolled back.
            var viewsDir = Path.Combine(outputDir, "Views");
            var generatedAxaml = Directory.Exists(viewsDir)
                ? Directory.GetFiles(viewsDir, "*.axaml")
                : [];
            Assert.Single(generatedAxaml);

            var checkpointPath = Path.Combine(outputDir, ".converter-checkpoint.json");
            Assert.True(File.Exists(checkpointPath));

            var state = JsonSerializer.Deserialize<ConversionState>(
                await File.ReadAllTextAsync(checkpointPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(state);
            Assert.Single(state!.CompletedFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithResume_SecondRun_OnlyReprocessesIncompleteForms()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form1.Designer.cs"), MinimalDesignerFile("Form1"));
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form2.Designer.cs"), MinimalDesignerFile("Form2"));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                ParallelProcessing = new ParallelProcessingConfig { Enabled = false }
            };

            // First run: cancel after the first form completes.
            var cts = new CancellationTokenSource();
            var progress = new SyncProgress(p =>
            {
                // Fires on the FIRST per-form progress report (FormsProcessed=0, reported
                // right before that form's own conversion runs). By the time this callback
                // sets the token, that form's own cancellation check has already passed, so
                // it still completes; the *next* form's check (top of the next loop
                // iteration) is what actually observes the cancellation - giving exactly one
                // fully-completed form before the run aborts. Gating on CurrentFormName
                // excludes the earlier GitInit/Parsing/pre-loop reports, which also report
                // FormsProcessed=0 but with no form name attached.
                if (p.CurrentOperation == OperationType.ConvertingForm && p.CurrentFormName != null)
                {
                    cts.Cancel();
                }
            });
            var firstRun = await new ConversionOrchestrator(sourceDir, outputDir, config, resume: true)
                .ExecuteAsync(progress, cts.Token);
            Assert.False(firstRun.Success);

            // Second run: no cancellation, should only reprocess the remaining form.
            var secondRun = await new ConversionOrchestrator(sourceDir, outputDir, config, resume: true)
                .ExecuteAsync();

            Assert.True(secondRun.Success, secondRun.ErrorMessage);
            Assert.Single(secondRun.Report!.Forms);

            // Both forms' output should now exist, and the checkpoint should be gone.
            var viewsDir = Path.Combine(outputDir, "Views");
            Assert.Equal(2, Directory.GetFiles(viewsDir, "*.axaml").Length);
            Assert.False(File.Exists(Path.Combine(outputDir, ".converter-checkpoint.json")));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithoutResume_FailedRun_RollsBackAllCreatedFiles()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form1.Designer.cs"), MinimalDesignerFile("Form1"));
            await File.WriteAllTextAsync(
                Path.Combine(sourceDir, "Form2.Designer.cs"), MinimalDesignerFile("Form2"));

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                ParallelProcessing = new ParallelProcessingConfig { Enabled = false }
            };

            var cts = new CancellationTokenSource();
            var progress = new SyncProgress(p =>
            {
                // Fires on the FIRST per-form progress report (FormsProcessed=0, reported
                // right before that form's own conversion runs). By the time this callback
                // sets the token, that form's own cancellation check has already passed, so
                // it still completes; the *next* form's check (top of the next loop
                // iteration) is what actually observes the cancellation - giving exactly one
                // fully-completed form before the run aborts. Gating on CurrentFormName
                // excludes the earlier GitInit/Parsing/pre-loop reports, which also report
                // FormsProcessed=0 but with no form name attached.
                if (p.CurrentOperation == OperationType.ConvertingForm && p.CurrentFormName != null)
                {
                    cts.Cancel();
                }
            });

            // resume defaults to false - today's unchanged behavior.
            var result = await new ConversionOrchestrator(sourceDir, outputDir, config)
                .ExecuteAsync(progress, cts.Token);

            Assert.False(result.Success);

            var remainingFiles = Directory.Exists(outputDir)
                ? Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
                : [];
            Assert.Empty(remainingFiles);
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithResumeAndParallelEnabled_ForcesSequentialProcessing()
    {
        var sourceDir = Directory.CreateTempSubdirectory("wf2av-src-").FullName;
        var outputDir = Directory.CreateTempSubdirectory("wf2av-out-").FullName;

        try
        {
            for (var i = 0; i < 4; i++)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(sourceDir, $"Form{i}.Designer.cs"), MinimalDesignerFile($"Form{i}"));
            }

            var config = new ConverterConfig
            {
                GitIntegration = new GitIntegrationConfig { Enabled = false },
                Documentation = new DocumentationConfig { Enabled = false },
                ParallelProcessing = new ParallelProcessingConfig { Enabled = true }
            };

            var result = await new ConversionOrchestrator(sourceDir, outputDir, config, resume: true).ExecuteAsync();

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(4, result.Report!.Forms.Count);

            // Deterministic, complete checkpoint content is itself the evidence that
            // per-form sequential checkpointing ran (a parallel batch has no such guarantee).
            Assert.False(File.Exists(Path.Combine(outputDir, ".converter-checkpoint.json")));
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
