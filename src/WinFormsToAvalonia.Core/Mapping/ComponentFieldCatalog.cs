namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="Namespace">The `using` the generated View needs to name the type.</param>
/// <param name="NuGetPackage">
/// Null when the type is in-box on .NET 10. Otherwise the package, which must *also* appear in
/// <c>AvaloniaProjectScaffolder.ExtraPackageVersions</c> - the csproj writer emits only what is
/// listed there, so a package named in one place and not the other is silently dropped and the
/// generated project fails to compile.
/// </param>
/// <param name="WindowsOnly">
/// True when the type carries <c>[SupportedOSPlatform("windows")]</c>. Verified against a real
/// build rather than assumed: <c>SerialPort</c> looks Windows-shaped and is not.
/// </param>
public sealed record ComponentFieldKind(
    string Namespace,
    string? NuGetPackage = null,
    bool WindowsOnly = false);

/// <param name="ArgsTypeName">The .NET EventArgs type, which is also what the handler is declared with.</param>
/// <param name="ArgsNamespace">Where that type lives, when it is not somewhere the View already imports.</param>
public sealed record ComponentEvent(string ArgsTypeName, string? ArgsNamespace = null);

/// <summary>
/// The non-visual WinForms components that are really plain .NET types, and therefore survive the
/// conversion *unchanged* - the generated View declares a real field of the same type.
/// </summary>
/// <remarks>
/// <para>
/// This is a different kind of table from <see cref="ControlMappingRegistry"/> and its siblings.
/// Those translate: a WinForms member becomes a differently-named Avalonia one, and the catalogs
/// exist to record exactly which translations are provable. Here nothing is translated at all -
/// <c>System.ComponentModel.BackgroundWorker</c> is the same class in a WinForms project and in
/// an Avalonia one. What the table records is *which* components that is true of, and what it
/// costs to say so (a package reference, a platform constraint).
/// </para>
/// <para>
/// That is also why there is no per-member whitelist. A member of an unchanged .NET object is
/// ordinary .NET, the same argument that lets a translated local's members through - so a handler
/// body may say anything about one of these fields, including a nested path like
/// <c>process1.StartInfo.FileName</c>. The one thing the conversion still decides is which
/// *designer* properties it can reproduce in the constructor, and that is decided by the value
/// being a literal, not by the property's name.
/// </para>
/// <para>
/// <c>Timer</c> is deliberately absent: it is the one component whose target type is *different*
/// (<c>DispatcherTimer</c>), with its own event wiring and start semantics, so it keeps its own
/// plan (<c>FormMigrationPlanner.PlanTimers</c>) rather than being forced into this shape.
/// </para>
/// </remarks>
public static class ComponentFieldCatalog
{
    private static readonly IReadOnlyDictionary<string, ComponentFieldKind> Kinds =
        new Dictionary<string, ComponentFieldKind>(StringComparer.Ordinal)
        {
            // In-box and cross-platform.
            ["BackgroundWorker"] = new("System.ComponentModel"),
            ["FileSystemWatcher"] = new("System.IO"),
            ["Process"] = new("System.Diagnostics"),

            // A package, but cross-platform: SerialPort really does work on Linux and macOS.
            ["SerialPort"] = new("System.IO.Ports", NuGetPackage: "System.IO.Ports"),

            // A package *and* Windows-only. The generated project still compiles everywhere -
            // see the pragma ViewCodeBehindEmitter writes - but these calls throw off Windows,
            // which is why the conversion reports each one by name.
            ["EventLog"] = new("System.Diagnostics", "System.Diagnostics.EventLog", WindowsOnly: true),
            ["PerformanceCounter"] = new("System.Diagnostics", "System.Diagnostics.PerformanceCounter", WindowsOnly: true),
            ["ServiceController"] = new("System.ServiceProcess", "System.ServiceProcess.ServiceController", WindowsOnly: true),
            ["SoundPlayer"] = new("System.Media", "System.Windows.Extensions", WindowsOnly: true),
        };

    /// <summary>
    /// The events these components raise, with the .NET args type each handler must be declared
    /// with. Only the ones a designer actually wires; an unlisted event falls through to the
    /// ordinary "no Avalonia equivalent" path and its handler is emitted unsubscribed, as before.
    /// </summary>
    private static readonly IReadOnlyDictionary<(string Type, string Event), ComponentEvent> Events =
        new Dictionary<(string, string), ComponentEvent>
        {
            [("BackgroundWorker", "DoWork")] = new("DoWorkEventArgs", "System.ComponentModel"),
            [("BackgroundWorker", "ProgressChanged")] = new("ProgressChangedEventArgs", "System.ComponentModel"),
            [("BackgroundWorker", "RunWorkerCompleted")] = new("RunWorkerCompletedEventArgs", "System.ComponentModel"),

            [("FileSystemWatcher", "Changed")] = new("FileSystemEventArgs", "System.IO"),
            [("FileSystemWatcher", "Created")] = new("FileSystemEventArgs", "System.IO"),
            [("FileSystemWatcher", "Deleted")] = new("FileSystemEventArgs", "System.IO"),
            [("FileSystemWatcher", "Renamed")] = new("RenamedEventArgs", "System.IO"),
            [("FileSystemWatcher", "Error")] = new("ErrorEventArgs", "System.IO"),

            [("Process", "Exited")] = new("EventArgs"),
            [("Process", "OutputDataReceived")] = new("DataReceivedEventArgs", "System.Diagnostics"),
            [("Process", "ErrorDataReceived")] = new("DataReceivedEventArgs", "System.Diagnostics"),

            [("SerialPort", "DataReceived")] = new("SerialDataReceivedEventArgs", "System.IO.Ports"),
            [("SerialPort", "ErrorReceived")] = new("SerialErrorReceivedEventArgs", "System.IO.Ports"),

            [("EventLog", "EntryWritten")] = new("EntryWrittenEventArgs", "System.Diagnostics"),
        };

    public static bool TryGet(string winFormsTypeName, out ComponentFieldKind kind) =>
        Kinds.TryGetValue(winFormsTypeName, out kind!);

    public static bool TryGetEvent(string winFormsTypeName, string eventName, out ComponentEvent componentEvent) =>
        Events.TryGetValue((winFormsTypeName, eventName), out componentEvent!);
}
