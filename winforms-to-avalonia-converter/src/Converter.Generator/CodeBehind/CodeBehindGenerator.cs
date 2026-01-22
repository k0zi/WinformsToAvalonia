using System.Text;

namespace Converter.Generator.CodeBehind;

/// <summary>
/// Generates code-behind files for Avalonia views.
/// </summary>
public class CodeBehindGenerator
{
    /// <summary>
    /// Generate code-behind (.axaml.cs) file.
    /// </summary>
    public string Generate(string namespaceName, string className)
    {
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using Avalonia.Controls;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {namespaceName}.Views;");
        sb.AppendLine();

        // Class declaration
        sb.AppendLine($"public partial class {className} : Window");
        sb.AppendLine("{");
        sb.AppendLine($"    public {className}()");
        sb.AppendLine("    {");
        sb.AppendLine("        InitializeComponent();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
