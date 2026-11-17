using System.Reflection;
using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// The MDI member names, against WinForms' own <c>Form</c>.
/// </summary>
/// <remarks>
/// This table is a claim about the *source* language rather than the target, so it is WinForms'
/// metadata it has to agree with. A misspelled name here would not break a build anywhere - it
/// would simply never match, and an MDI application would go on converting in the silence this
/// table exists to end. That is exactly the kind of wrong table nothing else in this repo can
/// catch.
/// </remarks>
public class MdiMemberCatalogTests
{
    [Theory]
    [MemberData(nameof(MdiMembers))]
    public void MdiMember_IsDeclaredByWinFormsForm(string memberName)
    {
        var form = WinFormsMetadata.FindType("Form");
        Assert.True(form is not null, "WinForms does not define Form - something is very wrong.");

        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        Assert.True(
            form!.GetProperty(memberName, flags) is not null || form.GetMethod(memberName, flags) is not null,
            $"MdiMemberCatalog names '{memberName}', which WinForms' Form does not declare - so it would "
            + "never match, and an MDI application would convert as silently as it did before.");
    }

    public static TheoryData<string> MdiMembers()
    {
        var data = new TheoryData<string>();
        foreach (var name in MdiMemberCatalog.All.OrderBy(n => n, StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }
}
