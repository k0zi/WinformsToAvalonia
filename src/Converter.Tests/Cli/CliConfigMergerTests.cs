using Converter.Cli.Services;
using Converter.Core.Configuration;

namespace Converter.Tests.Cli;

public class CliConfigMergerTests
{
    [Fact]
    public void Merge_ExplicitNoGit_OverridesConfigFileEnabledTrue()
    {
        var baseConfig = new ConverterConfig
        {
            GitIntegration = new GitIntegrationConfig { Enabled = true }
        };

        var merged = CliConfigMerger.Merge(baseConfig, new CliOverrides { NoGit = true });

        Assert.False(merged.GitIntegration.Enabled);
    }

    [Fact]
    public void Merge_NoGitOmitted_LeavesConfigFileEnabledFalseUntouched()
    {
        var baseConfig = new ConverterConfig
        {
            GitIntegration = new GitIntegrationConfig { Enabled = false }
        };

        // NoGit not set (null) - simulates the CLI option not being explicitly typed.
        var merged = CliConfigMerger.Merge(baseConfig, new CliOverrides());

        Assert.False(merged.GitIntegration.Enabled);
    }

    [Fact]
    public void Merge_ExplicitParallelFalse_OverridesConfigFileEnabledTrue()
    {
        var baseConfig = new ConverterConfig
        {
            ParallelProcessing = new ParallelProcessingConfig { Enabled = true }
        };

        var merged = CliConfigMerger.Merge(baseConfig, new CliOverrides { Parallel = false });

        Assert.False(merged.ParallelProcessing.Enabled);
    }

    [Fact]
    public void Merge_BranchNameProvided_OverridesConfigFilePattern()
    {
        var baseConfig = new ConverterConfig
        {
            GitIntegration = new GitIntegrationConfig { BranchNamePattern = "feature/from-config" }
        };

        var merged = CliConfigMerger.Merge(baseConfig, new CliOverrides { BranchName = "feature/from-cli" });

        Assert.Equal("feature/from-cli", merged.GitIntegration.BranchNamePattern);
    }

    [Fact]
    public void Merge_BranchNameOmitted_LeavesConfigFilePatternUntouched()
    {
        var baseConfig = new ConverterConfig
        {
            GitIntegration = new GitIntegrationConfig { BranchNamePattern = "feature/from-config" }
        };

        var merged = CliConfigMerger.Merge(baseConfig, new CliOverrides());

        Assert.Equal("feature/from-config", merged.GitIntegration.BranchNamePattern);
    }
}
