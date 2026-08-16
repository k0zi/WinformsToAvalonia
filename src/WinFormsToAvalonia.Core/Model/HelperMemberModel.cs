namespace WinFormsToAvalonia.Core.Model;

public enum HelperMemberKind
{
    Field,
    Property,
    Method,
    Other,
}

/// <summary>
/// A non-handler member of the original Form class (a private helper method like
/// <c>SetBusy</c>, a backing field, a property). Preserved verbatim so the generated
/// code-behind can carry it alongside the handlers that call it, instead of losing it in a
/// file-level comment.
/// </summary>
public sealed record HelperMemberModel(string Name, HelperMemberKind Kind, string SourceText);
