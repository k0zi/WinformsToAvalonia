using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// A single field declaration extracted verbatim from a code-behind file, e.g.
/// "private int _counter = 0;" - <paramref name="Names"/> holds every variable name declared
/// by it (a single FieldDeclarationSyntax can declare more than one, e.g. "int a, b;").
/// </summary>
public record CodeBehindField(IReadOnlyList<string> Names, string DeclarationText);

/// <summary>
/// Non-handler members discovered in a code-behind file by CodeBehindMemberExtractor.
/// </summary>
public class CodeBehindMembers(
    IReadOnlyList<CodeBehindField>? fields = null,
    IReadOnlyDictionary<string, string>? helperMethods = null,
    IReadOnlyList<string>? skippedOverrideMethodNames = null,
    IReadOnlyList<string>? usingDirectives = null)
{
    public IReadOnlyList<CodeBehindField> Fields { get; } = fields ?? [];
    public IReadOnlyDictionary<string, string> HelperMethods { get; } = helperMethods ?? new Dictionary<string, string>();
    public IReadOnlyList<string> SkippedOverrideMethodNames { get; } = skippedOverrideMethodNames ?? [];

    /// <summary>
    /// Namespaces the sibling code-behind file itself imports (e.g. "WarehouseApp.Data.Models"),
    /// in source order, deduplicated. Migrated fields/methods/handler bodies commonly reference
    /// types from these - without them, generated code referencing the app's own domain types
    /// fails to compile with "type or namespace not found" even though the type itself is fine.
    /// </summary>
    public IReadOnlyList<string> UsingDirectives { get; } = usingDirectives ?? [];

    public static readonly CodeBehindMembers Empty = new();
}

/// <summary>
/// Extracts private fields, non-handler helper methods (verbatim, unreformatted), and the
/// file's own "using" directives from the sibling non-designer .cs file (resolved via
/// SiblingFileResolver.ResolveCodeBehind) - alongside EventHandlerBodyParser, which owns the
/// named event-handler methods themselves. Same best-effort philosophy: a code-behind file is
/// arbitrary user code, so this is syntax-only and never throws - an unparseable/missing file
/// simply yields CodeBehindMembers.Empty, never a hard failure of the whole conversion.
/// </summary>
public static class CodeBehindMemberExtractor
{
    /// <summary>
    /// Well-known System.Windows.Forms.Control/Form virtual/protected method names - an
    /// "override" with one of these names is (almost certainly) a genuine WinForms
    /// lifecycle/rendering hook with no clean ViewModel equivalent (different rendering model,
    /// raw Win32 message plumbing, etc.), so it's excluded from migration. An "override" whose
    /// name is NOT on this list is (almost certainly) the project's own base class member
    /// instead - e.g. a shared "DetailFormBase&lt;T&gt;.SaveToEntity()" abstract method a
    /// concrete Form overrides - i.e. ordinary business logic that merely happens to use the
    /// "override" keyword, and gets migrated like any other helper method. Best-effort, not
    /// exhaustive: a real WinForms override name missing from this list would be migrated as
    /// live code and might not compile - the same "verify this compiles" risk this codebase
    /// already accepts for every other live-code migration path.
    /// </summary>
    private static readonly HashSet<string> KnownWinFormsOverrideMethodNames = new(StringComparer.Ordinal)
    {
        // Mouse
        "OnClick", "OnDoubleClick", "OnMouseClick", "OnMouseDoubleClick", "OnMouseDown", "OnMouseUp",
        "OnMouseMove", "OnMouseEnter", "OnMouseLeave", "OnMouseHover", "OnMouseWheel", "OnMouseCaptureChanged",
        // Keyboard
        "OnKeyDown", "OnKeyUp", "OnKeyPress", "OnPreviewKeyDown",
        // Focus / validation
        "OnGotFocus", "OnLostFocus", "OnEnter", "OnLeave", "OnValidating", "OnValidated",
        // Paint / rendering
        "OnPaint", "OnPaintBackground", "OnInvalidated",
        // Layout / geometry
        "OnResize", "OnSizeChanged", "OnClientSizeChanged", "OnLocationChanged", "OnMove", "OnLayout",
        "OnDockChanged", "OnAnchorChanged",
        // Appearance / state change notifications
        "OnTextChanged", "OnEnabledChanged", "OnVisibleChanged", "OnBackColorChanged", "OnForeColorChanged",
        "OnFontChanged", "OnCursorChanged", "OnStyleChanged",
        // Handle / control lifecycle
        "OnHandleCreated", "OnHandleDestroyed", "OnCreateControl", "OnControlAdded", "OnControlRemoved",
        "OnParentChanged",
        // Form lifecycle
        "OnLoad", "OnShown", "OnActivated", "OnDeactivate", "OnClosing", "OnClosed",
        "OnFormClosing", "OnFormClosed",
        // Drag and drop
        "OnDragEnter", "OnDragOver", "OnDragLeave", "OnDragDrop", "OnGiveFeedback", "OnQueryContinueDrag",
        // Misc Win32 / framework plumbing
        "OnScroll", "OnNotifyMessage", "OnHelpRequested", "Dispose", "ProcessCmdKey", "ProcessDialogKey",
        "ProcessKeyPreview", "ProcessMnemonic", "WndProc", "IsInputKey", "IsInputChar", "DefWndProc",
    };

    /// <summary>
    /// Extracts every field declaration and every non-handler method from
    /// <paramref name="codeBehindFilePath"/>. Methods whose name appears in
    /// <paramref name="handlerMethodNames"/> are skipped (EventHandlerBodyParser already owns
    /// those). A method marked "override" whose name is a known WinForms Control/Form virtual
    /// method (see KnownWinFormsOverrideMethodNames) is skipped from HelperMethods and reported
    /// in SkippedOverrideMethodNames instead; any other override is treated as ordinary
    /// business logic (the project's own base class member) and migrated into HelperMethods
    /// with "override" stripped - there's nothing to override in the ViewModel's own hierarchy.
    /// </summary>
    private static readonly HashSet<string> EmptyNames = [];

    /// <summary>
    /// <paramref name="controlFieldNames"/> - a single-file custom control's own child-control
    /// instance fields (e.g. "_textBox"), already captured by WinFormsParser as ControlNode
    /// instances - matters only when scanning that same file for its OWN "code-behind" (a
    /// composite custom control with no separate Foo.Designer.cs split has everything, control
    /// fields included, in one file); harmless no-op for the ordinary split Designer.cs/.cs pair
    /// case, where a control field never appears in the sibling file this scans. Without this,
    /// such a field would be wrongly re-migrated into the ViewModel as if it were a business
    /// field - it's a View concern, not a ViewModel one.
    /// </summary>
    public static async Task<CodeBehindMembers> ExtractAsync(
        string codeBehindFilePath, IReadOnlySet<string> handlerMethodNames,
        IReadOnlySet<string>? controlFieldNames = null)
    {
        controlFieldNames ??= EmptyNames;
        var fields = new List<CodeBehindField>();
        var helperMethods = new Dictionary<string, string>();
        var skippedOverrides = new List<string>();
        var usingDirectives = new List<string>();

        try
        {
            var sourceCode = await File.ReadAllTextAsync(codeBehindFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = await syntaxTree.GetRootAsync();

            // Usings can sit at file scope (CompilationUnitSyntax.Usings) or inside a
            // block/file-scoped namespace (BaseNamespaceDeclarationSyntax.Usings) - collecting
            // both covers every WinForms designer's common shape.
            var usingNames = root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                .OfType<UsingDirectiveSyntax>()
                .Where(u => u.Alias == null && !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && u.Name != null)
                .Select(u => u.Name!.ToString());
            foreach (var name in usingNames)
            {
                if (!usingDirectives.Contains(name))
                {
                    usingDirectives.Add(name);
                }
            }

            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                var names = field.Declaration.Variables.Select(v => v.Identifier.Text).ToList();
                if (names.All(controlFieldNames.Contains))
                {
                    continue;
                }

                fields.Add(new CodeBehindField(names, field.ToString().Trim()));
            }

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var name = method.Identifier.Text;
                // InitializeComponent is WinForms control-construction plumbing with no
                // ViewModel meaning (the generated AXAML is its replacement) - only ever
                // relevant here for a single-file custom control (see controlFieldNames), since
                // a split Designer.cs/.cs pair never has it in the sibling file this scans.
                if (name == "InitializeComponent" || handlerMethodNames.Contains(name))
                {
                    continue;
                }

                if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
                {
                    if (KnownWinFormsOverrideMethodNames.Contains(name))
                    {
                        skippedOverrides.Add(name);
                        continue;
                    }

                    if (!helperMethods.ContainsKey(name))
                    {
                        helperMethods[name] = StripOverrideModifier(method.ToString().Trim());
                    }
                    continue;
                }

                if (!helperMethods.ContainsKey(name))
                {
                    helperMethods[name] = method.ToString().Trim();
                }
            }
        }
        catch
        {
            // Best-effort: an unparseable/unreadable sibling file means nothing gets
            // extracted, not a failed conversion.
        }

        return new CodeBehindMembers(fields, helperMethods, skippedOverrides, usingDirectives);
    }

    /// <summary>
    /// Removes the "override" modifier token from a full method-source string - there's
    /// nothing to override in the ViewModel's own class hierarchy once a project-local base
    /// class member is migrated there as ordinary business logic.
    /// </summary>
    private static string StripOverrideModifier(string methodSource) =>
        Regex.Replace(methodSource, @"\boverride\s+", "", RegexOptions.None, TimeSpan.FromSeconds(1));
}
