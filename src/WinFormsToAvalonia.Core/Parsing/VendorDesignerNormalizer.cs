using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Mapping.Vendors;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Rebuilds what a vendor designer spreads over several objects into the WinForms shapes the rest
/// of the conversion already understands, then converts every vendor control into the WinForms
/// control it is.
/// </summary>
/// <remarks>
/// <para>
/// A model rewrite between the walk and <see cref="ControlGraphBuilder"/>, like
/// <c>PaintLayerInserter</c>: nothing downstream learns a vendor's vocabulary. A DevExpress grid's
/// columns hang off a separate GridView; its bars are links into a BarManager's item list, placed by
/// dock controls; a ribbon is pages of groups of links; a layout control keeps each caption on an
/// item beside the control. Afterwards each is a DataGridView with columns, a MenuStrip/ToolStrip/
/// StatusStrip with items, a Panel with labels.
/// </para>
/// <para>
/// Three rules hold throughout. A value it carries is <em>moved</em>, not copied - the source model
/// either goes away or loses the key, because reads made while parsing are forgotten before
/// planning and could not mark it. A model it removes takes its edges with it, and anything it
/// cannot place is reported, never left in a tree that silently drops it. And what is gone - a
/// GridView's options, a removed model's event handlers - is said, here, in the walk's warnings.
/// </para>
/// </remarks>
public static class VendorDesignerNormalizer
{
    private const string BarButtonItem = "DevExpress.XtraBars.BarButtonItem";
    private const string BarSubItem = "DevExpress.XtraBars.BarSubItem";
    private const string BarStaticItem = "DevExpress.XtraBars.BarStaticItem";
    private const string BarCheckItem = "DevExpress.XtraBars.BarCheckItem";

    /// <summary>Where a bar item is placed, which is what decides the WinForms control it becomes.</summary>
    private enum ItemContext
    {
        Menu,
        ToolBar,
        StatusBar,
    }

    public static DesignerWalkResult Normalize(DesignerWalkResult walk, VendorCatalog catalog, VendorBaseType? vendorBase)
    {
        var hasVendorModels = walk.Form.Controls.Values.Any(c => c.VendorTypeName is not null);
        if (!hasVendorModels && vendorBase is null)
        {
            return walk;
        }

        var run = new Run(walk, catalog);

        if (vendorBase is not null)
        {
            run.NormalizeForm(vendorBase);
        }

        run.FoldDevExpressGrids();
        run.HideInvisibleTelerikColumns();
        run.BuildDevExpressBars();
        run.FlattenDevExpressRibbons();
        run.RemoveUnplacedBarItems();
        run.FoldTelerikCommandBars();
        run.ClampTelerikRibbons();
        run.FoldTelerikSplitContainers();
        run.ReplaceLayoutItemsWithLabels();
        run.ExpandRadioGroups();
        run.DropTreeContent();
        run.ReadListItems();
        run.RetypeAndRemove();

        return new DesignerWalkResult(walk.Form, run.Edges, [.. walk.Warnings, .. run.Warnings], walk.HostedControlAliases);
    }

    private sealed class Run(DesignerWalkResult walk, VendorCatalog catalog)
    {
        private readonly FormModel _form = walk.Form;
        private readonly IReadOnlyList<VendorLink> _links = walk.VendorLinks;

        /// <summary>The WinForms control a model becomes where its catalog entry cannot say - it depends on placement.</summary>
        private readonly Dictionary<string, string> _targets = new(StringComparer.Ordinal);

        private readonly HashSet<string> _removed = new(StringComparer.Ordinal);

        /// <summary>Bar items already placed once - a second link to one is a copy.</summary>
        private readonly HashSet<string> _placedItems = new(StringComparer.Ordinal);

        private readonly List<string> _copiedItems = [];

        public List<ParentChildEdge> Edges { get; } = [.. walk.Edges];

        public List<string> Warnings { get; } = [];

        // ── Form ─────────────────────────────────────────────────────────────────────────────

        /// <summary>A vendor Form's own skin and icon options carry nothing; its Ribbon/StatusBar links are read below.</summary>
        public void NormalizeForm(VendorBaseType vendorBase)
        {
            foreach (var key in _form.FormProperties.Keys.Where(vendorBase.IsIgnored).ToList())
            {
                _form.FormProperties.Remove(key, out _);
            }

            // `this.Ribbon = this.ribbonControl1;` - the ribbon is placed by its own bounds, so the
            // link itself says nothing more.
            foreach (var key in new[] { "Ribbon", "StatusBar" })
            {
                if (_form.FormProperties.TryGetValue(key, out var value) && value is PropertyValue.ControlReference)
                {
                    _form.FormProperties.Remove(key, out _);
                }
            }
        }

        // ── DevExpress grid ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// <c>gridControl1.MainView = gridView1</c>: the view's columns become the grid's own, in
        /// <c>VisibleIndex</c> order, hidden ones left out, a check or combo editor deciding the
        /// column type. The view itself goes, with what it set reported.
        /// </summary>
        public void FoldDevExpressGrids()
        {
            foreach (var grid in OfType("DevExpress.XtraGrid.GridControl"))
            {
                if (!grid.Properties.TryGetValue("MainView", out var mainView)
                    || mainView is not PropertyValue.ControlReference { FieldName: var viewKey }
                    || !_form.Controls.TryGetValue(viewKey, out var view))
                {
                    continue;
                }

                grid.Properties.Remove("MainView", out _);

                var columns = Edges.Where(e => e.ParentFieldName == viewKey).Select(e => e.ChildFieldName).ToList();
                Edges.RemoveAll(e => e.ParentFieldName == viewKey);

                var hidden = new List<string>();
                var ordered = new List<(string Key, int Index)>();
                foreach (var (key, position) in columns.Select((k, i) => (k, i)))
                {
                    if (!_form.Controls.TryGetValue(key, out var column))
                    {
                        continue;
                    }

                    // GridColumn.Visible defaults to false, so the designer writes `Visible = true` for
                    // every column the grid shows and nothing for one it hides.
                    if (!(column.Properties.TryGetValue("Visible", out var visible) && visible is PropertyValue.Literal { Value: true }))
                    {
                        hidden.Add(key);
                        Remove(key);
                        continue;
                    }

                    var visibleIndex = column.Properties.TryGetValue("VisibleIndex", out var index) && index is PropertyValue.Literal { Value: int i }
                        ? i
                        : int.MaxValue;
                    ordered.Add((key, visibleIndex == int.MaxValue ? 1000 + position : visibleIndex));

                    if (column.Properties.TryGetValue("ColumnEdit", out var editor)
                        && editor is PropertyValue.ControlReference { FieldName: var editorKey }
                        && _form.Controls.TryGetValue(editorKey, out var repositoryItem))
                    {
                        _targets[key] = repositoryItem.VendorTypeName switch
                        {
                            "DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit" => "DataGridViewCheckBoxColumn",
                            "DevExpress.XtraEditors.Repository.RepositoryItemComboBox" => "DataGridViewComboBoxColumn",
                            _ => "DataGridViewTextBoxColumn",
                        };
                    }
                }

                foreach (var (key, _) in ordered.OrderBy(o => o.Index))
                {
                    Edges.Add(new ParentChildEdge(grid.FieldName, key));
                }

                if (view.Properties.TryGetValue("OptionsBehavior.Editable", out var editable) && editable is PropertyValue.Literal { Value: false })
                {
                    grid.Properties["ReadOnly"] = new PropertyValue.Literal(true);
                    view.Properties.Remove("OptionsBehavior.Editable", out _);
                }

                view.Properties.Remove("GridControl", out _);
                view.Properties.Remove("Name", out _);
                ReportLeftovers(view, $"the grid it is the view of, '{grid.FieldName}', became a DataGrid");
                Remove(viewKey);

                if (hidden.Count > 0)
                {
                    Warnings.Add(
                        $"'{grid.FieldName}' (GridControl) has hidden column(s) {Quoted(hidden)} - left out of the DataGrid, as they "
                        + "showed nothing. Add them back as columns with IsVisible=\"False\" if code relies on them.");
                }
            }

            // Repository items and any view no grid showed: plumbing with nothing left to stand for.
            foreach (var plumbing in _form.Controls.Values
                         .Where(c => !_removed.Contains(c.FieldName)
                             && c.VendorTypeName is { } n
                             && (n.StartsWith("DevExpress.XtraEditors.Repository.", StringComparison.Ordinal)
                                 || n == "DevExpress.XtraGrid.Views.Grid.GridView"))
                         .ToList())
            {
                if (plumbing.VendorTypeName == "DevExpress.XtraGrid.Views.Grid.GridView")
                {
                    ReportLeftovers(plumbing, "no GridControl shows it as its MainView");
                }

                Remove(plumbing.FieldName);
            }
        }

        /// <summary>Telerik writes <c>IsVisible = false</c> on a column it keeps hidden.</summary>
        public void HideInvisibleTelerikColumns()
        {
            foreach (var column in _form.Controls.Values
                         .Where(c => c.VendorTypeName is { } n && n.StartsWith("Telerik.WinControls.UI.GridView", StringComparison.Ordinal))
                         .ToList())
            {
                if (column.Properties.TryGetValue("IsVisible", out var visible) && visible is PropertyValue.Literal { Value: false })
                {
                    Warnings.Add($"'{Display(column.FieldName)}' ({column.ClrTypeName}) is a hidden grid column - left out of the DataGrid.");
                    Remove(column.FieldName);
                }
            }
        }

        // ── DevExpress bars ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// A BarManager's bars become a MenuStrip (its MainMenu), ToolStrips and a StatusStrip (its
        /// StatusBar), placed where the designer's dock controls were - the only bounds the designer
        /// records for them - and filled from their links.
        /// </summary>
        public void BuildDevExpressBars()
        {
            var manager = OfType("DevExpress.XtraBars.BarManager").FirstOrDefault();
            var bars = OfType("DevExpress.XtraBars.Bar");
            if (manager is null || bars.Count == 0)
            {
                foreach (var orphan in bars)
                {
                    Warnings.Add($"'{orphan.FieldName}' (Bar) belongs to no BarManager on this form - it is not carried over.");
                    Remove(orphan.FieldName);
                }

                return;
            }

            var mainMenu = Reference(manager, "MainMenu");
            var statusBar = Reference(manager, "StatusBar");
            var docks = OfType("DevExpress.XtraBars.BarDockControl");

            Rect? DockBounds(string side) =>
                docks.FirstOrDefault(d => d.Properties.TryGetValue("Dock", out var dock)
                    && dock is PropertyValue.EnumMembers { MemberNames: [var member] } && member == side) is { } found
                    ? Bounds(found)
                    : null;

            bool IsBottom(ControlModel bar) =>
                bar.FieldName == statusBar
                || (bar.Properties.TryGetValue("DockStyle", out var dockStyle)
                    && dockStyle is PropertyValue.EnumMembers { MemberNames: ["Bottom"] });

            var top = bars.Where(b => !IsBottom(b))
                .OrderBy(b => b.FieldName == mainMenu ? 0 : 1)
                .ThenBy(b => IntProperty(b, "DockRow") ?? 0)
                .ThenBy(b => IntProperty(b, "DockCol") ?? 0)
                .ToList();
            var bottom = bars.Where(IsBottom).ToList();

            var width = FormWidth();
            var placedEdges = new List<ParentChildEdge>();
            Place(top, DockBounds("Top") ?? new Rect(0, 0, width, 24 * Math.Max(top.Count, 1)));
            Place(bottom, DockBounds("Bottom") ?? new Rect(0, FormHeight() - 22, width, 22));

            if (top.Count > 1 || bottom.Count > 1)
            {
                Warnings.Add(
                    "The DevExpress bars sharing one dock area are stacked as separate strips, each an equal share of the "
                    + "height the designer gave the dock - an estimate, since the designer records the dock's size, not each bar's.");
            }

            // The strips take the dock controls' place in the Form's own child list.
            var firstDockEdge = Edges.FindIndex(e => docks.Any(d => d.FieldName == e.ChildFieldName));
            foreach (var dock in docks)
            {
                Remove(dock.FieldName);
            }

            Edges.InsertRange(firstDockEdge < 0 ? Edges.Count : Math.Min(firstDockEdge, Edges.Count), placedEdges);
            Remove(manager.FieldName);
            return;

            void Place(List<ControlModel> strips, Rect area)
            {
                if (strips.Count == 0)
                {
                    return;
                }

                var height = Math.Max(area.Height / strips.Count, 20);
                foreach (var (bar, row) in strips.Select((b, i) => (b, i)))
                {
                    var target = bar.FieldName == mainMenu ? "MenuStrip"
                        : bar.FieldName == statusBar || IsBottom(bar) ? "StatusStrip"
                        : "ToolStrip";
                    var context = target switch { "MenuStrip" => ItemContext.Menu, "StatusStrip" => ItemContext.StatusBar, _ => ItemContext.ToolBar };

                    // What a bar sets - its name, dock row, options - is all said by the strip's
                    // place and contents now.
                    bar.Properties.Clear();
                    bar.Properties["Location"] = new PropertyValue.PointValue(area.X, area.Y + (row * height));
                    bar.Properties["Size"] = new PropertyValue.SizeValue(area.Width, height);
                    _targets[bar.FieldName] = target;

                    placedEdges.Add(new ParentChildEdge(ParentChildEdge.FormOwner, bar.FieldName));
                    BuildItems(bar.FieldName, bar.FieldName, context);
                }
            }
        }

        /// <summary>
        /// The links of <paramref name="owner"/> as children of <paramref name="parent"/> - the same
        /// model, or a copy of a sub-menu linked twice - a separator before each one marked
        /// BeginGroup, and each item made the control its place calls for. A sub-menu recurses, so the
        /// entries of a copied sub-menu are copies too: one model can only be in one place.
        /// </summary>
        private void BuildItems(string owner, string parent, ItemContext context)
        {
            var linked = _links.Where(l => l.Owner == owner).ToList();
            var separators = 0;
            foreach (var (link, position) in linked.Select((l, i) => (l, i)))
            {
                if (!_form.Controls.TryGetValue(link.Item, out var item) || _removed.Contains(link.Item))
                {
                    continue;
                }

                if (link.BeginGroup && position > 0)
                {
                    var separatorKey = $"{parent}#separator{++separators}";
                    _form.Controls[separatorKey] = new ControlModel
                    {
                        FieldName = separatorKey, ClrTypeName = "ToolStripSeparator", IsDesignerLocal = true,
                    };
                    Edges.Add(new ParentChildEdge(parent, separatorKey));
                }

                // One item linked in two places is two elements - but only one of them can carry the
                // field's name. The copy has no x:Name, so code reaches the first one only.
                if (!_placedItems.Add(item.FieldName))
                {
                    item = Copy(item, $"{item.FieldName}#{_copiedItems.Count + 2}");
                    _copiedItems.Add(Display(link.Item));
                }

                if (ItemTarget(item, context) is not { } target)
                {
                    Warnings.Add(
                        $"'{Display(link.Item)}' ({item.ClrTypeName}) is linked into '{Display(owner)}', but this conversion has no "
                        + "counterpart for that kind of bar item - it is not carried over.");
                    Remove(item.FieldName);
                    continue;
                }

                _targets[item.FieldName] = target;
                Edges.Add(new ParentChildEdge(parent, item.FieldName));

                if (item.VendorTypeName == BarSubItem)
                {
                    // A sub-menu's own entries are menu entries, wherever the sub-menu sits.
                    BuildItems(link.Item, item.FieldName, ItemContext.Menu);
                }
            }
        }

        /// <summary>What the item's catalog entry says it becomes in this place, if it can be placed at all.</summary>
        private string? ItemTarget(ControlModel item, ItemContext context) =>
            item.VendorTypeName is { } name && catalog.TryGet(name, out var entry)
                ? entry.PlacedAs.GetValueOrDefault(context.ToString())
                : null;

        /// <summary>A bar item nothing links shows nothing; registered in the manager's list is all it ever was.</summary>
        public void RemoveUnplacedBarItems()
        {
            foreach (var item in _form.Controls.Values
                         .Where(c => c.VendorTypeName is BarButtonItem or BarSubItem or BarCheckItem or BarStaticItem
                             && !_placedItems.Contains(c.FieldName)
                             && !_targets.ContainsKey(c.FieldName))
                         .ToList())
            {
                if (item.Events.Count > 0)
                {
                    Warnings.Add(
                        $"'{item.FieldName}' ({item.ClrTypeName}) is not linked into any bar, menu or ribbon group, so it is not "
                        + $"shown - and its {string.Join(", ", item.Events.Select(e => e.EventName))} handler is not wired.");
                }

                Remove(item.FieldName);
            }

            if (_copiedItems.Count > 0)
            {
                Warnings.Add(
                    $"{Quoted(_copiedItems.Distinct())} {(_copiedItems.Count == 1 ? "is" : "are")} linked into more than one bar, menu "
                    + "or ribbon group, so each appears more than once. Every copy runs the same handler, but only the first carries "
                    + "the field's name - code that changes the item (Enabled, Caption) reaches that one only.");
            }
        }

        // ── DevExpress ribbon ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Pages and groups are already children (catalog collections); each group's item links
        /// become its entries. The ribbon keeps its place but not its height - a menu bar is one row.
        /// </summary>
        public void FlattenDevExpressRibbons()
        {
            foreach (var group in OfType("DevExpress.XtraBars.Ribbon.RibbonPageGroup"))
            {
                BuildItems(group.FieldName, group.FieldName, ItemContext.Menu);
            }

            foreach (var ribbon in OfType("DevExpress.XtraBars.Ribbon.RibbonControl"))
            {
                ClampToMenuHeight(ribbon, "DevExpress RibbonControl");
            }

            foreach (var statusBar in OfType("DevExpress.XtraBars.Ribbon.RibbonStatusBar"))
            {
                BuildItems(statusBar.FieldName, statusBar.FieldName, ItemContext.StatusBar);
            }
        }

        // ── Telerik ──────────────────────────────────────────────────────────────────────────

        /// <summary>A command bar's rows and strips are layout: their items become the ToolStrip's, a separator between strips.</summary>
        public void FoldTelerikCommandBars()
        {
            foreach (var commandBar in OfType("Telerik.WinControls.UI.RadCommandBar"))
            {
                var rows = ChildrenOf(commandBar.FieldName);
                var items = new List<string>();
                var separators = 0;
                foreach (var row in rows)
                {
                    foreach (var strip in ChildrenOf(row))
                    {
                        if (items.Count > 0)
                        {
                            var separatorKey = $"{commandBar.FieldName}#separator{++separators}";
                            _form.Controls[separatorKey] = new ControlModel
                            {
                                FieldName = separatorKey, ClrTypeName = "ToolStripSeparator", IsDesignerLocal = true,
                            };
                            items.Add(separatorKey);
                        }

                        items.AddRange(ChildrenOf(strip));
                        Edges.RemoveAll(e => e.ParentFieldName == strip);
                        Remove(strip);
                    }

                    Edges.RemoveAll(e => e.ParentFieldName == row);
                    Remove(row);
                }

                Edges.RemoveAll(e => e.ParentFieldName == commandBar.FieldName);
                Edges.AddRange(items.Select(i => new ParentChildEdge(commandBar.FieldName, i)));
            }
        }

        public void ClampTelerikRibbons()
        {
            foreach (var ribbon in OfType("Telerik.WinControls.UI.RadRibbonBar"))
            {
                ClampToMenuHeight(ribbon, "Telerik RadRibbonBar");
            }
        }

        /// <summary>
        /// A RadSplitContainer holds its SplitPanels as ordinary children; a SplitContainer has two
        /// named halves. Exactly two panels fold into them, anything else becomes a plain Panel of
        /// Panels, said as such.
        /// </summary>
        public void FoldTelerikSplitContainers()
        {
            foreach (var split in OfType("Telerik.WinControls.UI.RadSplitContainer"))
            {
                var panels = ChildrenOf(split.FieldName)
                    .Where(k => _form.Controls.TryGetValue(k, out var p) && p.VendorTypeName == "Telerik.WinControls.UI.SplitPanel")
                    .ToList();

                if (panels.Count == 2)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        var region = $"{split.FieldName}.Panel{i + 1}";
                        for (var e = 0; e < Edges.Count; e++)
                        {
                            if (Edges[e].ParentFieldName == panels[i])
                            {
                                Edges[e] = Edges[e] with { ParentFieldName = region };
                            }
                        }

                        Remove(panels[i]);
                    }

                    continue;
                }

                _targets[split.FieldName] = "Panel";
                foreach (var panel in panels)
                {
                    _targets[panel] = "Panel";
                }

                Warnings.Add(
                    $"'{split.FieldName}' (RadSplitContainer) has {panels.Count} panel(s); only two map onto a SplitContainer's halves, "
                    + "so it is converted as a Panel holding its panels at their designer positions, with no splitter.");
            }

            // A SplitPanel that is not a RadSplitContainer's child is a Panel in its own right.
            foreach (var panel in OfType("Telerik.WinControls.UI.SplitPanel"))
            {
                _targets.TryAdd(panel.FieldName, "Panel");
            }
        }

        // ── Layout controls ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// A layout item's caption is the one thing the layout control draws itself. Each becomes a
        /// Label beside the control it captions, at that control's height - an approximation of the
        /// layout engine, which positions captions at run time. The groups and spacers go.
        /// </summary>
        public void ReplaceLayoutItemsWithLabels()
        {
            foreach (var (typeName, controlKey) in new[]
                     {
                         ("DevExpress.XtraLayout.LayoutControlItem", "Control"),
                         ("Telerik.WinControls.UI.LayoutControlItem", "AssociatedControl"),
                     })
            {
                foreach (var item in OfType(typeName))
                {
                    var hostedKey = Reference(item, controlKey);
                    var text = item.Properties.TryGetValue("Text", out var t) && t is PropertyValue.Literal { Value: string s } ? s : null;
                    var textVisible = !(item.Properties.TryGetValue("TextVisible", out var tv) && tv is PropertyValue.Literal { Value: false });
                    var itemBounds = Bounds(item) ?? RectangleProperty(item, "Bounds");

                    if (hostedKey is null
                        || !_form.Controls.TryGetValue(hostedKey, out var hosted)
                        || string.IsNullOrEmpty(text)
                        || !textVisible
                        || itemBounds is not { } area
                        || Bounds(hosted) is not { } hostedArea)
                    {
                        Remove(item.FieldName);
                        continue;
                    }

                    // Caption to the left of the control when there is room, above it otherwise.
                    var label = hostedArea.X - area.X > 8
                        ? new Rect(area.X, hostedArea.Y + Math.Max(0, (hostedArea.Height - 16) / 2), hostedArea.X - area.X - 4, 16)
                        : new Rect(hostedArea.X, Math.Max(area.Y, hostedArea.Y - 18), hostedArea.Width, 16);

                    item.Properties.Clear();
                    item.Properties["Text"] = new PropertyValue.Literal(text);
                    item.Properties["Location"] = new PropertyValue.PointValue(label.X, label.Y);
                    item.Properties["Size"] = new PropertyValue.SizeValue(label.Width, label.Height);
                    item.Events.Clear();
                    _targets[item.FieldName] = "Label";

                    var hostedEdge = Edges.FindIndex(e => e.ChildFieldName == hostedKey);
                    if (hostedEdge >= 0)
                    {
                        Edges.Insert(hostedEdge, new ParentChildEdge(Edges[hostedEdge].ParentFieldName, item.FieldName));
                    }
                    else
                    {
                        Remove(item.FieldName);
                    }
                }
            }

            foreach (var plumbing in _form.Controls.Values
                         .Where(c => c.VendorTypeName is "DevExpress.XtraLayout.LayoutControlGroup"
                             or "DevExpress.XtraLayout.EmptySpaceItem"
                             or "DevExpress.XtraLayout.SplitterItem"
                             or "Telerik.WinControls.UI.LayoutControlGroupItem"
                             or "Telerik.WinControls.UI.LayoutControlSeparatorItem")
                         .ToList())
            {
                Remove(plumbing.FieldName);
            }
        }

        // ── Radio group ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A DevExpress RadioGroup draws its items as radio buttons inside one editor; Avalonia
        /// has the radio buttons. One per item, stacked inside the group's Panel.
        /// </summary>
        public void ExpandRadioGroups()
        {
            foreach (var group in OfType("DevExpress.XtraEditors.RadioGroup"))
            {
                if (group.LiteralItems.Count == 0)
                {
                    continue;
                }

                var bounds = Bounds(group) ?? new Rect(0, 0, 120, 24 * group.LiteralItems.Count);
                var height = Math.Max((bounds.Height - 4) / group.LiteralItems.Count, 20);
                var selected = IntProperty(group, "SelectedIndex")
                    ?? (group.Properties.TryGetValue("EditValue", out var editValue) && editValue is PropertyValue.Literal { Value: int value } ? value : null);

                foreach (var (caption, index) in group.LiteralItems.Select((c, i) => (c, i)))
                {
                    var key = $"{group.FieldName}#option{index}";
                    var option = new ControlModel { FieldName = key, ClrTypeName = "RadioButton", IsDesignerLocal = true };
                    option.Properties["Text"] = new PropertyValue.Literal(caption);
                    option.Properties["Location"] = new PropertyValue.PointValue(4, 2 + (index * height));
                    option.Properties["Size"] = new PropertyValue.SizeValue(Math.Max(bounds.Width - 8, 20), height);
                    if (index == selected)
                    {
                        option.Properties["Checked"] = new PropertyValue.Literal(true);
                    }

                    _form.Controls[key] = option;
                    Edges.Add(new ParentChildEdge(group.FieldName, key));
                }

                group.LiteralItems.Clear();
                group.Properties.Remove("SelectedIndex", out _);
                group.Properties.Remove("EditValue", out _);

                if (group.Events.Count > 0)
                {
                    Warnings.Add(
                        $"'{group.FieldName}' (RadioGroup) becomes a Panel of RadioButtons, which raise their own Checked events - its "
                        + $"{string.Join(", ", group.Events.Select(e => $"{e.EventName} handler '{e.HandlerMethodName ?? "(inline)"}'"))} "
                        + "is not wired. Subscribe each RadioButton's IsCheckedChanged instead.");
                    group.Events.Clear();
                }
            }
        }

        // ── Trees ────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A TreeView holds nodes, not columns; and nodes the designer added are not carried for
        /// any TreeView, WinForms' own included. Both are said rather than dropped.
        /// </summary>
        public void DropTreeContent()
        {
            foreach (var tree in OfType("DevExpress.XtraTreeList.TreeList"))
            {
                var columns = ChildrenOf(tree.FieldName);
                foreach (var column in columns)
                {
                    Remove(column);
                }

                Warnings.Add(
                    $"'{tree.FieldName}' (TreeList) becomes a TreeView, which has a single column"
                    + (columns.Count > 0 ? $" - its {columns.Count} column(s) are not carried over" : "")
                    + ". Its nodes come from data or code (KeyFieldName/ParentFieldName binding is not translated): fill the "
                    + "TreeView's ItemsSource with a hierarchical model.");
            }

            foreach (var tree in OfType("Telerik.WinControls.UI.RadTreeView"))
            {
                var nodes = Descendants(tree.FieldName).ToList();
                foreach (var node in nodes)
                {
                    Remove(node);
                }

                if (nodes.Count > 0)
                {
                    Warnings.Add(
                        $"'{tree.FieldName}' (RadTreeView) declares {nodes.Count} node(s) in the designer, which are not carried "
                        + "over - fill the TreeView's ItemsSource with a hierarchical model.");
                }
            }
        }

        // ── List items ───────────────────────────────────────────────────────────────────────

        /// <summary>A list control's item objects (<c>radListDataItem1</c>) are their text.</summary>
        public void ReadListItems()
        {
            foreach (var link in _links)
            {
                if (!_form.Controls.TryGetValue(link.Owner, out var owner)
                    || owner.VendorTypeName is not { } ownerType
                    || !catalog.TryGet(ownerType, out var entry)
                    || entry.CollectionAt(link.Path) is not { Kind: VendorCollectionKind.Items }
                    || !_form.Controls.TryGetValue(link.Item, out var item))
                {
                    continue;
                }

                if (item.Properties.TryGetValue("Text", out var text) && text is PropertyValue.Literal { Value: string caption })
                {
                    owner.LiteralItems.Add(caption);
                }
                else
                {
                    Warnings.Add($"'{Display(link.Item)}' is an item of '{Display(link.Owner)}' with no literal text - it is not carried over.");
                }

                Remove(link.Item);
            }
        }

        // ── Retype, then take away what went ────────────────────────────────────────────────

        public void RetypeAndRemove()
        {
            foreach (var control in _form.Controls.Values.ToList())
            {
                if (_removed.Contains(control.FieldName)
                    || control.VendorTypeName is not { } vendorType
                    || !catalog.TryGet(vendorType, out var entry))
                {
                    continue;
                }

                var target = _targets.GetValueOrDefault(control.FieldName) ?? entry.As;
                if (target is null)
                {
                    // Plumbing nothing above consumed - a GridView with no grid, a bar item in a
                    // container this conversion does not read. Said, then gone.
                    ReportLeftovers(control, "it has no counterpart of its own");
                    Remove(control.FieldName);
                    continue;
                }

                entry.Retype(control, target);
            }

            foreach (var key in _removed)
            {
                _form.Controls.Remove(key);
            }

            // A child whose parent went with nothing to take its place would land in neither the tree
            // nor the components - ControlGraphBuilder marks it placed before it looks at the parent.
            var stranded = Edges
                .Where(e => _removed.Contains(e.ParentFieldName) && !_removed.Contains(e.ChildFieldName))
                .Select(e => Display(e.ChildFieldName))
                .Distinct()
                .ToList();
            if (stranded.Count > 0)
            {
                Warnings.Add(
                    $"{Quoted(stranded)} {(stranded.Count == 1 ? "was" : "were")} inside a vendor container this conversion does not "
                    + "carry over, so it is not emitted either.");
            }

            Edges.RemoveAll(e => _removed.Contains(e.ParentFieldName) || _removed.Contains(e.ChildFieldName));
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────────────

        private List<ControlModel> OfType(string fullName) =>
            [.. _form.Controls.Values.Where(c => c.VendorTypeName == fullName && !_removed.Contains(c.FieldName))
                .OrderBy(c => c.FieldName, StringComparer.Ordinal)];

        private List<string> ChildrenOf(string parent) =>
            [.. Edges.Where(e => e.ParentFieldName == parent).Select(e => e.ChildFieldName)];

        private IEnumerable<string> Descendants(string parent)
        {
            foreach (var child in ChildrenOf(parent))
            {
                yield return child;
                foreach (var grandChild in Descendants(child))
                {
                    yield return grandChild;
                }
            }
        }

        private void Remove(string key) => _removed.Add(key);

        private ControlModel Copy(ControlModel item, string key)
        {
            var copy = new ControlModel
            {
                FieldName = key,
                ClrTypeName = item.ClrTypeName,
                WrittenTypeName = item.WrittenTypeName,
                VendorTypeName = item.VendorTypeName,
                IsDesignerLocal = true,
            };
            foreach (var (name, value) in item.Properties)
            {
                copy.Properties[name] = value;
            }

            copy.Events.AddRange(item.Events);
            _form.Controls[key] = copy;
            return copy;
        }

        private void ClampToMenuHeight(ControlModel ribbon, string what)
        {
            if (ribbon.Properties.TryGetValue("Size", out var size) && size is PropertyValue.SizeValue { Height: > 28 } tall)
            {
                ribbon.Properties["Size"] = new PropertyValue.SizeValue(tall.Width, 24);
                Warnings.Add(
                    $"'{ribbon.FieldName}' ({what}) becomes a menu bar: each page is a menu, each group a sub-menu, each item an "
                    + $"entry. A menu bar is one row, so the {tall.Height - 24} pixel(s) the ribbon took below it are left empty - "
                    + "move the controls up, or give the window a ribbon-like toolbar by hand.");
            }
        }

        /// <summary>Says what a removed vendor object had set or handled, so it is not lost silently.</summary>
        private void ReportLeftovers(ControlModel model, string why)
        {
            var entry = model.VendorTypeName is { } name && catalog.TryGet(name, out var found) ? found : null;
            var settings = model.Properties.Keys
                .Where(k => entry is null || !entry.IsIgnored(k))
                .Where(k => k is not ("Location" or "Size" or "Name" or "TabIndex"))
                .Order(StringComparer.Ordinal)
                .ToList();
            var handlers = model.Events
                .Select(e => $"{e.EventName} ('{e.HandlerMethodName ?? "inline"}')")
                .ToList();

            if (settings.Count == 0 && handlers.Count == 0)
            {
                return;
            }

            var label = $"'{Display(model.FieldName)}' ({entry?.SimpleName ?? model.ClrTypeName})";
            var parts = new List<string>();
            if (settings.Count > 0)
            {
                parts.Add($"its settings {string.Join(", ", settings)} are not carried over");
            }

            if (handlers.Count > 0)
            {
                parts.Add($"its {string.Join(", ", handlers)} handler(s) are not wired - the method(s) stay in the code-behind");
            }

            Warnings.Add($"{label} is not emitted ({why}): {string.Join("; ", parts)}.");
        }

        private string? Reference(ControlModel model, string key) =>
            model.Properties.TryGetValue(key, out var value) && value is PropertyValue.ControlReference { FieldName: var field }
                ? field
                : null;

        private static int? IntProperty(ControlModel model, string key) =>
            model.Properties.TryGetValue(key, out var value) && value is PropertyValue.Literal { Value: int number } ? number : null;

        private static Rect? Bounds(ControlModel model) =>
            model.Properties.TryGetValue("Location", out var location) && location is PropertyValue.PointValue point
            && model.Properties.TryGetValue("Size", out var size) && size is PropertyValue.SizeValue extent
                ? new Rect(point.X, point.Y, extent.Width, extent.Height)
                : null;

        /// <summary><c>new System.Drawing.Rectangle(x, y, w, h)</c>, which the evaluator leaves unresolved.</summary>
        private static Rect? RectangleProperty(ControlModel model, string key)
        {
            if (!model.Properties.TryGetValue(key, out var value) || value is not PropertyValue.Unresolved { RawExpression: var raw })
            {
                return null;
            }

            if (SyntaxFactory.ParseExpression(raw) is ObjectCreationExpressionSyntax { ArgumentList.Arguments: { Count: 4 } arguments }
                && arguments.All(a => a.Expression is LiteralExpressionSyntax { Token.Value: int })
                && arguments.Select(a => (int)((LiteralExpressionSyntax)a.Expression).Token.Value!).ToList() is [var x, var y, var w, var h])
            {
                return new Rect(x, y, w, h);
            }

            return null;
        }

        private int FormWidth() =>
            _form.FormProperties.TryGetValue("ClientSize", out var size) && size is PropertyValue.SizeValue { Width: var w } ? w : 800;

        private int FormHeight() =>
            _form.FormProperties.TryGetValue("ClientSize", out var size) && size is PropertyValue.SizeValue { Height: var h } ? h : 450;

        private static string Display(string key) => key.Split('@', '#')[0];

        private static string Quoted(IEnumerable<string> names) => string.Join(", ", names.Select(n => $"'{n}'"));
    }

    private readonly record struct Rect(int X, int Y, int Width, int Height);
}
