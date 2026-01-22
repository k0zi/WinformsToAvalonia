using System.Text;

namespace Converter.Generator.Project;

/// <summary>
/// Generates Avalonia project files (.csproj).
/// </summary>
public class ProjectFileGenerator
{
    /// <summary>
    /// Generate an Avalonia Desktop project file.
    /// </summary>
    public string GenerateAvaloniaProject(string projectName, string targetFramework = "net10.0")
    {
        var sb = new StringBuilder();

        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <OutputType>WinExe</OutputType>");
        sb.AppendLine($"    <TargetFramework>{targetFramework}</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>");
        sb.AppendLine("    <ApplicationManifest>app.manifest</ApplicationManifest>");
        sb.AppendLine("    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();

        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <AvaloniaResource Include=\"Assets\\**\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();

        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Avalonia\" Version=\"11.2.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Avalonia.Desktop\" Version=\"11.2.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Avalonia.Themes.Fluent\" Version=\"11.2.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Avalonia.Fonts.Inter\" Version=\"11.2.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Avalonia.ReactiveUI\" Version=\"11.2.0\" />");
        sb.AppendLine("    <PackageReference Include=\"CommunityToolkit.Mvvm\" Version=\"8.3.2\" />");
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
        sb.AppendLine($"using {namespaceName}.ViewModels;");
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
        sb.AppendLine($"            desktop.MainWindow = new {mainWindowName}");
        sb.AppendLine("            {");
        sb.AppendLine($"                DataContext = new {mainWindowName}ViewModel()");
        sb.AppendLine("            };");
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
}
