using System.Diagnostics;
using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Scaffolding;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Orchestrates the conversion stages: load the WinForms project, discover Forms, walk each
/// one's Designer.cs into a control tree, map controls to their Avalonia equivalents, emit
/// AXAML/ViewModel/code-behind, resolve any bundled fallback controls that were used, and
/// scaffold the resulting Avalonia project. Forms become Avalonia Windows and UserControls
/// Avalonia UserControls; Components are reported but have nothing to render - see
/// docs/known-limitations.md.
/// </summary>
public sealed class ConversionPipeline
{
    private readonly WinFormsProjectLoader _projectLoader = new();
    private readonly DesignerFileLocator _designerFileLocator = new();
    private readonly DesignerSyntaxWalker _designerSyntaxWalker = new();
    private readonly ControlGraphBuilder _controlGraphBuilder = new();
    private readonly EventMappingRegistry _eventMappingRegistry = new();
    private readonly CodeBehindExtractor _codeBehindExtractor = new();
    private readonly CodeBehindAnalyzer _codeBehindAnalyzer = new();
    private readonly AvaloniaProjectScaffolder _scaffolder = new();
    private readonly FallbackControlResolver _fallbackControlResolver = new();

    public ConversionRunResult Run(ConversionOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var projectName = NamingConventions.DeriveProjectName(options.OutputDirectory);
        var viewModelEmitter = new ViewModelEmitter();
        var codeBehindEmitter = new ViewCodeBehindEmitter();

        var project = _projectLoader.Load(options.SourceProjectPath);
        var allPairings = _designerFileLocator.Locate(project);

        if (!allPairings.Any(p => p.Kind is WinFormsArtifactKind.Form or WinFormsArtifactKind.UserControl or WinFormsArtifactKind.Component))
        {
            throw new NoConvertibleArtifactsException(project.ProjectFilePath);
        }

        // UserControls first: a Form that hosts one must already have its mapping (and its
        // View's xmlns prefix) available by the time the Form is emitted.
        var pairings = allPairings
            .Where(p => p.Kind is WinFormsArtifactKind.UserControl && p.DesignerFilePath is not null)
            .Concat(allPairings.Where(p => p.Kind is WinFormsArtifactKind.Form && p.DesignerFilePath is not null))
            .ToList();

        var userControlViews = BuildUserControlViews(project.ProjectDirectory, projectName, pairings);
        var mappingRegistry = new ControlMappingRegistry(DefaultControlMappers.All
            .Concat<IControlMapper>(userControlViews.Select(v => new UserControlMapper(v.WinFormsTypeName, v.ElementName)))
            .Concat(ProjectComponentMappers(allPairings)));

        var axamlEmitter = new AxamlEmitter(mappingRegistry);
        var migrationPlanner = new FormMigrationPlanner(mappingRegistry, _eventMappingRegistry);

        var convertedForms = new List<ConvertedFormOutput>();
        var allUsedFallbackKeys = new HashSet<string>(StringComparer.Ordinal);
        var allRequiredNuGetPackages = new HashSet<string>(StringComparer.Ordinal);
        var allWarnings = new List<string>();
        var notifyIcons = new List<NotifyIconInfo>();
        var assetsToCopy = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var directCount = 0;
        var fallbackCount = 0;
        var unsupportedCount = 0;

        foreach (var pairing in pairings)
        {
            var relativeFolder = GetRelativeFolder(project.ProjectDirectory, pairing.DesignerFilePath!);
            var designerContent = File.ReadAllText(pairing.DesignerFilePath!);
            var walkResult = _designerSyntaxWalker.Walk(designerContent, pairing.DesignerFilePath!, pairing.ClassName, pairing.Namespace);
            var formModel = _controlGraphBuilder.Build(walkResult);

            foreach (var component in formModel.Components)
            {
                var mappedComponent = mappingRegistry.Map(component);
                switch (mappedComponent.Status)
                {
                    case MappingStatus.Fallback:
                        fallbackCount++;
                        allWarnings.AddRange(mappedComponent.Warnings);
                        break;
                    case MappingStatus.Unsupported:
                        unsupportedCount++;
                        allWarnings.AddRange(mappedComponent.Warnings);
                        break;
                }

                if (component.ClrTypeName == "NotifyIcon")
                {
                    notifyIcons.Add(BuildNotifyIconInfo(component, project.ProjectDirectory, assetsToCopy, allWarnings));
                }
            }

            var viewClassName = NamingConventions.DeriveViewName(pairing.ClassName);
            var viewModelClassName = NamingConventions.DeriveViewModelName(pairing.ClassName);

            // One plan per Form, shared by all three emitters, so they can never disagree about
            // where a handler ended up or which properties are bound.
            var codeBehindModel = _codeBehindAnalyzer.Analyze(pairing.PrimaryFilePath, formModel);
            var migrationPlan = migrationPlanner.Plan(formModel, codeBehindModel);
            allWarnings.AddRange(migrationPlan.Warnings);

            var axamlResult = axamlEmitter.EmitView(
                formModel, projectName, viewClassName, viewModelClassName, migrationPlan, relativeFolder,
                emitFallbackControls: !options.NoFallbackControls, pairing.Kind, userControlViews);
            allUsedFallbackKeys.UnionWith(axamlResult.UsedFallbackKeys);
            allRequiredNuGetPackages.UnionWith(axamlResult.RequiredNuGetPackages);
            allWarnings.AddRange(axamlResult.Warnings);
            directCount += axamlResult.DirectControlCount;
            fallbackCount += axamlResult.FallbackControlCount;
            unsupportedCount += axamlResult.UnsupportedControlCount;

            var viewModel = viewModelEmitter.EmitViewModel(migrationPlan, projectName, relativeFolder, viewModelClassName);
            var rawCodeBehind = options.SkipCodeBehindComments ? null : _codeBehindExtractor.Extract(pairing.PrimaryFilePath);
            var viewCodeBehind = codeBehindEmitter.EmitViewCodeBehind(
                projectName, relativeFolder, viewClassName, viewModelClassName, migrationPlan, rawCodeBehind, pairing.Kind);

            convertedForms.Add(new ConvertedFormOutput(
                relativeFolder, viewClassName, viewModelClassName, axamlResult.Axaml, viewCodeBehind, viewModel, pairing.Kind));
        }

        var vfs = _scaffolder.BuildProject(projectName, convertedForms, allRequiredNuGetPackages, notifyIcons);
        _fallbackControlResolver.CopyResolvedTemplates(vfs, projectName, allUsedFallbackKeys);

        foreach (var (assetPath, content) in assetsToCopy)
        {
            vfs.AddBinary(assetPath, content);
        }

        if (!options.DryRun)
        {
            vfs.WriteToDisk(options.OutputDirectory);
        }

        stopwatch.Stop();

        var report = new ConversionReport(
            project.IsLegacyStyle,
            project.TargetFrameworks,
            convertedForms.Count(f => f.Kind == WinFormsArtifactKind.Form),
            convertedForms.Count(f => f.Kind == WinFormsArtifactKind.UserControl),
            directCount,
            fallbackCount,
            unsupportedCount,
            allUsedFallbackKeys.OrderBy(k => k, StringComparer.Ordinal).ToList(),
            allRequiredNuGetPackages.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            allWarnings,
            stopwatch.Elapsed);

        return new ConversionRunResult(vfs, report);
    }

    /// <summary>
    /// A tailored Unsupported entry per Component-kind class the source project defines, so a
    /// `new DemoComponent()` field reports "your own IComponent, move it to a service" instead
    /// of the registry's generic "no mapping registered for type X" fallback. Components have
    /// no visual representation at all, so unlike UserControls there is nothing to emit.
    /// </summary>
    private static IEnumerable<IControlMapper> ProjectComponentMappers(IReadOnlyList<DesignerFilePairing> allPairings) =>
        allPairings
            .Where(p => p.Kind == WinFormsArtifactKind.Component)
            .Select(p => new UnsupportedControlMapper(
                p.ClassName,
                $"'{p.ClassName}' is a Component defined by this project - it has no visual representation to convert. " +
                "Its code is plain .NET and works unchanged; move its construction out of View code-behind into a service/ViewModel."));

    /// <summary>
    /// Resolves every discovered UserControl to the View it will be emitted as, and assigns one
    /// xmlns prefix per distinct View namespace. Prefixes are positional ("uc0", "uc1", ...)
    /// rather than derived from the folder name, so they stay valid XML names whatever the
    /// source folders are called, and deterministic for the golden-file snapshot tests.
    /// </summary>
    private static IReadOnlyList<UserControlViewInfo> BuildUserControlViews(
        string projectDirectory, string projectName, IReadOnlyList<DesignerFilePairing> pairings)
    {
        var prefixesByNamespace = new Dictionary<string, string>(StringComparer.Ordinal);
        var views = new List<UserControlViewInfo>();

        foreach (var pairing in pairings.Where(p => p.Kind == WinFormsArtifactKind.UserControl))
        {
            var relativeFolder = GetRelativeFolder(projectDirectory, pairing.DesignerFilePath!);
            var viewNamespace = NamingConventions.NamespaceOf($"{projectName}.Views", relativeFolder);

            if (!prefixesByNamespace.TryGetValue(viewNamespace, out var prefix))
            {
                prefix = $"uc{prefixesByNamespace.Count}";
                prefixesByNamespace[viewNamespace] = prefix;
            }

            views.Add(new UserControlViewInfo(
                pairing.ClassName, NamingConventions.DeriveViewName(pairing.ClassName), viewNamespace, prefix));
        }

        return views;
    }

    /// <summary>
    /// Real Designer.cs almost never assigns NotifyIcon.Icon as a literal path (it's usually a
    /// resx resource lookup or a dynamically computed Icon) - ExpressionEvaluator only
    /// recognizes the literal-path `new Icon("app.ico")` shape. When it does resolve, and the
    /// file is really there, the icon is copied into the generated project's Assets/ folder so
    /// the emitted TrayIcon reference actually points at something.
    /// </summary>
    /// <remarks>
    /// Otherwise <see cref="NotifyIconInfo.IconAssetPath"/> stays null and the scaffolder emits
    /// the TrayIcon block commented out. Avalonia resolves TrayIcon.Icon at run time, so naming
    /// an asset the conversion never produced is not a build error - it is a
    /// FileNotFoundException thrown out of App.Initialize(), before any window opens, which
    /// took down the whole generated app.
    /// </remarks>
    private static NotifyIconInfo BuildNotifyIconInfo(
        ControlModel component, string projectDirectory, IDictionary<string, byte[]> assetsToCopy, List<string> warnings)
    {
        var tooltip = component.Properties.TryGetValue("Text", out var textValue) && textValue is PropertyValue.Literal { Value: string text }
            ? text
            : null;

        if (component.Properties.TryGetValue("Icon", out var iconValue)
            && iconValue is PropertyValue.Literal { Value: string iconPath }
            && !string.IsNullOrWhiteSpace(iconPath))
        {
            var sourcePath = Path.IsPathRooted(iconPath) ? iconPath : Path.Combine(projectDirectory, iconPath);
            if (File.Exists(sourcePath))
            {
                var assetPath = $"Assets/{Path.GetFileName(iconPath)}";
                assetsToCopy[assetPath] = File.ReadAllBytes(sourcePath);
                return new NotifyIconInfo(component.FieldName, assetPath, tooltip);
            }

            warnings.Add(
                $"NotifyIcon '{component.FieldName}': Designer.cs names icon file '{iconPath}', but it was not found at " +
                $"'{sourcePath}' - App.axaml's TrayIcon is emitted commented out; add the icon to Assets/ and uncomment it.");
            return new NotifyIconInfo(component.FieldName, null, tooltip);
        }

        warnings.Add(
            $"NotifyIcon '{component.FieldName}': couldn't resolve a literal icon file path from Designer.cs (it is usually a " +
            "resx resource) - App.axaml's TrayIcon is emitted commented out, since referencing an icon file the conversion " +
            "cannot produce would throw at startup. Copy the real icon into Assets/ and uncomment the block.");
        return new NotifyIconInfo(component.FieldName, null, tooltip);
    }

    private static string GetRelativeFolder(string projectDirectory, string filePath)
    {
        var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, filePath)) ?? "";
        return relativeDirectory == "." ? "" : relativeDirectory.Replace('\\', '/');
    }
}
