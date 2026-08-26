namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One event-handler method found in a Form's non-designer .cs file, together with the facts
/// FormMigrationPlanner needs to decide whether it can become a ViewModel [RelayCommand] or
/// has to stay an event-driven code-behind method. <see cref="BodyText"/> is the verbatim
/// original body (dedented, braces stripped) - it is never re-emitted as compiling code, only
/// preserved inside the generated method as a comment.
/// </summary>
public sealed record HandlerMethodModel
{
    public required string MethodName { get; init; }

    /// <summary>Name of the first (sender) parameter, or null for a parameterless handler.</summary>
    public string? SenderParameterName { get; init; }

    /// <summary>Name of the second (EventArgs) parameter, or null when the handler has fewer than two parameters.</summary>
    public string? EventArgsParameterName { get; init; }

    /// <summary>Simple type name of the second parameter, e.g. "EventArgs", "MouseEventArgs", "DragEventArgs".</summary>
    public string EventArgsTypeName { get; init; } = "EventArgs";

    public bool IsAsync { get; init; }

    /// <summary>The original body, verbatim and dedented. Empty for an empty handler.</summary>
    public string BodyText { get; init; } = "";

    /// <summary>True when the body references the sender parameter - a hard blocker for ViewModel promotion.</summary>
    public bool UsesSender { get; init; }

    /// <summary>True when the body references the EventArgs parameter - a hard blocker for ViewModel promotion.</summary>
    public bool UsesEventArgs { get; init; }

    /// <summary>True when the body constructs another Form/Dialog type (modal navigation) - stays in code-behind.</summary>
    public bool CreatesOtherForms { get; init; }

    /// <summary>
    /// True when the body calls <c>MessageBox.Show(...)</c>. Like <see cref="CreatesOtherForms"/>
    /// this keeps the handler in code-behind: Avalonia's replacement is a dialog that needs a
    /// TopLevel to own it, and a ViewModel has none.
    /// </summary>
    public bool CallsDialogApis { get; init; }

    /// <summary>Designer field names (keys of <see cref="FormModel.Controls"/>) the body touches.</summary>
    public IReadOnlyList<string> ReferencedControlFields { get; init; } = [];

    /// <summary>Per referenced control field, the member names accessed on it ("Text", "Nodes", "PerformStep", ...).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ControlMemberAccesses { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    /// <summary>Well-known Form members the body touches ("Close", "Hide", "ShowDialog", ...).</summary>
    public IReadOnlyList<string> TouchedFormMembers { get; init; } = [];

    /// <summary>Names of other (non-handler) methods declared on the same class that this body calls.</summary>
    public IReadOnlyList<string> CalledHelperMethods { get; init; } = [];
}
