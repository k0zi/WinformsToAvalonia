using System.Reflection;
using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

/// <summary>
/// The guidance a mapper carries is not a code comment - it is printed in the conversion report,
/// so it is the main thing a user reads about a control this converter cannot map. It therefore
/// rots the same way documentation does, and unlike documentation nothing else here checks it.
/// </summary>
public class GuidanceTextTests
{
    /// <summary>
    /// Every <c>Type.Member</c> this converter's own guidance names must actually exist.
    /// </summary>
    /// <remarks>
    /// Written after finding <c>Timer</c>'s guidance pointing at "ViewModelEmitter.AddTimerStub"
    /// - a method that had been gone for some time, in a sentence that also named the wrong
    /// emitter. Only first-party types are checked: a reference to <c>TrayIcon.Icons</c> or
    /// <c>Task.Run</c> names something this assembly cannot see, and is skipped rather than
    /// guessed at.
    /// </remarks>
    [Fact]
    public void MapperGuidance_NeverNamesASymbolThisConverterDoesNotHave()
    {
        var coreAssembly = typeof(ControlMappingRegistry).Assembly;
        var dangling = new List<string>();

        foreach (var (winFormsTypeName, guidance) in AllGuidance())
        {
            foreach (Match reference in Regex.Matches(guidance, @"\b([A-Z][A-Za-z0-9]*)\.([A-Z][A-Za-z0-9]*)\b"))
            {
                var typeName = reference.Groups[1].Value;
                var memberName = reference.Groups[2].Value;

                var type = coreAssembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type is null)
                {
                    continue; // Not ours - an Avalonia or BCL name.
                }

                const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

                if (type.GetMember(memberName, All).Length == 0 && type.GetNestedType(memberName, All) is null)
                {
                    dangling.Add($"'{winFormsTypeName}' guidance names {typeName}.{memberName}, which does not exist.");
                }
            }
        }

        Assert.True(dangling.Count == 0, string.Join("\n", dangling));
    }

    /// <summary>
    /// A component this run emits as a real field must not still be telling the user to write
    /// that field themselves - the exact drift this test class was created for.
    /// </summary>
    [Theory]
    [InlineData("BackgroundWorker")]
    [InlineData("FileSystemWatcher")]
    [InlineData("Process")]
    [InlineData("SerialPort")]
    [InlineData("EventLog")]
    [InlineData("PerformanceCounter")]
    [InlineData("ServiceController")]
    [InlineData("SoundPlayer")]
    public void ComponentGuidance_SaysThatTheFieldIsGenerated(string winFormsTypeName)
    {
        Assert.True(ComponentFieldCatalog.TryGet(winFormsTypeName, out _), $"{winFormsTypeName} is no longer emitted as a field - this row is stale.");

        var guidance = string.Join(" ", AllGuidance().Where(g => g.WinFormsTypeName == winFormsTypeName).Select(g => g.Guidance));

        Assert.Contains("emits it as a real field", guidance, StringComparison.Ordinal);
        Assert.DoesNotContain("move its construction", guidance, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<(string WinFormsTypeName, string Guidance)> AllGuidance()
    {
        var registry = new ControlMappingRegistry();

        foreach (var winFormsTypeName in registry.Mappers.Keys)
        {
            var probe = new ControlModel { FieldName = "probe1", ClrTypeName = winFormsTypeName };
            foreach (var warning in registry.Map(probe).Warnings)
            {
                yield return (winFormsTypeName, warning);
            }
        }
    }

    /// <summary>
    /// A dialog this converter actually handles must not still be telling the reader to go find a
    /// community package.
    /// </summary>
    /// <remarks>
    /// Both of these were stale for as long as the fallbacks have shipped - the guidance is the
    /// only thing a user reads about an Unsupported entry, and nothing checked it against what the
    /// converter had since learnt to do.
    /// </remarks>
    [Theory]
    [InlineData("ColorDialog", "ColorDialogFallback")]
    [InlineData("FontDialog", "FontDialogFallback")]
    public void VisualDialogGuidance_NamesTheFallbackItActuallyEmits(string winFormsTypeName, string templateKey)
    {
        var guidance = string.Join(
            " ",
            new ControlMappingRegistry()
                .Map(new ControlModel { FieldName = "field1", ClrTypeName = winFormsTypeName })
                .Warnings);

        Assert.Contains(templateKey, guidance, StringComparison.Ordinal);
        Assert.DoesNotContain("recommend a community package", guidance, StringComparison.Ordinal);
    }
}
