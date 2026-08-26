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
    private readonly ResxReader _resxReader = new();
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

        // Every Form resolved to its View up front, before any of them is emitted: a handler that
        // opens another Form has to name a type whose View may not have been converted yet, so
        // unlike the UserControl ordering trick this genuinely needs a separate discovery pass.
        var formViews = BuildFormViews(project.ProjectDirectory, projectName, pairings);
        var mappingRegistry = new ControlMappingRegistry(DefaultControlMappers.All
            .Concat<IControlMapper>(userControlViews.Select(v => new UserControlMapper(v.WinFormsTypeName, v.ElementName)))
            .Concat(ProjectComponentMappers(allPairings)));

        var axamlEmitter = new AxamlEmitter(mappingRegistry);
        var migrationPlanner = new FormMigrationPlanner(mappingRegistry, _eventMappingRegistry);

        var convertedForms = new List<ConvertedFormOutput>();
        var allUsedFallbackKeys = new HashSet<string>(StringComparer.Ordinal);
        var allRequiredNuGetPackages = new HashSet<string>(StringComparer.Ordinal);
        var allWarnings = new List<string>();

        // A group with a .Designer.cs that still didn't classify is a designer artifact this run
        // is about to skip - almost always a Form deriving from a base class that lives in a
        // referenced assembly, which syntax alone cannot follow. Say so rather than drop it.
        foreach (var skipped in allPairings.Where(p => p.UnresolvedBaseTypes.Count > 0))
        {
            allWarnings.Add(
                $"'{skipped.FullyQualifiedName}' has a designer file but derives from " +
                $"{string.Join("/", skipped.UnresolvedBaseTypes)}, which could not be traced to a WinForms " +
                "base type inside this project - it was not converted. If that base type is a Form or " +
                "UserControl from a referenced assembly, convert its project too, or temporarily change the " +
                "declaration to the WinForms base type and re-run.");
        }
        var notifyIcons = new List<NotifyIconInfo>();
        var assetsToCopy = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var directCount = 0;
        var fallbackCount = 0;
        var unsupportedCount = 0;
        var allMigratedStatements = 0;
        var allHandlerStatements = 0;

        foreach (var pairing in pairings)
        {
            var relativeFolder = GetRelativeFolder(project.ProjectDirectory, pairing.DesignerFilePath!);
            var designerContent = File.ReadAllText(pairing.DesignerFilePath!);
            var resx = _resxReader.Read(pairing.ResxFilePath);
            var walkResult = _designerSyntaxWalker.Walk(
                designerContent, pairing.DesignerFilePath!, pairing.ClassName, pairing.Namespace, resx);
            var formModel = _controlGraphBuilder.Build(walkResult);
            allWarnings.AddRange(walkResult.Warnings);
            ResolveResourceAssets(formModel, resx, assetsToCopy, allWarnings);

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
            var migrationPlan = migrationPlanner.Plan(formModel, codeBehindModel, formViews, pairing.Kind);
            allWarnings.AddRange(migrationPlan.Warnings);

            // Unlike every other fallback key, these come from a translated *handler body*
            // (MessageBoxFallback) rather than from an element in the AXAML, so they have to be
            // unioned in here - AxamlEmissionResult never sees them.
            allUsedFallbackKeys.UnionWith(migrationPlan.RequiredFallbackKeys);
            var (migratedStatements, totalStatements) = migrationPlan.StatementMigration;
            allMigratedStatements += migratedStatements;
            allHandlerStatements += totalStatements;

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

        var preservedFiles = new List<string>();
        if (!options.DryRun)
        {
            var writeResult = vfs.WriteToDisk(
                options.OutputDirectory,
                options.OverwriteAll ? ExistingFileStrategy.Overwrite : ExistingFileStrategy.PreserveExisting);
            preservedFiles.AddRange(writeResult.Preserved);
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
            stopwatch.Elapsed,
            preservedFiles,
            allMigratedStatements,
            allHandlerStatements);

        return new ConversionRunResult(vfs, report);
    }

    /// <summary>
    /// Turns every <see cref="PropertyValue.ResourceReference"/> the walker produced into a real
    /// file under the generated project's <c>Assets/</c>, rewriting the property to the asset
    /// path so the ordinary mapping/emission path can treat it as any other literal.
    /// </summary>
    /// <remarks>
    /// Runs over <see cref="FormModel.Controls"/> - the flat lookup - so nested controls and
    /// non-visual components are covered without walking the tree. A payload that cannot be
    /// decoded has its property removed rather than pointed at a file the conversion never
    /// wrote: Avalonia resolves an Image source at run time, so a dangling asset reference
    /// builds fine and then throws in front of the user.
    /// </remarks>
    private static void ResolveResourceAssets(
        FormModel formModel, ResxDocument resx, IDictionary<string, byte[]> assetsToCopy, List<string> warnings)
    {
        foreach (var control in formModel.Controls.Values)
        {
            foreach (var (propertyName, value) in control.Properties.ToList())
            {
                if (value is not PropertyValue.ResourceReference(var resourceKey))
                {
                    continue;
                }

                control.Properties.Remove(propertyName);

                var entry = resx.EntriesFor(OwnerOf(resourceKey)).FirstOrDefault(e => e.Name == resourceKey);
                if (entry is null || !entry.IsBinary)
                {
                    warnings.Add(
                        $"'{control.FieldName}.{propertyName}' reads resource '{resourceKey}', which was not found as a " +
                        "binary entry in the form's .resx - the property is not set in the generated view.");
                    continue;
                }

                if (!ResxImageExtractor.TryExtract(entry.Value, out var image))
                {
                    warnings.Add(
                        $"'{control.FieldName}.{propertyName}': the .resx payload for '{resourceKey}' is not a " +
                        "recognizable image (PNG/JPEG/GIF/BMP/ICO) - export it from the original project by hand and " +
                        "reference it from Assets/.");
                    continue;
                }

                var assetPath = $"Assets/{control.FieldName}_{propertyName}{image.FileExtension}";
                assetsToCopy[assetPath] = image.Bytes;
                control.Properties[propertyName] = new PropertyValue.Literal($"/{assetPath}");
            }
        }
    }

    /// <summary>"pictureBox1" for the resx key "pictureBox1.Image".</summary>
    private static string OwnerOf(string resourceKey)
    {
        var lastDot = resourceKey.LastIndexOf('.');
        return lastDot < 0 ? resourceKey : resourceKey[..lastDot];
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
    /// Resolves every discovered Form to the View it will be emitted as, keyed by the original
    /// WinForms class name - which is what a handler body writes in <c>new SettingsForm()</c>.
    /// </summary>
    private static IReadOnlyDictionary<string, FormViewInfo> BuildFormViews(
        string projectDirectory, string projectName, IReadOnlyList<DesignerFilePairing> pairings)
    {
        var views = new Dictionary<string, FormViewInfo>(StringComparer.Ordinal);

        foreach (var pairing in pairings.Where(p => p.Kind == WinFormsArtifactKind.Form))
        {
            var relativeFolder = GetRelativeFolder(projectDirectory, pairing.DesignerFilePath!);

            views[pairing.ClassName] = new FormViewInfo(
                pairing.ClassName,
                NamingConventions.DeriveViewName(pairing.ClassName),
                NamingConventions.NamespaceOf($"{projectName}.Views", relativeFolder));
        }

        return views;
    }

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

        // ResolveResourceAssets already recovered this icon from the .resx and staged it under
        // Assets/ - the common real-world shape, and the one this used to give up on. The
        // leading '/' is the XAML form of the path; NotifyIconInfo stores it without.
        if (component.Properties.TryGetValue("Icon", out var resolvedIcon)
            && resolvedIcon is PropertyValue.Literal { Value: string assetReference }
            && assetReference.StartsWith("/Assets/", StringComparison.Ordinal))
        {
            return new NotifyIconInfo(component.FieldName, assetReference[1..], tooltip);
        }

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
