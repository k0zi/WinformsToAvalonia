using Converter.Core.Configuration;

namespace Converter.Cli.Services;

/// <summary>
/// CLI-flag overrides to merge into a loaded ConverterConfig. Each field is null when the
/// corresponding CLI option was not explicitly supplied by the user (its value came from
/// the option's default), so an explicit .converterconfig setting is only overridden when
/// the user actually typed the flag - a bare `--parallel`/`--no-git` default never clobbers
/// a config-file value.
/// </summary>
public record CliOverrides
{
    public bool? NoGit { get; init; }
    public bool? CreateBranch { get; init; }
    public string? BranchName { get; init; }
    public bool? Parallel { get; init; }
    public bool? Incremental { get; init; }
}

/// <summary>
/// Pure function that merges explicit CLI overrides into a loaded ConverterConfig. Kept
/// free of System.CommandLine's ParseResult so it's testable with plain values.
/// </summary>
public static class CliConfigMerger
{
    public static ConverterConfig Merge(ConverterConfig baseConfig, CliOverrides overrides)
    {
        var gitIntegration = new GitIntegrationConfig
        {
            Enabled = overrides.NoGit == true ? false : baseConfig.GitIntegration.Enabled,
            CreateFeatureBranch = overrides.CreateBranch ?? baseConfig.GitIntegration.CreateFeatureBranch,
            BranchNamePattern = overrides.BranchName ?? baseConfig.GitIntegration.BranchNamePattern,
            AutoCommitCheckpoints = baseConfig.GitIntegration.AutoCommitCheckpoints,
            GenerateGitignore = baseConfig.GitIntegration.GenerateGitignore,
            GitignoreEntries = baseConfig.GitIntegration.GitignoreEntries
        };

        var parallelProcessing = new ParallelProcessingConfig
        {
            Enabled = overrides.Parallel ?? baseConfig.ParallelProcessing.Enabled,
            MaxDegreeOfParallelism = baseConfig.ParallelProcessing.MaxDegreeOfParallelism,
            BatchSize = baseConfig.ParallelProcessing.BatchSize
        };

        var incrementalSettings = new IncrementalConversionConfig
        {
            Enabled = overrides.Incremental ?? baseConfig.IncrementalSettings.Enabled,
            HashAlgorithm = baseConfig.IncrementalSettings.HashAlgorithm,
            CheckpointFrequency = baseConfig.IncrementalSettings.CheckpointFrequency,
            CacheFileName = baseConfig.IncrementalSettings.CacheFileName,
            CheckpointFileName = baseConfig.IncrementalSettings.CheckpointFileName
        };

        return new ConverterConfig
        {
            CustomMappings = baseConfig.CustomMappings,
            ThirdPartyMappings = baseConfig.ThirdPartyMappings,
            StyleExtraction = baseConfig.StyleExtraction,
            LayoutDetection = baseConfig.LayoutDetection,
            ExcludePatterns = baseConfig.ExcludePatterns,
            NamingConventions = baseConfig.NamingConventions,
            IncrementalSettings = incrementalSettings,
            ParallelProcessing = parallelProcessing,
            GitIntegration = gitIntegration,
            Documentation = baseConfig.Documentation,
            Plugins = baseConfig.Plugins,
            DefaultOptions = baseConfig.DefaultOptions
        };
    }
}
