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

            foreach (var field in controlFields)
            {
                var variable = field.Declaration.Variables.FirstOrDefault();
                if (variable == null) continue;

                var controlName = variable.Identifier.Text;
                var controlType = field.Declaration.Type.ToString();

                var controlNode = new ControlNode
                {
                    ControlType = GetShortTypeName(controlType),
                    FullTypeName = controlType,
                    Name = controlName,
                    SourceFile = filePath,
                    SourceLine = field.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                };

                controls[controlName] = controlNode;
            }

            // Parse InitializeComponent to build hierarchy and extract properties
            ParseInitializeComponent(initializeComponentMethod, controls, rootControl);

            result.RootControl = rootControl;
            result.AllControls = controls.Values.ToList();

            _logger?.LogInformation("Parsed {ControlCount} controls from {FilePath}", 
                controls.Count, filePath);

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
        var statements = method.Body?.Statements ?? [];

        foreach (var statement in statements)
        {
            // Look for assignment expressions (property settings)
            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                if (expressionStatement.Expression is AssignmentExpressionSyntax assignment)
                {
                    ParsePropertyAssignment(assignment, controls);
                }
                // Look for method invocations (e.g., Controls.Add, Events)
                else if (expressionStatement.Expression is InvocationExpressionSyntax invocation)
                {
                    ParseMethodInvocation(invocation, controls, rootControl);
                }
            }
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
                    Type = "object" // Type inference would require semantic model
                };
            }
        }
    }

    private void ParseMethodInvocation(InvocationExpressionSyntax invocation, 
        Dictionary<string, ControlNode> controls, ControlNode rootControl)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;

            // Handle Controls.Add(control)
            if (methodName == "Add" && memberAccess.Expression.ToString().EndsWith(".Controls"))
            {
                var parentControlName = GetControlNameFromExpression(memberAccess.Expression.ToString().Replace(".Controls", ""));
                var childControlName = invocation.ArgumentList.Arguments.FirstOrDefault()?.ToString();

                if (!string.IsNullOrEmpty(parentControlName) && !string.IsNullOrEmpty(childControlName) &&
                    controls.TryGetValue(parentControlName, out var parent) &&
                    controls.TryGetValue(childControlName, out var child))
                {
                    child.Parent = parent;
                    parent.Children.Add(child);
                }
            }
            // Handle event subscriptions (e.g., button1.Click += ...)
            else if (invocation.Expression is AssignmentExpressionSyntax eventAssignment &&
                     eventAssignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                // This is an event subscription
                // Would need more context to extract properly
            }
        }
    }

    private string GetControlNameFromExpression(object expression)
    {
        var exprString = expression.ToString();
        
        if (exprString.StartsWith("this."))
        {
            return exprString.Substring(5);
        }
        
        return exprString;
    }

    private bool IsControlField(FieldDeclarationSyntax field)
    {
        var type = field.Declaration.Type.ToString();
        
        // Common WinForms controls
        var controlTypes = new[]
        {
            "Button", "TextBox", "Label", "Panel", "GroupBox", "DataGridView",
            "TreeView", "ListView", "ComboBox", "CheckBox", "RadioButton",
            "PictureBox", "ProgressBar", "TabControl", "MenuStrip", "ToolStrip"
        };

        return controlTypes.Any(t => type.Contains(t)) || 
               type.Contains("Control") || 
               type.Contains("Component");
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
