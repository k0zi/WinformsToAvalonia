using System.Text;
using Converter.Generator.Mapping;
using Converter.Mappings.BuiltIn;
using Converter.Plugin.Abstractions;

namespace Converter.Generator.CodeBehind;

/// <summary>
/// Generates code-behind files for Avalonia views.
/// </summary>
public class CodeBehindGenerator
{
    /// <summary>
    /// Generate code-behind (.axaml.cs) file. For every event WinForms attached whose
    /// EventMappingRegistry entry says PreserveEventHandler (and that a plugin hasn't
    /// already claimed via <paramref name="overrides"/>), emits a correctly-signed stub
    /// method under the original handler name; the original handler body, if found in
    /// <paramref name="handlerBodies"/>, is embedded as an inert `//`-prefixed comment block
    /// - never live/compiled code - inside it. A handler not found in
    /// <paramref name="handlerBodies"/> gets a plain "port manually" TODO comment instead.
    /// </summary>
    public string Generate(
        string namespaceName, string className, ControlNode root,
        IReadOnlyDictionary<string, string>? handlerBodies = null,
        PluginMappingOverrides? overrides = null)
    {
        overrides ??= PluginMappingOverrides.Empty;
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

        var handlers = new List<(string AvaloniaEvent, string HandlerName)>();
        CollectPreservedHandlers(root, overrides, handlers);

        foreach (var (avaloniaEvent, handlerName) in handlers.DistinctBy(h => h.HandlerName))
        {
            var signature = EventSignatureRegistry.GetSignature(avaloniaEvent);

            sb.AppendLine();
            sb.AppendLine($"    private void {handlerName}(object? sender, {signature.EventArgsType} e)");
            sb.AppendLine("    {");

            if (handlerBodies != null && handlerBodies.TryGetValue(handlerName, out var originalSource))
            {
                sb.AppendLine("        // Original WinForms handler, preserved for reference - review and adapt:");
                foreach (var line in originalSource.Replace("\r\n", "\n").Split('\n'))
                {
                    sb.AppendLine(string.IsNullOrWhiteSpace(line) ? "        //" : $"        // {line}");
                }
            }
            else
            {
                sb.AppendLine($"        // TODO: original \"{handlerName}\" handler body not found - port manually");
            }

            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void CollectPreservedHandlers(
        ControlNode control, PluginMappingOverrides overrides, List<(string, string)> handlers)
    {
        foreach (var (eventName, handlerName) in control.EventHandlers)
        {
            if (overrides.EventMappings.ContainsKey((control, eventName)))
            {
                continue;
            }

            var mapping = EventMappingRegistry.GetMapping(eventName);
            if (mapping?.PreserveEventHandler == true)
            {
                handlers.Add((mapping.AvaloniaEvent, handlerName));
            }
        }

        foreach (var child in control.Children)
        {
            CollectPreservedHandlers(child, overrides, handlers);
        }
    }
}
