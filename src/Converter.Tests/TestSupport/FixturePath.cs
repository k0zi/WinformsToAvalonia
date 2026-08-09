namespace Converter.Tests.TestSupport;

public static class FixturePath
{
    public static string Get(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    /// <summary>
    /// Locates the checked-in WarehouseApp sample WinForms project (src/SampleWinFormsApp/WarehouseApp)
    /// by walking up from the test assembly's output directory, rather than hardcoding a fixed
    /// "../../.." depth that would break if the TFM or build configuration changes.
    /// </summary>
    public static string WarehouseAppDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "SampleWinFormsApp", "WarehouseApp");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate SampleWinFormsApp/WarehouseApp by walking up from {AppContext.BaseDirectory}.");
    }
}
