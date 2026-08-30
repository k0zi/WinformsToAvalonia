using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>
/// The thin <c>Window</c> that hosts a <c>UserControl</c>-rooted main View on the desktop head.
/// </summary>
/// <remarks>
/// <para>
/// Only ever produced under <c>--with-web</c>, and only for the one Form that becomes the startup
/// window. Avalonia's browser backend offers a single-view lifetime and no windowing platform at
/// all, so the main View has to be a <c>UserControl</c> to be showable there - which leaves the
/// desktop head needing something to put in <c>desktop.MainWindow</c>. That is all this is: the
/// window chrome (title and size) the Form used to carry, plus the forwarding of the events only
/// a Window raises.
/// </para>
/// <para>
/// It carries no logic of its own on purpose. Every handler body still lives in the View, which
/// is where the control fields it names are; the wrapper only re-raises into it.
/// </para>
/// </remarks>
public sealed class WindowWrapperEmitter
{
    /// <summary>The x:Name of the hosted View, which the forwarding methods call through.</summary>
    private const string HostedViewName = "View";

    public string EmitAxaml(
        FormModel formModel,
        string rootNamespace,
        string relativeFolder,
        string viewClassName,
        string windowClassName,
        IReadOnlyList<(string AttributeName, string HandlerMethodName)> deferredWindowEvents)
    {
        var viewNamespace = NamingConventions.NamespaceOf($"{rootNamespace}.Views", relativeFolder);

        var builder = new AxamlDocumentBuilder();
        builder.OpenElement("Window");
        builder.Attribute("xmlns", "https://github.com/avaloniaui");
        builder.Attribute("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");
        builder.Attribute("xmlns:views", $"using:{viewNamespace}");
        builder.Attribute("x:Class", $"{viewNamespace}.{windowClassName}");

        // The Form's own chrome, read from exactly the same properties the Window-rooted View
        // would have read - so the two layouts agree about how big the window is.
        if (AxamlEmitter.TryGetSize(formModel.FormProperties, "ClientSize", out var width, out var height))
        {
            builder.Attribute("Width", AxamlEmitter.FormatInt(width));
            builder.Attribute("Height", AxamlEmitter.FormatInt(height));
        }

        builder.Attribute("Title", AxamlEmitter.GetFormTitle(formModel, viewClassName));

        // One attribute per distinct event: XAML has nowhere to put a second, so the generated
        // forwarder below calls every handler that subscribed to it.
        foreach (var attributeName in DistinctEventNames(deferredWindowEvents))
        {
            builder.Attribute(attributeName, ForwarderName(attributeName));
        }

        builder.OpenElement($"views:{viewClassName}");
        builder.Attribute("x:Name", HostedViewName);
        builder.CloseElement();

        builder.CloseElement();
        return builder.ToString();
    }

    public string EmitCodeBehind(
        string rootNamespace,
        string relativeFolder,
        string viewClassName,
        string windowClassName,
        IReadOnlyList<(string AttributeName, string HandlerMethodName)> deferredWindowEvents,
        IReadOnlyDictionary<string, string> eventArgsTypeNames)
    {
        var viewNamespace = NamingConventions.NamespaceOf($"{rootNamespace}.Views", relativeFolder);

        var sb = new System.Text.StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("using Avalonia;");
        Line("using Avalonia.Controls;");
        Line("using Avalonia.Interactivity;");
        Line("using Avalonia.Markup.Xaml;");
        Line();
        Line($"namespace {viewNamespace};");
        Line();
        Line("/// <summary>");
        Line($"/// Desktop window chrome for <see cref=\"{viewClassName}\"/>, which is a UserControl so the");
        Line("/// browser head can show it under a single-view lifetime. Generated - it holds no logic.");
        Line("/// </summary>");
        Line($"public partial class {windowClassName} : Window");
        Line("{");
        Line($"    public {windowClassName}()");
        Line("    {");
        Line("        InitializeComponent();");
        Line("    }");

        foreach (var attributeName in DistinctEventNames(deferredWindowEvents))
        {
            var argsTypeName = eventArgsTypeNames.TryGetValue(attributeName, out var declared)
                ? declared
                : "EventArgs";

            Line();
            Line($"    private void {ForwarderName(attributeName)}(object? sender, {argsTypeName} e)");
            Line("    {");
            foreach (var (_, handlerMethodName) in deferredWindowEvents.Where(e =>
                         string.Equals(e.AttributeName, attributeName, StringComparison.Ordinal)))
            {
                // Only reachable once the XAML has run, which is the only time the event can fire.
                Line($"        {HostedViewName}.{handlerMethodName}(sender, e);");
            }

            Line("    }");
        }

        Line("}");
        return sb.ToString();
    }

    private static IEnumerable<string> DistinctEventNames(
        IReadOnlyList<(string AttributeName, string HandlerMethodName)> events) =>
        events.Select(e => e.AttributeName).Distinct(StringComparer.Ordinal);

    private static string ForwarderName(string attributeName) => $"OnWindow{attributeName}";
}
