using System.Text.RegularExpressions;

namespace Converter.Core.Configuration;

/// <summary>
/// Checks a file path against ConverterConfig.ExcludePatterns using simple wildcard matching
/// (`*`/`?`) plus a plain substring fallback, so patterns like "*.Designer.cs" or a bare
/// folder name ("Legacy") both work without pulling in a full glob library. Shared between
/// ConversionOrchestrator's designer-file discovery and SupportFileScanner's non-Form file
/// discovery - both need the exact same exclusion semantics.
/// </summary>
public static class ExcludePatternMatcher
{
    public static bool IsExcluded(string filePath, IReadOnlyList<string> excludePatterns)
    {
        if (excludePatterns.Count == 0)
        {
            return false;
        }

        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var pattern in excludePatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            var normalizedPattern = pattern.Replace('\\', '/');

            if (normalizedPattern.Contains('*') || normalizedPattern.Contains('?'))
            {
                var regexPattern = "^" + Regex.Escape(normalizedPattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                if (Regex.IsMatch(Path.GetFileName(normalizedPath), regexPattern, RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(normalizedPath, regexPattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            else if (normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
