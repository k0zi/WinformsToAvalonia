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
        var formModel = BuildFallbackFormModel();

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("RichTextBoxFallback", result.UsedFallbackKeys);
        Assert.Contains("<controls:RichTextBoxFallback", result.Axaml);
        Assert.Contains("xmlns:controls=\"using:Demo.Controls\"", result.Axaml);
    }

    [Fact]
    public void EmitView_FallbackControlsDisabled_EmitsCommentInsteadAndReportsNoUsedKeys()
    {
        var formModel = BuildFallbackFormModel();

        var result = new AxamlEmitter(new ControlMappingRegistry())
            .EmitView(formModel, "Demo", "MainView", "MainViewModel", emitFallbackControls: false);

        Assert.Empty(result.UsedFallbackKeys);
        Assert.DoesNotContain("<controls:RichTextBoxFallback", result.Axaml);
        Assert.Contains("TODO(Winforms2Avalonia)", result.Axaml);
    }

    /// <summary>
    /// A GroupBox is a ContentControl, so its positioned children go into the wrapper Canvas -
    /// the same shape TabPage/TabItem produces, and what keeps absolute layout working inside it.
    /// </summary>
    [Fact]
    public void EmitView_GroupBox_EmitsARealGroupBoxWithItsChildrenInsideACanvas()
    {
        var formModel = BuildGroupBoxFormModel();

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<GroupBox x:Name=\"groupBox1\"", result.Axaml);
        Assert.Contains("Header=\"Options\"", result.Axaml);
        Assert.DoesNotContain("GroupBoxFallback", result.Axaml);
        Assert.Empty(result.UsedFallbackKeys);

        var groupBoxStart = result.Axaml.IndexOf("<GroupBox ", StringComparison.Ordinal);
        var buttonStart = result.Axaml.IndexOf("<Button ", StringComparison.Ordinal);
        var canvasStart = result.Axaml.IndexOf("<Canvas>", groupBoxStart, StringComparison.Ordinal);
        Assert.True(groupBoxStart < canvasStart && canvasStart < buttonStart,
            $"The child should sit inside the GroupBox's wrapper Canvas.\n{result.Axaml}");
    }

    /// <summary>
    /// A grid column's <c>DataPropertyName</c> becomes the column's binding - but only on the two
    /// column types Avalonia gives a <c>Binding</c> property to.
    /// </summary>
    [Fact]
    public void EmitView_DataPropertyName_BindsOnlyTheBoundColumnTypes()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var grid = new ControlModel { FieldName = "grid1", ClrTypeName = "DataGridView" };

        foreach (var (field, clrType) in new[]
                 {
                     ("nameColumn", "DataGridViewTextBoxColumn"),
                     ("activeColumn", "DataGridViewCheckBoxColumn"),
                     ("categoryColumn", "DataGridViewComboBoxColumn"),
                 })
        {
            var column = new ControlModel { FieldName = field, ClrTypeName = clrType };
            column.Properties["HeaderText"] = new PropertyValue.Literal(field);
            column.Properties["DataPropertyName"] = new PropertyValue.Literal("Category");
            grid.Children.Add(column);
        }

        formModel.RootControls.Add(grid);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<DataGridTextColumn Header=\"nameColumn\" Binding=\"{ReflectionBinding Category}\" />", result.Axaml);
        Assert.Contains("<DataGridCheckBoxColumn Header=\"activeColumn\" Binding=\"{ReflectionBinding Category}\" />", result.Axaml);

        // DataGridTemplateColumn has no Binding property at all, and which member of the cell the
        // value belongs to is not decidable - so the name is reported rather than bound.
        Assert.DoesNotContain("<DataGridTemplateColumn Header=\"categoryColumn\" Binding=", result.Axaml);
        Assert.Contains("bind this cell to the row model's 'Category'", result.Axaml);
    }

    /// <summary>
    /// An extender provider's value lands on the target as an attached property, and only where
    /// the target can carry one.
    /// </summary>
    [Fact]
    public void EmitView_ExtenderProviderValues_BecomeAttachedPropertiesOnTheTarget()
    {
        var formModel = new FormModel { ClassName = "MainForm" };

        var textBox = new ControlModel { FieldName = "notesBox", ClrTypeName = "TextBox" };
        textBox.Properties["ToolTipText"] = new PropertyValue.Literal("Hover text");
        textBox.Properties["HelpString"] = new PropertyValue.Literal("F1 text");
        formModel.RootControls.Add(textBox);

        // A DataGrid column is not a StyledElement, so an attached property on it is an AVLN2000
        // in the generated project - and nowhere else.
        var grid = new ControlModel { FieldName = "grid1", ClrTypeName = "DataGridView" };
        var column = new ControlModel { FieldName = "col1", ClrTypeName = "DataGridViewTextBoxColumn" };
        column.Properties["ToolTipText"] = new PropertyValue.Literal("never emitted");
        grid.Children.Add(column);
        formModel.RootControls.Add(grid);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("ToolTip.Tip=\"Hover text\"", result.Axaml);
        Assert.Contains("AutomationProperties.HelpText=\"F1 text\"", result.Axaml);
        Assert.DoesNotContain("never emitted", result.Axaml);
    }

    /// <summary>
    /// Absolute coordinates only describe a layout if the controls are the size they were told
    /// to be. Avalonia's theme would rather they were bigger.
    /// </summary>
    /// <remarks>
    /// Measured on the headless platform: a TextBox asked for 23 pixels renders 32 without
    /// these, and 23 with them - so on a Canvas it silently covered whatever the designer put
    /// 26 pixels below it.
    /// </remarks>
    [Fact]
    public void EmitView_PinsCanvasChildrenToTheSizeTheDesignerRecorded()
    {
        var formModel = new FormModel { ClassName = "MainForm" };

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<Window.Styles>", result.Axaml);
        Assert.Contains("<Style Selector=\"Canvas &gt; :is(Control)\">", result.Axaml);
        Assert.Contains("<Setter Property=\"MinWidth\" Value=\"0\" />", result.Axaml);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"0\" />", result.Axaml);

        // The minimum alone only trades the overlap for clipped text - the theme's padding does
        // not fit a line into 23 pixels either.
        Assert.Contains("<Style Selector=\"Canvas &gt; :is(TemplatedControl)\">", result.Axaml);
        Assert.Contains("<Setter Property=\"Padding\" Value=\"4,1\" />", result.Axaml);
    }

    /// <summary>
    /// A caption's ampersand is a keyboard mnemonic, not a character - and the sample's menu
    /// read "&amp;File" until this was handled.
    /// </summary>
    [Fact]
    public void EmitView_TranslatesCaptionMnemonicsPerTarget()
    {
        var formModel = new FormModel { ClassName = "MainForm" };

        var menu = new ControlModel { FieldName = "menuStrip1", ClrTypeName = "MenuStrip" };
        var item = new ControlModel { FieldName = "fileMenuItem", ClrTypeName = "ToolStripMenuItem" };
        item.Properties["Text"] = new PropertyValue.Literal("&File");
        menu.Children.Add(item);

        // A Label becomes a TextBlock, which renders an underscore literally - so the marker has
        // to go rather than move.
        var label = new ControlModel { FieldName = "label1", ClrTypeName = "Label" };
        label.Properties["Text"] = new PropertyValue.Literal("&Name && title");

        // ...and a TextBox's Text is the user's data, where an ampersand is just an ampersand.
        var textBox = new ControlModel { FieldName = "textBox1", ClrTypeName = "TextBox" };
        textBox.Properties["Text"] = new PropertyValue.Literal("Smith & Sons");

        formModel.RootControls.Add(menu);
        formModel.RootControls.Add(label);
        formModel.RootControls.Add(textBox);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("Header=\"_File\"", result.Axaml);
        Assert.Contains("Text=\"Name &amp; title\"", result.Axaml);
        Assert.Contains("Text=\"Smith &amp; Sons\"", result.Axaml);
    }

    /// <summary>
    /// A TabPage's WinForms bounds are the tab control's client area, not a position anyone
    /// chose - and in Avalonia they would size the <em>tab</em> rather than the page.
    /// </summary>
    /// <remarks>
    /// This is not a tidiness point. The sample's nine 992x602 pages each turned into a 602-pixel
    /// tab header, so the header strip filled the window and every page was pushed out of it: the
    /// converted app came up blank while building, starting and passing every test.
    /// </remarks>
    [Fact]
    public void EmitView_TabPage_DoesNotCarryTheTabControlsClientBounds()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var tabControl = new ControlModel { FieldName = "tabControl1", ClrTypeName = "TabControl" };
        tabControl.Properties["Location"] = new PropertyValue.PointValue(12, 58);
        tabControl.Properties["Size"] = new PropertyValue.SizeValue(1000, 630);
        var tabPage = new ControlModel { FieldName = "tabPage1", ClrTypeName = "TabPage" };
        tabPage.Properties["Text"] = new PropertyValue.Literal("First");
        tabPage.Properties["Location"] = new PropertyValue.PointValue(4, 24);
        tabPage.Properties["Size"] = new PropertyValue.SizeValue(992, 602);
        var button = new ControlModel { FieldName = "button1", ClrTypeName = "Button" };
        button.Properties["Location"] = new PropertyValue.PointValue(10, 10);
        button.Properties["Size"] = new PropertyValue.SizeValue(75, 23);
        tabPage.Children.Add(button);
        tabControl.Children.Add(tabPage);
        formModel.RootControls.Add(tabControl);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<TabItem x:Name=\"tabPage1\" Header=\"First\">", result.Axaml);

        // The TabControl itself is a child of the form's Canvas, and the button a child of the
        // TabItem's - both keep the absolute layout the whole strategy rests on.
        Assert.Contains("<TabControl x:Name=\"tabControl1\" Canvas.Left=\"12\" Canvas.Top=\"58\" Width=\"1000\" Height=\"630\">", result.Axaml);
        Assert.Contains("<Button x:Name=\"button1\" Canvas.Left=\"10\" Canvas.Top=\"10\" Width=\"75\" Height=\"23\" />", result.Axaml);
    }

    /// <summary>
    /// The counterpart: a menu item is an item too, and a designer that recorded bounds for one
    /// must not have them turned into a fixed-size menu entry.
    /// </summary>
    [Fact]
    public void EmitView_MenuItem_DoesNotCarryAbsoluteLayout()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var menu = new ControlModel { FieldName = "menuStrip1", ClrTypeName = "MenuStrip" };
        menu.Properties["Location"] = new PropertyValue.PointValue(0, 0);
        menu.Properties["Size"] = new PropertyValue.SizeValue(240, 24);
        var item = new ControlModel { FieldName = "fileMenuItem", ClrTypeName = "ToolStripMenuItem" };
        item.Properties["Text"] = new PropertyValue.Literal("File");
        item.Properties["Size"] = new PropertyValue.SizeValue(37, 20);
        menu.Children.Add(item);
        formModel.RootControls.Add(menu);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<MenuItem x:Name=\"fileMenuItem\" Header=\"File\" />", result.Axaml);
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

    /// <summary>
    /// A UserControl that another project of the same solution defines. Avalonia's short
    /// <c>using:</c> form can only name a namespace in the assembly being compiled, so this is
    /// the one case that has to spell out <c>clr-namespace:...;assembly=...</c>.
    /// </summary>
    [Fact]
    public void EmitView_FormHostingAUserControlFromAnotherProject_QualifiesTheXmlnsWithItsAssembly()
    {
        var userControlViews = new[]
        {
            new UserControlViewInfo("OwnControl", "OwnControlView", "Demo.Views", "uc0"),
            new UserControlViewInfo("SharedPanel", "SharedPanelView", "Widgets.Views", "uc1", AssemblyName: "Widgets"),
        };
        var registry = new ControlMappingRegistry(DefaultControlMappers.All
            .Append(new UserControlMapper("OwnControl", "uc0:OwnControlView"))
            .Append(new UserControlMapper("SharedPanel", "uc1:SharedPanelView")));

        var formModel = new FormModel { ClassName = "MainForm" };
        formModel.RootControls.Add(new ControlModel { FieldName = "ownControl1", ClrTypeName = "OwnControl" });
        formModel.RootControls.Add(new ControlModel { FieldName = "sharedPanel1", ClrTypeName = "SharedPanel" });

        var result = new AxamlEmitter(registry).EmitView(
            formModel, "Demo", "MainView", "MainViewModel", userControlViews: userControlViews);

        Assert.Contains("xmlns:uc0=\"using:Demo.Views\"", result.Axaml);
        Assert.Contains("xmlns:uc1=\"clr-namespace:Widgets.Views;assembly=Widgets\"", result.Axaml);
        Assert.Contains("<uc1:SharedPanelView x:Name=\"sharedPanel1\" />", result.Axaml);
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
    /// A target with no row in the items table does not accept item elements, and emitting them
    /// anyway would be an AVLN error - so the entries are reported rather than dropped silently.
    /// </summary>
    [Fact]
    public void EmitView_ItemsOnATargetThatTakesNone_WarnsInsteadOfEmitting()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var tree = new ControlModel { FieldName = "treeView1", ClrTypeName = "TreeView" };
        tree.LiteralItems.AddRange(["One", "Two"]);
        formModel.RootControls.Add(tree);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("Content=\"One\"", result.Axaml);
        Assert.Contains(result.Warnings, w => w.Contains("treeView1", StringComparison.Ordinal) && w.Contains("item(s)", StringComparison.Ordinal));
    }

    /// <summary>
    /// A bundled template can take items too, in the shape its own API dictates: DomainUpDown's
    /// are bare strings in a get-only collection property, not item elements with a Content
    /// attribute. The xmlns those need goes on the *root* - Avalonia's XAML compiler rejects an
    /// attribute on a property element, which is where it would otherwise belong.
    /// </summary>
    [Fact]
    public void EmitView_ItemsOnAFallbackWithACollectionProperty_EmitsThemAsBareStrings()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var upDown = new ControlModel { FieldName = "domainUpDown1", ClrTypeName = "DomainUpDown" };
        upDown.LiteralItems.AddRange(["One", "Two"]);
        formModel.RootControls.Add(upDown);
        formModel.Controls["domainUpDown1"] = upDown;

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("xmlns:sys=\"using:System\"", result.Axaml);
        Assert.Contains("<controls:DomainUpDownFallback.Items>", result.Axaml);
        Assert.Contains("<sys:String>One</sys:String>", result.Axaml);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("item(s)", StringComparison.Ordinal));
    }

    /// <summary>...and a form with no such items keeps exactly the root attributes it had.</summary>
    [Fact]
    public void EmitView_NoItemsNeedingAnXmlns_DoesNotDeclareOne()
    {
        var result = new AxamlEmitter(new ControlMappingRegistry())
            .EmitView(BuildGroupBoxFormModel(), "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("xmlns:sys", result.Axaml);
    }

    /// <summary>
    /// A Direct mapping can lose something too, and until this those warnings went nowhere: only
    /// the not-emitted branch read them, so a CheckedListBox shed its check state in silence.
    /// </summary>
    [Fact]
    public void EmitView_DirectMapperWithAWarning_ReportsItAndLeavesItInTheAxaml()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var list = new ControlModel { FieldName = "optionsList", ClrTypeName = "CheckedListBox" };
        list.Properties["Location"] = new PropertyValue.PointValue(12, 12);
        list.Properties["Size"] = new PropertyValue.SizeValue(150, 90);
        formModel.RootControls.Add(list);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains("<ListBox x:Name=\"optionsList\"", result.Axaml);
        Assert.Contains("SelectionMode=\"Multiple\"", result.Axaml);
        Assert.Contains(result.Warnings, w => w.Contains("optionsList", StringComparison.Ordinal));
        Assert.Contains("TODO(Winforms2Avalonia)", result.Axaml);
    }

    /// <summary>
    /// The mapper and the universal styling pass can both want to write one attribute name. A
    /// duplicate XML attribute does not merge, it fails to parse - so exactly one has to win.
    /// </summary>
    [Fact]
    public void EmitView_GroupBoxWithADesignerPadding_EmitsPaddingExactlyOnce()
    {
        var formModel = BuildGroupBoxFormModel();
        formModel.RootControls[0].Properties["Padding"] = new PropertyValue.PaddingValue(8, 8, 8, 8);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        var groupBoxElement = result.Axaml[result.Axaml.IndexOf("<GroupBox ", StringComparison.Ordinal)..];
        groupBoxElement = groupBoxElement[..groupBoxElement.IndexOf('>')];

        Assert.Equal(1, groupBoxElement.Split("Padding=").Length - 1);
        Assert.Contains("Padding=\"8,8,8,8\"", groupBoxElement);
    }

    /// <summary>
    /// Every generated view pins `Canvas > :is(TemplatedControl)` to Padding="4,1", which on a
    /// container is four more pixels of inset applied to the designer's child coordinates - so
    /// the mapper spends the attribute itself rather than letting the style have it.
    /// </summary>
    [Fact]
    public void EmitView_GroupBoxWithoutADesignerPadding_PinsPaddingToZero()
    {
        var result = new AxamlEmitter(new ControlMappingRegistry())
            .EmitView(BuildGroupBoxFormModel(), "Demo", "MainView", "MainViewModel");

        Assert.Contains("Padding=\"0\"", result.Axaml);
    }

    /// <summary>
    /// A control carrying both a ContextMenuStrip and designer styling. The emitted document has
    /// to parse - which it did not, because a ContextMenu is a child *element* and every
    /// attribute written after it landed outside the start tag.
    /// </summary>
    [Fact]
    public void EmitView_ControlWithBothAContextMenuAndStyling_EmitsWellFormedXml()
    {
        var formModel = new FormModel { ClassName = "MainForm" };

        var menu = new ControlModel { FieldName = "contextMenuStrip1", ClrTypeName = "ContextMenuStrip" };
        var menuItem = new ControlModel { FieldName = "copyMenuItem", ClrTypeName = "ToolStripMenuItem" };
        menuItem.Properties["Text"] = new PropertyValue.Literal("Copy");
        menu.Children.Add(menuItem);
        formModel.Controls["contextMenuStrip1"] = menu;

        var panel = new ControlModel { FieldName = "panel1", ClrTypeName = "Panel" };
        panel.Properties["Location"] = new PropertyValue.PointValue(12, 12);
        panel.Properties["Size"] = new PropertyValue.SizeValue(200, 100);
        panel.Properties["ContextMenuStrip"] = new PropertyValue.ControlReference("contextMenuStrip1");
        panel.Properties["BackColor"] = new PropertyValue.ColorValue("Red", null, null, null, null);
        panel.Properties["ToolTip"] = new PropertyValue.Literal("Right-click me");
        formModel.RootControls.Add(panel);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        // The assertion that matters: it is a document at all.
        var parsed = Record.Exception(() => System.Xml.Linq.XDocument.Parse(result.Axaml));
        Assert.True(parsed is null, $"The emitted AXAML is not well-formed XML: {parsed?.Message}\n{result.Axaml}");

        Assert.Contains("<Control.ContextMenu>", result.Axaml);
        Assert.Contains("Background=", result.Axaml);
    }

    /// <summary>
    /// A leaf's RightToLeft means exactly what Avalonia's FlowDirection means: right-aligned text,
    /// nothing moved. So it is emitted.
    /// </summary>
    [Theory]
    [InlineData("Yes", "RightToLeft")]
    [InlineData("No", "LeftToRight")]
    public void EmitView_RightToLeftOnALeaf_EmitsFlowDirection(string winFormsValue, string expected)
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var label = new ControlModel { FieldName = "label1", ClrTypeName = "Label" };
        label.Properties["Location"] = new PropertyValue.PointValue(12, 12);
        label.Properties["RightToLeft"] = new PropertyValue.EnumMembers([winFormsValue]);
        formModel.RootControls.Add(label);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.Contains($"FlowDirection=\"{expected}\"", result.Axaml);
    }

    /// <summary>
    /// On a container it does not: Avalonia mirrors the whole subtree, and every child here is
    /// positioned by absolute Canvas coordinates. WinForms moves nothing for RightToLeft alone.
    /// </summary>
    [Fact]
    public void EmitView_RightToLeftOnAContainer_IsReportedRatherThanMirroringTheLayout()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var panel = new ControlModel { FieldName = "panel1", ClrTypeName = "Panel" };
        panel.Properties["Location"] = new PropertyValue.PointValue(12, 12);
        panel.Properties["RightToLeft"] = new PropertyValue.EnumMembers(["Yes"]);
        panel.Children.Add(new ControlModel { FieldName = "inner", ClrTypeName = "Button" });
        formModel.RootControls.Add(panel);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("FlowDirection=", result.Axaml);
        Assert.Contains(result.Warnings, w => w.Contains("panel1", StringComparison.Ordinal) && w.Contains("FlowDirection", StringComparison.Ordinal));
    }

    /// <summary>Avalonia's FlowDirection inherits, so Inherit is exactly "emit nothing".</summary>
    [Fact]
    public void EmitView_RightToLeftInherit_EmitsNothing()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var label = new ControlModel { FieldName = "label1", ClrTypeName = "Label" };
        label.Properties["RightToLeft"] = new PropertyValue.EnumMembers(["Inherit"]);
        formModel.RootControls.Add(label);

        var result = new AxamlEmitter(new ControlMappingRegistry()).EmitView(formModel, "Demo", "MainView", "MainViewModel");

        Assert.DoesNotContain("FlowDirection=", result.Axaml);
        Assert.Empty(result.Warnings);
    }

    private static FormModel BuildGroupBoxFormModel()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var groupBox = new ControlModel { FieldName = "groupBox1", ClrTypeName = "GroupBox" };
        groupBox.Properties["Text"] = new PropertyValue.Literal("Options");
        groupBox.Properties["Location"] = new PropertyValue.PointValue(12, 12);
        groupBox.Properties["Size"] = new PropertyValue.SizeValue(200, 100);

        var okButton = new ControlModel { FieldName = "okButton", ClrTypeName = "Button" };
        okButton.Properties["Location"] = new PropertyValue.PointValue(10, 30);
        okButton.Properties["Size"] = new PropertyValue.SizeValue(75, 23);
        groupBox.Children.Add(okButton);

        formModel.RootControls.Add(groupBox);
        return formModel;
    }

    /// <summary>A control that still has no in-box Avalonia equivalent, for the fallback paths.</summary>
    private static FormModel BuildFallbackFormModel()
    {
        var formModel = new FormModel { ClassName = "MainForm" };
        var richTextBox = new ControlModel { FieldName = "richTextBox1", ClrTypeName = "RichTextBox" };
        richTextBox.Properties["Location"] = new PropertyValue.PointValue(12, 12);
        richTextBox.Properties["Size"] = new PropertyValue.SizeValue(200, 100);
        formModel.RootControls.Add(richTextBox);
        return formModel;
    }
}
