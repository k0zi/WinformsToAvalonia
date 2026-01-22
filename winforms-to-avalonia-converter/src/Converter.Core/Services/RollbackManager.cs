using Microsoft.Extensions.Logging;

namespace Converter.Core.Services;

/// <summary>
/// Manages transactional file operations with rollback support.
/// </summary>
public class RollbackManager
{
    private readonly ILogger<RollbackManager>? _logger;
    private readonly List<string> _createdFiles = [];
    private readonly List<string> _modifiedFiles = [];
    private readonly Dictionary<string, string> _backups = [];
    private bool _isInTransaction;

    public RollbackManager(ILogger<RollbackManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Begin a new transaction.
    /// </summary>
    public void BeginTransaction()
    {
        if (_isInTransaction)
        {
            throw new InvalidOperationException("Transaction already in progress.");
        }

        _createdFiles.Clear();
        _modifiedFiles.Clear();
        _backups.Clear();
        _isInTransaction = true;

        _logger?.LogInformation("Transaction started");
    }

    /// <summary>
    /// Track a file creation.
    /// </summary>
    public void TrackFileCreation(string filePath)
    {
        if (!_isInTransaction)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        _createdFiles.Add(filePath);
        _logger?.LogDebug("Tracking file creation: {FilePath}", filePath);
    }

    /// <summary>
    /// Track a file modification (backs up original).
    /// </summary>
    public async Task TrackFileModificationAsync(string filePath)
    {
        if (!_isInTransaction)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        if (File.Exists(filePath) && !_backups.ContainsKey(filePath))
        {
            var backupPath = $"{filePath}.backup_{Guid.NewGuid():N}";
            File.Copy(filePath, backupPath, true);
            _backups[filePath] = backupPath;
            _modifiedFiles.Add(filePath);
            
            _logger?.LogDebug("Backing up file: {FilePath} -> {BackupPath}", filePath, backupPath);
        }
    }

    /// <summary>
    /// Commit the transaction (delete backups).
    /// </summary>
    public void CommitTransaction()
    {
        if (!_isInTransaction)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        // Delete backup files
        foreach (var backupPath in _backups.Values)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to delete backup file: {BackupPath}", backupPath);
            }
        }

        _createdFiles.Clear();
        _modifiedFiles.Clear();
        _backups.Clear();
        _isInTransaction = false;

        _logger?.LogInformation("Transaction committed successfully");
    }

    /// <summary>
    /// Rollback the transaction (delete created files, restore backups).
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        if (!_isInTransaction)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        _logger?.LogWarning("Rolling back transaction");

        // Delete created files
        foreach (var filePath in _createdFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger?.LogDebug("Deleted created file: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete created file during rollback: {FilePath}", filePath);
            }
        }

        // Restore modified files from backups
        foreach (var (originalPath, backupPath) in _backups)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, originalPath, true);
                    File.Delete(backupPath);
                    _logger?.LogDebug("Restored file from backup: {FilePath}", originalPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to restore file during rollback: {FilePath}", originalPath);
            }
        }

        _createdFiles.Clear();
        _modifiedFiles.Clear();
        _backups.Clear();
        _isInTransaction = false;

        _logger?.LogInformation("Transaction rolled back");
    }

    /// <summary>
    /// Get the rollback manifest.
    /// </summary>
    public RollbackManifest GetManifest()
    {
        return new RollbackManifest
        {
            CreatedFiles = [.. _createdFiles],
            ModifiedFiles = [.. _modifiedFiles],
            Backups = new Dictionary<string, string>(_backups)
        };
    }
}

/// <summary>
/// Represents the rollback manifest.
/// </summary>
public class RollbackManifest
{
    public required List<string> CreatedFiles { get; init; }
    public required List<string> ModifiedFiles { get; init; }
    public required Dictionary<string, string> Backups { get; init; }
}
