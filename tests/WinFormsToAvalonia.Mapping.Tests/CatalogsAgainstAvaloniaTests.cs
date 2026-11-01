using System.Reflection;
using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Scaffolding;
using WinFormsToAvalonia.FallbackControls;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// The smaller tables - the ones a translated <em>statement</em> is written from - checked against
/// Avalonia.
/// </summary>
/// <remarks>
/// Each of these names an Avalonia member in text that ends up in generated C#. A name that is
/// wrong compiles here and fails there, which is the whole reason this project exists.
/// </remarks>
public class CatalogsAgainstAvaloniaTests
{
    [Theory]
    [MemberData(nameof(StyleClaims))]
    public void StyleSupport_ClaimsOnlyPropertiesTheElementHas(
        string avaloniaElementName, string avaloniaPropertyName)
    {
        var element = AvaloniaMetadata.FindElement(avaloniaElementName);
        Assert.True(element is not null, $"Avalonia has no '{avaloniaElementName}' element at all.");

        Assert.True(
            AvaloniaMetadata.FindProperty(element!, avaloniaPropertyName) is not null,
            $"AvaloniaStylePropertySupport says a '{avaloniaElementName}' can carry "
            + $"'{avaloniaPropertyName}', but it has no such property - emitting it would be an "
            + "AVLN2000 in the generated project.");
    }

    [Theory]
    [MemberData(nameof(ControlMethods))]
    public void ControlMethod_NamesAMemberTheElementHas(
        string winFormsSource, string avaloniaElementName, string avaloniaMemberName)
    {
        var element = AvaloniaMetadata.FindElement(avaloniaElementName);
        Assert.True(element is not null, $"Avalonia has no '{avaloniaElementName}' element at all.");

        // The catalog names the member a call *reaches* - Focus() the method, but Text the
        // property, because `AppendText` is emitted as `Text += ...`. Either kind counts.
        Assert.True(
            AvaloniaMetadata.FindProperty(element!, avaloniaMemberName) is not null
            || AvaloniaMetadata.FindMethod(element!, avaloniaMemberName, 0) is not null,
            $"ControlMethodCatalog translates {winFormsSource} through '{avaloniaMemberName}' on a "
            + $"'{avaloniaElementName}', which has no such member.");
    }

    [Theory]
    [MemberData(nameof(EventArgsMembers))]
    public void EventArgsMember_ExistsOnItsArgsType(string argsTypeName, string memberName, string source)
    {
        // Not every args type is Avalonia's. A bundled template can raise an event of its own -
        // the paint surface's - and that type ships in this repo, so its members are checked
        // against the template source the generated project will actually compile.
        if (BundledArgsTypeMembers(argsTypeName) is { } bundled)
        {
            Assert.True(
                bundled.Contains(memberName),
                $"{source} is translated through '{argsTypeName}.{memberName}', which the bundled "
                + "template does not declare.");
            return;
        }

        var argsType = AvaloniaMetadata.FindElement(argsTypeName);
        Assert.True(argsType is not null, $"Avalonia has no '{argsTypeName}' type at all.");

        Assert.True(
            AvaloniaMetadata.FindProperty(argsType!, memberName) is not null
            || AvaloniaMetadata.FindMethod(argsType!, memberName, 1) is not null
            || AvaloniaMetadata.FindMethod(argsType!, memberName, 0) is not null,
            $"{source} is translated through '{argsTypeName}.{memberName}', which does not exist.");
    }

    /// <summary>
    /// The events a bundled fallback is allowed to carry an AXAML attribute for, against the
    /// Avalonia <c>Control</c> every one of those templates derives from.
    /// </summary>
    /// <remarks>
    /// This table is what lifted a real restriction - a fallback used to get no event attribute at
    /// all - so it has to stay honest in exactly one direction: a name here that Control does not
    /// declare is an AVLN error in the generated project and nowhere else.
    /// </remarks>
    [Theory]
    [MemberData(nameof(FallbackControlEvents))]
    public void FallbackControlEvent_IsDeclaredByAvaloniaControl(string eventName)
    {
        var control = AvaloniaMetadata.FindElement("Control");
        Assert.True(control is not null, "Avalonia has no Control type - something is very wrong.");

        Assert.True(
            AvaloniaMetadata.FindEvent(control!, eventName) is not null,
            $"FallbackControlMemberSupport lets a bundled template carry '{eventName}', but Avalonia's "
            + "Control does not declare it - so the attribute would not resolve.");
    }

    public static TheoryData<string> FallbackControlEvents()
    {
        var data = new TheoryData<string>();
        foreach (var name in FallbackControlMemberSupport.ControlEventNames.OrderBy(n => n, StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }

    /// <summary>
    /// Every <c>StringFormat</c> setting this converter carries over, against the Avalonia enum it
    /// is written as.
    /// </summary>
    /// <remarks>
    /// The WinForms side needs no check - a name that is not a real <c>StringAlignment</c> simply
    /// never matches a handler body. The Avalonia side does: a member that does not exist is a
    /// compile error in the *generated* project and nowhere else.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TextFormatSettings))]
    public void TextFormatSetting_NamesAMemberTheAvaloniaEnumHas(
        string winFormsEnum, string winFormsMember, string avaloniaType, string avaloniaMember)
    {
        var enumType = AvaloniaMetadata.FindElement(avaloniaType);
        Assert.True(enumType is not null, $"Avalonia has no '{avaloniaType}' type at all.");

        Assert.True(
            enumType!.GetMember(avaloniaMember).Length > 0,
            $"TextFormatCatalog turns {winFormsEnum}.{winFormsMember} into "
            + $"{avaloniaType}.{avaloniaMember}, which Avalonia does not define.");
    }

    public static TheoryData<string, string, string, string> TextFormatSettings()
    {
        var data = new TheoryData<string, string, string, string>();

        foreach (var (winFormsEnum, winFormsMember, avaloniaType, avaloniaMember) in TextFormatCatalog.All)
        {
            data.Add(winFormsEnum, winFormsMember, avaloniaType, avaloniaMember);
        }

        return data;
    }

    /// <summary>
    /// Every <c>Graphics</c> call this converter translates, against Avalonia's
    /// <c>DrawingContext</c>.
    /// </summary>
    /// <remarks>
    /// The two frameworks do not agree on shape - WinForms names the operation twice
    /// (<c>DrawEllipse</c> outlines, <c>FillEllipse</c> fills) where Avalonia has one method taking
    /// both a brush and a pen - so the catalog's value is the call it emits, and this checks that
    /// call exists rather than the WinForms name.
    /// </remarks>
    [Theory]
    [MemberData(nameof(GraphicsCalls))]
    public void GraphicsCall_NamesAMethodDrawingContextHas(string winFormsMethod, string avaloniaMethod, int arity)
    {
        var context = AvaloniaMetadata.FindElement("DrawingContext");
        Assert.True(context is not null, "Avalonia has no DrawingContext type - something is very wrong.");

        Assert.True(
            AvaloniaMetadata.FindMethod(context!, avaloniaMethod, arity) is not null,
            $"GraphicsMemberCatalog translates Graphics.{winFormsMethod} into "
            + $"DrawingContext.{avaloniaMethod} with {arity} arguments, which does not exist.");
    }

    public static TheoryData<string, string, int> GraphicsCalls()
    {
        var data = new TheoryData<string, string, int>();

        foreach (var (methodName, call) in GraphicsMemberCatalog.All.OrderBy(e => e.MethodName, StringComparer.Ordinal))
        {
            var open = call.Format.IndexOf('(', StringComparison.Ordinal);
            var emitted = call.Format[..open];

            // Top-level commas only: the WinForms coordinates collapse into a Rect or a pair of
            // Points on the way across, so counting every comma would count theirs too.
            var depth = 0;
            var arity = 1;
            foreach (var character in call.Format[(open + 1)..^1])
            {
                depth += character switch { '(' => 1, ')' => -1, _ => 0 };
                if (character == ',' && depth == 0)
                {
                    arity++;
                }
            }

            data.Add(methodName, emitted, arity);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(WindowProperties))]
    public void WindowProperty_ExistsOnWindow(string winFormsPropertyName, string avaloniaPropertyName)
    {
        var window = AvaloniaMetadata.FindElement("Window");
        Assert.True(window is not null, "Avalonia has no Window type - something is very wrong.");

        Assert.True(
            AvaloniaMetadata.FindProperty(window!, avaloniaPropertyName) is not null,
            $"Form.{winFormsPropertyName} is translated to Window.{avaloniaPropertyName}, which "
            + "does not exist.");
    }

    [Theory]
    [MemberData(nameof(TimerMembers))]
    public void TimerMember_ExistsOnDispatcherTimer(string memberName, bool isMethod)
    {
        var timer = AvaloniaMetadata.FindElement("DispatcherTimer");
        Assert.True(timer is not null, "Avalonia has no DispatcherTimer type.");

        var found = isMethod
            ? AvaloniaMetadata.FindMethod(timer!, memberName, 0) is not null
            : AvaloniaMetadata.FindProperty(timer!, memberName) is not null;

        Assert.True(
            found,
            $"DispatcherTimerMemberCatalog names '{memberName}', which a DispatcherTimer does not "
            + "have - and this converter emits that field itself, so nothing else would catch it.");
    }

    [Theory]
    [MemberData(nameof(TrayIconProperties))]
    public void TrayIconProperty_ExistsOnTrayIcon(string winFormsPropertyName, string avaloniaPropertyName)
    {
        var trayIcon = AvaloniaMetadata.FindElement("TrayIcon");
        Assert.True(trayIcon is not null, "Avalonia has no TrayIcon type.");

        Assert.True(
            AvaloniaMetadata.FindProperty(trayIcon!, avaloniaPropertyName) is not null,
            $"NotifyIcon.{winFormsPropertyName} is translated to TrayIcon.{avaloniaPropertyName}, "
            + "which does not exist - and this converter emits that accessor itself.");
    }

    /// <summary>
    /// The Avalonia this suite reads has to be the Avalonia the generated projects compile
    /// against, or every check above is measuring the wrong API.
    /// </summary>
    [Fact]
    public void TestProject_ReferencesTheAvaloniaTheScaffolderWrites()
    {
        var csproj = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "tests", "WinFormsToAvalonia.Mapping.Tests",
                "WinFormsToAvalonia.Mapping.Tests.csproj"));

        AssertReferenced(csproj, "Avalonia", AvaloniaProjectScaffolder.AvaloniaVersion);

        foreach (var (package, version) in AvaloniaProjectScaffolder.ExtraPackageVersions)
        {
            // Only the Avalonia ones: the rest are plain .NET packages a component field needs,
            // and this suite has no opinion about them.
            if (package.StartsWith("Avalonia", StringComparison.Ordinal))
            {
                AssertReferenced(csproj, package, version);
            }
        }
    }

    public static TheoryData<string, string> StyleClaims()
    {
        var data = new TheoryData<string, string>();

        foreach (var (element, supported) in AvaloniaStylePropertySupport.AllEntries.OrderBy(e => e.AvaloniaElementName, StringComparer.Ordinal))
        {
            foreach (var group in Enum.GetValues<AvaloniaStyleProperties>())
            {
                if (group != AvaloniaStyleProperties.None && supported.HasFlag(group))
                {
                    foreach (var member in AvaloniaStylePropertySupport.MemberNamesOf(group))
                    {
                        data.Add(element, member);
                    }
                }
            }
        }

        return data;
    }

    public static TheoryData<string, string, string> ControlMethods()
    {
        var data = new TheoryData<string, string, string>();
        var registry = new ControlMappingRegistry();
        var seen = new HashSet<(string, string)>();

        foreach (var (typeName, methodName, method) in ControlMethodCatalog.AllEntries)
        {
            // A universal entry is a claim about every control, so it is checked against the base
            // - exactly the argument the generic event entries are held to.
            var element = typeName is null
                ? "Control"
                : registry.Map(new ControlModel { FieldName = "field1", ClrTypeName = typeName }) is
                    { Status: MappingStatus.Direct, AvaloniaElementName: { } mapped }
                    ? mapped
                    : null;

            if (element is not null && seen.Add((element, method.AvaloniaMemberName)))
            {
                data.Add($"{typeName ?? "Control"}.{methodName}()", element, method.AvaloniaMemberName);
            }
        }

        return data;
    }

    public static TheoryData<string, string, string> EventArgsMembers()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (argsType, memberName, member) in EventArgsMemberCatalog.AvaloniaEntries
            .OrderBy(e => e.ArgsTypeName, StringComparer.Ordinal)
            .ThenBy(e => e.MemberName, StringComparer.Ordinal))
        {
            // The format is what gets emitted; the member it reaches is the first name after the
            // args parameter, whether it is a property or a call.
            var reached = Regex.Match(member.Format, @"\{0\}\.(\w+)");
            if (reached.Success)
            {
                data.Add(argsType, reached.Groups[1].Value, $"{argsType}.{memberName}");
            }
        }

        return data;
    }

    public static TheoryData<string, string> WindowProperties()
    {
        var data = new TheoryData<string, string>();

        foreach (var (winFormsName, property) in WindowPropertyCatalog.AllEntries.OrderBy(e => e.WinFormsPropertyName, StringComparer.Ordinal))
        {
            data.Add(winFormsName, property.AvaloniaPropertyName);
        }

        return data;
    }

    public static TheoryData<string, bool> TimerMembers()
    {
        var data = new TheoryData<string, bool>();

        foreach (var (memberName, isMethod) in DispatcherTimerMemberCatalog.AllAvaloniaMembers.OrderBy(m => m.MemberName, StringComparer.Ordinal))
        {
            data.Add(memberName, isMethod);
        }

        return data;
    }

    public static TheoryData<string, string> TrayIconProperties()
    {
        var data = new TheoryData<string, string>();

        foreach (var (winFormsName, avaloniaName) in TrayIconMemberCatalog.AllEntries.OrderBy(e => e.WinFormsPropertyName, StringComparer.Ordinal))
        {
            data.Add(winFormsName, avaloniaName);
        }

        return data;
    }

    private static void AssertReferenced(string csproj, string package, string version)
    {
        var expected = $"<PackageReference Include=\"{package}\" Version=\"{version}\" />";
        Assert.True(
            csproj.Contains(expected, StringComparison.Ordinal),
            $"This suite must read the same '{package}' the generated projects use ({version}), "
            + "or it is checking a different API than the one that has to compile.");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WinFormsToAvalonia.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "Could not find the repository root from the test output directory.");
        return directory!.FullName;
    }

    /// <summary>
    /// The tray-icon events, against the real <c>TrayIcon</c>.
    /// </summary>
    /// <remarks>
    /// The event registry's own suite cannot check these: a <c>SubscribeInCode</c> mapping does
    /// not name a declaring type, so all three of its tests skip them. This is where that gap is
    /// closed.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TrayIconEvents))]
    public void TrayIconEvent_ExistsAndCarriesEventArgs(string winFormsEventName, string avaloniaEventName)
    {
        var trayIcon = AvaloniaMetadata.FindElement("TrayIcon");
        Assert.True(trayIcon is not null, "Avalonia does not define TrayIcon.");

        var declared = AvaloniaMetadata.FindEvent(trayIcon!, avaloniaEventName);

        Assert.True(
            declared is not null,
            $"TrayIconMemberCatalog maps '{winFormsEventName}' to TrayIcon.{avaloniaEventName}, which does not exist.");

        var invoke = declared!.EventHandlerType!.GetMethod("Invoke");
        Assert.Equal("EventArgs", invoke!.GetParameters()[1].ParameterType.Name);
    }

    public static TheoryData<string, string> TrayIconEvents()
    {
        var data = new TheoryData<string, string>();
        foreach (var (winFormsName, avaloniaName) in TrayIconMemberCatalog.AllEventEntries)
        {
            data.Add(winFormsName, avaloniaName);
        }

        return data;
    }

    /// <summary>
    /// Every attribute the Form's window chrome can become must be a real, non-obsolete property of
    /// <c>Window</c>. Not obsolete matters as much as present: <c>SystemDecorations</c> exists in
    /// Avalonia 12 and is <c>[Obsolete]</c>, which in a generated project that must build
    /// warning-free is as much a failure as a missing property.
    /// </summary>
    [Fact]
    public void FormWindowAttributes_AreLiveWindowProperties()
    {
        var window = AvaloniaMetadata.FindElement("Window")!;
        foreach (var name in FormWindowAttributeCatalog.AttributeNames)
        {
            var property = AvaloniaMetadata.FindProperty(window, name);
            Assert.True(property is not null, $"Window has no '{name}'.");
            Assert.DoesNotContain(property!.CustomAttributes, a => a.AttributeType.Name == "ObsoleteAttribute");
        }
    }

    [Fact]
    public void FormWindowAttributes_EnumValuesExistInAvalonia()
    {
        Assert.Subset(EnumMembers("WindowStartupLocation"), FormWindowAttributeCatalog.StartPositions.Values.ToHashSet());
        Assert.Contains(FormWindowAttributeCatalog.BorderlessDecorations, EnumMembers("WindowDecorations"));

        // The emitter writes WindowDecorations="None"; make sure that is the property's own type.
        var window = AvaloniaMetadata.FindElement("Window")!;
        Assert.Equal("WindowDecorations", AvaloniaMetadata.FindProperty(window, "WindowDecorations")!.PropertyType.Name);
        Assert.Equal("WindowStartupLocation", AvaloniaMetadata.FindProperty(window, "WindowStartupLocation")!.PropertyType.Name);
    }

    /// <summary>
    /// A Form's AcceptButton/CancelButton turn round into a property on the button itself, so every
    /// element the emitter may write it on must really have both.
    /// </summary>
    [Fact]
    public void DefaultButtonElements_CarryIsDefaultAndIsCancel()
    {
        foreach (var elementName in FormWindowAttributeCatalog.DefaultButtonElements)
        {
            var element = AvaloniaMetadata.FindElement(elementName);
            Assert.True(element is not null, $"Avalonia has no '{elementName}'.");
            foreach (var (_, attribute) in FormWindowAttributeCatalog.DialogKeyButtons)
            {
                Assert.True(
                    AvaloniaMetadata.FindProperty(element!, attribute) is { PropertyType.Name: "Boolean" },
                    $"'{elementName}' has no bool '{attribute}'.");
            }
        }
    }

    /// <summary>
    /// TabIndex/IsTabStop are written on every element that has a name, on the grounds that they
    /// are declared on <c>InputElement</c> - which every such element derives from.
    /// </summary>
    [Fact]
    public void TabNavigation_IsDeclaredOnInputElement()
    {
        var inputElement = AvaloniaMetadata.FindElement("InputElement")!;
        Assert.Equal("Int32", AvaloniaMetadata.FindProperty(inputElement, "TabIndex")!.PropertyType.Name);
        Assert.Equal("Boolean", AvaloniaMetadata.FindProperty(inputElement, "IsTabStop")!.PropertyType.Name);
    }

    [Fact]
    public void HitTestSupport_ElementsHaveABackgroundToSet()
    {
        foreach (var elementName in AvaloniaHitTestSupport.NeedBackgroundToBeHit)
        {
            var element = AvaloniaMetadata.FindElement(elementName);
            Assert.True(element is not null, $"Avalonia has no '{elementName}'.");
            Assert.True(
                AvaloniaMetadata.FindProperty(element!, "Background") is not null,
                $"'{elementName}' has no Background, so Background=\"Transparent\" would not compile.");
        }
    }

    [Fact]
    public void HitTestSupport_PointerEventsAndAllowDropAreReal()
    {
        var inputElement = AvaloniaMetadata.FindElement("InputElement")!;
        var dragDrop = AvaloniaMetadata.FindElement("DragDrop")!;

        foreach (var eventName in AvaloniaHitTestSupport.PointerEvents)
        {
            Assert.True(
                AvaloniaMetadata.FindEvent(inputElement, eventName) is not null
                    || AvaloniaMetadata.FindField(dragDrop, eventName + "Event") is not null,
                $"'{eventName}' is neither an InputElement event nor a DragDrop attached event.");
        }

        var (owner, property) = (AvaloniaHitTestSupport.AllowDropAttribute.Split('.')[0], AvaloniaHitTestSupport.AllowDropAttribute.Split('.')[1]);
        Assert.Equal("DragDrop", owner);
        Assert.NotNull(AvaloniaMetadata.FindAttachedProperty(dragDrop, property));
    }

    /// <summary>
    /// The formatters that translate one WinForms enum into another write their answers as
    /// literal strings, so each answer is held up against the Avalonia enum it has to parse as -
    /// every WinForms member each one knows, run through it.
    /// </summary>
    [Theory]
    [InlineData("AsImageStretch", "Stretch", new[] { "Normal", "CenterImage", "AutoSize", "StretchImage", "Zoom" })]
    [InlineData("AsTickPlacement", "TickPlacement", new[] { "None", "TopLeft", "BottomRight", "Both" })]
    [InlineData("AsHorizontalScrollBar", "ScrollBarVisibility", new[] { "None", "Horizontal", "Vertical", "Both" })]
    [InlineData("AsVerticalScrollBar", "ScrollBarVisibility", new[] { "None", "Horizontal", "Vertical", "Both" })]
    public void EnumFormatters_AnswerWithAvaloniaMembers(string formatterName, string avaloniaEnum, string[] winFormsMembers)
    {
        var formatter = typeof(PropertyValueFormatters).GetMethod(formatterName)!;
        var avaloniaMembers = EnumMembers(avaloniaEnum);

        foreach (var member in winFormsMembers)
        {
            var answer = (string?)formatter.Invoke(null, [new PropertyValue.EnumMembers([member])]);
            Assert.True(answer is not null, $"{formatterName} has no answer for '{member}'.");
            Assert.Contains(answer!, avaloniaMembers);
        }
    }

    [Fact]
    public void BoolFormatters_AnswerWithAvaloniaMembers()
    {
        Assert.Contains(PropertyValueFormatters.AsGridSelectionMode(new PropertyValue.Literal(true))!, EnumMembers("DataGridSelectionMode"));
        Assert.Contains(PropertyValueFormatters.AsGridSelectionMode(new PropertyValue.Literal(false))!, EnumMembers("DataGridSelectionMode"));
        Assert.Contains(PropertyValueFormatters.AsCheckToggleType(new PropertyValue.Literal(true))!, EnumMembers("MenuItemToggleType"));
        Assert.Contains(PropertyValueFormatters.AsMultilineWrapping(new PropertyValue.Literal(true))!, EnumMembers("TextWrapping"));
        Assert.Contains(PropertyValueFormatters.AsWordWrapping(new PropertyValue.Literal(false))!, EnumMembers("TextWrapping"));
    }

    /// <summary>A gesture string names its key by Avalonia's <c>Key</c> member name.</summary>
    [Fact]
    public void GestureKeys_AreAvaloniaKeys()
    {
        var keys = EnumMembers("Key");
        Assert.All(PropertyValueFormatters.GestureKeys.Values, key => Assert.Contains(key, keys));
    }

    /// <summary>
    /// Every options member the file-dialog builder sets has to be a settable property of the
    /// options type it is set on - a wrong one is a CS0117 in the generated project only.
    /// </summary>
    [Fact]
    public void FileDialogOptions_SetSettableMembersOfTheirOptionsType()
    {
        foreach (var (optionsType, members) in FileDialogOptions.EmittedMembers)
        {
            var type = AvaloniaMetadata.FindElement(optionsType);
            Assert.True(type is not null, $"Avalonia has no '{optionsType}'.");
            foreach (var member in members)
            {
                Assert.True(
                    AvaloniaMetadata.FindProperty(type!, member)?.SetMethod is { IsPublic: true },
                    $"'{optionsType}.{member}' is not a settable property.");
            }
        }

        // `new FilePickerFileType(name) { Patterns = [...] }`, and the awaited start folder.
        var fileType = AvaloniaMetadata.FindElement("FilePickerFileType")!;
        Assert.Contains(fileType.GetConstructors(), c => c.GetParameters() is [{ ParameterType.Name: "String" }]);
        Assert.NotNull(AvaloniaMetadata.FindProperty(fileType, "Patterns")?.SetMethod);
        Assert.Contains(
            AvaloniaMetadata.FindElement("StorageProviderExtensions")!.GetMethods(),
            m => m.Name == "TryGetFolderFromPathAsync" && m.GetParameters() is [_, { ParameterType.Name: "String" }]);
    }

    /// <summary>A WinForms ToolTip's delays land on each target as these two attached properties.</summary>
    [Fact]
    public void ToolTipTiming_IsAttachedToToolTip()
    {
        var toolTip = AvaloniaMetadata.FindElement("ToolTip")!;
        Assert.NotNull(AvaloniaMetadata.FindAttachedProperty(toolTip, "ShowDelay"));
        Assert.NotNull(AvaloniaMetadata.FindAttachedProperty(toolTip, "BetweenShowDelay"));
    }

    /// <summary>The modifier names a translated key comparison writes, and the routed event it tunnels.</summary>
    [Fact]
    public void KeyTranslation_UsesRealKeyModifiersAndRoutedEvents()
    {
        var modifiers = EnumMembers("KeyModifiers");
        Assert.Contains("Control", modifiers);
        Assert.Contains("Shift", modifiers);
        Assert.Contains("Alt", modifiers);
        Assert.Contains("None", modifiers);

        var inputElement = AvaloniaMetadata.FindElement("InputElement")!;
        Assert.NotNull(AvaloniaMetadata.FindField(inputElement, "KeyDownEvent"));
        Assert.NotNull(AvaloniaMetadata.FindField(inputElement, "KeyUpEvent"));
        Assert.Contains(EnumMembers("RoutingStrategies"), m => m == "Tunnel");
    }

    private static HashSet<string> EnumMembers(string enumName) =>
        AvaloniaMetadata.FindElement(enumName)!
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.Name)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// <c>FlowDirection</c> is the one styling-ish property the emitter writes with no per-element
    /// table behind it, on the grounds that it is declared on <c>Visual</c> - i.e. on everything.
    /// That grounds is a claim about Avalonia, so it gets checked like every other claim.
    /// </summary>
    [Fact]
    public void FlowDirection_IsDeclaredOnVisualAndSpellsBothDirections()
    {
        var visual = AvaloniaMetadata.FindElement("Visual");
        Assert.True(visual is not null, "Avalonia has no Visual type - something is very wrong.");

        Assert.True(
            AvaloniaMetadata.FindProperty(visual!, "FlowDirection") is not null,
            "FlowDirection is not on Visual, so emitting it on every element is not safe after all.");

        var flowDirection = AvaloniaMetadata.FindElement("FlowDirection");
        Assert.True(flowDirection is { IsEnum: true }, "Avalonia.Media.FlowDirection is not an enum.");

        var members = flowDirection!.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.Name)
            .ToList();

        Assert.Contains("LeftToRight", members);
        Assert.Contains("RightToLeft", members);
    }
    /// <summary>
    /// The public properties a bundled template's own EventArgs type declares, or null when no
    /// template declares a type of that name.
    /// </summary>
    private static IReadOnlySet<string>? BundledArgsTypeMembers(string argsTypeName)
    {
        foreach (var definition in FallbackControlCatalog.All.Values)
        {
            var templateSource = FallbackControlCatalog.ReadTemplateSource(definition.ResourceLogicalName);
            if (!Regex.IsMatch(templateSource, $@"class\s+{Regex.Escape(argsTypeName)}\b"))
            {
                continue;
            }

            return Regex.Matches(templateSource, @"public\s+[\w.<>?]+\s+(\w+)\s*\{\s*get;")
                .Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);
        }

        return null;
    }

}
