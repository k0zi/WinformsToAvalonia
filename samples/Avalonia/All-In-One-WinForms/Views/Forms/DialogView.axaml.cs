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

    private void okButton_Click(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'okButton_Click' - TODO(Winforms2Avalonia): migrate it into this method.
        DialogResult = DialogResult.OK;
        Close();
        */
        MigrationTodo.NotMigrated(nameof(okButton_Click), "okButton_Click");
    }

    private void cancelButton_Click(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'cancelButton_Click' - TODO(Winforms2Avalonia): migrate it into this method.
        DialogResult = DialogResult.Cancel;
        Close();
        */
        MigrationTodo.NotMigrated(nameof(cancelButton_Click), "cancelButton_Click");
    }

    /* ORIGINAL WINFORMS MEMBERS - NOT COMPILED, PRESERVED FOR MANUAL MIGRATION

    public string EnteredText => this.inputTextBox.Text;
    */

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
