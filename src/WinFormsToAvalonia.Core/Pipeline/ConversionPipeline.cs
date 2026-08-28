using System.Diagnostics;
using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Scaffolding;
using WinFormsToAvalonia.FallbackControls;

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

    private readonly CodeBehindExtractor _codeBehindExtractor = new();
    private readonly CodeBehindAnalyzer _codeBehindAnalyzer = new();
    private readonly AvaloniaProjectScaffolder _scaffolder = new();
    private readonly FallbackControlResolver _fallbackControlResolver = new();

    /// <param name="solutionContext">
    /// What the rest of the solution contributes, when this project is one of several being
    /// converted together. Null - the default - is the ordinary single-project run.
    /// </param>
    public ConversionRunResult Run(ConversionOptions options, SolutionConversionContext? solutionContext = null)
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

        var allWarnings = new List<string>();

        // A Component this project defines is plain .NET when it names nothing that would not
        // survive - so its source comes across and it gets a real field, instead of the whole
        // artifact kind being dropped with guidance.
        var carriedComponents = CarryOverProjectComponents(allPairings, projectName, allWarnings);
        var carriedComponentNamespaces = carriedComponents.ToDictionary(
            c => c.ClassName, c => c.TargetNamespace, StringComparer.Ordinal);
        var carriedComponentEvents = carriedComponents
            .SelectMany(c => c.Events.Select(e => (Key: (c.ClassName, e.Key), e.Value)))
            .ToDictionary(x => x.Key, x => x.Value);
        var eventMappingRegistry = new EventMappingRegistry(carriedComponentEvents);

        var userControlViews = BuildUserControlViews(
            project.ProjectDirectory, projectName, pairings,
            solutionContext?.ExternalUserControls ?? []);

        // Every Form resolved to its View up front, before any of them is emitted: a handler that
        // opens another Form has to name a type whose View may not have been converted yet, so
        // unlike the UserControl ordering trick this genuinely needs a separate discovery pass.
        var formViews = BuildFormViews(project.ProjectDirectory, projectName, pairings);
        var mappingRegistry = new ControlMappingRegistry(DefaultControlMappers.All
            .Concat<IControlMapper>(userControlViews.Select(v => new UserControlMapper(v.WinFormsTypeName, v.ElementName)))
            .Concat(ProjectComponentMappers(allPairings, carriedComponentNamespaces.Keys.ToHashSet(StringComparer.Ordinal))));

        var axamlEmitter = new AxamlEmitter(mappingRegistry);
        var migrationPlanner = new FormMigrationPlanner(mappingRegistry, eventMappingRegistry);

        var convertedForms = new List<ConvertedFormOutput>();
        var migrationSummaries = new List<ArtifactMigrationSummary>();
        var allUsedFallbackKeys = new HashSet<string>(StringComparer.Ordinal);
        var allRequiredNuGetPackages = new HashSet<string>(StringComparer.Ordinal);

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

        // Every artifact parsed before any of them is planned. A handler that says
        // `dialog.EnteredText` names a property of a View that may not be planned yet, so - like
        // BuildFormViews for the Forms themselves - ordering alone cannot settle it.
        var parsedArtifacts = pairings
            .Select(p => ParseArtifact(project.ProjectDirectory, p))
            .ToList();

        var promotedPropertiesByArtifact = parsedArtifacts.ToDictionary(
            a => a.Pairing.ClassName,
            a => migrationPlanner.PlanProperties(a.FormModel, a.CodeBehind, a.Pairing.Kind),
            StringComparer.Ordinal);

        // Only the public ones cross an artifact boundary - a private property is nobody else's
        // vocabulary, exactly as in C#.
        var viewPropertiesByType = promotedPropertiesByArtifact.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<ViewPropertyInfo>)
            [
                .. entry.Value
                    .Where(p => p.ModifiersText.Split(' ').Contains("public"))
                    .Select(p => new ViewPropertyInfo(p.Name, p.TypeText, p.Setter is not null)),
            ],
            StringComparer.Ordinal);

        foreach (var (pairing, relativeFolder, formModel, codeBehindModel, resx, walkWarnings) in parsedArtifacts)
        {
            allWarnings.AddRange(walkWarnings);
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
            var migrationPlan = migrationPlanner.Plan(
                formModel, codeBehindModel, formViews, pairing.Kind, carriedComponentNamespaces,
                new ViewSurfaceContext(promotedPropertiesByArtifact[pairing.ClassName], viewPropertiesByType),
                // Collected just above, in this same iteration: only an icon whose file resolved
                // reaches App.axaml, and only those get an accessor a handler could name.
                notifyIcons.Where(i => i.IconAssetPath is not null)
                    .Select(i => i.FieldName)
                    .ToHashSet(StringComparer.Ordinal));
            allWarnings.AddRange(migrationPlan.Warnings);

            // Unlike every other fallback key, these come from a translated *handler body*
            // (MessageBoxFallback) rather than from an element in the AXAML, so they have to be
            // unioned in here - AxamlEmissionResult never sees them.
            allUsedFallbackKeys.UnionWith(migrationPlan.RequiredFallbackKeys);

            // Non-visual components emitted as real fields can need a package of their own, the
            // same double-allowlist path a mapper's RequiredNuGetPackage takes.
            allRequiredNuGetPackages.UnionWith(migrationPlan.RequiredNuGetPackages);
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

            migrationSummaries.Add(SummarizeMigration(
                pairing.ClassName, relativeFolder, viewClassName, viewModelClassName, migrationPlan));
        }

        // A bundled template can need a package of its own (ColorDialogFallback wraps Avalonia's
        // separately-shipped ColorView). Unioned here rather than in the loop because the full set
        // of used templates is only known once every form has been emitted - and the csproj is
        // written by the very next call.
        foreach (var key in allUsedFallbackKeys)
        {
            if (FallbackControlCatalog.All.TryGetValue(key, out var template)
                && template.RequiredNuGetPackage is { } package)
            {
                allRequiredNuGetPackages.Add(package);
            }
        }

        var vfs = _scaffolder.BuildProject(
            projectName, convertedForms, allRequiredNuGetPackages, notifyIcons,
            solutionContext?.ProjectReferences ?? []);
        _fallbackControlResolver.CopyResolvedTemplates(vfs, projectName, allUsedFallbackKeys);

        foreach (var (relativePath, text) in carriedComponents.SelectMany(c => c.Files))
        {
            vfs.AddText(relativePath, text);
        }

        foreach (var (assetPath, content) in assetsToCopy)
        {
            vfs.AddBinary(assetPath, content);
        }

        // The map to the work this conversion deliberately left for a human. Written through the
        // VFS like everything else, so --dry-run and the preserve-existing re-run behave on it.
        vfs.AddText("MIGRATION.md", new MigrationChecklistEmitter().Emit(
            projectName, options.SourceProjectPath, allMigratedStatements, allHandlerStatements,
            allWarnings, migrationSummaries));

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
        var imageListAssets = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

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

                // An ImageList is a whole strip of images in one payload rather than a single
                // file, so it takes the reader that knows that structure - and produces N assets.
                if (propertyName == "ImageStream")
                {
                    ResolveImageList(control, resourceKey, entry.Value, assetsToCopy, imageListAssets, warnings);
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

        ResolveImageListReferences(formModel, imageListAssets, warnings);
    }

    /// <summary>
    /// Unpacks one <c>ImageList.ImageStream</c> into a numbered PNG per image under
    /// <c>Assets/</c>, and remembers the list so <see cref="ResolveImageListReferences"/> can
    /// turn an <c>ImageIndex</c> into one of those paths.
    /// </summary>
    /// <remarks>
    /// The images are written even when nothing ends up referencing them. They are the one thing
    /// in a WinForms project that a developer genuinely cannot get at by hand - the payload is a
    /// BinaryFormatter blob no modern .NET can open - so having them sitting in <c>Assets/</c>,
    /// named after the field and index the original code used, is worth more than the bytes cost.
    /// </remarks>
    private static void ResolveImageList(
        ControlModel control,
        string resourceKey,
        string base64,
        IDictionary<string, byte[]> assetsToCopy,
        IDictionary<string, IReadOnlyList<string>> imageListAssets,
        List<string> warnings)
    {
        if (ImageListExtractor.TryExtract(base64) is not { } imageList)
        {
            warnings.Add(
                $"ImageList '{control.FieldName}': the .resx payload for '{resourceKey}' is not a readable " +
                "ImageListStreamer - its images are not extracted. Export them from the original project by hand " +
                "and reference them from Assets/.");
            return;
        }

        var assetPaths = new List<string>(imageList.Images.Count);
        for (var index = 0; index < imageList.Images.Count; index++)
        {
            var assetPath = $"Assets/{control.FieldName}_{index}.png";
            assetsToCopy[assetPath] = imageList.Images[index];
            assetPaths.Add(assetPath);
        }

        imageListAssets[control.FieldName] = assetPaths;
        warnings.Add(
            $"ImageList '{control.FieldName}': {assetPaths.Count} image(s) of {imageList.ImageWidth}x" +
            $"{imageList.ImageHeight} were extracted to Assets/{control.FieldName}_0.png .. " +
            $"{Path.GetFileName(assetPaths[^1])}. Controls that took an image from it by ImageIndex are wired up " +
            "where Avalonia has somewhere to put one; the rest are listed separately.");
    }

    /// <summary>
    /// Rewrites <c>SomeControl.ImageIndex</c> into the <c>Image</c> property the rest of the
    /// pipeline already understands - the same shape a <c>pictureBox1.Image</c> resource ends up
    /// in - so an extracted ImageList image reaches emission through the ordinary path.
    /// </summary>
    /// <remarks>
    /// The list is inherited from the owner when the control does not name one itself, because
    /// that is how WinForms resolves it: a <c>ToolStripItem</c> has an <c>ImageIndex</c> of its
    /// own but takes the <c>ImageList</c> from the ToolStrip that owns it.
    /// </remarks>
    private static void ResolveImageListReferences(
        FormModel formModel, IReadOnlyDictionary<string, IReadOnlyList<string>> imageListAssets, List<string> warnings)
    {
        var owners = BuildOwnerMap(formModel);

        // Resolved for every control before any of them is rewritten: an owner's ImageList is
        // still needed by its children, so consuming it as the owner is visited would leave
        // whichever items happen to come later in the walk with nothing to look up.
        var listsInScope = formModel.Controls.Values.ToDictionary(
            control => control.FieldName,
            control => OwnedImageListName(control, owners, imageListAssets),
            StringComparer.Ordinal);

        foreach (var control in formModel.Controls.Values)
        {
            var declaredList = listsInScope[control.FieldName];

            // Both are consumed either way: leaving them behind would only make an emitter warn
            // about a property it has no Avalonia counterpart for.
            var hasIndex = control.Properties.TryGetValue("ImageIndex", out var indexValue);
            var hasKey = control.Properties.ContainsKey("ImageKey");
            control.Properties.Remove("ImageList");
            control.Properties.Remove("ImageIndex");
            control.Properties.Remove("ImageKey");

            if (hasKey && !hasIndex)
            {
                warnings.Add(
                    $"'{control.FieldName}.ImageKey' names an image by key, and an ImageList's keys live in the " +
                    "designer rather than in the payload this conversion reads - set the image by hand from Assets/.");
                continue;
            }

            if (!hasIndex || declaredList is null)
            {
                continue;
            }

            if (indexValue is not PropertyValue.Literal { Value: int imageIndex }
                || imageIndex < 0
                || imageIndex >= imageListAssets[declaredList].Count)
            {
                warnings.Add(
                    $"'{control.FieldName}.ImageIndex' does not point at an image in '{declaredList}' - no image is " +
                    "set on the generated element.");
                continue;
            }

            control.Properties["Image"] = new PropertyValue.Literal($"/{imageListAssets[declaredList][imageIndex]}");
        }
    }

    /// <summary>
    /// The decoded ImageList this control draws from: its own if it names one, otherwise its
    /// owner's, walking up until one is found.
    /// </summary>
    private static string? OwnedImageListName(
        ControlModel control,
        IReadOnlyDictionary<string, ControlModel> owners,
        IReadOnlyDictionary<string, IReadOnlyList<string>> imageListAssets)
    {
        for (var current = control; current is not null; current = owners.GetValueOrDefault(current.FieldName))
        {
            if (current.Properties.TryGetValue("ImageList", out var value)
                && value is PropertyValue.ControlReference(var listFieldName)
                && imageListAssets.ContainsKey(listFieldName))
            {
                return listFieldName;
            }
        }

        return null;
    }

    /// <summary>Child field name to its parent - the link ControlModel deliberately does not carry.</summary>
    private static Dictionary<string, ControlModel> BuildOwnerMap(FormModel formModel)
    {
        var owners = new Dictionary<string, ControlModel>(StringComparer.Ordinal);

        foreach (var parent in formModel.Controls.Values)
        {
            foreach (var child in parent.Children.Concat(parent.Panel1Children).Concat(parent.Panel2Children))
            {
                owners[child.FieldName] = parent;
            }
        }

        return owners;
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
    private static IEnumerable<IControlMapper> ProjectComponentMappers(
        IReadOnlyList<DesignerFilePairing> allPairings, IReadOnlySet<string> carriedOver) =>
        allPairings
            .Where(p => p.Kind == WinFormsArtifactKind.Component)
            .Select(p => new UnsupportedControlMapper(
                p.ClassName,
                carriedOver.Contains(p.ClassName)
                    ? $"'{p.ClassName}' is a Component defined by this project - no visual representation, so no control "
                        + "mapping. Its source names nothing that would not survive the conversion, so it is copied into "
                        + "the generated project and a real field is emitted for it."
                    : $"'{p.ClassName}' is a Component defined by this project - it has no visual representation to convert. "
                        + "Its code is plain .NET and works unchanged; move its construction out of View code-behind into a service/ViewModel."));

    /// <summary>
    /// Every project-defined Component whose source can be carried over verbatim, with the reason
    /// reported for each one that cannot.
    /// </summary>
    private static List<CarriedOverComponent> CarryOverProjectComponents(
        IReadOnlyList<DesignerFilePairing> allPairings, string projectName, List<string> warnings)
    {
        var winFormsTypeNames = new ControlMappingRegistry().Mappers.Keys.ToHashSet(StringComparer.Ordinal);
        var carried = new List<CarriedOverComponent>();

        foreach (var pairing in allPairings.Where(p => p.Kind == WinFormsArtifactKind.Component))
        {
            // Another class this project declares is not carried over with it, so naming one would
            // leave the copied file referring to something that does not exist there.
            var otherClassNames = allPairings
                .Where(p => p.ClassName != pairing.ClassName)
                .Select(p => p.ClassName)
                .ToHashSet(StringComparer.Ordinal);

            if (ComponentSourceAnalyzer.TryCarryOver(
                    pairing, $"{projectName}.Components", winFormsTypeNames, otherClassNames, out var component, out var reason))
            {
                carried.Add(component);
            }
            else
            {
                warnings.Add($"Component '{pairing.ClassName}' was not carried over: {reason}.");
            }
        }

        return carried;
    }

    /// <summary>
    /// What one converted artifact still needs a human for, taken from the plan rather than from
    /// the emitted text - the same <c>IsUnfinished</c> predicate the code emitters use.
    /// </summary>
    private static ArtifactMigrationSummary SummarizeMigration(
        string sourceArtifactName,
        string relativeFolder,
        string viewClassName,
        string viewModelClassName,
        FormMigrationPlan plan)
    {
        var viewPath = CombineOutputFolder("Views", relativeFolder) + $"/{viewClassName}.axaml.cs";
        var viewModelPath = CombineOutputFolder("ViewModels", relativeFolder) + $"/{viewModelClassName}.cs";

        var unfinished = plan.CodeBehindHandlers
            .Where(h => h.IsUnfinished)
            .Select(h => new UnfinishedMember(viewPath, h.MethodName, h.OriginalMethodName, FirstLineOf(h.RemainingBody)))
            .Concat(plan.ViewModelCommands
                .Where(c => c.IsUnfinished)
                .Select(c => new UnfinishedMember(viewModelPath, c.CommandMethodName, c.OriginalMethodName, FirstLineOf(c.RemainingBody))))
            .ToList();

        return new ArtifactMigrationSummary(
            sourceArtifactName,
            unfinished,
            [.. plan.PreservedMembers.Select(m => m.Name).Where(n => n.Length > 0)]);
    }

    private static string CombineOutputFolder(string root, string relativeFolder) =>
        string.IsNullOrEmpty(relativeFolder) ? root : $"{root}/{relativeFolder}";

    private static string FirstLineOf(string body) =>
        body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "";

    /// <summary>
    /// Resolves every discovered Form to the View it will be emitted as, keyed by the original
    /// WinForms class name - which is what a handler body writes in <c>new SettingsForm()</c>.
    /// </summary>
    /// <summary>
    /// One artifact read off disk: its designer walked into a control graph, and its code-behind
    /// analysed. Everything the conversion needs before it can decide anything.
    /// </summary>
    private sealed record ParsedArtifact(
        DesignerFilePairing Pairing,
        string RelativeFolder,
        FormModel FormModel,
        CodeBehindModel CodeBehind,
        ResxDocument Resx,
        IReadOnlyList<string> WalkWarnings);

    /// <summary>
    /// Parsing only - no warnings are reported and no assets resolved here, so that the main loop
    /// below keeps saying everything in the order it always did.
    /// </summary>
    private ParsedArtifact ParseArtifact(string projectDirectory, DesignerFilePairing pairing)
    {
        var designerContent = File.ReadAllText(pairing.DesignerFilePath!);
        var resx = _resxReader.Read(pairing.ResxFilePath);
        var walkResult = _designerSyntaxWalker.Walk(
            designerContent, pairing.DesignerFilePath!, pairing.ClassName, pairing.Namespace, resx);
        var formModel = _controlGraphBuilder.Build(walkResult);

        return new ParsedArtifact(
            pairing,
            GetRelativeFolder(projectDirectory, pairing.DesignerFilePath!),
            formModel,
            _codeBehindAnalyzer.Analyze(pairing.PrimaryFilePath, formModel),
            resx,
            walkResult.Warnings);
    }

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
    /// <param name="externalUserControls">
    /// UserControls contributed by other projects of the same solution. They share the prefix
    /// counter with this project's own, since a prefix only has to be unique within the document
    /// it is declared on; a name this project defines itself wins, exactly as the C# compiler
    /// would resolve it.
    /// </param>
    private static IReadOnlyList<UserControlViewInfo> BuildUserControlViews(
        string projectDirectory,
        string projectName,
        IReadOnlyList<DesignerFilePairing> pairings,
        IReadOnlyList<ExternalUserControl> externalUserControls)
    {
        var prefixesByNamespace = new Dictionary<string, string>(StringComparer.Ordinal);
        var views = new List<UserControlViewInfo>();
        var claimedTypeNames = new HashSet<string>(StringComparer.Ordinal);

        string PrefixFor(string viewNamespace)
        {
            if (!prefixesByNamespace.TryGetValue(viewNamespace, out var prefix))
            {
                prefix = $"uc{prefixesByNamespace.Count}";
                prefixesByNamespace[viewNamespace] = prefix;
            }

            return prefix;
        }

        foreach (var pairing in pairings.Where(p => p.Kind == WinFormsArtifactKind.UserControl))
        {
            var relativeFolder = GetRelativeFolder(projectDirectory, pairing.DesignerFilePath!);
            var viewNamespace = NamingConventions.NamespaceOf($"{projectName}.Views", relativeFolder);

            claimedTypeNames.Add(pairing.ClassName);
            views.Add(new UserControlViewInfo(
                pairing.ClassName,
                NamingConventions.DeriveViewName(pairing.ClassName),
                viewNamespace,
                PrefixFor(viewNamespace)));
        }

        foreach (var external in externalUserControls)
        {
            if (!claimedTypeNames.Add(external.WinFormsTypeName))
            {
                continue;
            }

            views.Add(new UserControlViewInfo(
                external.WinFormsTypeName,
                external.ViewClassName,
                external.ViewNamespace,
                PrefixFor(external.ViewNamespace),
                external.AssemblyName));
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

    internal static string GetRelativeFolder(string projectDirectory, string filePath)
    {
        var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, filePath)) ?? "";
        return relativeDirectory == "." ? "" : relativeDirectory.Replace('\\', '/');
    }
}
