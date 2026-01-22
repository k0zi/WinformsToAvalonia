using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Converter.Core.Git;

/// <summary>
/// Manages git operations for the converter.
/// </summary>
public class GitIntegrationManager
{
    private readonly ILogger<GitIntegrationManager>? _logger;

    public GitIntegrationManager(ILogger<GitIntegrationManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if a directory is a git repository.
    /// </summary>
    public bool IsGitRepository(string path)
    {
        try
        {
            return Repository.IsValid(path) || FindGitDirectory(path) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Find the git repository root directory.
    /// </summary>
    public string? FindGitDirectory(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);

        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Create a feature branch for the conversion.
    /// </summary>
    public string? CreateFeatureBranch(string repositoryPath, string branchNamePattern)
    {
        try
        {
            var gitDir = FindGitDirectory(repositoryPath);
            if (gitDir == null)
            {
                _logger?.LogWarning("Not a git repository: {Path}", repositoryPath);
                return null;
            }

            using var repo = new Repository(gitDir);

            // Check for uncommitted changes
            if (repo.RetrieveStatus().IsDirty)
            {
                _logger?.LogWarning("Repository has uncommitted changes");
                // Continue anyway, but warn
            }

            // Generate branch name
            var branchName = branchNamePattern
                .Replace("{timestamp}", DateTime.Now.ToString("yyyyMMdd-HHmmss"))
                .Replace("{date}", DateTime.Now.ToString("yyyyMMdd"));

            // Check if branch already exists
            if (repo.Branches[branchName] != null)
            {
                branchName += $"-{Guid.NewGuid():N}".Substring(0, 8);
            }

            // Create branch
            var branch = repo.CreateBranch(branchName);
            Commands.Checkout(repo, branch);

            _logger?.LogInformation("Created and checked out branch: {BranchName}", branchName);
            return branchName;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create feature branch");
            return null;
        }
    }

    /// <summary>
    /// Stage and commit files.
    /// </summary>
    public bool CommitChanges(string repositoryPath, string message, IEnumerable<string> filePaths)
    {
        try
        {
            var gitDir = FindGitDirectory(repositoryPath);
            if (gitDir == null)
            {
                return false;
            }

            using var repo = new Repository(gitDir);

            // Stage files
            foreach (var filePath in filePaths)
            {
                var relativePath = Path.GetRelativePath(gitDir, filePath);
                Commands.Stage(repo, relativePath);
            }

            // Create signature
            var signature = repo.Config.BuildSignature(DateTimeOffset.Now);

            // Commit
            var commit = repo.Commit(message, signature, signature);

            _logger?.LogInformation("Committed changes: {Message} ({CommitId})", 
                message, commit.Id.ToString().Substring(0, 7));

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to commit changes");
            return false;
        }
    }

    /// <summary>
    /// Generate .gitignore entries.
    /// </summary>
    public async Task AddGitignoreEntriesAsync(string repositoryPath, IEnumerable<string> entries)
    {
        try
        {
            var gitDir = FindGitDirectory(repositoryPath);
            if (gitDir == null)
            {
                return;
            }

            var gitignorePath = Path.Combine(gitDir, ".gitignore");
            var existingContent = File.Exists(gitignorePath) 
                ? await File.ReadAllTextAsync(gitignorePath)
                : string.Empty;

            var newEntries = new List<string>();

            foreach (var entry in entries)
            {
                if (!existingContent.Contains(entry))
                {
                    newEntries.Add(entry);
                }
            }

            if (newEntries.Count > 0)
            {
                await File.AppendAllLinesAsync(gitignorePath, new[] 
                { 
                    "",
                    "# Converter cache files",
                    string.Join(Environment.NewLine, newEntries)
                });

                _logger?.LogInformation("Added {Count} entries to .gitignore", newEntries.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update .gitignore");
        }
    }

    /// <summary>
    /// Reset repository to a specific commit (for rollback).
    /// </summary>
    public bool ResetToCommit(string repositoryPath, string commitId)
    {
        try
        {
            var gitDir = FindGitDirectory(repositoryPath);
            if (gitDir == null)
            {
                return false;
            }

            using var repo = new Repository(gitDir);
            var commit = repo.Lookup<Commit>(commitId);
            
            if (commit == null)
            {
                _logger?.LogError("Commit not found: {CommitId}", commitId);
                return false;
            }

            repo.Reset(ResetMode.Hard, commit);
            _logger?.LogInformation("Reset to commit: {CommitId}", commitId);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reset repository");
            return false;
        }
    }

    /// <summary>
    /// Get current branch name.
    /// </summary>
    public string? GetCurrentBranch(string repositoryPath)
    {
        try
        {
            var gitDir = FindGitDirectory(repositoryPath);
            if (gitDir == null)
            {
                return null;
            }

            using var repo = new Repository(gitDir);
            return repo.Head.FriendlyName;
        }
        catch
        {
            return null;
        }
    }
}
