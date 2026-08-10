using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converter.Plugin.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Core.Parsing;

/// <summary>
/// Parses WinForms Designer.cs files using Roslyn.
/// </summary>
public class WinFormsParser
{
    private readonly ILogger<WinFormsParser>? _logger;

    /// <summary>
    /// WinForms/Avalonia control-ish type names recognized regardless of what they contain
    /// as a substring. Kept in sync loosely with Converter.Mappings' built-in registry, but
    /// duplicated here rather than referenced to avoid a Converter.Mappings -> Converter.Core
    /// -> Converter.Mappings project reference cycle (Converter.Mappings already references
    /// Converter.Core).
    /// </summary>
    private static readonly HashSet<string> KnownControlTypeNames = new(StringComparer.Ordinal)
    {
        "Form", "UserControl", "Panel", "GroupBox", "TabControl", "TabPage", "SplitContainer",
        "FlowLayoutPanel", "TableLayoutPanel", "Button", "TextBox", "Label", "CheckBox",
        "RadioButton", "ComboBox", "ListBox", "DataGridView", "TreeView", "ListView",
        "RichTextBox", "PictureBox", "ProgressBar", "TrackBar", "NumericUpDown",
        "DateTimePicker", "MonthCalendar", "MenuStrip", "ToolStrip", "StatusStrip",
        "ContextMenuStrip", "ToolStripMenuItem", "ToolStripButton", "ToolStripLabel",
        "ToolStripSeparator", "ToolStripComboBox", "ToolStripTextBox", "ToolStripProgressBar",
        "MaskedTextBox", "LinkLabel", "Timer", "ToolTip", "NotifyIcon", "WebBrowser",
        "PrintPreviewControl", "CheckedListBox", "DomainUpDown", "Splitter", "HScrollBar",
        "VScrollBar", "PropertyGrid", "ErrorProvider", "BindingSource", "ImageList",
        "ColorDialog", "OpenFileDialog", "SaveFileDialog", "FolderBrowserDialog",
        "FontDialog", "PrintDialog", "PrintDocument", "BackgroundWorker", "SplitterPanel"
    };

    /// <summary>
    /// Short type names that are known non-control BCL/system types, so the custom-control
    /// fallback heuristic in <see cref="IsControlField"/> doesn't misclassify them.
    /// </summary>
    private static readonly HashSet<string> NonControlBclShortTypeNames = new(StringComparer.Ordinal)
    {
        "string", "String", "int", "Int32", "bool", "Boolean", "double", "Double",
        "float", "Single", "decimal", "Decimal", "object", "Object", "DateTime",
        "TimeSpan", "Guid", "byte", "Byte", "long", "Int64", "short", "Int16",
        "char", "Char", "uint", "ulong", "ushort", "sbyte", "EventHandler",
        "IContainer", "ComponentResourceManager", "Exception", "Action", "Func"
    };

    /// <summary>
    /// TableLayoutPanel cell-placement setter methods (e.g. `tableLayoutPanel1.SetColumn(control1, 0);`)
    /// called directly on the panel rather than through the Controls.Add 3-arg overload.
    /// </summary>
    private static readonly HashSet<string> TableLayoutPanelCellSetters = new(StringComparer.Ordinal)
    {
        "SetColumn", "SetRow", "SetColumnSpan", "SetRowSpan"
    };

    /// <summary>
    /// Dictionary key used to resolve "this" references (e.g. this.Controls.Add(...)) back
    /// to the root form's ControlNode.
    /// </summary>
    private const string ThisAlias = "this";

    /// <summary>
    /// Local variable name WinForms designer code always uses for the form's
    /// ComponentResourceManager (e.g. `resources.GetObject("button1.Image")`).
    /// </summary>
    private const string ResourceManagerVariableName = "resources";

    public WinFormsParser(ILogger<WinFormsParser>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a Designer.cs file into a control tree. When <paramref name="resources"/> is
    /// provided (the sibling .resx file's parsed entries), resx-backed property values
    /// (`resources.GetObject("key")`/`GetString("key")`/`ApplyResources(target, "prefix")`)
    /// are resolved to their real values instead of being stored as opaque raw C# text.
    /// </summary>
    public async Task<ParseResult> ParseDesignerFileAsync(
        string filePath, IReadOnlyDictionary<string, ResxEntry>? resources = null)
    {
        try
        {
            var sourceCode = await File.ReadAllTextAsync(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = await syntaxTree.GetRootAsync();

            var result = new ParseResult
            {
                FilePath = filePath,
                Success = true
            };

            // Find the InitializeComponent method
            var initializeComponentMethod = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "InitializeComponent");

            if (initializeComponentMethod == null)
            {
                result.Success = false;
                result.Errors.Add("InitializeComponent method not found");
                return result;
            }

            // Find the class declaration to get the form name
            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            if (classDeclaration == null)
            {
                result.Success = false;
                result.Errors.Add("Class declaration not found");
                return result;
            }

            var className = classDeclaration.Identifier.Text;
            var baseType = classDeclaration.BaseList?.Types.FirstOrDefault()?.Type.ToString() ?? "Form";

            // Create root control node (the Form itself)
            var rootControl = new ControlNode
            {
                ControlType = baseType,
                FullTypeName = $"System.Windows.Forms.{baseType}",
                Name = className,
                SourceFile = filePath,
                SourceLine = classDeclaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            };

            // Parse control declarations (fields)
            var controlFields = classDeclaration.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(f => IsControlField(f))
                .ToList();

            var controls = new Dictionary<string, ControlNode>();
            controls[className] = rootControl;
            // "this" inside InitializeComponent() refers to the form itself; without this
            // alias, top-level `this.Controls.Add(...)` calls can never resolve their owner.
            controls[ThisAlias] = rootControl;

            foreach (var field in controlFields)
            {
                var variable = field.Declaration.Variables.FirstOrDefault();
                if (variable == null) continue;

                var controlName = variable.Identifier.Text;
                var controlType = field.Declaration.Type.ToString();
                var shortTypeName = GetShortTypeName(controlType);

                var controlNode = new ControlNode
                {
                    ControlType = shortTypeName,
                    FullTypeName = controlType,
                    Name = controlName,
                    SourceFile = filePath,
                    SourceLine = field.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    IsCustomControl = !KnownControlTypeNames.Contains(shortTypeName) &&
                                       !shortTypeName.Contains("Control", StringComparison.Ordinal) &&
                                       !shortTypeName.Contains("Component", StringComparison.Ordinal)
                };

                controls[controlName] = controlNode;
            }

            // Parse InitializeComponent to build hierarchy and extract properties
            ParseInitializeComponent(initializeComponentMethod, controls, rootControl, resources);

            result.RootControl = rootControl;
            result.AllControls = controls.Values.Where(c => !ReferenceEquals(c, rootControl))
                .Prepend(rootControl)
                .Distinct()
                .ToList();

            _logger?.LogInformation("Parsed {ControlCount} controls from {FilePath}",
                result.AllControls.Count, filePath);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing designer file: {FilePath}", filePath);
            return new ParseResult
            {
                FilePath = filePath,
                Success = false,
                Errors = [ex.Message]
            };
        }
    }

    private void ParseInitializeComponent(MethodDeclarationSyntax method,
        Dictionary<string, ControlNode> controls, ControlNode rootControl,
        IReadOnlyDictionary<string, ResxEntry>? resources)
    {
        var statements = method.Body?.Statements ?? default;
        WalkStatements(statements, controls, rootControl, resources);
    }

    /// <summary>
    /// Recursively walks statements, descending into blocks/if/foreach so control
    /// declarations wrapped in conditional or looping designer code aren't skipped.
    /// </summary>
    private void WalkStatements(SyntaxList<StatementSyntax> statements,
        Dictionary<string, ControlNode> controls, ControlNode rootControl,
        IReadOnlyDictionary<string, ResxEntry>? resources)
    {
        foreach (var statement in statements)
        {
            WalkStatement(statement, controls, rootControl, resources);
        }
    }

    private void WalkStatement(StatementSyntax statement,
        Dictionary<string, ControlNode> controls, ControlNode rootControl,
        IReadOnlyDictionary<string, ResxEntry>? resources)
    {
        switch (statement)
        {
            case ExpressionStatementSyntax expressionStatement:
                DispatchExpressionStatement(expressionStatement, controls, rootControl, resources);
                break;

            case BlockSyntax block:
                WalkStatements(block.Statements, controls, rootControl, resources);
                break;

            case IfStatementSyntax ifStatement:
                WalkStatement(ifStatement.Statement, controls, rootControl, resources);
                if (ifStatement.Else != null)
                {
                    WalkStatement(ifStatement.Else.Statement, controls, rootControl, resources);
                }
                break;

            case ForEachStatementSyntax forEachStatement:
                WalkStatement(forEachStatement.Statement, controls, rootControl, resources);
                break;
        }
    }

    private void DispatchExpressionStatement(ExpressionStatementSyntax expressionStatement,
        Dictionary<string, ControlNode> controls, ControlNode rootControl,
        IReadOnlyDictionary<string, ResxEntry>? resources)
    {
        if (expressionStatement.Expression is AssignmentExpressionSyntax assignment)
        {
            if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                ParseEventSubscription(assignment, controls);
            }
            else
            {
                ParsePropertyAssignment(assignment, controls, resources);
            }
        }
        else if (expressionStatement.Expression is InvocationExpressionSyntax invocation)
        {
            ParseMethodInvocation(invocation, controls, rootControl, resources);
        }
    }

    private void ParsePropertyAssignment(AssignmentExpressionSyntax assignment,
        Dictionary<string, ControlNode> controls, IReadOnlyDictionary<string, ResxEntry>? resources)
    {
        if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
        {
            var controlName = GetControlNameFromExpression(memberAccess.Expression);
            var propertyName = memberAccess.Name.Identifier.Text;

            if (!string.IsNullOrEmpty(controlName) && controls.TryGetValue(controlName, out var control))
            {
                if (resources != null &&
                    TryGetResxKey(assignment.Right, out var resxKey) &&
                    resources.TryGetValue(resxKey, out var entry))
                {
                    control.Properties[propertyName] = BuildResxPropertyValue(propertyName, entry);
                    return;
                }

                var value = GetPropertyAssignmentValue(assignment.Right);
                control.Properties[propertyName] = new PropertyValue
                {
                    Name = propertyName,
                    Value = value,
                    Type = "object" // Type inference would require a semantic model.
                };
            }
        }
    }

    /// <summary>
    /// Parses an event subscription such as `button1.Click += new EventHandler(this.button1_Click);`
    /// or `button1.Click += button1_Click;` into ControlNode.EventHandlers.
    /// </summary>
    private void ParseEventSubscription(AssignmentExpressionSyntax assignment,
        Dictionary<string, ControlNode> controls)
    {
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var controlName = GetControlNameFromExpression(memberAccess.Expression);
        var eventName = memberAccess.Name.Identifier.Text;

        if (string.IsNullOrEmpty(controlName) || !controls.TryGetValue(controlName, out var control))
        {
            return;
        }

        var handlerName = GetEventHandlerName(assignment.Right);
        if (!string.IsNullOrEmpty(handlerName))
        {
            control.EventHandlers[eventName] = handlerName;
        }
    }

    private string GetEventHandlerName(ExpressionSyntax right)
    {
        // e.g. new System.EventHandler(this.button1_Click)
        if (right is ObjectCreationExpressionSyntax objectCreation)
        {
            var arg = objectCreation.ArgumentList?.Arguments.FirstOrDefault();
            return arg != null ? GetControlNameFromExpression(arg.Expression) : string.Empty;
        }

        // e.g. button1.Click += button1_Click; (bare method group)
        // e.g. button1.Click += (s, e) => { ... }; (inline lambda - no stable name to extract)
        if (right is LambdaExpressionSyntax)
        {
            return InlineLambdaHandlerMarker;
        }

        return GetControlNameFromExpression(right);
    }

    private void ParseMethodInvocation(InvocationExpressionSyntax invocation,
        Dictionary<string, ControlNode> controls, ControlNode rootControl,
        IReadOnlyDictionary<string, ResxEntry>? resources)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var methodName = memberAccess.Name.Identifier.Text;

        if (methodName == "ApplyResources" &&
            memberAccess.Expression.ToString() == ResourceManagerVariableName)
        {
            ParseApplyResourcesInvocation(invocation, controls, resources);
            return;
        }

        if (TableLayoutPanelCellSetters.Contains(methodName))
        {
            ParseTableLayoutPanelSetterInvocation(invocation, methodName, memberAccess, controls);
            return;
        }

        if (methodName != "Add" || memberAccess.Expression is not MemberAccessExpressionSyntax collectionAccess)
        {
            return;
        }

        var collectionName = collectionAccess.Name.Identifier.Text;
        var ownerControlName = GetControlNameFromExpression(collectionAccess.Expression);

        if (string.IsNullOrEmpty(ownerControlName))
        {
            return;
        }

        switch (collectionName)
        {
            case "Controls":
                ParseControlsAddInvocation(invocation, ownerControlName, controls);
                break;

            case "DataBindings":
                ParseDataBindingsAddInvocation(invocation, ownerControlName, controls);
                break;
        }
    }

    private void ParseControlsAddInvocation(InvocationExpressionSyntax invocation, string ownerControlName,
        Dictionary<string, ControlNode> controls)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var childArgument = arguments.FirstOrDefault();
        if (childArgument == null)
        {
            return;
        }

        var childControlName = GetControlNameFromExpression(childArgument.Expression);

        if (!string.IsNullOrEmpty(childControlName) &&
            controls.TryGetValue(ownerControlName, out var parent) &&
            controls.TryGetValue(childControlName, out var child) &&
            !ReferenceEquals(parent, child))
        {
            child.Parent = parent;
            parent.Children.Add(child);

            // TableLayoutPanel's Controls.Add has a 3-arg overload (control, column, row) that
            // is the primary way WinForms designer code records cell placement - the 1-arg
            // overload above still runs first to establish the parent/child relationship.
            if (parent.ControlType == "TableLayoutPanel" && arguments.Count >= 3 &&
                int.TryParse(arguments[1].Expression.ToString(), out var column) &&
                int.TryParse(arguments[2].Expression.ToString(), out var row))
            {
                SetTableLayoutPanelCellProperty(child, "TableLayoutPanel.Column", column);
                SetTableLayoutPanelCellProperty(child, "TableLayoutPanel.Row", row);
            }
        }
    }

    private static void SetTableLayoutPanelCellProperty(ControlNode control, string propertyKey, int value)
    {
        control.Properties[propertyKey] = new PropertyValue
        {
            Name = propertyKey,
            Value = value.ToString(),
            Type = "int"
        };
    }

    /// <summary>
    /// Parses `tableLayoutPanel1.SetColumn(control1, 0)` / `SetRow` / `SetColumnSpan` /
    /// `SetRowSpan` - the alternative to the Controls.Add 3-arg overload for recording
    /// TableLayoutPanel cell placement. Writes directly into the target control's
    /// ControlNode.Properties (via the shared `controls` dictionary), so it works regardless
    /// of statement order relative to the corresponding Controls.Add(...) call.
    /// </summary>
    private static void ParseTableLayoutPanelSetterInvocation(InvocationExpressionSyntax invocation,
        string methodName, MemberAccessExpressionSyntax memberAccess, Dictionary<string, ControlNode> controls)
    {
        var panelName = GetControlNameFromExpression(memberAccess.Expression);
        if (string.IsNullOrEmpty(panelName) ||
            !controls.TryGetValue(panelName, out var panel) ||
            panel.ControlType != "TableLayoutPanel")
        {
            return;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 2)
        {
            return;
        }

        var targetControlName = GetControlNameFromExpression(arguments[0].Expression);
        if (string.IsNullOrEmpty(targetControlName) || !controls.TryGetValue(targetControlName, out var target))
        {
            return;
        }

        if (!int.TryParse(arguments[1].Expression.ToString(), out var value))
        {
            return;
        }

        var propertyKey = methodName switch
        {
            "SetColumn" => "TableLayoutPanel.Column",
            "SetRow" => "TableLayoutPanel.Row",
            "SetColumnSpan" => "TableLayoutPanel.ColumnSpan",
            "SetRowSpan" => "TableLayoutPanel.RowSpan",
            _ => null
        };

        if (propertyKey != null)
        {
            SetTableLayoutPanelCellProperty(target, propertyKey, value);
        }
    }

    /// <summary>
    /// Parses `control.DataBindings.Add(...)`, handling both the common designer-generated
    /// shape (`Add(new Binding("Text", source, "Member", true))`) and the direct overload
    /// (`Add("Text", source, "Member"[, true])`).
    /// </summary>
    private void ParseDataBindingsAddInvocation(InvocationExpressionSyntax invocation, string controlName,
        Dictionary<string, ControlNode> controls)
    {
        if (!controls.TryGetValue(controlName, out var control))
        {
            return;
        }

        var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;

        if (firstArg is ObjectCreationExpressionSyntax bindingCreation && bindingCreation.ArgumentList != null)
        {
            AddDataBindingFromArguments(control, bindingCreation.ArgumentList.Arguments);
            return;
        }

        AddDataBindingFromArguments(control, invocation.ArgumentList.Arguments);
    }

    private void AddDataBindingFromArguments(ControlNode control, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        if (arguments.Count < 3)
        {
            return;
        }

        var propertyName = UnquoteStringLiteral(arguments[0].Expression.ToString());
        var dataSource = arguments[1].Expression.ToString();
        var dataMember = UnquoteStringLiteral(arguments[2].Expression.ToString());
        var formattingEnabled = arguments.Count > 3 &&
            bool.TryParse(arguments[3].Expression.ToString(), out var parsedFormatting) && parsedFormatting;

        control.DataBindings.Add(new DataBinding
        {
            PropertyName = propertyName,
            DataSource = dataSource,
            DataMember = dataMember,
            FormattingEnabled = formattingEnabled
        });
    }

    /// <summary>
    /// Parses `resources.ApplyResources(target, "prefix")`, synthesizing a PropertyValue on
    /// the target control for every resx entry whose key starts with "{prefix}." (e.g. key
    /// "button1.Text" with prefix "button1" becomes property "Text"). Nested/compound keys
    /// (more than one dot after the prefix) are skipped as out of scope for v1 - they fall
    /// back to "unmapped property", same as any other property this parser doesn't recognize.
    /// </summary>
    private void ParseApplyResourcesInvocation(InvocationExpressionSyntax invocation,
        Dictionary<string, ControlNode> controls, IReadOnlyDictionary<string, ResxEntry>? resources)
    {
        if (resources == null)
        {
            return;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 2)
        {
            return;
        }

        var targetControlName = GetControlNameFromExpression(arguments[0].Expression);
        if (string.IsNullOrEmpty(targetControlName) || !controls.TryGetValue(targetControlName, out var control))
        {
            return;
        }

        if (arguments[1].Expression is not LiteralExpressionSyntax prefixLiteral ||
            !prefixLiteral.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return;
        }

        var searchPrefix = UnquoteStringLiteral(prefixLiteral.ToString()) + ".";

        foreach (var (key, entry) in resources)
        {
            if (!key.StartsWith(searchPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var suffix = key[searchPrefix.Length..];
            if (suffix.Contains('.'))
            {
                continue;
            }

            control.Properties[suffix] = BuildResxPropertyValue(suffix, entry);
        }
    }

    /// <summary>
    /// Detects `resources.GetObject("key")`/`resources.GetString("key")`, unwrapping any
    /// leading cast/parenthesization (e.g. `((System.Drawing.Image)(resources.GetObject(...)))`,
    /// the shape the WinForms designer actually emits for non-string resources).
    /// </summary>
    private static bool TryGetResxKey(ExpressionSyntax expression, out string key)
    {
        key = string.Empty;
        var unwrapped = UnwrapCastsAndParens(expression);

        if (unwrapped is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "GetObject" && methodName != "GetString")
        {
            return false;
        }

        if (memberAccess.Expression.ToString() != ResourceManagerVariableName)
        {
            return false;
        }

        if (invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not LiteralExpressionSyntax literal ||
            !literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return false;
        }

        key = UnquoteStringLiteral(literal.ToString());
        return true;
    }

    private static ExpressionSyntax UnwrapCastsAndParens(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case CastExpressionSyntax cast:
                    expression = cast.Expression;
                    continue;
                case ParenthesizedExpressionSyntax paren:
                    expression = paren.Expression;
                    continue;
                default:
                    return expression;
            }
        }
    }

    /// <summary>
    /// Builds a PropertyValue for a resolved resx entry. String entries resolve fully here
    /// (the value flows straight into the existing property-mapping/generation path
    /// unchanged); binary/external-file entries are marked "resource-binary" with a null
    /// Value, resolved later by ConversionOrchestrator (which knows the output directory -
    /// this parser doesn't).
    /// </summary>
    private static PropertyValue BuildResxPropertyValue(string propertyName, ResxEntry entry)
    {
        if (entry.StringValue != null)
        {
            return new PropertyValue
            {
                Name = propertyName,
                Value = entry.StringValue,
                Type = "string",
                IsResource = true,
                ResourceKey = entry.Name
            };
        }

        return new PropertyValue
        {
            Name = propertyName,
            Value = null,
            Type = "resource-binary",
            IsResource = true,
            ResourceKey = entry.Name
        };
    }

    /// <summary>
    /// Returns the raw C# source text of a property assignment's right-hand side, except for
    /// string/char literals, where Roslyn's <c>ToString()</c> would otherwise include the
    /// surrounding quote characters verbatim (e.g. <c>this.Text = "Sign In";</c> would yield
    /// <c>"Sign In"</c> quotes and all) - those instead use <see cref="LiteralExpressionSyntax.Token"/>'s
    /// <c>ValueText</c>, Roslyn's already-unescaped value (also correctly resolves escape
    /// sequences and verbatim-string prefixes, which a naive quote-strip would not). Every
    /// other expression shape (member access like <c>Color.Gray</c>, <c>new Point(...)</c>,
    /// <c>DockStyle.Fill</c>, concatenation, interpolation) keeps the raw source text, since
    /// PropertyValueConverter's regex-based converters parse that shape directly.
    /// </summary>
    private static string GetPropertyAssignmentValue(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal &&
            (literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.CharacterLiteralExpression)))
        {
            return literal.Token.ValueText;
        }

        return expression.ToString();
    }

    private static string UnquoteStringLiteral(string text)
    {
        return text.Length >= 2 && text[0] == '"' && text[^1] == '"'
            ? text[1..^1]
            : text;
    }

    private static string GetControlNameFromExpression(ExpressionSyntax? expression)
    {
        if (expression == null)
        {
            return string.Empty;
        }

        var exprString = expression.ToString();

        if (exprString.StartsWith("this.", StringComparison.Ordinal))
        {
            return exprString[5..];
        }

        return exprString;
    }

    /// <summary>
    /// Sentinel handler "name" recorded in <see cref="ControlNode.EventHandlers"/> when a
    /// subscription's right-hand side is an inline lambda (e.g. `button.Click += (s, e) =>
    /// {...}`) with no stable method name to extract - not a valid C# identifier. Downstream
    /// generators (ViewModelGenerator, CodeBehindGenerator, AxamlGenerator) must special-case
    /// this value - skipping codegen for that event rather than emitting it as a method/attribute
    /// name - and ConversionOrchestrator surfaces it as a manual step instead.
    /// </summary>
    public const string InlineLambdaHandlerMarker = "<inline lambda - manual review required>";

    private bool IsControlField(FieldDeclarationSyntax field)
    {
        var type = field.Declaration.Type.ToString();
        var shortType = GetShortTypeName(type);

        if (KnownControlTypeNames.Contains(shortType) ||
            shortType.Contains("Control", StringComparison.Ordinal) ||
            shortType.Contains("Component", StringComparison.Ordinal))
        {
            return true;
        }

        // Fallback heuristic for custom/third-party controls not in the known-type list:
        // treat anything as a plausible control field unless it's a recognized BCL/system
        // type, a generic collection, or an explicitly excluded non-control member. This is
        // a syntax-only heuristic (no semantic model), so it can still misclassify unusual
        // field types - a true fix would require resolving the field's base type.
        if (NonControlBclShortTypeNames.Contains(shortType) ||
            type.StartsWith("System.", StringComparison.Ordinal) ||
            shortType.Contains('<'))
        {
            return false;
        }

        return true;
    }

    private string GetShortTypeName(string fullTypeName)
    {
        var parts = fullTypeName.Split('.');
        return parts.Length > 0 ? parts[^1] : fullTypeName;
    }
}

/// <summary>
/// Result of parsing a designer file.
/// </summary>
public class ParseResult
{
    public required string FilePath { get; init; }
    public bool Success { get; set; }
    public List<string> Errors { get; init; } = [];
    public ControlNode? RootControl { get; set; }
    public List<ControlNode> AllControls { get; set; } = [];

    /// <summary>
    /// Original WinForms event-handler method bodies (keyed by method name, e.g.
    /// "button1_Click"), extracted from the sibling non-designer .cs file by
    /// EventHandlerBodyParser. Populated by the orchestrator after parsing (this parser only
    /// reads the .Designer.cs file itself), so it defaults empty here - every existing
    /// ParseResult construction/test keeps compiling and behaving identically.
    /// </summary>
    public Dictionary<string, string> EventHandlerBodies { get; set; } = [];

    /// <summary>
    /// Non-handler members (private fields, helper methods) discovered in the sibling
    /// non-designer .cs file by CodeBehindMemberExtractor. Populated by the orchestrator
    /// alongside EventHandlerBodies, same gating and empty-default rationale.
    /// </summary>
    public CodeBehindMembers CodeBehindMembers { get; set; } = CodeBehindMembers.Empty;
}
