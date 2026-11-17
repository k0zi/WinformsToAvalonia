namespace WinFormsToAvalonia.Core.Model;

/// <param name="FilePath">Where the method lives in the generated project.</param>
/// <param name="MemberName">The generated method's name.</param>
/// <param name="OriginalMethodName">What it was called in the WinForms code-behind.</param>
/// <param name="FirstRemainingLine">
/// The first statement that did not translate - enough to recognise the work at a glance without
/// opening the file.
/// </param>
public sealed record UnfinishedMember(
    string FilePath,
    string MemberName,
    string OriginalMethodName,
    string FirstRemainingLine);

/// <summary>
/// What one converted artifact still needs a human for, collected while it is emitted.
/// </summary>
public sealed record ArtifactMigrationSummary(
    string SourceArtifactName,
    IReadOnlyList<UnfinishedMember> Unfinished,
    IReadOnlyList<string> PreservedMemberNames);
