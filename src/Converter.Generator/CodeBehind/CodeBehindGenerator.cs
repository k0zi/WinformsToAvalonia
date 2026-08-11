using System.Text;
using System.Text.RegularExpressions;
using Converter.Core.Parsing;
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
    /// Generate code-behind (.axaml.cs) file. The constructor wires DataContext to a new
    /// instance of the form's ViewModel and exposes it through a typed "ViewModel" accessor
    /// property, so PreserveEventHandler stubs below can reach fields/methods that
    /// ViewModelGenerator.BuildEditableClass migrated there. For every event WinForms attached
    /// whose EventMappingRegistry entry says PreserveEventHandler (and that a plugin hasn't
    /// already claimed via <paramref name="overrides"/>), emits a correctly-signed stub method
    /// under the original handler name; the original handler body, if found in
    /// <paramref name="handlerBodies"/>, is embedded as live code (best-effort identifier
    /// rewrite of any migrated field/method reference to "ViewModel.name" - a plain word-
    /// boundary regex, not Roslyn-token-aware, so it can also match inside string/comment
    /// literals or a shadowed local/parameter sharing a migrated name; an accepted limitation,
    /// not chased to zero). A handler not found in <paramref name="handlerBodies"/> gets a
    /// plain "port manually" TODO comment instead. <paramref name="avaloniaMajorVersion"/>
    /// picks which EventSignatureRegistry entries apply (defaults to 12, matching this
    /// converter's current default generated-project target) - the only entries that currently
    /// differ by version are GotFocus/LostFocus.
    /// </summary>
    public string Generate(
        string namespaceName, string className, ControlNode root,
        IReadOnlyDictionary<string, string>? handlerBodies = null,
        PluginMappingOverrides? overrides = null,
        int avaloniaMajorVersion = 12,
        CodeBehindMembers? codeBehindMembers = null,
        string viewModelSuffix = "ViewModel",
        IReadOnlyList<CustomControlProperty>? bindableProperties = null)
    {
        overrides ??= PluginMappingOverrides.Empty;
        codeBehindMembers ??= CodeBehindMembers.Empty;
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using Avalonia.Controls;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {namespaceName}.Views;");
        sb.AppendLine();

        var vmType = $"{namespaceName}.ViewModels.{className}{viewModelSuffix}";

        // Mirrors AxamlGenerator's own root-element choice: "Form" -> "Window", "UserControl"
        // -> "UserControl", defaulting to "Window" for anything unrecognized.
        var rootBaseType = ControlMappingRegistry.GetMapping(root.ControlType)?.AvaloniaType ?? "Window";

        // Class declaration
        sb.AppendLine($"public partial class {className} : {rootBaseType}");
        sb.AppendLine("{");
        sb.AppendLine($"    public {className}()");
        sb.AppendLine("    {");
        sb.AppendLine("        InitializeComponent();");
        sb.AppendLine($"        DataContext = new {vmType}();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private {vmType} ViewModel => ({vmType})DataContext!;");

        // Re-exposes this custom control's own simple public auto-properties (see
        // CustomControlPropertyExtractor) as real Avalonia bindable properties, under their
        // original name, so a parent embedding this control can set them as a plain XAML
        // attribute (e.g. CustomerId="5") instead of the value being silently dropped.
        if (bindableProperties is { Count: > 0 })
        {
            foreach (var property in bindableProperties)
            {
                sb.AppendLine();
                sb.AppendLine($"    public static readonly Avalonia.StyledProperty<{property.TypeName}> {property.Name}Property =");
                sb.AppendLine($"        Avalonia.AvaloniaProperty.Register<{className}, {property.TypeName}>(nameof({property.Name}));");
                sb.AppendLine();
                sb.AppendLine($"    public {property.TypeName} {property.Name}");
                sb.AppendLine("    {");
                sb.AppendLine($"        get => GetValue({property.Name}Property);");
                sb.AppendLine($"        set => SetValue({property.Name}Property, value);");
                sb.AppendLine("    }");
            }
        }

        var migratedNames = codeBehindMembers.Fields
            .SelectMany(f => f.Names)
            .Concat(codeBehindMembers.HelperMethods.Keys)
            .Distinct()
            .OrderByDescending(n => n.Length)
            .ToList();

        var handlers = new List<(string AvaloniaEvent, string HandlerName)>();
        CollectPreservedHandlers(root, overrides, handlers);

        foreach (var (avaloniaEvent, handlerName) in handlers.DistinctBy(h => h.HandlerName))
        {
            var signature = EventSignatureRegistry.GetSignature(avaloniaEvent, avaloniaMajorVersion);
            string? originalSource = null;
            var hasOriginalSource = handlerBodies != null && handlerBodies.TryGetValue(handlerName, out originalSource);
            // Mirrors ViewModelGenerator's same handling: WinForms event handlers are commonly
            // "async void", and a body containing "await" needs that modifier preserved on the
            // freshly-constructed stub signature or the generated code won't compile.
            var asyncModifier = hasOriginalSource && EventHandlerBodyParser.IsAsyncMethodSignature(originalSource!) ? "async " : "";

            sb.AppendLine();
            sb.AppendLine($"    private {asyncModifier}void {handlerName}(object? sender, {signature.EventArgsType} e)");

            if (hasOriginalSource)
            {
                var body = EventHandlerBodyParser.ExtractBodyText(originalSource!);
                foreach (var name in migratedNames)
                {
                    body = Regex.Replace(body, $@"\b{Regex.Escape(name)}\b", $"ViewModel.{name}");
                }

                sb.AppendLine("    " + body.Replace("\n", "\n    "));
            }
            else
            {
                sb.AppendLine("    {");
                sb.AppendLine($"        // TODO: original \"{handlerName}\" handler body not found - port manually");
                sb.AppendLine("    }");
            }
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

            if (handlerName == WinFormsParser.InlineLambdaHandlerMarker)
            {
                // No stable method name to emit a stub under; surfaced as a manual step by
                // ConversionOrchestrator.CollectManualSteps instead.
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
