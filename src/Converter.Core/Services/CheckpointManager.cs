using System.Text.Json;

namespace Converter.Core.Services;

/// <summary>
/// Manages conversion checkpoints for resumability.
/// </summary>
public class CheckpointManager
{
    private readonly string _checkpointFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public CheckpointManager(string workingDirectory, string checkpointFileName = ".converter-checkpoint.json")
    {
        _checkpointFilePath = Path.Combine(workingDirectory, checkpointFileName);
    }

    /// <summary>
    /// Save a checkpoint of the current conversion state.
    /// </summary>
    public async Task SaveCheckpointAsync(Models.ConversionState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await File.WriteAllTextAsync(_checkpointFilePath, json);
    }

    /// <summary>
    /// Load the last checkpoint if it exists.
    /// </summary>
    public async Task<Models.ConversionState?> LoadCheckpointAsync()
    {
        if (!File.Exists(_checkpointFilePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_checkpointFilePath);
            return JsonSerializer.Deserialize<Models.ConversionState>(json, JsonOptions);
        }
        catch
        {
            // If checkpoint is corrupted, return null
            return null;
        }
    }

    /// <summary>
    /// Delete the checkpoint file.
    /// </summary>
    public void ClearCheckpoint()
    {
        if (File.Exists(_checkpointFilePath))
        {
            File.Delete(_checkpointFilePath);
        }
    }

    /// <summary>
    /// Check if a checkpoint exists.
    /// </summary>
    public bool CheckpointExists()
    {
        return File.Exists(_checkpointFilePath);
    }
}
