using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using All_In_One_WinForms.Generated;
using All_In_One_WinForms.ViewModels.Controls;

namespace All_In_One_WinForms.Views.Controls;

public partial class DemoUserControlView : UserControl
{
    public DemoUserControlView()
    {
        InitializeComponent();
        DataContext = new DemoUserControlViewModel();
    }

    public string Caption
    {
        get
        {
            return (captionLabel.Text ?? string.Empty);
        }

        set
        {
            captionLabel.Text = value;
        }
    }

    /* ORIGINAL WINFORMS CODE-BEHIND - NOT COMPILED, PRESERVED FOR REFERENCE
       Original file: DemoUserControl.cs

using System.ComponentModel;

namespace AllInOneWinForms.Controls;

/// <summary>
/// A composite UserControl - the second artifact kind a WinForms project can contain.
/// </summary>
public partial class DemoUserControl : UserControl
{
    public DemoUserControl()
    {
        InitializeComponent();
    }

    [DefaultValue("Demo user control")]
    public string Caption
    {
        get => this.captionLabel.Text;
        set => this.captionLabel.Text = value;
    }

    private void incrementButton_Click(object? sender, EventArgs e)
    {
        this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();
    }
}

    */
}
