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
    /// Dictionary key used to resolve "this" references (e.g. this.Controls.Add(...)) back
    /// to the root form's ControlNode.
    /// </summary>
    private const string ThisAlias = "this";

    public WinFormsParser(ILogger<WinFormsParser>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a Designer.cs file into a control tree.
    /// </summary>
    public async Task<ParseResult> ParseDesignerFileAsync(string filePath)
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
            ParseInitializeComponent(initializeComponentMethod, controls, rootControl);

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
        Dictionary<string, ControlNode> controls, ControlNode rootControl)
    {
        var statements = method.Body?.Statements ?? default;
        WalkStatements(statements, controls, rootControl);
    }

    /// <summary>
    /// Recursively walks statements, descending into blocks/if/foreach so control
    /// declarations wrapped in conditional or looping designer code aren't skipped.
    /// </summary>
    private void WalkStatements(SyntaxList<StatementSyntax> statements,
        Dictionary<string, ControlNode> controls, ControlNode rootControl)
    {
        foreach (var statement in statements)
        {
            WalkStatement(statement, controls, rootControl);
        }
    }

    private void WalkStatement(StatementSyntax statement,
        Dictionary<string, ControlNode> controls, ControlNode rootControl)
    {
        switch (statement)
        {
            case ExpressionStatementSyntax expressionStatement:
                DispatchExpressionStatement(expressionStatement, controls, rootControl);
                break;

            case BlockSyntax block:
                WalkStatements(block.Statements, controls, rootControl);
                break;

            case IfStatementSyntax ifStatement:
                WalkStatement(ifStatement.Statement, controls, rootControl);
                if (ifStatement.Else != null)
                {
                    WalkStatement(ifStatement.Else.Statement, controls, rootControl);
                }
                break;

            case ForEachStatementSyntax forEachStatement:
                WalkStatement(forEachStatement.Statement, controls, rootControl);
                break;
        }
    }

    private void DispatchExpressionStatement(ExpressionStatementSyntax expressionStatement,
        Dictionary<string, ControlNode> controls, ControlNode rootControl)
    {
        if (expressionStatement.Expression is AssignmentExpressionSyntax assignment)
        {
            if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                ParseEventSubscription(assignment, controls);
            }
            else
            {
                ParsePropertyAssignment(assignment, controls);
            }
        }
        else if (expressionStatement.Expression is InvocationExpressionSyntax invocation)
        {
            ParseMethodInvocation(invocation, controls, rootControl);
        }
    }

    private void ParsePropertyAssignment(AssignmentExpressionSyntax assignment,
        Dictionary<string, ControlNode> controls)
    {
        if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
        {
            var controlName = GetControlNameFromExpression(memberAccess.Expression);
            var propertyName = memberAccess.Name.Identifier.Text;

            if (!string.IsNullOrEmpty(controlName) && controls.TryGetValue(controlName, out var control))
            {
                var value = assignment.Right.ToString();
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
            return "<inline lambda - manual review required>";
        }

        return GetControlNameFromExpression(right);
    }

    private void ParseMethodInvocation(InvocationExpressionSyntax invocation,
        Dictionary<string, ControlNode> controls, ControlNode rootControl)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var methodName = memberAccess.Name.Identifier.Text;

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
        var childArgument = invocation.ArgumentList.Arguments.FirstOrDefault();
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
}
