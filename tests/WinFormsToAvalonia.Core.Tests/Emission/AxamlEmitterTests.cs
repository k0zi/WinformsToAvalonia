using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Emission;

public class AxamlEmitterTests
{
    private static readonly string FixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DesignerCs");
    private static readonly string GoldenRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ExpectedAxaml");

    [Fact]
    public void EmitView_DirectMappedTreeFixture_MatchesGoldenFile()
    {
        var designerPath = Path.Combine(FixturesRoot, "DirectMappedTree.designer.cs");
        var content = File.ReadAllText(designerPath);

        var walkResult = new DesignerSyntaxWalker().Walk(content, designerPath, "DirectMappedTreeForm", "Demo");
        var formModel = new ControlGraphBuilder().Build(walkResult);

        var emitter = new AxamlEmitter(new ControlMappingRegistry());
        var result = emitter.EmitView(formModel, "Demo", "DirectMappedTreeView", "DirectMappedTreeViewModel");

        Assert.Empty(result.UsedFallbackKeys);

        var goldenPath = Path.Combine(GoldenRoot, "DirectMappedTree.axaml");
        var expected = File.ReadAllText(goldenPath).Replace("\r\n", "\n");

        Assert.Equal(expected, result.Axaml);
    }

    [Fact]
    public void EmitView_FallbackMappedControl_EmitsControlsPrefixedElementAndReportsUsedKey()
    {
        var formModel = BuildGroupBoxFormModel();

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("GroupBoxFallback", result.UsedFallbackKeys);
        Assert.Contains("<controls:GroupBoxFallback", result.Axaml);
        Assert.Contains("Header=\"Options\"", result.Axaml);
        Assert.Contains("xmlns:controls=\"using:Demo.Controls\"", result.Axaml);
    }

    [Fact]
    public void EmitView_FallbackControlsDisabled_EmitsCommentInsteadAndReportsNoUsedKeys()
    {
        var formModel = BuildGroupBoxFormModel();

        var result = new AxamlEmitter(new ControlMappingRegistry())
            .EmitView(formModel, "Demo", "MainView", "MainViewModel", emitFallbackControls: false);

        Assert.Empty(result.UsedFallbackKeys);
        Assert.DoesNotContain("<controls:GroupBoxFallback", result.Axaml);
        Assert.Contains("TODO(Winforms2Avalonia)", result.Axaml);
    }

    [Fact]
    public void EmitView_TabPageWithChildren_WrapsChildrenInCanvas()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var tabControl = new ControlModel { FieldName = "tabControl1", ClrTypeName = "TabControl" };
        var tabPage = new ControlModel { FieldName = "tabPage1", ClrTypeName = "TabPage" };
        tabPage.Properties["Text"] = new PropertyValue.Literal("First");
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["Location"] = new PropertyValue.PointValue(10, 10);
        button.Properties["Size"] = new PropertyValue.SizeValue(75, 23);
        tabPage.Children.Add(button);
        tabControl.Children.Add(tabPage);
        formModel.RootControls.Add(tabControl);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<TabItem x:Name=\"tabPage1\" Header=\"First\">", result.Axaml);
        Assert.Contains("<Canvas>", result.Axaml);
        Assert.Contains("<Button x:Name=\"button1\"", result.Axaml);
        Assert.Empty(result.RequiredNuGetPackages);
    }

    [Fact]
    public void EmitView_EmptyTabPage_DoesNotEmitEmptyCanvasWrapper()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var tabControl = new ControlModel { FieldName = "tabControl1", ClrTypeName = "TabControl" };
        var tabPage = new ControlModel { FieldName = "tabPage2", ClrTypeName = "TabPage" };
        tabPage.Properties["Text"] = new PropertyValue.Literal("Second");
        tabControl.Children.Add(tabPage);
        formModel.RootControls.Add(tabControl);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<TabItem x:Name=\"tabPage2\" Header=\"Second\" />", result.Axaml);
        Assert.DoesNotContain("<Canvas />", result.Axaml);
    }

    [Fact]
    public void EmitView_TemplateColumn_EmitsTheMapperPrescribedCellTemplateSubtree()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var grid = new ControlModel { FieldName = "dataGridView1", ClrTypeName = "DataGridView" };
        var column = new ControlModel { FieldName = "actionColumn", ClrTypeName = "DataGridViewButtonColumn" };
        column.Properties["HeaderText"] = new PropertyValue.Literal("Action");
        column.Properties["Text"] = new PropertyValue.Literal("Go");
        grid.Children.Add(column);
        formModel.RootControls.Add(grid);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<DataGrid.Columns>", result.Axaml);
        Assert.Contains("<DataGridTemplateColumn Header=\"Action\">", result.Axaml);
        Assert.Contains("<DataGridTemplateColumn.CellTemplate>", result.Axaml);
        Assert.Contains("<DataTemplate>", result.Axaml);
        Assert.Contains("<Button Content=\"Go\" />", result.Axaml);
        // The column types are not StyledElements, so x:Name on them is an AVLN2000 error.
        Assert.DoesNotContain("x:Name=\"actionColumn\"", result.Axaml);
    }

    [Fact]
    public void EmitView_DropDownButtonWithItems_NestsThemThroughBothWrapperLevels()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var dropDown = new ControlModel { FieldName = "dropDownButton1", ClrTypeName = "ToolStripDropDownButton" };
        dropDown.Properties["Text"] = new PropertyValue.Literal("Layout");
        var item = new ControlModel { FieldName = "itemA", ClrTypeName = "ToolStripMenuItem" };
        item.Properties["Text"] = new PropertyValue.Literal("Item A");
        dropDown.Children.Add(item);
        formModel.RootControls.Add(dropDown);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<Button x:Name=\"dropDownButton1\" Content=\"Layout\">", result.Axaml);
        Assert.Contains("<Button.Flyout>", result.Axaml);
        Assert.Contains("<MenuFlyout>", result.Axaml);
        Assert.Contains("<MenuItem x:Name=\"itemA\" Header=\"Item A\" />", result.Axaml);
    }

    [Fact]
    public void EmitView_UserControlArtifact_EmitsAUserControlRootSizedFromItsOwnSizeProperty()
    {
        var formModel = new FormModel { ClassName = "DemoUserControl" };
        formModel.FormProperties["Size"] = new PropertyValue.SizeValue(220, 70);
        formModel.FormProperties["Text"] = new PropertyValue.Literal("ignored for a UserControl");

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(
            formModel, "Demo", "DemoUserControlView", "DemoUserControlViewModel",
            artifactKind: WinFormsArtifactKind.UserControl);

        Assert.StartsWith("<UserControl ", result.Axaml);
        Assert.EndsWith("</UserControl>\n", result.Axaml);
        Assert.Contains("Width=\"220\" Height=\"70\"", result.Axaml);
        // A UserControl has no Title - emitting one would be an AVLN2000 error.
        Assert.DoesNotContain("Title=", result.Axaml);
    }

    [Fact]
    public void EmitView_FormHostingAProjectUserControl_EmitsThePrefixedViewElementAndDeclaresItsXmlns()
    {
        var userControlViews = new[]
        {
            new UserControlViewInfo("DemoUserControl", "DemoUserControlView", "Demo.Views.Controls", "uc0"),
        };
        var registry = new ControlMappingRegistry(
            DefaultControlMappers.All.Append(new UserControlMapper("DemoUserControl", "uc0:DemoUserControlView")));

        var formModel = new FormModel { ClassName = "MainForm" };
        formModel.RootControls.Add(new ControlModel { FieldName = "demoUserControl1", ClrTypeName = "DemoUserControl" });

        var result = new AxamlEmitter(registry).EmitView(
            formModel, "Demo", "MainView", "MainViewModel", userControlViews: userControlViews);

        Assert.Contains("xmlns:uc0=\"using:Demo.Views.Controls\"", result.Axaml);
        Assert.Contains("<uc0:DemoUserControlView x:Name=\"demoUserControl1\" />", result.Axaml);
        Assert.DoesNotContain("TODO(Winforms2Avalonia)", result.Axaml);
    }

    [Fact]
    public void EmitView_TwoUserControlsInTheSameFolder_DeclaresTheirSharedNamespaceOnlyOnce()
    {
        var userControlViews = new[]
        {
            new UserControlViewInfo("FirstControl", "FirstControlView", "Demo.Views.Controls", "uc0"),
            new UserControlViewInfo("SecondControl", "SecondControlView", "Demo.Views.Controls", "uc0"),
        };

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(
            new FormModel { ClassName = "MainForm" }, "Demo", "MainView", "MainViewModel", userControlViews: userControlViews);

        var declarations = result.Axaml.Split("xmlns:uc0=").Length - 1;
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void EmitView_FormInSubfolder_QualifiesViewAndViewModelNamespacesWithRelativeFolder()
    {
        var formModel = BuildGroupBoxFormModel();

        var result = new AxamlEmitter(new ControlMappingRegistry())
            .EmitView(formModel, "Demo", "MainView", "MainViewModel", relativeFolder: "Forms");

        Assert.Contains("xmlns:vm=\"using:Demo.ViewModels.Forms\"", result.Axaml);
        Assert.Contains("x:Class=\"Demo.Views.Forms.MainView\"", result.Axaml);
    }

    [Fact]
    public void EmitView_DataGridView_ReportsRequiredNuGetPackage()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var grid = new ControlModel { FieldName = "dataGridView1", ClrTypeName = "DataGridView" };
        formModel.RootControls.Add(grid);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<DataGrid x:Name=\"dataGridView1\" />", result.Axaml);
        Assert.Contains("Avalonia.Controls.DataGrid", result.RequiredNuGetPackages);
    }

    [Fact]
    public void EmitView_SplitContainer_EmitsGridWithGridSplitterAndBothPanelRegions()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var splitContainer = new ControlModel { FieldName = "splitContainer1", ClrTypeName = "SplitContainer" };
        var leftButton = new ControlModel { FieldName = "leftButton", ClrTypeName = "Button" };
        var rightLabel = new ControlModel { FieldName = "rightLabel", ClrTypeName = "Label" };
        splitContainer.Panel1Children.Add(leftButton);
        splitContainer.Panel2Children.Add(rightLabel);
        formModel.RootControls.Add(splitContainer);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<Grid x:Name=\"splitContainer1\" ColumnDefinitions=\"*,Auto,*\">", result.Axaml);
        Assert.Contains("<Canvas Grid.Column=\"0\">", result.Axaml);
        Assert.Contains("<Button x:Name=\"leftButton\"", result.Axaml);
        Assert.Contains("<GridSplitter Grid.Column=\"1\" Width=\"4\" ResizeDirection=\"Columns\" />", result.Axaml);
        Assert.Contains("<Canvas Grid.Column=\"2\">", result.Axaml);
        Assert.Contains("<TextBlock x:Name=\"rightLabel\"", result.Axaml);
    }

    [Fact]
    public void EmitView_SplitContainerHorizontalOrientation_UsesRowDefinitions()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var splitContainer = new ControlModel { FieldName = "splitContainer1", ClrTypeName = "SplitContainer" };
        splitContainer.Properties["Orientation"] = new PropertyValue.EnumMembers(["Horizontal"]);
        formModel.RootControls.Add(splitContainer);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("RowDefinitions=\"*,Auto,*\"", result.Axaml);
        Assert.Contains("<GridSplitter Grid.Row=\"1\" Height=\"4\" ResizeDirection=\"Rows\" />", result.Axaml);
    }

    [Fact]
    public void EmitView_ControlWithToolTipText_EmitsToolTipTipAttribute()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["ToolTipText"] = new PropertyValue.Literal("Click me!");
        formModel.RootControls.Add(button);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("ToolTip.Tip=\"Click me!\"", result.Axaml);
    }

    [Fact]
    public void EmitView_MenuStripWithNestedItems_EmitsMenuMenuItemAndSeparator()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var menuStrip = new ControlModel { FieldName = "menuStrip1", ClrTypeName = "MenuStrip" };
        var fileMenuItem = new ControlModel { FieldName = "fileMenuItem", ClrTypeName = "ToolStripMenuItem" };
        fileMenuItem.Properties["Text"] = new PropertyValue.Literal("File");
        var exitMenuItem = new ControlModel { FieldName = "exitMenuItem", ClrTypeName = "ToolStripMenuItem" };
        exitMenuItem.Properties["Text"] = new PropertyValue.Literal("Exit");
        var separator = new ControlModel { FieldName = "fileSeparator", ClrTypeName = "ToolStripSeparator" };
        fileMenuItem.Children.Add(exitMenuItem);
        fileMenuItem.Children.Add(separator);
        menuStrip.Children.Add(fileMenuItem);
        formModel.RootControls.Add(menuStrip);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<Menu x:Name=\"menuStrip1\">", result.Axaml);
        Assert.Contains("<MenuItem x:Name=\"fileMenuItem\" Header=\"File\">", result.Axaml);
        Assert.Contains("<MenuItem x:Name=\"exitMenuItem\" Header=\"Exit\" />", result.Axaml);
        Assert.Contains("<Separator x:Name=\"fileSeparator\" />", result.Axaml);
    }

    [Fact]
    public void EmitView_DataGridViewWithColumns_WrapsColumnsInDataGridColumnsPropertyElement()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var grid = new ControlModel { FieldName = "dataGridView1", ClrTypeName = "DataGridView" };
        var nameColumn = new ControlModel { FieldName = "nameColumn", ClrTypeName = "DataGridViewTextBoxColumn" };
        nameColumn.Properties["HeaderText"] = new PropertyValue.Literal("Name");
        var activeColumn = new ControlModel { FieldName = "activeColumn", ClrTypeName = "DataGridViewCheckBoxColumn" };
        activeColumn.Properties["HeaderText"] = new PropertyValue.Literal("Active");
        grid.Children.Add(nameColumn);
        grid.Children.Add(activeColumn);
        formModel.RootControls.Add(grid);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<DataGrid x:Name=\"dataGridView1\">", result.Axaml);
        Assert.Contains("<DataGrid.Columns>", result.Axaml);
        // DataGrid column types aren't a Visual/StyledElement - no x:Name (Avalonia rejects it).
        Assert.Contains("<DataGridTextColumn Header=\"Name\" />", result.Axaml);
        Assert.Contains("<DataGridCheckBoxColumn Header=\"Active\" />", result.Axaml);
        Assert.DoesNotContain("nameColumn", result.Axaml);
        Assert.Contains("Avalonia.Controls.DataGrid", result.RequiredNuGetPackages);
    }

    [Fact]
    public void EmitView_ControlWithContextMenuStripReference_EmitsControlContextMenuWithMenuItems()
    {
        var formModel = new FormModel { ClassName = "MainForm" };

        var contextMenuStrip = new ControlModel { FieldName = "contextMenuStrip1", ClrTypeName = "ContextMenuStrip" };
        var copyItem = new ControlModel { FieldName = "copyMenuItem", ClrTypeName = "ToolStripMenuItem" };
        copyItem.Properties["Text"] = new PropertyValue.Literal("Copy");
        contextMenuStrip.Children.Add(copyItem);
        formModel.Controls["contextMenuStrip1"] = contextMenuStrip;
        formModel.Components.Add(contextMenuStrip);

        var treeView = new ControlModel { FieldName = "treeView1", ClrTypeName = "TreeView" };
        treeView.Properties["ContextMenuStrip"] = new PropertyValue.ControlReference("contextMenuStrip1");
        formModel.Controls["treeView1"] = treeView;
        formModel.RootControls.Add(treeView);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<Control.ContextMenu>", result.Axaml);
        Assert.Contains("<ContextMenu>", result.Axaml);
        Assert.Contains("<MenuItem x:Name=\"copyMenuItem\" Header=\"Copy\" />", result.Axaml);
    }

    [Fact]
    public void EmitView_ControlWithoutContextMenuStripReference_EmitsNoControlContextMenu()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var treeView = new ControlModel { FieldName = "treeView1", ClrTypeName = "TreeView" };
        formModel.Controls["treeView1"] = treeView;
        formModel.RootControls.Add(treeView);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("Control.ContextMenu", result.Axaml);
    }

    [Fact]
    public void EmitView_UnresolvedLocation_WarnsAndOmitsCanvasLeftTop()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["Location"] = new PropertyValue.Unresolved("SomeHelper.ComputeLocation()");
        formModel.RootControls.Add(button);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("Canvas.Left", result.Axaml);
        Assert.DoesNotContain("Canvas.Top", result.Axaml);
        Assert.Contains(result.Warnings, w => w.Contains("button1", StringComparison.Ordinal) && w.Contains("Location", StringComparison.Ordinal));
    }

    [Fact]
    public void EmitView_UnresolvedSize_WarnsAndOmitsWidthHeight()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["Size"] = new PropertyValue.Unresolved("SomeHelper.ComputeSize()");
        formModel.RootControls.Add(button);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("Width=", result.Axaml);
        Assert.DoesNotContain("Height=", result.Axaml);
        Assert.Contains(result.Warnings, w => w.Contains("button1", StringComparison.Ordinal) && w.Contains("Size", StringComparison.Ordinal));
    }

    [Fact]
    public void EmitView_ControlWithNoLocationOrSize_ProducesNoWarning()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var label = new ControlModel { FieldName = "autoSizeLabel", ClrTypeName = "Label" };
        formModel.RootControls.Add(label);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void EmitView_TemplatedControlWithColorsAndFont_EmitsEveryStyleAttribute()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["BackColor"] = new PropertyValue.ColorValue(null, 255, 0x1E, 0x90, 0xFF);
        button.Properties["ForeColor"] = new PropertyValue.ColorValue("White", null, null, null, null);
        button.Properties["Font"] = new PropertyValue.FontValue("Segoe UI", 12f, ["Bold"]);
        button.Properties["Padding"] = new PropertyValue.PaddingValue(2, 4, 2, 4);
        formModel.RootControls.Add(button);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("Background=\"#FF1E90FF\"", result.Axaml);
        Assert.Contains("Foreground=\"#FFFFFFFF\"", result.Axaml);
        Assert.Contains("FontFamily=\"Segoe UI\"", result.Axaml);
        Assert.Contains("FontSize=\"16\"", result.Axaml);
        Assert.Contains("FontWeight=\"Bold\"", result.Axaml);
        Assert.Contains("Padding=\"2,4,2,4\"", result.Axaml);
    }

    /// <summary>
    /// A Panel has a Background and nothing else. Emitting Foreground/FontSize on one would be
    /// an AVLN2000 in the generated project, so the target element - not the WinForms type -
    /// decides what survives.
    /// </summary>
    [Fact]
    public void EmitView_PanelWithForeColorAndFont_EmitsOnlyBackground()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var panel = new ControlModel { FieldName = "panel1", ClrTypeName = "Panel" };
        panel.Properties["BackColor"] = new PropertyValue.ColorValue("Red", null, null, null, null);
        panel.Properties["ForeColor"] = new PropertyValue.ColorValue("White", null, null, null, null);
        panel.Properties["Font"] = new PropertyValue.FontValue("Segoe UI", 12f, []);
        formModel.RootControls.Add(panel);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("Background=\"#FFFF0000\"", result.Axaml);
        Assert.DoesNotContain("Foreground=", result.Axaml);
        Assert.DoesNotContain("FontSize=", result.Axaml);
    }

    /// <summary>Avalonia's Image derives straight from Control - it has no styling surface at all.</summary>
    [Fact]
    public void EmitView_PictureBoxWithBackColor_EmitsNoStyleAttributes()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var pictureBox = new ControlModel { FieldName = "pictureBox1", ClrTypeName = "PictureBox" };
        pictureBox.Properties["BackColor"] = new PropertyValue.ColorValue("Red", null, null, null, null);
        formModel.RootControls.Add(pictureBox);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<Image x:Name=\"pictureBox1\"", result.Axaml);
        Assert.DoesNotContain("Background=", result.Axaml);
    }

    [Fact]
    public void EmitView_LabelWithUnderlineFont_EmitsTextDecorations()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var label = new ControlModel { FieldName = "label1", ClrTypeName = "Label" };
        label.Properties["Font"] = new PropertyValue.FontValue("Segoe UI", 9f, ["Underline"]);
        formModel.RootControls.Add(label);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("TextDecorations=\"Underline\"", result.Axaml);
    }

    /// <summary>
    /// A WinForms Form's Font is inherited by every child that never overrode it, and Avalonia's
    /// font properties inherit the same way - so the root element carries it for the whole view.
    /// </summary>
    [Fact]
    public void EmitView_FormLevelBackColorAndFont_LandOnTheRootElement()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        formModel.FormProperties["BackColor"] = new PropertyValue.ColorValue("Control", null, null, null, null);
        formModel.FormProperties["Font"] = new PropertyValue.FontValue("Tahoma", 9f, []);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("Background=\"#FFF0F0F0\"", result.Axaml);
        Assert.Contains("FontFamily=\"Tahoma\"", result.Axaml);
        Assert.Contains("FontSize=\"12\"", result.Axaml);
    }

    /// <summary>
    /// An unresolvable color must produce no attribute at all rather than a guess: an
    /// unparseable value would fail the generated project's XAML compile.
    /// </summary>
    [Fact]
    public void EmitView_UnresolvedBackColor_EmitsNoBackgroundAttribute()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["BackColor"] = new PropertyValue.Unresolved("Theme.AccentColor");
        formModel.RootControls.Add(button);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("Background=", result.Axaml);
    }

    [Fact]
    public void EmitView_ComboBoxWithDesignerItems_EmitsThemAsComboBoxItems()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var comboBox = new ControlModel { FieldName = "comboBox1", ClrTypeName = "ComboBox" };
        comboBox.LiteralItems.AddRange(["Alpha", "Beta"]);
        formModel.RootControls.Add(comboBox);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<ComboBoxItem Content=\"Alpha\" />", result.Axaml);
        Assert.Contains("<ComboBoxItem Content=\"Beta\" />", result.Axaml);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void EmitView_ListBoxWithDesignerItems_EmitsListBoxItems()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var listBox = new ControlModel { FieldName = "listBox1", ClrTypeName = "ListBox" };
        listBox.LiteralItems.Add("Only");
        formModel.RootControls.Add(listBox);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<ListBoxItem Content=\"Only\" />", result.Axaml);
    }

    /// <summary>
    /// A fallback control does not accept item elements, and emitting them anyway would be an
    /// AVLN error - so the entries are reported rather than dropped silently.
    /// </summary>
    [Fact]
    public void EmitView_ItemsOnATargetThatTakesNone_WarnsInsteadOfEmitting()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var upDown = new ControlModel { FieldName = "domainUpDown1", ClrTypeName = "DomainUpDown" };
        upDown.LiteralItems.AddRange(["One", "Two"]);
        formModel.RootControls.Add(upDown);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("Content=\"One\"", result.Axaml);
        Assert.Contains(result.Warnings, w => w.Contains("domainUpDown1", StringComparison.Ordinal) && w.Contains("item(s)", StringComparison.Ordinal));
    }

    private static FormModel BuildGroupBoxFormModel()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var groupBox = new ControlModel { FieldName = "groupBox1", ClrTypeName = "GroupBox" };
        groupBox.Properties["Text"] = new PropertyValue.Literal("Options");
        groupBox.Properties["Location"] = new PropertyValue.PointValue(12, 12);
        groupBox.Properties["Size"] = new PropertyValue.SizeValue(200, 100);
        formModel.RootControls.Add(groupBox);
        return formModel;
    }
}
