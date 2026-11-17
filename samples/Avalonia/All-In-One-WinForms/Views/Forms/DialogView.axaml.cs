using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using All_In_One_WinForms.Generated;
using All_In_One_WinForms.ViewModels.Forms;

namespace All_In_One_WinForms.Views.Forms;

public partial class DialogView : Window
{
    public DialogView()
    {
        InitializeComponent();
        DataContext = new DialogViewModel();
    }

    public string EnteredText
    {
        get
        {
            return (inputTextBox.Text ?? string.Empty);
        }
    }

    private void okButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void cancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    /* ORIGINAL WINFORMS CODE-BEHIND - NOT COMPILED, PRESERVED FOR REFERENCE
       Original file: DialogForm.cs

namespace AllInOneWinForms.Forms;

/// <summary>
/// A second Form, opened with <see cref="Form.ShowDialog()"/> from the main window - it makes
/// the owning handler navigation code, which is exactly the kind of handler that can never be
/// promoted to a ViewModel command.
/// </summary>
public partial class DialogForm : Form
{
    public DialogForm()
    {
        InitializeComponent();
    }

    public string EnteredText => this.inputTextBox.Text;

    private void okButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private void cancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}

    */
}
