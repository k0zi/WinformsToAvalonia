using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converter.Core.Parsing;
using Converter.Generator.Mapping;
using Converter.Plugin.Abstractions;
using Converter.Mappings.BuiltIn;

namespace Converter.Generator.ViewModels;

/// <summary>
/// Flat manifest of every member name BuildEditableClass seeded into the hand-editable
/// ViewModel file this run, alongside its full source. Used by the caller both to decide
/// whether to write the file at all (only on first creation - never touched again after) and,
/// on later runs where the file already exists, to detect drift (a name discovered this run
/// that isn't present in the existing hand-edited file yet) without recomputing anything twice.
/// </summary>
public record EditableClassContent(string Source, IReadOnlyList<string> MemberNames);

/// <summary>
/// Generates ViewModel classes using CommunityToolkit.Mvvm.
/// </summary>
public class ViewModelGenerator
{
    /// <summary>
    /// Generate the auto-regenerated partial class (.g.cs) - properties only, mechanically
    /// derived from ControlNode.DataBindings, safe to regenerate every run since none of it is
    /// migrated user logic. Returns string.Empty when there are zero bound properties; callers
    /// should skip writing (or delete a stale) .g.cs in that case rather than emit an empty
    /// shell. Declares no base type - ObservableObject is declared once, on the always-present
    /// hand-editable partial (see BuildEditableClass), since this file may or may not exist.
    /// </summary>
    public string GeneratePartialClass(
        ControlNode root, string namespaceName, string className,
        IReadOnlyDictionary<(string ControlName, string Property), string>? inferredBindings = null)
    {
        var properties = ExtractBoundProperties(root, inferredBindings);
        if (properties.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.AppendLine("using CommunityToolkit.Mvvm.ComponentModel;");
        sb.AppendLine("using System.Collections.ObjectModel;");
        sb.AppendLine();

        sb.AppendLine($"namespace {namespaceName}.ViewModels;");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// ViewModel for {className} (auto-generated - observable properties only).");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className}ViewModel");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            sb.AppendLine($"    [ObservableProperty]");
            sb.AppendLine($"    private {prop.Type} {prop.FieldName} = {prop.DefaultValue};");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the hand-editable partial class (className + ViewModelSuffix + ".cs", no ".g."
    /// infix) - written only once by the caller (skipped entirely on subsequent runs if it
    /// already exists on disk), so user edits survive reconversion. Declares
    /// ": ObservableObject" (this file always exists, unlike the conditional .g.cs, so the
    /// base type lives here). Seeds it with everything CodeBehindMemberExtractor/
    /// EventHandlerBodyParser found in the sibling WinForms code-behind: migrated fields and
    /// helper methods (accessibility upgraded to "internal" so CodeBehindGenerator's
    /// ViewModel-accessor rewrite can reach them from a different class/file), and
    /// [RelayCommand] methods for ConvertToCommand-bucket events with their real bodies as
    /// live code (falling back to a TODO when no original body was found).
    /// </summary>
    private static readonly HashSet<(string FormName, string DialogResultValue)> EmptyDialogResultButtons = [];
    private static readonly HashSet<string> EmptyCommandFallback = [];
    private static readonly HashSet<string> EmptyFormClassNames = [];

    public EditableClassContent BuildEditableClass(
        ControlNode root, string namespaceName, string className,
        PluginMappingOverrides? overrides = null,
        IReadOnlyDictionary<string, string>? handlerBodies = null,
        CodeBehindMembers? codeBehindMembers = null,
        IReadOnlyDictionary<(string ControlName, string Property), string>? inferredBindings = null,
        IReadOnlySet<(string FormName, string DialogResultValue)>? formsWithDialogResultButton = null,
        IReadOnlySet<string>? commandFallbackHandlerNames = null,
        IReadOnlySet<string>? convertedFormClassNames = null)
    {
        overrides ??= PluginMappingOverrides.Empty;
        codeBehindMembers ??= CodeBehindMembers.Empty;
        formsWithDialogResultButton ??= EmptyDialogResultButtons;
        commandFallbackHandlerNames ??= EmptyCommandFallback;
        convertedFormClassNames ??= EmptyFormClassNames;
        var boundControlProperties = BuildBoundControlPropertyLookup(root, inferredBindings);

        var memberNames = new List<string>();
        var sb = new StringBuilder();

        foreach (var usingNamespace in BuildUsingDirectives(codeBehindMembers))
        {
            sb.AppendLine($"using {usingNamespace};");
        }
        sb.AppendLine();

        sb.AppendLine($"namespace {namespaceName}.ViewModels;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// ViewModel for {className} (user customizations).");
        sb.AppendLine("/// This file is preserved during reconversion - add your custom code here.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className}ViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject");
        sb.AppendLine("{");

        foreach (var field in codeBehindMembers.Fields)
        {
            sb.AppendLine("    " + IndentContinuationLines(EnsureInternalAccessibility(field.DeclarationText)));
            sb.AppendLine();
            memberNames.AddRange(field.Names);
        }

        // A small local helper type (e.g. "private sealed class NewLine { ... }") a migrated
        // method's own signature references - migrated verbatim, same accessibility-upgrade
        // treatment as fields/methods so it's reachable if CodeBehindGenerator's "ViewModel."
        // rewrite ever needs to reference it from a different class/file.
        foreach (var (name, source) in codeBehindMembers.NestedTypes)
        {
            sb.AppendLine("    " + IndentContinuationLines(EnsureInternalAccessibility(source)));
            sb.AppendLine();
            memberNames.Add(name);
        }

        foreach (var (name, source) in codeBehindMembers.HelperMethods)
        {
            // A migrated helper method (e.g. LoadFromEntity/ValidateInput/SaveToEntity) is
            // exactly as likely as a RelayCommand body to read/write another control directly
            // (e.g. "skuTextBox.Text") - the same rewrite that already fixes that for
            // RelayCommand/property-changed-hook bodies below applies here too, so the
            // ViewModel doesn't end up referencing View-only controls.
            var rewritten = RewriteBoundControlReferences(source, boundControlProperties);
            // A migrated method commonly shows a MessageBox to confirm/report something -
            // rewrite it into the generated Dialogs.ShowAsync helper the same way RelayCommand
            // bodies below get. Unlike those, a helper method's signature is verbatim migrated
            // text (not reconstructed here) - TranspileMethod (not Transpile) operates on the
            // full signature+body text this loop has, and making it async when the rewrite adds
            // an "await" goes through EnsureAsyncModifier's own syntax-tree edit rather than a
            // string-built signature.
            var helperTranspiled = MessageBoxTranspiler.TranspileMethod(rewritten, namespaceName);
            // Same "recognize a fixed shape, rewrite it" treatment for the "show a child form
            // modally, check DialogResult" idiom - run on MessageBoxTranspiler's own output so
            // the two compose (a method can use both patterns).
            var childDialogTranspiled = ChildDialogTranspiler.TranspileMethod(
                helperTranspiled.TransformedBody, namespaceName, formsWithDialogResultButton, convertedFormClassNames);
            var helperSource = EnsureInternalAccessibility(childDialogTranspiled.TransformedBody);
            if (helperTranspiled.AddedAwait || childDialogTranspiled.AddedAwait)
            {
                helperSource = EventHandlerBodyParser.EnsureAsyncModifier(helperSource);
            }

            sb.AppendLine("    " + IndentContinuationLines(helperSource));
            sb.AppendLine();
            memberNames.Add(name);
        }

        var commands = ExtractCommands(root, overrides);
        foreach (var command in commands)
        {
            // Single-view-path safety valve: a handler whose migrated body reads/writes another
            // control directly with no [ObservableProperty] to rewrite into (findable via
            // FindUnresolvedControlReferences) cannot live in the ViewModel at all - it would
            // reference View-only controls from a class that has no access to them. Such
            // handlers are downgraded back to code-behind by the caller (see
            // FindDowngradedCommandHandlerNames / CodeBehindGenerator's same-named collect):
            // this file simply must not emit a [RelayCommand] for them, or the generated
            // command would compile to a stub that's never bound anywhere.
            if (commandFallbackHandlerNames.Contains(command.OriginalHandlerMethodName))
            {
                continue;
            }

            string? originalSource = null;
            var hasOriginalSource = handlerBodies != null &&
                handlerBodies.TryGetValue(command.OriginalHandlerMethodName, out originalSource);

            var body = string.Empty;
            var addedAwait = false;
            if (hasOriginalSource)
            {
                body = EventHandlerBodyParser.ExtractBodyText(originalSource!);
                body = RewriteBoundControlReferences(body, boundControlProperties);
                var transpiled = MessageBoxTranspiler.Transpile(body, namespaceName);
                body = transpiled.TransformedBody;
                var childDialogTranspiled = ChildDialogTranspiler.Transpile(body, namespaceName, formsWithDialogResultButton, convertedFormClassNames);
                body = childDialogTranspiled.TransformedBody;
                addedAwait = transpiled.AddedAwait || childDialogTranspiled.AddedAwait;
            }

            // The original handler is commonly "async void" (WinForms event handlers are
            // fire-and-forget by nature); the body carries "await" regardless of whether we
            // preserve that modifier, so dropping it would emit code that doesn't compile. A
            // MessageBoxTranspiler rewrite adds its own "await" for the same reason even when
            // the original handler wasn't async.
            var asyncModifier = (hasOriginalSource && EventHandlerBodyParser.IsAsyncMethodSignature(originalSource!)) || addedAwait
                ? "async " : "";

            sb.AppendLine($"    [CommunityToolkit.Mvvm.Input.RelayCommand]");
            if (command.HasParameter)
            {
                sb.AppendLine($"    private {asyncModifier}void {command.MethodName}({command.ParameterType} parameter)");
            }
            else
            {
                sb.AppendLine($"    private {asyncModifier}void {command.MethodName}()");
            }

            if (hasOriginalSource)
            {
                sb.AppendLine("    " + IndentContinuationLines(body));
            }
            else
            {
                sb.AppendLine("    {");
                sb.AppendLine($"        // TODO: Implement {command.OriginalEvent} logic");
                sb.AppendLine("    }");
            }

            sb.AppendLine();
            memberNames.Add(command.MethodName);
        }

        foreach (var hook in ExtractPropertyChangedHooks(root))
        {
            string? originalSource = null;
            var hasOriginalSource = handlerBodies != null &&
                handlerBodies.TryGetValue(hook.OriginalHandlerMethodName, out originalSource);
            if (!hasOriginalSource)
            {
                // No body to migrate - fall through to the "RequiresCustomLogic" manual step
                // ConversionOrchestrator.CollectManualStepsRecursive emits for this case instead
                // of emitting an empty/TODO hook here.
                continue;
            }

            // CommunityToolkit.Mvvm's source generator always declares this hook's partial
            // signature as plain "partial void" (never async) - an "async" implementation is
            // legal C# (partial method halves only need matching name/params/return type, not
            // matching async-ness) but muddies a feature that's already best-effort, so this
            // intentionally does not add an async modifier even if the original handler had one.
            sb.AppendLine($"    partial void On{hook.BoundPropertyName}Changed({hook.ParameterType} value)");
            var body = EventHandlerBodyParser.ExtractBodyText(originalSource!);
            body = RewriteBoundControlReferences(body, boundControlProperties);
            sb.AppendLine("    " + IndentContinuationLines(body));
            sb.AppendLine();
            memberNames.Add($"On{hook.BoundPropertyName}Changed");
        }

        sb.AppendLine("}");

        return new EditableClassContent(sb.ToString(), memberNames);
    }

    /// <summary>
    /// Maps every "controlName.ControlSideProperty" pair backed by a DataBindings.Add(...) entry
    /// anywhere under <paramref name="root"/> to the [ObservableProperty] this generator emits
    /// for it (see ExtractBoundProperties/GeneratePartialClass). A migrated event-handler body
    /// spliced into this ViewModel as live code (BuildEditableClass, below) commonly reads/writes
    /// another control directly - e.g. "textBox1.Text" - which does not compile here (the
    /// ViewModel has no "textBox1" field; that's a View concern). When the control's property is
    /// already bound, this lookup lets RewriteBoundControlReferences replace that reference with
    /// the ViewModel's own property instead, keeping the ViewModel from reaching into the View.
    /// Keyed by (control.Name, binding.PropertyName); value is the PascalCase form of
    /// binding.DataMember, matching CommunityToolkit.Mvvm's [ObservableProperty] source-generator
    /// naming convention (same casing ExtractBoundProperties' FieldName/ToCamelCase already
    /// assumes, just capitalized back to the generated public property's name).
    /// </summary>
    public IReadOnlyDictionary<(string ControlName, string ControlProperty), string> BuildBoundControlPropertyLookup(
        ControlNode root, IReadOnlyDictionary<(string ControlName, string Property), string>? additionalBindings = null)
    {
        var lookup = new Dictionary<(string, string), string>();
        BuildBoundControlPropertyLookupRecursive(root, lookup);

        // UsageInferredBindingDetector's own entries - merged in here so every existing
        // consumer of this lookup (the rewrite below, and ConversionOrchestrator's own
        // unresolved-reference flagging, which calls this same method) picks them up for free
        // without a second, parallel mechanism. DataBindings-sourced entries win on conflict
        // (shouldn't happen - the detector already excludes anything already bound).
        if (additionalBindings != null)
        {
            foreach (var (key, value) in additionalBindings)
            {
                lookup.TryAdd(key, value);
            }
        }

        return lookup;
    }

    /// <summary>
    /// The single-view-path fallback detector. Every ConvertToCommand event with a migrated
    /// body is inspected: if that body references another control's property that has no
    /// [ObservableProperty] to rewrite it into (see <see cref="FindUnresolvedControlReferences"/>),
    /// the handler cannot compile inside the ViewModel - it would reach into the View - so it
    /// must fall back to a plain code-behind event handler instead of a [RelayCommand]. The
    /// returned set is keyed by the original WinForms handler method name and consumed
    /// consistently by three call sites so they can never disagree: BuildEditableClass skips the
    /// command, AxamlGenerator emits the event attribute instead of Command="{Binding ...}", and
    /// CodeBehindGenerator collects it as a preserved handler stub with its migrated body.
    /// </summary>
    public IReadOnlySet<string> FindDowngradedCommandHandlerNames(
        ControlNode root,
        PluginMappingOverrides? overrides,
        IReadOnlyDictionary<string, string>? handlerBodies,
        IReadOnlyDictionary<(string ControlName, string Property), string>? inferredBindings)
    {
        if (handlerBodies == null || handlerBodies.Count == 0)
        {
            return EmptyCommandFallback;
        }

        var controlNames = CollectControlNames(root);
        var boundControlProperties = BuildBoundControlPropertyLookup(root, inferredBindings);
        var fallback = new HashSet<string>(StringComparer.Ordinal);

        CollectDowngraded(root, overrides ?? PluginMappingOverrides.Empty, controlNames, boundControlProperties, handlerBodies, fallback);
        return fallback;
    }

    private static void CollectDowngraded(
        ControlNode control, PluginMappingOverrides overrides, IReadOnlySet<string> controlNames,
        IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties,
        IReadOnlyDictionary<string, string> handlerBodies, HashSet<string> fallback)
    {
        foreach (var (eventName, handlerName) in control.EventHandlers)
        {
            if (handlerName == WinFormsParser.InlineLambdaHandlerMarker)
            {
                continue;
            }

            // v1 scope covers the static EventMappingRegistry path only - plugin-claimed events
            // are skipped everywhere else (WriteEventAttributes, CollectPreservedHandlers,
            // CollectManualSteps) and never get AXAML wiring, so downgrading them here would
            // strand the migrated body with no home at all.
            if (overrides.EventMappings.ContainsKey((control, eventName)) ||
                !EventMappingRegistry.ShouldConvertToCommand(eventName))
            {
                continue;
            }

            if (!handlerBodies.TryGetValue(handlerName, out var originalSource))
            {
                // No body to migrate - BuildEditableClass emits a TODO stub, which compiles;
                // nothing to fall back on.
                continue;
            }

            var body = EventHandlerBodyParser.ExtractBodyText(originalSource);
            var unresolved = FindUnresolvedControlReferences(body, controlNames, boundControlProperties);
            if (unresolved.Count > 0)
            {
                fallback.Add(handlerName);
            }
        }

        foreach (var child in control.Children)
        {
            CollectDowngraded(child, overrides, controlNames, boundControlProperties, handlerBodies, fallback);
        }
    }

    private static HashSet<string> CollectControlNames(ControlNode root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        CollectControlNamesRecursive(root, names);
        return names;
    }

    private static void CollectControlNamesRecursive(ControlNode control, HashSet<string> names)
    {
        names.Add(control.Name);
        foreach (var child in control.Children)
        {
            CollectControlNamesRecursive(child, names);
        }
    }

    /// <summary>
    /// Re-parses a migrated body (already run through EventHandlerBodyParser.ExtractBodyText)
    /// and finds every "controlName.Property" member access whose controlName matches another
    /// control in the same form's tree but has no [ObservableProperty] binding
    /// (<see cref="RewriteBoundControlReferences"/> already rewrote the ones that do) - a
    /// reference the ViewModel cannot resolve without reaching into the View. Best-effort,
    /// mirroring EventHandlerBodyParser's own tolerance for unparseable text. Shared by
    /// <see cref="FindDowngradedCommandHandlerNames"/> and ConversionOrchestrator's manual-step
    /// collection so the fallback and the manual step can never disagree about what counts as
    /// unresolved.
    /// </summary>
    public static List<(string ControlName, string Property)> FindUnresolvedControlReferences(
        string body, IReadOnlySet<string> controlNames,
        IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties)
    {
        var results = new List<(string, string)>();
        try
        {
            var wrapper = $"class __Wrapper {{ void __M() {body} }}";
            var root = CSharpSyntaxTree.ParseText(wrapper).GetRoot();

            foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (memberAccess.Expression is not IdentifierNameSyntax identifier)
                {
                    continue;
                }

                var controlName = identifier.Identifier.Text;
                if (!controlNames.Contains(controlName))
                {
                    continue;
                }

                var property = memberAccess.Name.Identifier.Text;
                if (boundControlProperties.ContainsKey((controlName, property)))
                {
                    continue;
                }

                results.Add((controlName, property));
            }
        }
        catch
        {
            // Best-effort: an unparseable body simply yields no findings, not a hard failure.
        }

        return results.Distinct().ToList();
    }

    private void BuildBoundControlPropertyLookupRecursive(
        ControlNode control, Dictionary<(string, string), string> lookup)
    {
        foreach (var binding in control.DataBindings)
        {
            if (string.IsNullOrEmpty(binding.DataMember))
            {
                continue;
            }

            lookup[(control.Name, binding.PropertyName)] = ToPascalCase(binding.DataMember);
        }

        foreach (var child in control.Children)
        {
            BuildBoundControlPropertyLookupRecursive(child, lookup);
        }
    }

    /// <summary>
    /// Rewrites "controlName.ControlProperty" references in a migrated body (already spliced in
    /// as live code) into the corresponding [ObservableProperty]'s name, for every pair
    /// BuildBoundControlPropertyLookup found. A per-pair word-boundary regex, mirroring
    /// CodeBehindGenerator's identical treatment of its own migratedNames rewrite - same accepted
    /// limitation (not Roslyn-token-aware, so it can also match inside a string/comment literal
    /// or a shadowed local sharing the same name) rather than a different approach for the same
    /// class of problem. References to unbound controls are left untouched here - flagged
    /// instead as a manual step by ConversionOrchestrator using the same lookup.
    /// </summary>
    private static string RewriteBoundControlReferences(
        string body, IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties)
    {
        foreach (var ((controlName, controlProperty), observableProperty) in boundControlProperties)
        {
            body = Regex.Replace(
                body,
                $@"\b{Regex.Escape(controlName)}\.{Regex.Escape(controlProperty)}\b",
                observableProperty);
        }

        return body;
    }

    private List<PropertyInfo> ExtractBoundProperties(
        ControlNode root, IReadOnlyDictionary<(string ControlName, string Property), string>? inferredBindings = null)
    {
        var properties = new List<PropertyInfo>();
        ExtractPropertiesRecursive(root, properties);

        // UsageInferredBindingDetector's own entries (a control property touched from 2+
        // migrated members with no DataBindings.Add(...) declaring it) - same PropertyInfo
        // shape as a DataBindings-sourced one, just named from the control's own field name
        // (DerivePropertyName) instead of a DataMember.
        if (inferredBindings != null)
        {
            foreach (var ((_, controlProperty), observableName) in inferredBindings)
            {
                properties.Add(new PropertyInfo
                {
                    Name = observableName,
                    FieldName = ToCamelCase(observableName),
                    Type = InferPropertyType(controlProperty),
                    DefaultValue = GetDefaultValue(InferPropertyType(controlProperty))
                });
            }
        }

        return properties.DistinctBy(p => p.Name).ToList();
    }

    private void ExtractPropertiesRecursive(ControlNode control, List<PropertyInfo> properties)
    {
        // Extract from data bindings
        foreach (var binding in control.DataBindings)
        {
            var propName = binding.DataMember;
            if (string.IsNullOrEmpty(propName)) continue;

            properties.Add(new PropertyInfo
            {
                Name = propName,
                FieldName = ToCamelCase(propName),
                Type = InferPropertyType(binding.PropertyName),
                DefaultValue = GetDefaultValue(InferPropertyType(binding.PropertyName))
            });
        }

        // Recursively process children
        foreach (var child in control.Children)
        {
            ExtractPropertiesRecursive(child, properties);
        }
    }

    private List<CommandInfo> ExtractCommands(ControlNode root, PluginMappingOverrides overrides)
    {
        var commands = new List<CommandInfo>();
        ExtractCommandsRecursive(root, commands, overrides);
        return commands;
    }

    private void ExtractCommandsRecursive(ControlNode control, List<CommandInfo> commands, PluginMappingOverrides overrides)
    {
        foreach (var eventHandler in control.EventHandlers)
        {
            if (eventHandler.Value == WinFormsParser.InlineLambdaHandlerMarker)
            {
                // No stable method name to generate a [RelayCommand] for; surfaced as a manual
                // step by ConversionOrchestrator.CollectManualSteps instead.
                continue;
            }

            if (overrides.EventMappings.TryGetValue((control, eventHandler.Key), out var pluginMapping))
            {
                if (pluginMapping.ConvertToCommand)
                {
                    commands.Add(new CommandInfo
                    {
                        MethodName = CommandNaming.MethodName(eventHandler.Value),
                        OriginalEvent = eventHandler.Key,
                        OriginalHandlerMethodName = eventHandler.Value,
                        CommandName = pluginMapping.CommandName ?? $"{eventHandler.Key}Command",
                        HasParameter = RequiresParameter(eventHandler.Key),
                        ParameterType = GetParameterType(eventHandler.Key)
                    });
                }

                continue;
            }

            if (EventMappingRegistry.ShouldConvertToCommand(eventHandler.Key))
            {
                var mapping = EventMappingRegistry.GetMapping(eventHandler.Key);
                var commandName = mapping?.CommandName ?? $"{eventHandler.Key}Command";

                commands.Add(new CommandInfo
                {
                    MethodName = CommandNaming.MethodName(eventHandler.Value),
                    OriginalEvent = eventHandler.Key,
                    OriginalHandlerMethodName = eventHandler.Value,
                    CommandName = commandName,
                    HasParameter = RequiresParameter(eventHandler.Key),
                    ParameterType = GetParameterType(eventHandler.Key)
                });
            }
        }

        foreach (var child in control.Children)
        {
            ExtractCommandsRecursive(child, commands, overrides);
        }
    }

    /// <summary>
    /// Finds TextChanged/ValueChanged/CheckedChanged handlers whose control has a matching
    /// DataBindings entry (see EventMappingRegistry.FindBoundPropertyName) - these are
    /// automatable as a CommunityToolkit property-changed hook instead of staying a manual
    /// step, since the property is already an [ObservableProperty] in the .g.cs partial.
    /// </summary>
    private List<PropertyChangedHookInfo> ExtractPropertyChangedHooks(ControlNode root)
    {
        var hooks = new List<PropertyChangedHookInfo>();
        ExtractPropertyChangedHooksRecursive(root, hooks);
        return hooks.DistinctBy(h => h.BoundPropertyName).ToList();
    }

    private void ExtractPropertyChangedHooksRecursive(ControlNode control, List<PropertyChangedHookInfo> hooks)
    {
        foreach (var (eventName, handlerName) in control.EventHandlers)
        {
            if (handlerName == WinFormsParser.InlineLambdaHandlerMarker)
            {
                continue;
            }

            var boundPropertyName = EventMappingRegistry.FindBoundPropertyName(control, eventName);
            if (boundPropertyName == null)
            {
                continue;
            }

            var winFormsPropertyName = eventName switch
            {
                "TextChanged" => "Text",
                "ValueChanged" => "Value",
                "CheckedChanged" => "Checked",
                _ => "Text"
            };

            hooks.Add(new PropertyChangedHookInfo
            {
                OriginalHandlerMethodName = handlerName,
                BoundPropertyName = boundPropertyName,
                ParameterType = InferPropertyType(winFormsPropertyName)
            });
        }

        foreach (var child in control.Children)
        {
            ExtractPropertyChangedHooksRecursive(child, hooks);
        }
    }

    private string InferPropertyType(string propertyName)
    {
        return propertyName switch
        {
            "Text" or "Name" or "Title" => "string",
            "Checked" or "Visible" or "Enabled" => "bool",
            "Value" or "SelectedIndex" => "int",
            "Items" or "DataSource" => "ObservableCollection<object>",
            _ => "string"
        };
    }

    private string GetDefaultValue(string type)
    {
        return type switch
        {
            "string" => "string.Empty",
            "bool" => "false",
            "int" => "0",
            "ObservableCollection<object>" => "new()",
            _ => "default!"
        };
    }

    private bool RequiresParameter(string eventName)
    {
        return eventName is "CellClick" or "NodeClick" or "SelectedIndexChanged";
    }

    private string GetParameterType(string eventName)
    {
        return "object";
    }

    private string ToCamelCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToLowerInvariant(text[0]) + text.Substring(1);
    }

    private static string ToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    /// <summary>
    /// Namespaces every "ImplicitUsings"-enabled non-web .NET SDK project gets for free
    /// (System, System.Collections.Generic, ...). The generated .csproj does not enable that
    /// SDK feature, and migrated code commonly relies on it (e.g. List&lt;T&gt;, Task,
    /// Environment.NewLine) without any explicit "using" in the original WinForms file to copy
    /// forward - unlike a genuinely custom namespace, there's nothing to extract, so this list
    /// is emitted unconditionally instead.
    /// </summary>
    private static readonly string[] BaselineImplicitUsings =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Threading",
        "System.Threading.Tasks",
    ];

    /// <summary>
    /// Namespace prefixes that are pointless to carry over from the code-behind file's own
    /// "using" directives: WinForms/GDI+ types they exist for (System.Windows.Forms,
    /// System.Drawing.*) have no Avalonia equivalent and no reference in the generated project,
    /// so keeping the "using" just trades one CS0246 (unresolved type) for another (unresolved
    /// namespace) - the underlying manual-porting need is already surfaced separately via the
    /// "Preserved Event Handlers"/"Skipped Override Methods" manual steps.
    /// </summary>
    private static readonly string[] UnusableUsingPrefixes = ["System.Windows.Forms", "System.Drawing"];

    /// <summary>
    /// Combines BaselineImplicitUsings with whatever the sibling code-behind file itself
    /// imported (typically the app's own domain-model namespaces, e.g.
    /// "WarehouseApp.Data.Models") - filtered through UnusableUsingPrefixes and deduplicated,
    /// baseline first, then in the order the original file declared them.
    /// </summary>
    private static IEnumerable<string> BuildUsingDirectives(CodeBehindMembers codeBehindMembers)
    {
        var seen = new HashSet<string>(BaselineImplicitUsings);
        foreach (var ns in BaselineImplicitUsings)
        {
            yield return ns;
        }

        foreach (var ns in codeBehindMembers.UsingDirectives)
        {
            if (UnusableUsingPrefixes.Any(prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal)))
            {
                continue;
            }

            if (seen.Add(ns))
            {
                yield return ns;
            }
        }
    }

    /// <summary>
    /// Migrated fields/helper methods are "private" or "protected" (explicit or implicit) in
    /// the original WinForms code-behind class (a migrated business-logic override - see
    /// CodeBehindMemberExtractor.KnownWinFormsOverrideMethodNames - is commonly "protected
    /// override"). Re-emitted verbatim here, CodeBehindGenerator's "ViewModel.someField"
    /// accessor rewrite (a different class, in a different file) would not compile - neither
    /// "private" nor "protected" members are visible cross-class through a typed property
    /// (protected only reaches derived classes, which this isn't). Upgrading to "internal"
    /// (same assembly, so still safely reachable) fixes that. Only ever applied to migrated
    /// fields/helper methods - freshly generated [RelayCommand] methods stay "private" exactly
    /// as before, since code-behind never calls them directly (AXAML binds through the
    /// source-generator's ICommand property instead).
    /// </summary>
    private static string EnsureInternalAccessibility(string declarationText)
    {
        if (Regex.IsMatch(declarationText, @"^\s*(private|protected)\b"))
        {
            return Regex.Replace(declarationText, @"^\s*(private|protected)\b", "internal");
        }

        if (Regex.IsMatch(declarationText, @"^\s*(public|internal)\b"))
        {
            return declarationText;
        }

        return "internal " + declarationText;
    }

    /// <summary>
    /// The generator writes source line-by-line via StringBuilder.AppendLine, each new entry
    /// starting flush at column 0; multi-line text (a field declaration spanning lines, a
    /// method body) needs every line after the first indented to match, or the emitted file
    /// looks malformed (still compiles - C# doesn't care about whitespace - but is unreadable).
    /// </summary>
    private static string IndentContinuationLines(string text) => text.Replace("\n", "\n    ");

    private record PropertyInfo
    {
        public required string Name { get; init; }
        public required string FieldName { get; init; }
        public required string Type { get; init; }
        public required string DefaultValue { get; init; }
    }

    private record CommandInfo
    {
        public required string MethodName { get; init; }
        public required string OriginalEvent { get; init; }
        public required string OriginalHandlerMethodName { get; init; }
        public required string CommandName { get; init; }
        public bool HasParameter { get; init; }
        public string ParameterType { get; init; } = "object";
    }

    private record PropertyChangedHookInfo
    {
        public required string OriginalHandlerMethodName { get; init; }
        public required string BoundPropertyName { get; init; }
        public required string ParameterType { get; init; }
    }
}
