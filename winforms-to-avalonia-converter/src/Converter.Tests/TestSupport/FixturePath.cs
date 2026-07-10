namespace Converter.Tests.TestSupport;

public static class FixturePath
{
    public static string Get(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
}
