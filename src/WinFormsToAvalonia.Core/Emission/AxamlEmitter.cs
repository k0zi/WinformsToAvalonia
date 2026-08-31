using System.Globalization;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>
/// Emits one Form's .axaml: a Canvas-rooted Window document (per the project's fixed
/// layout-strategy decision - absolute Canvas.Left/Top/Width/Height from WinForms
/// Location/Size, no automatic Dock/Anchor-to-panel translation). Anchor/Dock are preserved
/// as both an XML comment and a `w2a:LayoutHint` attached property (see
/// Scaffolding/AvaloniaProjectScaffolder's LayoutHint.cs template) so a human - or a future
/// automated pass - can find them without re-parsing the original WinForms source.
/// </summary>
/// <remarks>
/// Direct-mapped controls are emitted as their real Avalonia element. Fallback-mapped
/// controls are emitted as a `controls:{FallbackTemplateKey}` element (the caller is
/// responsible for actually copying that template into the project via
/// FallbackControlResolver, using <see cref="AxamlEmissionResult.UsedFallbackKeys"/>) -
/// unless <paramref name="emitFallbackControls"/> is false (--no-fallback-controls strict
/// mode), in which case Fallback is treated the same as Unsupported. Unsupported controls
/// always become a `TODO` comment instead of a real element, since there is nothing to
/// reference.
/// </remarks>
public sealed class AxamlEmitter
{
    /// <summary>The root element has no ViewModel bindings competing for its attribute names.</summary>
    private static readonly IReadOnlySet<string> NoBoundAttributes = new HashSet<string>(StringComparer.Ordinal);

    private readonly ControlMappingRegistry _registry;

    public AxamlEmitter(ControlMappingRegistry registry)
    {
        _registry = registry;
    }

    /// <param name="artifactKind">
    /// The WinForms semantics: a Form takes its size from <c>ClientSize</c>, a UserControl from
    /// the designer's own <c>Size</c> assignment.
    /// </param>
    /// <param name="rootKind">
    /// The Avalonia element to root the document at, when it must differ from what
    /// <paramref name="artifactKind"/> would choose - a Form emitted as a <c>UserControl</c> so
    /// it can be shown under the browser's single-view lifetime. Null keeps the two in step.
    /// </param>
    /// <param name="userControlViews">
    /// Every UserControl the source project defines, so their <c>xmlns</c> prefixes can be
    /// declared up front on the root element (the prefixed elements themselves are emitted
    /// later, by the UserControlMapper entries the caller put in the registry).
    /// </param>
    public AxamlEmissionResult EmitView(
        FormModel formModel,
        string rootNamespace,
        string viewClassName,
        string viewModelClassName,
        FormMigrationPlan? plan = null,
        string relativeFolder = "",
        bool emitFallbackControls = true,
        WinFormsArtifactKind artifactKind = WinFormsArtifactKind.Form,
        IReadOnlyList<UserControlViewInfo>? userControlViews = null,
        ViewRootKind? rootKind = null)
    {
        var viewNamespace = NamingConventions.NamespaceOf($"{rootNamespace}.Views", relativeFolder);
        var viewModelNamespace = NamingConventions.NamespaceOf($"{rootNamespace}.ViewModels", relativeFolder);
        var isUserControl = artifactKind == WinFormsArtifactKind.UserControl;

        // The root element and the artifact kind are two different questions - see ViewRootKind.
        // Everything below that reads `isUserControl` is asking about WinForms semantics; the
        // ones asking what element we are actually writing go through `rootElementName`.
        var root = rootKind ?? artifactKind.DefaultRootKind();
        var rootElementName = root.ElementName();

        var builder = new AxamlDocumentBuilder();
        var state = new EmissionState(formModel, plan ?? FormMigrationPlan.Empty);

        builder.OpenElement(rootElementName);
        builder.Attribute("xmlns", "https://github.com/avaloniaui");
        builder.Attribute("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");
        builder.Attribute("xmlns:vm", $"using:{viewModelNamespace}");
        builder.Attribute("xmlns:controls", $"using:{rootNamespace}.Controls");
        builder.Attribute("xmlns:w2a", $"clr-namespace:{rootNamespace}.Controls.Generated");

        foreach (var (prefix, xmlnsValue) in DistinctUserControlNamespaces(userControlViews))
        {
            builder.Attribute($"xmlns:{prefix}", xmlnsValue);
        }

        // An xmlns some literal item element needs - `sys:String` for a collection of bare
        // strings. It has to be declared here rather than on the property element that uses it:
        // Avalonia's XAML compiler rejects an attribute on a property element outright
        // ("Attributes aren't allowed on element properties"). Conditional, so a view with no
        // such items keeps exactly the root attribute list it has always had.
        foreach (var (prefix, xmlnsValue) in RequiredItemNamespaces(formModel))
        {
            builder.Attribute($"xmlns:{prefix}", xmlnsValue);
        }

        builder.Attribute("xmlns:d", "http://schemas.microsoft.com/expression/blend/2008");
        builder.Attribute("xmlns:mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");
        builder.Attribute("mc:Ignorable", "d");
        builder.Attribute("x:Class", $"{viewNamespace}.{viewClassName}");
        builder.Attribute("x:DataType", $"vm:{viewModelClassName}");

        var sizeSourceProperty = isUserControl ? "Size" : "ClientSize";
        if (TryGetSize(formModel.FormProperties, sizeSourceProperty, out var formWidth, out var formHeight))
        {
            builder.Attribute("Width", FormatInt(formWidth));
            builder.Attribute("Height", FormatInt(formHeight));
        }

        if (root == ViewRootKind.Window)
        {
            builder.Attribute("Title", GetFormTitle(formModel, viewClassName));
        }

        // The Form's own BackColor/ForeColor/Font. Worth emitting on the root even though most
        // controls carry their own: a WinForms Form's Font is *inherited* by every child that
        // never overrode it, and Avalonia's font properties inherit the same way - so one
        // attribute here restores the typeface for a whole form's worth of controls.
        EmitVisualStyleAttributes(
            builder,
            formModel.FormProperties,
            AvaloniaStylePropertySupport.For(rootElementName),
            NoBoundAttributes);

        // Form-level events (Load/FormClosing/...) subscribe on the root element itself - except
        // the ones only a Window declares, which a UserControl-rooted View has to hand to the
        // wrapper Window that hosts it.
        var deferredWindowEvents = new List<(string AttributeName, string HandlerMethodName)>();
        foreach (var (attributeName, handlerMethodName) in state.Plan.XamlEventAttributesFor(null))
        {
            if (root == ViewRootKind.UserControl && WindowOnlyEventCatalog.IsWindowOnly(attributeName))
            {
                deferredWindowEvents.Add((attributeName, handlerMethodName));
                continue;
            }

            builder.Attribute(attributeName, handlerMethodName);
        }

        EmitCanvasLayoutStyles(builder, rootElementName);

        builder.OpenElement("Design.DataContext");
        builder.OpenElement($"vm:{viewModelClassName}");
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement("Canvas");
        foreach (var control in formModel.RootControls)
        {
            EmitControl(builder, control, emitFallbackControls, state, isItemOfParent: false);
        }

        builder.CloseElement();

        builder.CloseElement();

        return new AxamlEmissionResult(
            builder.ToString(),
            state.UsedFallbackKeys,
            state.RequiredNuGetPackages,
            state.DirectCount,
            state.FallbackCount,
            state.UnsupportedCount,
            state.Warnings,
            deferredWindowEvents,
            state.ConvertedElsewhereCount,
            state.ConvertedElsewhereNotes);
    }

    /// <summary>
    /// Makes the Canvas-everywhere layout mean what it says: a control ends up the size the
    /// WinForms designer recorded, not the size Avalonia's theme would rather it were.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia's default theme gives an editable control a <c>MinHeight</c> of 32 and generous
    /// padding, both aimed at touch. A designer's 23-pixel TextBox therefore rendered 32 pixels
    /// tall and swallowed whatever sat 26 pixels below it - in the sample, the LinkLabel above
    /// every text box on the first tab. With absolute coordinates and no layout panel to absorb
    /// the difference, that is a silent overlap on every form ever converted.
    /// </para>
    /// <para>
    /// Two setters rather than one, because dropping the minimum alone only trades the overlap
    /// for clipped text: the theme's padding does not fit a line of text into 23 pixels either.
    /// Both were measured on the headless platform - with them a 23-pixel TextBox, Button and
    /// ComboBox each render at exactly 23 pixels with their text fully visible.
    /// </para>
    /// <para>
    /// A style, not per-element attributes: a designer-set value on the element itself still
    /// wins over a style setter, so a control whose padding the WinForms designer really did
    /// specify keeps it.
    /// </para>
    /// </remarks>
    private static void EmitCanvasLayoutStyles(AxamlDocumentBuilder builder, string rootElementName)
    {
        builder.OpenElement($"{rootElementName}.Styles");

        builder.OpenElement("Style");
        builder.Attribute("Selector", "Canvas > :is(Control)");
        EmitSetter(builder, "MinWidth", "0");
        EmitSetter(builder, "MinHeight", "0");
        builder.CloseElement();

        builder.OpenElement("Style");
        builder.Attribute("Selector", "Canvas > :is(TemplatedControl)");
        EmitSetter(builder, "Padding", "4,1");
        builder.CloseElement();

        builder.CloseElement();
    }

    private static void EmitSetter(AxamlDocumentBuilder builder, string propertyName, string value)
    {
        builder.OpenElement("Setter");
        builder.Attribute("Property", propertyName);
        builder.Attribute("Value", value);
        builder.CloseElement();
    }

    /// <summary>
    /// One xmlns declaration per distinct UserControl-View namespace, keyed by the prefix the
    /// pipeline already assigned. Several UserControls in the same folder share one namespace
    /// (and therefore one prefix), and a duplicate xmlns attribute would be a XAML parse error.
    /// </summary>
    private static IEnumerable<(string Prefix, string XmlnsValue)> DistinctUserControlNamespaces(
        IReadOnlyList<UserControlViewInfo>? userControlViews) =>
        (userControlViews ?? [])
            .GroupBy(v => v.ViewNamespace, StringComparer.Ordinal)
            .Select(g => (g.First().XmlnsPrefix, g.First().XmlnsValue))
            .OrderBy(x => x.XmlnsPrefix, StringComparer.Ordinal);

    /// <param name="isItemOfParent">
    /// Whether this element is an <em>item</em> of its parent rather than a child positioned in
    /// it. Absolute position and size are what the Canvas-everywhere layout is built on, and they
    /// are emitted everywhere else - but on an item they land on the wrong thing entirely: a
    /// TabItem's Width/Height size its <em>tab</em>, and a TabPage's WinForms bounds are the tab
    /// control's client area rather than anything the user chose. Emitting them made every tab
    /// header 600px tall, which pushed all nine pages of the sample out of the window.
    /// </param>
    private void EmitControl(
        AxamlDocumentBuilder builder,
        ControlModel control,
        bool emitFallbackControls,
        EmissionState state,
        bool isItemOfParent)
    {
        var mapped = _registry.Map(control);
        var treatAsFallback = mapped.Status == MappingStatus.Fallback && emitFallbackControls;

        if (mapped.Status != MappingStatus.Direct && !treatAsFallback)
        {
            // Converted, just not as an element - a ContextMenuStrip is emitted onto its owner,
            // a ToolStripControlHost is replaced by what it hosted. Nothing is lost, so it is not
            // a TODO and not a warning; it belongs in the "converted differently" list.
            if (mapped.Disposition == UnsupportedDisposition.FeatureElsewhere)
            {
                state.ConvertedElsewhereCount++;
                state.ConvertedElsewhereNotes.AddRange(mapped.Warnings);
                return;
            }

            var reason = mapped.Status == MappingStatus.Fallback
                ? $"requires the bundled fallback control '{mapped.FallbackTemplateKey}' (skipped: --no-fallback-controls)"
                : "has no Avalonia mapping";
            var message = $"field '{control.FieldName}' ({control.ClrTypeName}) {reason}: {string.Join(" ", mapped.Warnings)}";
            builder.Comment($"TODO(Winforms2Avalonia): {message} - not emitted.");
            state.Warnings.Add(message);
            if (mapped.Status == MappingStatus.Fallback)
            {
                state.FallbackCount++;
            }
            else
            {
                state.UnsupportedCount++;
            }

            return;
        }

        EmitLayoutHintComment(builder, control);

        // A Direct mapping can still have lost something - a CheckedListBox's per-item check
        // state, say - and until now those warnings were dropped on the floor, because only the
        // not-emitted branch above read them. They belong in both places a human looks: the
        // conversion report (via state.Warnings, which reaches MIGRATION.md) and the AXAML itself.
        //
        // Direct only. A fallback's warning is boilerplate saying it is a fallback, which the
        // emitted `controls:XFallback` element name already says, once per instance.
        if (mapped.Status == MappingStatus.Direct)
        {
            foreach (var warning in mapped.Warnings)
            {
                builder.Comment($"TODO(Winforms2Avalonia): {warning}");
                state.Warnings.Add(warning);
            }
        }

        var elementName = treatAsFallback ? $"controls:{mapped.FallbackTemplateKey}" : mapped.AvaloniaElementName!;
        if (treatAsFallback)
        {
            state.UsedFallbackKeys.Add(mapped.FallbackTemplateKey!);
            state.FallbackCount++;
        }
        else
        {
            state.DirectCount++;
            if (mapped.RequiredNuGetPackage is { } package)
            {
                state.RequiredNuGetPackages.Add(package);
            }
        }

        builder.OpenElement(elementName);
        if (mapped.SupportsName)
        {
            builder.Attribute("x:Name", control.FieldName);
        }

        if (!isItemOfParent)
        {
            if (TryGetPoint(control.Properties, "Location", out var x, out var y))
            {
                builder.Attribute("Canvas.Left", FormatInt(x));
                builder.Attribute("Canvas.Top", FormatInt(y));
            }
            else
            {
                WarnIfPropertyUnresolved(control, "Location", "Point", state);
            }

            if (TryGetSize(control.Properties, "Size", out var width, out var height))
            {
                builder.Attribute("Width", FormatInt(width));
                builder.Attribute("Height", FormatInt(height));
            }
            else
            {
                WarnIfPropertyUnresolved(control, "Size", "Size", state);
            }
        }

        // A bound property's designer literal moves to the ViewModel property's initializer, so
        // emitting it here as well would produce a duplicate XML attribute for the same name.
        var boundProperties = mapped.SupportsName
            ? FilterBindableForTarget(control, mapped, state).ToList()
            : [];
        var boundAttributeNames = boundProperties.Select(p => p.AvaloniaPropertyName).ToHashSet(StringComparer.Ordinal);

        // Everything already written to this element. The universal passes below have to skip
        // these or they would write the same attribute twice, which is not a losing merge but a
        // malformed document: AxamlDocumentBuilder appends, and a duplicate XML attribute fails
        // to parse at all.
        var emittedAttributeNames = new HashSet<string>(boundAttributeNames, StringComparer.Ordinal);

        foreach (var (attributeName, value) in mapped.Attributes)
        {
            if (!boundAttributeNames.Contains(attributeName))
            {
                builder.Attribute(attributeName, value);
                emittedAttributeNames.Add(attributeName);
            }
        }

        EmitBindingsAndEvents(builder, control, mapped, boundProperties, state);
        EmitLayoutHintAttributes(builder, control);
        // Universal, not gated by WinForms type: an extender provider's
        // `this.toolTip1.SetToolTip(this.control1, ...)` is resolved onto the *target* control's
        // own Properties by DesignerSyntaxWalker, regardless of which provider field made the
        // call - so any control can carry one. Ordered, because attribute order is output order.
        foreach (var setter in ExtenderProviderCatalog.Setters)
        {
            // Every one of these is an attached property on StyledElement or Control, and
            // `SupportsName` is this project's existing flag for "this target is not one" - the
            // DataGrid column types. Setting one on a DataGridTextColumn is an AVLN2000 in the
            // generated project and nowhere else.
            if (!mapped.SupportsName || emittedAttributeNames.Contains(setter.AvaloniaAttributeName))
            {
                continue;
            }

            if (control.Properties.TryGetValue(setter.PropertyKey, out var providedValue)
                && setter.Format(providedValue) is { } providedText)
            {
                builder.Attribute(setter.AvaloniaAttributeName, providedText);
            }
        }

        EmitFlowDirection(builder, control, mapped, emittedAttributeNames, state);

        // Also universal, and for the same reason: BackColor/ForeColor/Font/Padding exist on
        // every WinForms Control, so they belong here rather than in each mapper's property
        // list. Which of them actually reach the AXAML is decided by the *target* element
        // (AvaloniaStylePropertySupport), not by the WinForms type.
        // A fallback's element name is its template key, which no Avalonia-element-keyed table
        // can answer for - so ask the table that is keyed by template instead. Same rule as the
        // handler-body side, and now literally the same method.
        var supportedStyles = treatAsFallback
            ? AvaloniaStylePropertySupport.ForFallbackTemplate(mapped.FallbackTemplateKey)
            : AvaloniaStylePropertySupport.For(elementName);

        EmitVisualStyleAttributes(builder, control.Properties, supportedStyles, emittedAttributeNames);

        // Every attribute is written by now, and that ordering is load-bearing rather than
        // tidy: AxamlDocumentBuilder.Attribute appends to the raw text, so the first child
        // element closes the parent's start tag and any attribute written afterwards lands
        // *outside* it - a document that does not parse. A ContextMenu is a child element, so
        // it has to come after the attribute passes above, not before them.
        EmitContextMenuIfPresent(builder, control, emitFallbackControls, state);

        foreach (var nested in mapped.NestedElements)
        {
            EmitElementSpec(builder, nested);
        }

        // A child element, so it belongs with the ContextMenu after every attribute pass - and it
        // replaces the literal items rather than joining them: a templated ListBox binds its rows.
        if (EmitCheckedListTemplate(builder, control, elementName, state))
        {
            // handled
        }
        else
        {
            // A fallback's element name is prefixed with its xmlns; the table is keyed on the bare
            // template key, exactly as FallbackControlMemberSupport is.
            EmitLiteralItems(builder, control, elementName, mapped.FallbackTemplateKey ?? elementName, state);
        }
        EmitIconIfPresent(builder, control, elementName, state);

        EmitContainerRegions(builder, control, elementName, emitFallbackControls, state);

        if (control.ClrTypeName == "SplitContainer")
        {
            EmitSplitContainerRegions(builder, control, emitFallbackControls, state);
        }
        else
        {
            var wrappers = control.Children.Count > 0 ? mapped.ChildWrapperElementNames : [];
            foreach (var wrapperElementName in wrappers)
            {
                builder.OpenElement(wrapperElementName);
            }

            // Whatever the children actually land in: the innermost wrapper if the mapper asked
            // for one (TabItem's Canvas, DataGrid.Columns), otherwise this element itself.
            var childParent = wrappers.Count > 0 ? wrappers[^1] : elementName;

            foreach (var child in control.Children)
            {
                EmitControl(builder, child, emitFallbackControls, state, HostsItems.Contains(childParent));
            }

            for (var i = 0; i < wrappers.Count; i++)
            {
                builder.CloseElement();
            }
        }

        builder.CloseElement();
    }

    /// <summary>
    /// Emits a mapper-prescribed <see cref="AxamlElementSpec"/> subtree (a DataGridTemplateColumn's
    /// CellTemplate, a MenuFlyout shell, ...) - fixed content the WinForms designer never records,
    /// so it comes from the mapping table rather than from the ControlModel.
    /// </summary>
    private static void EmitElementSpec(AxamlDocumentBuilder builder, AxamlElementSpec spec)
    {
        builder.OpenElement(spec.ElementName);
        foreach (var (attributeName, value) in spec.Attributes)
        {
            builder.Attribute(attributeName, value);
        }

        if (spec.Comment is { } comment)
        {
            builder.Comment(comment);
        }

        foreach (var child in spec.Children)
        {
            EmitElementSpec(builder, child);
        }

        builder.CloseElement();
    }

    /// <summary>
    /// Wires this element to the migration plan: two-way {Binding}s for the properties a promoted
    /// ViewModel command drives, a Command binding when this control's Click became a
    /// [RelayCommand], and an attribute per event handler that stayed in code-behind.
    /// </summary>
    /// <remarks>
    /// Only Direct-mapped, nameable elements are wired. A Fallback element is one of the tool's
    /// own bundled controls, which does not necessarily expose the Avalonia event or Command
    /// property the mapping table names - emitting the attribute anyway would fail the Avalonia
    /// XAML compiler (AVLN2000) and break the generated build, so the handler method is still
    /// emitted but the subscription is reported as a warning instead.
    /// </remarks>
    /// <summary>
    /// Which column of a planned Details ListView this <c>ColumnHeader</c> field is, or null when
    /// it belongs to no such ListView.
    /// </summary>
    private static int? ListViewColumnIndex(string fieldName, EmissionState state)
    {
        foreach (var rows in state.Plan.ListViewRows)
        {
            for (var i = 0; i < rows.ColumnFieldNames.Count; i++)
            {
                if (string.Equals(rows.ColumnFieldNames[i], fieldName, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        return null;
    }

    private static void EmitBindingsAndEvents(
        AxamlDocumentBuilder builder,
        ControlModel control,
        MappedControl mapped,
        IReadOnlyList<BoundPropertyPlan> boundProperties,
        EmissionState state)
    {
        var isWireable = mapped.Status == MappingStatus.Direct && mapped.SupportsName;

        foreach (var bound in boundProperties)
        {
            builder.Attribute(bound.AvaloniaPropertyName, $"{{Binding {bound.ViewModelPropertyName}, Mode=TwoWay}}");
        }

        // Not through the loop above: that one hardcodes Mode=TwoWay, which an ItemsSource is not.
        foreach (var collection in state.Plan.DataSourceBindings.Where(
                     b => string.Equals(b.ControlFieldName, control.FieldName, StringComparison.Ordinal)))
        {
            builder.Attribute("ItemsSource", $"{{Binding {collection.ViewModelPropertyName}}}");
        }

        foreach (var rows in state.Plan.ListViewRows.Where(
                     r => string.Equals(r.ControlFieldName, control.FieldName, StringComparison.Ordinal)))
        {
            builder.Attribute("ItemsSource", $"{{Binding {rows.ViewModelPropertyName}}}");
        }

        foreach (var checkedList in state.Plan.CheckedLists.Where(
                     c => string.Equals(c.ControlFieldName, control.FieldName, StringComparison.Ordinal)))
        {
            builder.Attribute("ItemsSource", $"{{Binding {checkedList.ViewModelPropertyName}}}");
        }

        // The navigator's Position comes through the bound-property loop above, like any other
        // two-way binding. These two do not: Count is a read-only path into the collection itself,
        // and SelectedIndex belongs to a *different* element than the property was planned on -
        // which is the whole point, since BindingSource.Position was the one number both showed.
        foreach (var navigator in state.Plan.BindingNavigators)
        {
            if (string.Equals(navigator.ControlFieldName, control.FieldName, StringComparison.Ordinal))
            {
                builder.Attribute("Count", $"{{Binding {navigator.CollectionPropertyName}.Count}}");
            }

            if (string.Equals(navigator.BoundControlFieldName, control.FieldName, StringComparison.Ordinal))
            {
                builder.Attribute("SelectedIndex", $"{{Binding {navigator.PositionPropertyName}, Mode=TwoWay}}");
            }
        }

        // A ColumnHeader carries a caption and nothing else, so its DataGridTextColumn used to be
        // emitted with no Binding at all - a column that can never show a cell, in a grid nothing
        // could ever fill. Its index in the owning ListView's column list *is* its binding, since
        // a row is that ListViewItem's sub-item texts in order. Reflection rather than compiled,
        // for the same reason the DataGridView columns use it: the row type is not the DataContext.
        if (ListViewColumnIndex(control.FieldName, state) is { } columnIndex)
        {
            builder.Attribute("Binding", $"{{ReflectionBinding [{columnIndex}]}}");
        }

        if (state.Plan.CommandPropertyFor(control.FieldName) is { } commandProperty)
        {
            builder.Attribute("Command", $"{{Binding {commandProperty}}}");
        }

        foreach (var (attributeName, handlerMethodName) in state.Plan.XamlEventAttributesFor(control.FieldName))
        {
            // A bundled template is a real Avalonia control with a real x:Name, so the events it
            // inherits from Control can be wired like any other element's. Only those: a property
            // differs template by template, and so does an event a template adds itself.
            if (isWireable
                || (mapped is { Status: MappingStatus.Fallback, SupportsName: true }
                    && FallbackControlMemberSupport.ExposesEvent(mapped.FallbackTemplateKey, attributeName)))
            {
                builder.Attribute(attributeName, handlerMethodName);
            }
            else
            {
                state.Warnings.Add(
                    $"field '{control.FieldName}' ({control.ClrTypeName}) is not a direct Avalonia element, so its " +
                    $"'{attributeName}' handler '{handlerMethodName}' could not be subscribed - wire it up by hand.");
            }
        }
    }

    /// <summary>
    /// Universal, not gated by WinForms type: `this.someControl.ContextMenuStrip =
    /// this.contextMenuStrip1;` is resolved onto the target control's own Properties by
    /// DesignerSyntaxWalker (as a <see cref="PropertyValue.ControlReference"/>), regardless
    /// of which control it targets - so any control can carry it. Emitted as a nested
    /// `&lt;Control.ContextMenu&gt;` property element (not a plain attribute like ToolTip.Tip,
    /// since it wraps child MenuItem/Separator elements), reusing the same recursive
    /// EmitControl already used for regular children so nested DropDownItems work for free.
    /// </summary>
    private void EmitContextMenuIfPresent(AxamlDocumentBuilder builder, ControlModel control, bool emitFallbackControls, EmissionState state)
    {
        if (control.Properties.TryGetValue("ContextMenuStrip", out var value)
            && value is PropertyValue.ControlReference(var fieldName)
            && state.FormModel.Controls.TryGetValue(fieldName, out var menuControl)
            && menuControl.ClrTypeName == "ContextMenuStrip")
        {
            builder.OpenElement("Control.ContextMenu");
            builder.OpenElement("ContextMenu");
            foreach (var item in menuControl.Children)
            {
                EmitControl(builder, item, emitFallbackControls, state, isItemOfParent: true);
            }

            builder.CloseElement();
            builder.CloseElement();
        }
    }

    /// <summary>
    /// A container's named sub-regions, as XAML property elements holding the bundled panel each
    /// one is - a ToolStripContainer's <c>ContentPanel</c> and its four strips.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Property-element syntax rather than plain children, because these are not children: a
    /// ToolStripContainer has none of its own, and which region a control went into is the whole
    /// of what the designer recorded. The template's region properties are settable for exactly
    /// this reason - a get-only one could not receive the panel.
    /// </para>
    /// <para>
    /// A child element, so it belongs after every attribute pass; and only regions that actually
    /// hold something are emitted, so a container the designer left empty produces the same AXAML
    /// it always did.
    /// </para>
    /// </remarks>
    private void EmitContainerRegions(
        AxamlDocumentBuilder builder, ControlModel control, string elementName, bool emitFallbackControls, EmissionState state)
    {
        if (control.RegionChildren.Count == 0)
        {
            return;
        }

        foreach (var (regionName, templateKey) in ToolStripContainerRegionCatalog.All)
        {
            if (!control.RegionChildren.TryGetValue(regionName, out var children) || children.Count == 0)
            {
                continue;
            }

            state.UsedFallbackKeys.Add(templateKey);

            builder.OpenElement($"{elementName}.{regionName}");
            builder.OpenElement($"controls:{templateKey}");

            foreach (var child in children)
            {
                EmitControl(builder, child, emitFallbackControls, state, isItemOfParent: false);
            }

            builder.CloseElement();
            builder.CloseElement();
        }
    }

    /// <summary>
    /// SplitContainer's children live in <see cref="ControlModel.Panel1Children"/>/
    /// <see cref="ControlModel.Panel2Children"/>, not the regular Children list (see
    /// ControlGraphBuilder) - so it needs its own emission instead of the generic
    /// single-wrapper Children loop every other control uses. The Grid element itself was
    /// already opened by the caller (EmitControl, same as for every other control - mapped
    /// AvaloniaElementName is "Grid"); this only adds the Row/ColumnDefinitions attribute
    /// (must happen before any child element is opened) and emits the two Canvas regions +
    /// GridSplitter as its children, either side by side (Orientation=Vertical, WinForms'
    /// default) or stacked (Orientation=Horizontal).
    /// </summary>
    private void EmitSplitContainerRegions(AxamlDocumentBuilder builder, ControlModel control, bool emitFallbackControls, EmissionState state)
    {
        var isHorizontal = control.Properties.TryGetValue("Orientation", out var orientation)
            && orientation is PropertyValue.EnumMembers { MemberNames: var members }
            && members.Contains("Horizontal");

        builder.Attribute(isHorizontal ? "RowDefinitions" : "ColumnDefinitions", "*,Auto,*");

        EmitSplitContainerRegion(builder, control.Panel1Children, isHorizontal, 0, emitFallbackControls, state);

        builder.OpenElement("GridSplitter");
        builder.Attribute(isHorizontal ? "Grid.Row" : "Grid.Column", "1");
        builder.Attribute(isHorizontal ? "Height" : "Width", "4");
        builder.Attribute("ResizeDirection", isHorizontal ? "Rows" : "Columns");
        builder.CloseElement();

        EmitSplitContainerRegion(builder, control.Panel2Children, isHorizontal, 2, emitFallbackControls, state);
    }

    private void EmitSplitContainerRegion(
        AxamlDocumentBuilder builder, List<ControlModel> children, bool isHorizontal, int gridIndex, bool emitFallbackControls, EmissionState state)
    {
        builder.OpenElement("Canvas");
        builder.Attribute(isHorizontal ? "Grid.Row" : "Grid.Column", FormatInt(gridIndex));

        foreach (var child in children)
        {
            EmitControl(builder, child, emitFallbackControls, state, isItemOfParent: false);
        }

        builder.CloseElement();
    }

    /// <summary>
    /// The target elements whose children are items rather than positioned content.
    /// </summary>
    /// <remarks>
    /// Keyed on the Avalonia element, like the other emission tables, and deliberately a list of
    /// what is <em>not</em> a panel rather than a list of what is: every WinForms container maps
    /// to a Canvas or to a bundled template that hosts one, so absolute layout is right for all
    /// of them and the exceptions are countable. Adding a mapper whose target holds items -
    /// rather than lays children out - means adding it here.
    /// </remarks>
    private static readonly HashSet<string> HostsItems = new(StringComparer.Ordinal)
    {
        "TabControl",
        "Menu",
        "MenuItem",
        "ContextMenu",
        "DataGrid.Columns",
    };

    private sealed class EmissionState
    {
        public EmissionState(FormModel formModel, FormMigrationPlan plan)
        {
            FormModel = formModel;
            Plan = plan;
        }

        public FormModel FormModel { get; }

        public FormMigrationPlan Plan { get; }

        public HashSet<string> UsedFallbackKeys { get; } = new(StringComparer.Ordinal);

        public HashSet<string> RequiredNuGetPackages { get; } = new(StringComparer.Ordinal);

        public List<string> Warnings { get; } = [];

        public int DirectCount { get; set; }

        public int FallbackCount { get; set; }

        public int UnsupportedCount { get; set; }

        /// <summary>Controls whose feature was converted somewhere other than an element.</summary>
        public int ConvertedElsewhereCount { get; set; }

        public List<string> ConvertedElsewhereNotes { get; } = [];
    }

    private static void EmitLayoutHintComment(AxamlDocumentBuilder builder, ControlModel control)
    {
        var parts = new List<string>();

        if (TryFormatEnumMembers(control, "Anchor", out var anchor))
        {
            parts.Add($"Anchor={anchor}");
        }

        if (TryFormatEnumMembers(control, "Dock", out var dock))
        {
            parts.Add($"Dock={dock}");
        }

        if (parts.Count > 0)
        {
            builder.Comment("WinForms layout: " + string.Join(" ", parts));
        }
    }

    /// <summary>
    /// The plan's bindings for this control, minus any the *target element* cannot carry.
    /// </summary>
    /// <remarks>
    /// Only fallback controls can lose anything here: a Direct-mapped element is the real Avalonia
    /// control, so the catalog's answer holds. For a fallback the binding is only emitted when the
    /// bundled template demonstrably exposes that property
    /// (<see cref="FallbackControlMemberSupport"/>) - otherwise it would be an AVLN2000 in the
    /// generated project. A dropped binding is reported, since the ViewModel property behind it
    /// stays and would silently do nothing.
    /// </remarks>
    private static IEnumerable<BoundPropertyPlan> FilterBindableForTarget(
        ControlModel control, MappedControl mapped, EmissionState state)
    {
        foreach (var bound in state.Plan.BoundPropertiesFor(control.FieldName))
        {
            // A mapper that narrowed the target element to one of several says so here - the
            // catalog only knows the WinForms type, which is not enough to answer for both.
            if (mapped.UnreachableBindableMembers.Contains(bound.AvaloniaPropertyName, StringComparer.Ordinal))
            {
                state.Warnings.Add(
                    $"field '{control.FieldName}' ({control.ClrTypeName}) maps to a " +
                    $"'{mapped.AvaloniaElementName}', which has no '{bound.AvaloniaPropertyName}' - the " +
                    $"ViewModel's '{bound.ViewModelPropertyName}' is generated but not bound to anything. " +
                    "Wire it up by hand.");
                continue;
            }

            if (mapped.Status != MappingStatus.Fallback
                || FallbackControlMemberSupport.Exposes(mapped.FallbackTemplateKey, bound.AvaloniaPropertyName))
            {
                yield return bound;
                continue;
            }

            state.Warnings.Add(
                $"field '{control.FieldName}' ({control.ClrTypeName}) maps to the bundled " +
                $"'{mapped.FallbackTemplateKey}', which has no '{bound.AvaloniaPropertyName}' - the ViewModel's " +
                $"'{bound.ViewModelPropertyName}' is generated but not bound to anything. Wire it up by hand.");
        }
    }

    /// <summary>
    /// The image a control carried - from its own .resx entry, or from an ImageList by
    /// <c>ImageIndex</c> - emitted into the one slot Avalonia has for a per-item icon.
    /// </summary>
    /// <remarks>
    /// <para>
    /// That slot is <c>MenuItem.Icon</c>, and only <c>MenuItem.Icon</c>: Avalonia's
    /// <c>TreeViewItem</c>, <c>ListBoxItem</c> and <c>TabItem</c> have no icon property at all, so
    /// the WinForms shape of "text plus a small picture" would have to be invented there as a
    /// panel inside the header - a layout decision this converter does not get to make. A Button
    /// is the same story from the other side: its Content already holds the text.
    /// </para>
    /// <para>
    /// Gated on the target element rather than the WinForms type, like the other two emission
    /// tables. <c>PictureBox</c> is excluded because its own mapper already turns
    /// <c>Image</c> into <c>Image.Source</c>; anything else is reported, naming the asset that
    /// was written so wiring it up by hand is a one-liner rather than a search.
    /// </para>
    /// </remarks>
    private static void EmitIconIfPresent(
        AxamlDocumentBuilder builder, ControlModel control, string elementName, EmissionState state)
    {
        if (!control.Properties.TryGetValue("Image", out var value)
            || PropertyValueFormatters.AsText(value) is not { } assetPath
            || !assetPath.StartsWith("/Assets/", StringComparison.Ordinal))
        {
            return;
        }

        if (elementName == "Image")
        {
            return;
        }

        if (elementName != "MenuItem")
        {
            state.Warnings.Add(
                $"field '{control.FieldName}' ({control.ClrTypeName}) has an image, extracted to '{assetPath[1..]}', but " +
                $"'{elementName}' has no icon property in Avalonia - place it by hand if you want it shown.");
            return;
        }

        builder.OpenElement("MenuItem.Icon");
        builder.OpenElement("Image");
        builder.Attribute("Source", assetPath);
        builder.CloseElement();
        builder.CloseElement();
    }

    /// <summary>
    /// The literal entries the designer put in a control's <c>Items</c> collection, emitted as
    /// real item elements so a converted ComboBox/ListBox opens with its list already filled.
    /// </summary>
    /// <remarks>
    /// Gated on the target element (<see cref="AvaloniaItemsSupport"/>), not the WinForms type:
    /// an item element the target does not accept is an AVLN error in the generated project.
    /// When a control has entries the target cannot take, they are reported rather than dropped
    /// silently - that list is usually visible content the user would notice missing.
    /// </remarks>
    /// <summary>
    /// A WinForms <c>RightToLeft</c> as Avalonia's <c>FlowDirection</c> - but only where it means
    /// the same thing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property is declared on <c>Visual</c>, so unlike Background/Foreground/Font it needs no
    /// per-element table: every element this converter emits has it, except the DataGrid column
    /// types, which are not Visuals at all - the same <c>SupportsName</c> test the extender
    /// providers use.
    /// </para>
    /// <para>
    /// <b>The gate is the point.</b> Avalonia's FlowDirection mirrors the whole subtree when it
    /// differs from the parent's, and this converter lays everything out with absolute
    /// <c>Canvas.Left</c>. So on a container it would silently flip every child's position -
    /// which WinForms only does for <c>RightToLeftLayout</c>, not for <c>RightToLeft</c> alone
    /// (that right-aligns text and moves scrollbars, and moves nothing). Emitted on a leaf, where
    /// the two agree; reported on a container, where they do not.
    /// </para>
    /// </remarks>
    private static void EmitFlowDirection(
        AxamlDocumentBuilder builder,
        ControlModel control,
        MappedControl mapped,
        IReadOnlySet<string> emittedAttributeNames,
        EmissionState state)
    {
        if (!mapped.SupportsName
            || emittedAttributeNames.Contains("FlowDirection")
            || !control.Properties.TryGetValue("RightToLeft", out var value)
            || PropertyValueFormatters.AsFlowDirection(value) is not { } flowDirection)
        {
            return;
        }

        var hasPositionedChildren =
            control.Children.Count > 0 || control.Panel1Children.Count > 0 || control.Panel2Children.Count > 0;

        if (hasPositionedChildren)
        {
            if (flowDirection == "RightToLeft")
            {
                state.Warnings.Add(
                    $"field '{control.FieldName}' ({control.ClrTypeName}) sets RightToLeft, but Avalonia's "
                    + "FlowDirection also mirrors layout and this conversion positions children with absolute "
                    + "Canvas coordinates - it is not emitted on a container. Set FlowDirection by hand if you "
                    + "want the whole subtree mirrored.");
            }

            return;
        }

        builder.Attribute("FlowDirection", flowDirection);
    }

    /// <summary>
    /// The xmlns declarations this form's literal item elements need, ordered and de-duplicated.
    /// </summary>
    /// <remarks>
    /// A pre-scan rather than a fact discovered while emitting, because the root element's
    /// attributes are written before any control is mapped - the same reason
    /// <see cref="DistinctUserControlNamespaces"/> exists.
    /// </remarks>
    private IEnumerable<(string Prefix, string Value)> RequiredItemNamespaces(FormModel formModel) =>
        formModel.Controls.Values
            .Where(c => c.LiteralItems.Count > 0)
            .Select(c => _registry.Map(c))
            .Select(m => AvaloniaItemsSupport.For(m.FallbackTemplateKey ?? m.AvaloniaElementName))
            .OfType<AvaloniaItemsTarget>()
            .Where(t => t.XmlnsPrefix is not null && t.XmlnsValue is not null)
            .Select(t => (t.XmlnsPrefix!, t.XmlnsValue!))
            .Distinct()
            .OrderBy(x => x.Item1, StringComparer.Ordinal);

    /// <summary>
    /// The <c>ItemTemplate</c> that gives a converted <c>CheckedListBox</c> its tick boxes.
    /// </summary>
    /// <remarks>
    /// The one place this converter emits a <c>DataTemplate</c>. It is not a layout decision it
    /// invented: a CheckedListBox row *is* a caption and a tick, and Avalonia has no control that
    /// says so - only a template. <c>Mode=TwoWay</c> on the tick because clicking it is the whole
    /// point, and the row type raises change notifications so a handler writing it moves the box.
    /// </remarks>
    private static bool EmitCheckedListTemplate(
        AxamlDocumentBuilder builder, ControlModel control, string elementName, EmissionState state)
    {
        if (state.Plan.CheckedLists.FirstOrDefault(c =>
                string.Equals(c.ControlFieldName, control.FieldName, StringComparison.Ordinal)) is null)
        {
            return false;
        }

        builder.OpenElement($"{elementName}.ItemTemplate");
        builder.OpenElement("DataTemplate");
        builder.OpenElement("CheckBox");
        builder.Attribute("IsChecked", "{Binding IsChecked, Mode=TwoWay}");
        builder.Attribute("Content", "{Binding Text}");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
        return true;
    }

    private static void EmitLiteralItems(
        AxamlDocumentBuilder builder, ControlModel control, string elementName, string itemsTarget, EmissionState state)
    {
        if (control.LiteralItems.Count == 0)
        {
            return;
        }

        if (AvaloniaItemsSupport.For(itemsTarget) is not { } items)
        {
            state.Warnings.Add(
                $"field '{control.FieldName}' ({control.ClrTypeName}) has {control.LiteralItems.Count} designer-declared " +
                $"item(s), but '{elementName}' does not take item elements - add them by hand, or bind ItemsSource.");
            return;
        }

        // A named collection property gets a wrapper element; direct children do not.
        if (items.CollectionPropertyName is { } collectionProperty)
        {
            builder.OpenElement($"{elementName}.{collectionProperty}");
        }

        foreach (var item in control.LiteralItems)
        {
            if (items.ItemContentAttributeName is { } contentAttribute)
            {
                builder.OpenElement(items.ItemElementName);
                builder.Attribute(contentAttribute, item);
                builder.CloseElement();
                continue;
            }

            builder.TextElement(items.ItemElementName, item);
        }

        if (items.CollectionPropertyName is not null)
        {
            builder.CloseElement();
        }
    }

    /// <summary>
    /// The WinForms styling properties every <c>Control</c> has, emitted as whichever Avalonia
    /// attributes the *target* element is able to carry (see
    /// <see cref="AvaloniaStylePropertySupport"/>). Values the formatters cannot resolve to a
    /// literal - and every attribute already claimed by a two-way {Binding} - are skipped
    /// rather than guessed at, since a duplicate or unparseable attribute would break the
    /// generated project's build.
    /// </summary>
    private static void EmitVisualStyleAttributes(
        AxamlDocumentBuilder builder,
        IReadOnlyDictionary<string, PropertyValue> properties,
        AvaloniaStyleProperties supported,
        IReadOnlySet<string> boundAttributeNames)
    {
        if (supported == AvaloniaStyleProperties.None)
        {
            return;
        }

        void Emit(string attributeName, string? value)
        {
            if (value is not null && !boundAttributeNames.Contains(attributeName))
            {
                builder.Attribute(attributeName, value);
            }
        }

        if (supported.HasFlag(AvaloniaStyleProperties.Background)
            && properties.TryGetValue("BackColor", out var backColor))
        {
            Emit("Background", PropertyValueFormatters.AsBrush(backColor));
        }

        if (supported.HasFlag(AvaloniaStyleProperties.Foreground)
            && properties.TryGetValue("ForeColor", out var foreColor))
        {
            Emit("Foreground", PropertyValueFormatters.AsBrush(foreColor));
        }

        if (supported.HasFlag(AvaloniaStyleProperties.Font)
            && properties.TryGetValue("Font", out var font))
        {
            Emit("FontFamily", PropertyValueFormatters.AsFontFamily(font));
            Emit("FontSize", PropertyValueFormatters.AsFontSize(font));
            Emit("FontWeight", PropertyValueFormatters.AsFontWeight(font));
            Emit("FontStyle", PropertyValueFormatters.AsFontStyle(font));

            if (supported.HasFlag(AvaloniaStyleProperties.TextDecorations))
            {
                Emit("TextDecorations", PropertyValueFormatters.AsTextDecorations(font));
            }
        }

        if (supported.HasFlag(AvaloniaStyleProperties.Padding)
            && properties.TryGetValue("Padding", out var padding))
        {
            Emit("Padding", PropertyValueFormatters.AsThickness(padding));
        }
    }

    private static void EmitLayoutHintAttributes(AxamlDocumentBuilder builder, ControlModel control)
    {
        if (TryFormatEnumMembers(control, "Anchor", out var anchor))
        {
            builder.Attribute("w2a:LayoutHint.Anchor", anchor);
        }

        if (TryFormatEnumMembers(control, "Dock", out var dock))
        {
            builder.Attribute("w2a:LayoutHint.Dock", dock);
        }
    }

    private static bool TryFormatEnumMembers(ControlModel control, string propertyName, out string formatted)
    {
        if (control.Properties.TryGetValue(propertyName, out var value) && value is PropertyValue.EnumMembers members)
        {
            formatted = string.Join(",", members.MemberNames);
            return true;
        }

        formatted = "";
        return false;
    }

    /// <summary>
    /// Only fires when Designer.cs actually assigned the property but ExpressionEvaluator
    /// couldn't resolve it to a literal Point/Size (a computed expression, a field
    /// reference, ...) - never for a control that simply never had a Location/Size
    /// assignment at all (e.g. an AutoSize Label), which is correct as-is, not a bug.
    /// </summary>
    private static void WarnIfPropertyUnresolved(ControlModel control, string propertyName, string expectedShape, EmissionState state)
    {
        if (control.Properties.TryGetValue(propertyName, out var value))
        {
            var raw = value is PropertyValue.Unresolved unresolved ? unresolved.RawExpression : value.ToString();
            state.Warnings.Add($"field '{control.FieldName}' ({control.ClrTypeName}): {propertyName} expression '{raw}' could not be resolved to a literal {expectedShape} - not emitted, control may be mispositioned/mis-sized.");
        }
    }

    private static bool TryGetPoint(IReadOnlyDictionary<string, PropertyValue> properties, string propertyName, out int x, out int y)
    {
        if (properties.TryGetValue(propertyName, out var value) && value is PropertyValue.PointValue point)
        {
            x = point.X;
            y = point.Y;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    internal static bool TryGetSize(IReadOnlyDictionary<string, PropertyValue> properties, string propertyName, out int width, out int height)
    {
        if (properties.TryGetValue(propertyName, out var value) && value is PropertyValue.SizeValue size)
        {
            width = size.Width;
            height = size.Height;
            return true;
        }

        width = 0;
        height = 0;
        return false;
    }

    internal static string GetFormTitle(FormModel formModel, string fallback)
    {
        if (formModel.FormProperties.TryGetValue("Text", out var value)
            && value is PropertyValue.Literal { Value: string text }
            && !string.IsNullOrEmpty(text))
        {
            return text;
        }

        return fallback;
    }

    internal static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);
}
