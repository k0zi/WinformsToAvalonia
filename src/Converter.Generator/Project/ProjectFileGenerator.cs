using System.Text;

namespace Converter.Generator.Project;

/// <summary>
/// Generates Avalonia project files (.csproj).
/// </summary>
public class ProjectFileGenerator
{
    /// <summary>
    /// Centralized NuGet package versions for generated Avalonia projects, so bumping a
    /// version is a one-line change instead of a find-replace across several literals.
    /// </summary>
    public static class PackageVersions
    {
        public const string Avalonia = "12.0.0";
        public const string CommunityToolkitMvvm = "8.3.2";
    }

    /// <summary>
    /// Generate an Avalonia Desktop project file. <paramref name="projectReferencePaths"/> -
    /// paths (relative to the output directory, MSBuild-style separators not required - both
    /// work) to sibling projects the source WinForms project itself referenced and that
    /// ProjectReferenceResolver determined are safe to reference as-is (a non-WinForms class
    /// library, typically a data/domain layer) - null/empty emits no extra ItemGroup, same
    /// output as before this parameter existed.
    /// </summary>
    public string GenerateAvaloniaProject(
        string projectName,
        string targetFramework = "net10.0",
        string avaloniaVersion = PackageVersions.Avalonia,
        string communityToolkitMvvmVersion = PackageVersions.CommunityToolkitMvvm,
        IReadOnlyList<string>? projectReferencePaths = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <OutputType>WinExe</OutputType>");
        sb.AppendLine($"    <TargetFramework>{targetFramework}</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>");
        sb.AppendLine("    <ApplicationManifest>app.manifest</ApplicationManifest>");
        sb.AppendLine("    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();

        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <AvaloniaResource Include=\"Assets\\**\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();

        if (projectReferencePaths is { Count: > 0 })
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var path in projectReferencePaths)
            {
                sb.AppendLine($"    <ProjectReference Include=\"{path}\" />");
            }
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
        }

        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine($"    <PackageReference Include=\"Avalonia\" Version=\"{avaloniaVersion}\" />");
        sb.AppendLine($"    <PackageReference Include=\"Avalonia.Desktop\" Version=\"{avaloniaVersion}\" />");
        sb.AppendLine($"    <PackageReference Include=\"Avalonia.Themes.Fluent\" Version=\"{avaloniaVersion}\" />");
        sb.AppendLine($"    <PackageReference Include=\"Avalonia.Fonts.Inter\" Version=\"{avaloniaVersion}\" />");
        // DataGridView is a common-enough WinForms control (ControlMappingRegistry maps it to
        // Avalonia.Controls.DataGrid) that the generated project must always carry this package -
        // unlike the core controls above, DataGrid ships as a separate NuGet package, not part of
        // the base Avalonia/Avalonia.Desktop set, so AXAML referencing <DataGrid> would otherwise
        // fail to compile for any converted form that used a DataGridView.
        sb.AppendLine($"    <PackageReference Include=\"Avalonia.Controls.DataGrid\" Version=\"{avaloniaVersion}\" />");
        sb.AppendLine($"    <PackageReference Include=\"CommunityToolkit.Mvvm\" Version=\"{communityToolkitMvvmVersion}\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();

        sb.AppendLine("</Project>");

        return sb.ToString();
    }

    /// <summary>
    /// Generate App.axaml file.
    /// </summary>
    public string GenerateAppAxaml(string namespaceName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<Application xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine($"             x:Class=\"{namespaceName}.App\"");
        sb.AppendLine("             RequestedThemeVariant=\"Default\">");
        sb.AppendLine();
        sb.AppendLine("    <Application.Styles>");
        sb.AppendLine("        <FluentTheme />");
        sb.AppendLine("    </Application.Styles>");
        sb.AppendLine();
        sb.AppendLine("    <Application.Resources>");
        sb.AppendLine("        <!-- Add global resources here -->");
        sb.AppendLine("    </Application.Resources>");
        sb.AppendLine("</Application>");

        return sb.ToString();
    }

    /// <summary>
    /// Generate App.axaml.cs file.
    /// </summary>
    public string GenerateAppCodeBehind(string namespaceName, string mainWindowName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Avalonia;");
        sb.AppendLine("using Avalonia.Controls.ApplicationLifetimes;");
        sb.AppendLine("using Avalonia.Markup.Xaml;");
        sb.AppendLine($"using {namespaceName}.Views;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("public partial class App : Application");
        sb.AppendLine("{");
        sb.AppendLine("    public override void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine("        AvaloniaXamlLoader.Load(this);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void OnFrameworkInitializationCompleted()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)");
        sb.AppendLine("        {");
        sb.AppendLine($"            desktop.MainWindow = new {mainWindowName}();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        base.OnFrameworkInitializationCompleted();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate Program.cs entry point.
    /// </summary>
    public string GenerateProgramFile(string namespaceName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Avalonia;");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("class Program");
        sb.AppendLine("{");
        sb.AppendLine("    [STAThread]");
        sb.AppendLine("    public static void Main(string[] args) => BuildAvaloniaApp()");
        sb.AppendLine("        .StartWithClassicDesktopLifetime(args);");
        sb.AppendLine();
        sb.AppendLine("    public static AppBuilder BuildAvaloniaApp()");
        sb.AppendLine("        => AppBuilder.Configure<App>()");
        sb.AppendLine("            .UsePlatformDetect()");
        sb.AppendLine("            .WithInterFont()");
        sb.AppendLine("            .LogToTrace();");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate app.manifest file for Windows.
    /// </summary>
    public string GenerateAppManifest()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<assembly manifestVersion=""1.0"" xmlns=""urn:schemas-microsoft-com:asm.v1"">
  <assemblyIdentity version=""1.0.0.0"" name=""MyApplication.app""/>

  <compatibility xmlns=""urn:schemas-microsoft-com:compatibility.v1"">
    <application>
      <!-- Windows 10 and Windows 11 -->
      <supportedOS Id=""{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}"" />
    </application>
  </compatibility>
</assembly>";
    }

    /// <summary>
    /// Generate Common/MessageBoxTypes.cs - the three WinForms-shaped enums
    /// (MessageBoxButtons/MessageBoxIcon/DialogResult) MessageBoxTranspiler's rewritten calls
    /// reference, and Dialogs.ShowAsync returns. Same member names as the WinForms originals so
    /// a migrated call site's enum arguments need no rewriting beyond namespace-qualifying them.
    /// Only generated when at least one form actually uses MessageBox.Show (see
    /// ConversionOrchestrator) - most projects don't, and this would otherwise clutter every
    /// generated project with unused types.
    /// </summary>
    public string GenerateMessageBoxTypes(string namespaceName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {namespaceName}.Common;");
        sb.AppendLine();
        sb.AppendLine("public enum MessageBoxButtons");
        sb.AppendLine("{");
        sb.AppendLine("    OK,");
        sb.AppendLine("    OKCancel,");
        sb.AppendLine("    YesNo,");
        sb.AppendLine("    YesNoCancel,");
        sb.AppendLine("    RetryCancel,");
        sb.AppendLine("    AbortRetryIgnore");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public enum MessageBoxIcon");
        sb.AppendLine("{");
        sb.AppendLine("    None,");
        sb.AppendLine("    Information,");
        sb.AppendLine("    Warning,");
        sb.AppendLine("    Error,");
        sb.AppendLine("    Question");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public enum DialogResult");
        sb.AppendLine("{");
        sb.AppendLine("    None,");
        sb.AppendLine("    OK,");
        sb.AppendLine("    Cancel,");
        sb.AppendLine("    Yes,");
        sb.AppendLine("    No,");
        sb.AppendLine("    Retry,");
        sb.AppendLine("    Abort,");
        sb.AppendLine("    Ignore");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate Common/Dialogs.cs - the static helper MessageBoxTranspiler's rewritten
    /// "MessageBox.Show(...)" calls target. Deliberately takes no owner-window parameter (unlike
    /// WinForms' MessageBox.Show(owner, ...)): this is called from ViewModel code as often as
    /// code-behind, and a ViewModel must not hold a View/Window reference - it resolves the
    /// desktop lifetime's MainWindow itself instead. Avalonia has no synchronous modal API (by
    /// design - a blocking wrapper around an awaited dialog risks a UI-thread deadlock), so this
    /// is async-only; MessageBoxTranspiler forces the calling method async wherever needed.
    /// </summary>
    public string GenerateDialogsHelper(string namespaceName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Avalonia;");
        sb.AppendLine("using Avalonia.Controls;");
        sb.AppendLine("using Avalonia.Controls.ApplicationLifetimes;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}.Common;");
        sb.AppendLine();
        sb.AppendLine("public static class Dialogs");
        sb.AppendLine("{");
        sb.AppendLine("    public static async Task<DialogResult> ShowAsync(");
        sb.AppendLine("        string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)");
        sb.AppendLine("    {");
        sb.AppendLine("        var owner = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;");
        sb.AppendLine($"        var window = new {namespaceName}.Views.MessageBoxWindow(text, caption, buttons, icon);");
        sb.AppendLine("        return await window.ShowDialog<DialogResult>(owner);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static async Task<DialogResult?> ShowChildAsync<TView, TViewModel>()");
        sb.AppendLine("        where TView : Window, new()");
        sb.AppendLine("        where TViewModel : new()");
        sb.AppendLine("    {");
        sb.AppendLine("        var owner = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;");
        sb.AppendLine("        var view = new TView { DataContext = new TViewModel() };");
        sb.AppendLine("        return await view.ShowDialog<DialogResult?>(owner);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate Views/MessageBoxWindow.axaml - the actual dialog UI Dialogs.ShowAsync shows.
    /// Intentionally simple (message text + a row of buttons matching the requested
    /// MessageBoxButtons combination) rather than trying to replicate the native WinForms
    /// MessageBox's exact chrome/icon rendering.
    /// </summary>
    public string GenerateMessageBoxWindowAxaml(string namespaceName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<Window xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine($"        x:Class=\"{namespaceName}.Views.MessageBoxWindow\"");
        sb.AppendLine("        Width=\"400\" SizeToContent=\"Height\" CanResize=\"False\"");
        sb.AppendLine("        WindowStartupLocation=\"CenterOwner\">");
        sb.AppendLine("    <StackPanel Margin=\"20\" Spacing=\"20\">");
        sb.AppendLine("        <TextBlock Name=\"MessageText\" TextWrapping=\"Wrap\"/>");
        sb.AppendLine("        <StackPanel Name=\"ButtonPanel\" Orientation=\"Horizontal\" HorizontalAlignment=\"Right\" Spacing=\"8\"/>");
        sb.AppendLine("    </StackPanel>");
        sb.AppendLine("</Window>");

        return sb.ToString();
    }

    /// <summary>
    /// Generate Views/MessageBoxWindow.axaml.cs - builds its button row from the requested
    /// MessageBoxButtons combination and closes with the matching DialogResult (Avalonia's
    /// Window.Close(object? dialogResult) is what ShowDialog&lt;DialogResult&gt; in
    /// Dialogs.ShowAsync awaits and returns).
    /// </summary>
    public string GenerateMessageBoxWindowCodeBehind(string namespaceName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Avalonia.Controls;");
        sb.AppendLine($"using {namespaceName}.Common;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}.Views;");
        sb.AppendLine();
        sb.AppendLine("public partial class MessageBoxWindow : Window");
        sb.AppendLine("{");
        sb.AppendLine("    public MessageBoxWindow(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)");
        sb.AppendLine("    {");
        sb.AppendLine("        InitializeComponent();");
        sb.AppendLine("        Title = caption;");
        sb.AppendLine("        MessageText.Text = text;");
        sb.AppendLine();
        sb.AppendLine("        foreach (var (label, result) in GetButtons(buttons))");
        sb.AppendLine("        {");
        sb.AppendLine("            var button = new Button { Content = label, MinWidth = 75 };");
        sb.AppendLine("            button.Click += (_, _) => Close(result);");
        sb.AppendLine("            ButtonPanel.Children.Add(button);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static IEnumerable<(string Label, DialogResult Result)> GetButtons(MessageBoxButtons buttons) => buttons switch");
        sb.AppendLine("    {");
        sb.AppendLine("        MessageBoxButtons.OKCancel => [(\"OK\", DialogResult.OK), (\"Cancel\", DialogResult.Cancel)],");
        sb.AppendLine("        MessageBoxButtons.YesNo => [(\"Yes\", DialogResult.Yes), (\"No\", DialogResult.No)],");
        sb.AppendLine("        MessageBoxButtons.YesNoCancel => [(\"Yes\", DialogResult.Yes), (\"No\", DialogResult.No), (\"Cancel\", DialogResult.Cancel)],");
        sb.AppendLine("        MessageBoxButtons.RetryCancel => [(\"Retry\", DialogResult.Retry), (\"Cancel\", DialogResult.Cancel)],");
        sb.AppendLine("        MessageBoxButtons.AbortRetryIgnore => [(\"Abort\", DialogResult.Abort), (\"Retry\", DialogResult.Retry), (\"Ignore\", DialogResult.Ignore)],");
        sb.AppendLine("        _ => [(\"OK\", DialogResult.OK)]");
        sb.AppendLine("    };");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
