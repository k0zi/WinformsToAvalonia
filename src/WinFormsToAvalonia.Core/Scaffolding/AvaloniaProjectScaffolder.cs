using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Scaffolding;

/// <summary>
/// Produces the fixed Avalonia MVVM project skeleton (App/Program/ViewLocator/csproj) that
/// every conversion output is built on top of, then merges in either a placeholder
/// MainWindow view+viewmodel (<see cref="BuildEmptySkeleton"/> - used when zero forms were
/// discovered/converted) or the real converted Views/ViewModels (<see cref="BuildProject"/>).
/// </summary>
public sealed class AvaloniaProjectScaffolder
{
    /// <summary>
    /// The Avalonia every generated project is written against. Public because it is a contract
    /// with more than the csproj writer: the mapping tables describe *this* Avalonia's API, and
    /// WinFormsToAvalonia.Mapping.Tests checks them against exactly this version's assemblies.
    /// </summary>
    public const string AvaloniaVersion = "12.1.1";
    private const string CommunityToolkitMvvmVersion = "8.4.2";

    /// <summary>The allowlist of extra packages a generated csproj may carry, and at which version.</summary>
    /// <summary>
    /// The theme each extra package ships for its own controls, which App.axaml has to include.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A control that ships outside core Avalonia brings its own <c>ControlTheme</c> in a
    /// resource dictionary the application has to ask for. Referencing the package is not enough:
    /// without the include the control finds no theme, gets no template, and renders as
    /// <em>nothing</em> - the converted sample's two DataGrids were blank rectangles, and its
    /// ColorDialog opened an empty window, while the project compiled and started cleanly.
    /// </para>
    /// <para>
    /// The Simple variants, because <see cref="BuildAppAxaml"/> uses <c>SimpleTheme</c>. Both
    /// halves have to move together; the Mapping tests check that every entry names a resource
    /// its package really contains.
    /// </para>
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, string> PackageStyleIncludes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Avalonia.Controls.DataGrid"] = "avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml",
        ["Avalonia.Controls.ColorPicker"] = "avares://Avalonia.Controls.ColorPicker/Themes/Simple/Simple.xaml",
    };

    public static readonly IReadOnlyDictionary<string, string> ExtraPackageVersions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Avalonia.Controls.DataGrid"] = "12.1.2",

        // Avalonia's real ColorView, which ColorDialogFallback wraps.
        ["Avalonia.Controls.ColorPicker"] = "12.1.1",

        // Non-visual components that survive the conversion unchanged but do not ship in-box.
        // Named here *and* in ComponentFieldCatalog: this table is what the csproj writer emits,
        // so a package missing from it is silently dropped and the generated project fails.
        ["System.Diagnostics.EventLog"] = "10.0.0",
        ["System.Diagnostics.PerformanceCounter"] = "10.0.0",
        ["System.IO.Ports"] = "10.0.0",
        ["System.ServiceProcess.ServiceController"] = "10.0.0",
        ["System.Windows.Extensions"] = "10.0.0",
    };

    public VirtualFileSystem BuildEmptySkeleton(string projectName, IReadOnlyList<NotifyIconInfo>? notifyIcons = null)
    {
        var vfs = new VirtualFileSystem();

        vfs.AddText($"{projectName}.csproj", BuildCsproj(projectName, new HashSet<string>(), []));
        vfs.AddText("app.manifest", AppManifest);
        vfs.AddText("Program.cs", BuildProgram(projectName));
        vfs.AddText("App.axaml", BuildAppAxaml(projectName, notifyIcons ?? []));
        vfs.AddText("App.axaml.cs", BuildAppAxamlCs(projectName, "MainWindowView", ""));
        vfs.AddText("ViewLocator.cs", BuildViewLocator(projectName));
        vfs.AddText("ViewModels/ViewModelBase.cs", BuildViewModelBase(projectName));
        vfs.AddText("ViewModels/MainWindowViewModel.cs", BuildMainWindowViewModel(projectName));
        vfs.AddText("Views/MainWindowView.axaml", BuildMainWindowViewAxaml(projectName));
        vfs.AddText("Views/MainWindowView.axaml.cs", BuildMainWindowViewAxamlCs(projectName));

        return vfs;
    }

    /// <summary>
    /// Builds the real generated project from converted forms. The first Form-kind entry in
    /// <paramref name="forms"/> is wired up as the application's MainWindow - a UserControl
    /// entry never can be, since it is emitted as an Avalonia UserControl, not a Window.
    /// </summary>
    /// <remarks>
    /// A form converted from a subfolder gets its own correctly-nested namespace (see
    /// ViewCodeBehindEmitter/ViewModelEmitter/AxamlEmitter, which all derive it via
    /// <see cref="NamingConventions.NamespaceOf"/>). When the MainWindow form itself
    /// is nested, <see cref="BuildAppAxamlCs"/> fully-qualifies its View/ViewModel
    /// construction with that same nested namespace instead of relying on the flat
    /// `using {Project}.Views;` / `using {Project}.ViewModels;` directives.
    /// </remarks>
    /// <param name="projectReferences">
    /// Other generated projects this one must reference, relative to its own folder. Only a
    /// solution-wide conversion produces any: a Form here hosting a UserControl from there.
    /// </param>
    public VirtualFileSystem BuildProject(
        string projectName,
        IReadOnlyList<ConvertedFormOutput> forms,
        IReadOnlySet<string>? extraNuGetPackages = null,
        IReadOnlyList<NotifyIconInfo>? notifyIcons = null,
        IReadOnlyList<string>? projectReferences = null)
    {
        var packages = extraNuGetPackages ?? (IReadOnlySet<string>)new HashSet<string>();
        var references = projectReferences ?? [];

        if (forms.Count == 0)
        {
            var skeleton = BuildEmptySkeleton(projectName, notifyIcons);
            if (references.Count > 0)
            {
                skeleton.AddText($"{projectName}.csproj", BuildCsproj(projectName, packages, references));
            }

            return skeleton;
        }

        // Only a Form becomes an Avalonia Window, so only a Form can be the startup window. A
        // project of nothing but UserControls still needs one, and gets the placeholder
        // MainWindowView the empty skeleton provides - its converted UserControl Views are
        // then merged in below, exactly as they would be alongside a real Form.
        var mainForm = forms.FirstOrDefault(f => f.Kind == WinFormsArtifactKind.Form);
        var vfs = new VirtualFileSystem();

        if (mainForm is null)
        {
            vfs = BuildEmptySkeleton(projectName, notifyIcons);
            vfs.AddText($"{projectName}.csproj", BuildCsproj(projectName, packages, references));
        }
        else
        {
            vfs.AddText($"{projectName}.csproj", BuildCsproj(projectName, packages, references));
            vfs.AddText("app.manifest", AppManifest);
            vfs.AddText("Program.cs", BuildProgram(projectName));
            vfs.AddText("App.axaml", BuildAppAxaml(projectName, notifyIcons ?? [], packages));
            vfs.AddText("App.axaml.cs", BuildAppAxamlCs(
                projectName, mainForm.ViewClassName, mainForm.RelativeFolder, notifyIcons));
            vfs.AddText("ViewLocator.cs", BuildViewLocator(projectName));
            vfs.AddText("ViewModels/ViewModelBase.cs", BuildViewModelBase(projectName));
        }

        vfs.AddText("Controls/Generated/LayoutHint.cs", BuildLayoutHint(projectName));
        vfs.AddText("Generated/MigrationTodo.cs", BuildMigrationTodo(projectName));

        foreach (var form in forms)
        {
            var viewsFolder = CombineFolder("Views", form.RelativeFolder);
            var viewModelsFolder = CombineFolder("ViewModels", form.RelativeFolder);

            vfs.AddText($"{viewsFolder}/{form.ViewClassName}.axaml", form.AxamlContent);
            vfs.AddText($"{viewsFolder}/{form.ViewClassName}.axaml.cs", form.ViewCodeBehindContent);
            vfs.AddText($"{viewModelsFolder}/{form.ViewModelClassName}.cs", form.ViewModelContent);
        }

        return vfs;
    }

    private static string CombineFolder(string root, string relativeFolder) =>
        string.IsNullOrEmpty(relativeFolder) ? root : $"{root}/{relativeFolder}";

    /// <param name="projectReferences">
    /// Other generated projects this one needs, as paths relative to its own folder. Only ever
    /// non-empty when a solution is being converted and one project's Form hosts another's
    /// UserControl.
    /// </param>
    private static string BuildCsproj(
        string projectName,
        IReadOnlySet<string> extraNuGetPackages,
        IReadOnlyList<string> projectReferences)
    {
        var extraPackageLines = string.Concat(extraNuGetPackages
            .Where(ExtraPackageVersions.ContainsKey)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => $"\n    <PackageReference Include=\"{p}\" Version=\"{ExtraPackageVersions[p]}\" />"));

        var projectReferenceGroup = projectReferences.Count == 0
            ? ""
            : "\n\n  <ItemGroup>"
                + string.Concat(projectReferences
                    .OrderBy(p => p, StringComparer.Ordinal)
                    .Select(p => $"\n    <ProjectReference Include=\"{p.Replace('/', '\\')}\" />"))
                + "\n  </ItemGroup>";

        return $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <ApplicationManifest>app.manifest</ApplicationManifest>
                <RootNamespace>{projectName}</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <Folder Include="Models\" />
                <Folder Include="Controls\" />
                <AvaloniaResource Include="Assets\**" />
              </ItemGroup>

              <ItemGroup>
                <PackageReference Include="Avalonia" Version="{AvaloniaVersion}" />
                <PackageReference Include="Avalonia.Desktop" Version="{AvaloniaVersion}" />
                <PackageReference Include="Avalonia.Themes.Simple" Version="{AvaloniaVersion}" />
                <PackageReference Include="Avalonia.Fonts.Inter" Version="{AvaloniaVersion}" />
                <PackageReference Include="CommunityToolkit.Mvvm" Version="{CommunityToolkitMvvmVersion}" />{extraPackageLines}
              </ItemGroup>{projectReferenceGroup}
            </Project>
            """;
    }

    private const string AppManifest = """
        <?xml version="1.0" encoding="utf-8"?>
        <assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
          <!-- This manifest is used on Windows only. Don't remove it: it prevents issues with
               window transparency and embedded controls. -->
          <assemblyIdentity version="1.0.0.0" name="Winforms2Avalonia.GeneratedApp.Desktop"/>
          <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
            <application>
              <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
            </application>
          </compatibility>
        </assembly>
        """;

    private static string BuildProgram(string projectName) => $$"""
        using Avalonia;

        namespace {{projectName}};

        internal sealed class Program
        {
            // Initialization code. Don't use any Avalonia, third-party APIs or any
            // SynchronizationContext-reliant code before AppMain is called: things aren't
            // initialized yet and stuff might break.
            [STAThread]
            public static void Main(string[] args) => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            public static AppBuilder BuildAvaloniaApp()
                => AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .LogToTrace();
        }
        """;

    /// <summary>
    /// The theme variant is pinned to <c>Light</c> rather than following the OS.
    /// </summary>
    /// <remarks>
    /// A WinForms design is a light-mode design, and AxamlEmitter now carries its literal
    /// BackColor/ForeColor values through to the AXAML. Those two only agree under a light
    /// shell: a control that set only its ForeColor (black text, background left to the
    /// framework) would render black-on-dark if the host happened to be in dark mode - a
    /// regression the conversion itself introduced. Pinning is also the honest signal that a
    /// converted app has not been themed yet; delete this attribute (or set "Default") once
    /// the generated views have been reworked to use theme resources instead of fixed colors.
    /// </remarks>
    private static string BuildAppAxaml(
        string projectName, IReadOnlyList<NotifyIconInfo> notifyIcons, IReadOnlyCollection<string>? packages = null)
    {
        var trayIconsSection = notifyIcons.Count == 0 ? "" : BuildTrayIconsSection(notifyIcons);

        var styleIncludes = string.Concat((packages ?? [])
            .Where(PackageStyleIncludes.ContainsKey)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => $"\n        <StyleInclude Source=\"{PackageStyleIncludes[p]}\" />"));

        return $"""
            <Application xmlns="https://github.com/avaloniaui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         x:Class="{projectName}.App"
                         xmlns:local="using:{projectName}"
                         RequestedThemeVariant="Light">
            {trayIconsSection}
                <Application.DataTemplates>
                    <local:ViewLocator />
                </Application.DataTemplates>

                <Application.Styles>
                    <SimpleTheme />{styleIncludes}
                </Application.Styles>
            </Application>
            """;
    }

    /// <summary>
    /// NotifyIcon components are app-level (App.axaml's TrayIcon.Icons), not per-View - see
    /// ConversionPipeline.Run's aggregation across all forms. Empty when there are none, so
    /// BuildAppAxaml's output stays byte-identical to before this feature existed.
    /// </summary>
    /// <remarks>
    /// Only icons whose file was actually resolved and copied into Assets/ are emitted as live
    /// XAML. Avalonia resolves TrayIcon.Icon at run time, so a reference to an asset the
    /// conversion never produced is a FileNotFoundException out of App.Initialize() - the
    /// generated app would build and then die before showing a window. Those are emitted as a
    /// commented-out block with a TODO instead, which is also why an all-unresolved set still
    /// produces no live TrayIcon.Icons element at all.
    /// </remarks>
    private static string BuildTrayIconsSection(IReadOnlyList<NotifyIconInfo> notifyIcons)
    {
        var resolved = notifyIcons.Where(i => i.IconAssetPath is not null).ToList();
        var unresolved = notifyIcons.Where(i => i.IconAssetPath is null).ToList();
        var section = "";

        if (resolved.Count > 0)
        {
            var trayIconLines = string.Concat(resolved.Select(icon =>
                $"            <TrayIcon Icon=\"/{icon.IconAssetPath}\"{ToolTipAttribute(icon)} />\n"));
            section += $"    <TrayIcon.Icons>\n        <TrayIcons>\n{trayIconLines}        </TrayIcons>\n    </TrayIcon.Icons>\n";
        }

        if (unresolved.Count > 0)
        {
            var fieldNames = string.Join(", ", unresolved.Select(i => $"'{i.FieldName}'"));
            var trayIconLines = string.Concat(unresolved.Select(icon =>
                $"                 <TrayIcon Icon=\"/Assets/{icon.FieldName}.ico\"{ToolTipAttribute(icon)} />\n"));
            section +=
                $"    <!-- TODO(Winforms2Avalonia): NotifyIcon {fieldNames} had no resolvable icon file in Designer.cs\n" +
                "         (it is usually a resx resource). Copy the real icon into Assets/, then uncomment:\n" +
                "         <TrayIcon.Icons>\n" +
                "             <TrayIcons>\n" +
                trayIconLines +
                "             </TrayIcons>\n" +
                "         </TrayIcon.Icons> -->\n";
        }

        return section;
    }

    /// <summary>
    /// One accessor per emitted tray icon, so a converted handler can name the NotifyIcon it came
    /// from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A NotifyIcon is app-level in Avalonia - it lives in App.axaml's <c>TrayIcon.Icons</c>, not
    /// in any View - so a View has no field for it and <c>notifyIcon1.Visible = false;</c> had
    /// nowhere to go. These give it one, in the order App.axaml declares them, which is the order
    /// this same list produced.
    /// </para>
    /// <para>
    /// Only the icons whose file resolved: an unresolved one is emitted commented out (see
    /// <see cref="BuildTrayIconsSection"/>), so an accessor for it would index past the end of a
    /// collection that was never populated.
    /// </para>
    /// </remarks>
    private static string BuildTrayIconAccessors(IReadOnlyList<NotifyIconInfo> notifyIcons)
    {
        var resolved = notifyIcons.Where(i => i.IconAssetPath is not null).ToList();
        if (resolved.Count == 0)
        {
            return "";
        }

        return string.Concat(resolved.Select((icon, index) => $"""

                /// <summary>The tray icon converted from the WinForms NotifyIcon '{icon.FieldName}'.</summary>
                /// <remarks>
                /// Populated by App.axaml, which Initialize() loads before any View is constructed -
                /// so this is never null by the time a handler can run.
                /// </remarks>
                public static TrayIcon {NamingConventions.Capitalize(icon.FieldName)} =>
                    TrayIcon.GetIcons(Current!)![{index}];

            """));
    }

    private static string ToolTipAttribute(NotifyIconInfo icon) =>
        icon.TooltipText is null ? "" : $" ToolTipText=\"{icon.TooltipText}\"";

    private static string BuildAppAxamlCs(
        string projectName,
        string mainViewClassName,
        string relativeFolder,
        IReadOnlyList<NotifyIconInfo>? notifyIcons = null)
    {
        var mainViewRef = string.IsNullOrEmpty(relativeFolder)
            ? mainViewClassName
            : $"{NamingConventions.NamespaceOf($"{projectName}.Views", relativeFolder)}.{mainViewClassName}";
        return $$"""
            using Avalonia;
            using Avalonia.Controls;
            using Avalonia.Controls.ApplicationLifetimes;
            using Avalonia.Markup.Xaml;
            using {{projectName}}.ViewModels;
            using {{projectName}}.Views;

            namespace {{projectName}};

            public partial class App : Application
            {
                public override void Initialize()
                {
                    AvaloniaXamlLoader.Load(this);
                }
            {{BuildTrayIconAccessors(notifyIcons ?? [])}}

                public override void OnFrameworkInitializationCompleted()
                {
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        // Every generated View sets its own DataContext in its constructor, so the
                        // {Binding}s work no matter how the window is opened - not only for the one
                        // window App happens to construct here.
                        desktop.MainWindow = new {{mainViewRef}}();
                    }

                    base.OnFrameworkInitializationCompleted();
                }
            }
            """;
    }

    private static string BuildViewLocator(string projectName) => $$"""
        using System;
        using System.Diagnostics.CodeAnalysis;
        using Avalonia.Controls;
        using Avalonia.Controls.Templates;
        using {{projectName}}.ViewModels;

        namespace {{projectName}};

        /// <summary>
        /// Given a view model, returns the corresponding view by naming convention
        /// (FooViewModel -> FooView) if possible.
        /// </summary>
        [RequiresUnreferencedCode(
            "Default implementation of ViewLocator involves reflection which may be trimmed away.",
            Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
        public sealed class ViewLocator : IDataTemplate
        {
            public Control? Build(object? param)
            {
                if (param is null)
                {
                    return null;
                }

                var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
                var type = Type.GetType(name);

                if (type != null)
                {
                    return (Control)Activator.CreateInstance(type)!;
                }

                return new TextBlock { Text = "Not Found: " + name };
            }

            public bool Match(object? data) => data is ViewModelBase;
        }
        """;

    private static string BuildViewModelBase(string projectName) => $$"""
        using CommunityToolkit.Mvvm.ComponentModel;

        namespace {{projectName}}.ViewModels;

        public abstract class ViewModelBase : ObservableObject
        {
        }
        """;

    private static string BuildMainWindowViewModel(string projectName) => $$"""
        namespace {{projectName}}.ViewModels;

        public sealed class MainWindowViewModel : ViewModelBase
        {
        }
        """;

    private static string BuildMainWindowViewAxaml(string projectName) => $"""
        <Window xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:vm="using:{projectName}.ViewModels"
                mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
                xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                x:Class="{projectName}.Views.MainWindowView"
                x:DataType="vm:MainWindowViewModel"
                Title="{projectName}">

            <Design.DataContext>
                <vm:MainWindowViewModel />
            </Design.DataContext>

            <TextBlock Text="Winforms2Avalonia: no WinForms project converted yet."
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
        </Window>
        """;

    private static string BuildMainWindowViewAxamlCs(string projectName) => $$"""
        using Avalonia.Controls;

        namespace {{projectName}}.Views;

        public partial class MainWindowView : Window
        {
            public MainWindowView()
            {
                InitializeComponent();
            }
        }
        """;

    /// <summary>
    /// The runtime marker every un-migrated generated handler/command calls. It exists because
    /// the alternative - a bare `throw new NotImplementedException(...)` in each stub - made the
    /// generated app unrunnable: Avalonia raises SelectionChanged/Loaded during XAML
    /// initialization, so a stub handler on a TabControl or a Window killed the process before
    /// the first window appeared. Reporting instead of throwing lets a developer launch the
    /// converted app and migrate it screen by screen, which is the whole point of the output.
    /// </summary>
    private static string BuildMigrationTodo(string projectName) => $$"""
        using System;
        using System.Collections.Generic;
        using System.Diagnostics;
        using System.Linq;

        namespace {{projectName}}.Generated;

        /// <summary>
        /// Marks a generated member whose original WinForms body has not been migrated yet. Every
        /// such member keeps that body as a comment and then calls <see cref="NotMigrated"/>.
        /// </summary>
        /// <remarks>
        /// Reporting rather than throwing is deliberate: these members are invoked by the
        /// framework, including during XAML initialization (a TabControl selects its first tab,
        /// a Window raises Loaded), so throwing would take the app down before it is visible.
        /// Set <see cref="ThrowOnUnmigratedCall"/> to true - e.g. in a test run - to get the
        /// strict behaviour back once you want un-migrated code to fail loudly.
        /// </remarks>
        public static class MigrationTodo
        {
            private static readonly HashSet<string> Reported = new(StringComparer.Ordinal);

            /// <summary>Throw a <see cref="NotImplementedException"/> instead of reporting.</summary>
            public static bool ThrowOnUnmigratedCall { get; set; }

            /// <summary>Every member reported so far, in first-call order - handy in a smoke test.</summary>
            public static IReadOnlyCollection<string> ReportedMembers
            {
                get
                {
                    lock (Reported)
                    {
                        return Reported.ToArray();
                    }
                }
            }

            /// <param name="member">The generated member that ran, e.g. nameof(button1_Click).</param>
            /// <param name="originalWinFormsMember">The WinForms method its body came from.</param>
            public static void NotMigrated(string member, string originalWinFormsMember)
            {
                var message =
                    $"TODO(Winforms2Avalonia): '{member}' is not migrated yet - the original WinForms body of " +
                    $"'{originalWinFormsMember}' is preserved inside it as a comment.";

                if (ThrowOnUnmigratedCall)
                {
                    throw new NotImplementedException(message);
                }

                bool isFirstCall;
                lock (Reported)
                {
                    isFirstCall = Reported.Add(member);
                }

                if (isFirstCall)
                {
                    // Both, on purpose: stderr is what you see running `dotnet run` from a
                    // terminal, Debug output is what you see attached to a debugger on Windows,
                    // where a WinExe has no console at all.
                    Console.Error.WriteLine(message);
                    Debug.WriteLine(message);
                }
            }
        }
        """;

    private static string BuildLayoutHint(string projectName) => $$"""
        using Avalonia;
        using Avalonia.Controls;

        namespace {{projectName}}.Controls.Generated;

        /// <summary>
        /// Pure metadata carriers preserving the original WinForms Anchor/Dock values for a
        /// control - not wired to any runtime layout behavior. See the XML comment above
        /// each control in the generated Views for the human-readable form of the same data.
        /// </summary>
        /// <remarks>Not a `static class`: AvaloniaProperty.RegisterAttached's owner-type
        /// argument can't be a static type, even though every member here is static.</remarks>
        public sealed class LayoutHint
        {
            private LayoutHint()
            {
            }

            public static readonly AttachedProperty<string?> AnchorProperty =
                AvaloniaProperty.RegisterAttached<LayoutHint, Control, string?>("Anchor");

            public static readonly AttachedProperty<string?> DockProperty =
                AvaloniaProperty.RegisterAttached<LayoutHint, Control, string?>("Dock");

            public static string? GetAnchor(Control control) => control.GetValue(AnchorProperty);

            public static void SetAnchor(Control control, string? value) => control.SetValue(AnchorProperty, value);

            public static string? GetDock(Control control) => control.GetValue(DockProperty);

            public static void SetDock(Control control, string? value) => control.SetValue(DockProperty, value);
        }
        """;
}
