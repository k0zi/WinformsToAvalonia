namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// One converted Form resolved to the View it becomes, so a handler body that opens it
/// (<c>new SettingsForm().ShowDialog()</c>) can name the generated type.
/// </summary>
/// <remarks>
/// The Form counterpart of <see cref="UserControlViewInfo"/>, minus the xmlns prefix: a Form's
/// View is never referenced from AXAML - only from code - so it needs a namespace to
/// <c>using</c>, not a prefix to declare.
/// </remarks>
public sealed record FormViewInfo(string WinFormsTypeName, string ViewClassName, string ViewNamespace);
