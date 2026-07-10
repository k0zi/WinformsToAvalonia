using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Converter.Core.Services;

/// <summary>
/// Manages file hash tracking for incremental conversion.
/// </summary>
public class FileHashTracker
{
    private readonly string _cacheFilePath;
    private Dictionary<string, FileHashEntry> _cache = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FileHashTracker(string workingDirectory, string cacheFileName = ".converter-cache.json")
    {
        _cacheFilePath = Path.Combine(workingDirectory, cacheFileName);
    }

    /// <summary>
    /// Load the hash cache from disk.
    /// </summary>
    public async Task LoadCacheAsync()
    {
        if (!File.Exists(_cacheFilePath))
        {
            _cache = new Dictionary<string, FileHashEntry>();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_cacheFilePath);
            _cache = JsonSerializer.Deserialize<Dictionary<string, FileHashEntry>>(json, JsonOptions) 
                ?? new Dictionary<string, FileHashEntry>();
        }
        catch
        {
            _cache = new Dictionary<string, FileHashEntry>();
        }
    }

    /// <summary>
    /// Save the hash cache to disk.
    /// </summary>
    public async Task SaveCacheAsync()
    {
        var json = JsonSerializer.Serialize(_cache, JsonOptions);
        await File.WriteAllTextAsync(_cacheFilePath, json);
    }

    /// <summary>
    /// Compute hash for a file.
    /// </summary>
    public async Task<string> ComputeFileHashAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Check if a file has changed since last conversion.
    /// </summary>
    public async Task<bool> HasFileChangedAsync(string filePath)
    {
        var currentHash = await ComputeFileHashAsync(filePath);
        
        if (!_cache.TryGetValue(filePath, out var entry))
        {
            // File is new
            return true;
        }

        return entry.Hash != currentHash;
    }

    /// <summary>
    /// Update the hash for a file.
    /// </summary>
    public async Task UpdateFileHashAsync(string filePath)
    {
        var hash = await ComputeFileHashAsync(filePath);
        _cache[filePath] = new FileHashEntry
        {
            Hash = hash,
            LastModified = File.GetLastWriteTimeUtc(filePath),
            LastConverted = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get all files that have changed.
    /// </summary>
    public async Task<List<string>> GetChangedFilesAsync(IEnumerable<string> filePaths)
    {
        var changedFiles = new List<string>();

        foreach (var filePath in filePaths)
        {
            if (await HasFileChangedAsync(filePath))
            {
                changedFiles.Add(filePath);
            }
        }

        return changedFiles;
    }

    /// <summary>
    /// Clear the hash cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        if (File.Exists(_cacheFilePath))
        {
            File.Delete(_cacheFilePath);
        }
    }
}

/// <summary>
/// Represents a file hash cache entry.
/// </summary>
public class FileHashEntry
{
    public required string Hash { get; init; }
    public DateTime LastModified { get; init; }
    public DateTime LastConverted { get; init; }
}
