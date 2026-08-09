namespace WarehouseApp.Common;

/// <summary>
/// Shared Save/Cancel + validation scaffolding for add/edit detail screens.
/// Derived forms lay out their own fields, then implement LoadFromEntity/ValidateInput/SaveToEntity/PersistAsync.
/// </summary>
public abstract class DetailFormBase<TEntity> : Form where TEntity : class, new()
{
    protected ErrorProvider Validation { get; private set; } = null!;
    protected Button SaveButton { get; private set; } = null!;
    protected Button DialogCancelButton { get; private set; } = null!;
    protected TEntity Entity { get; }
    protected bool IsNew { get; }

    protected DetailFormBase(TEntity? entity)
    {
        Entity = entity ?? new TEntity();
        IsNew = entity is null;
        InitializeBaseComponents();
    }

    private void InitializeBaseComponents()
    {
        Validation = new ErrorProvider { ContainerControl = this, BlinkStyle = ErrorBlinkStyle.NeverBlink };

        DialogCancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(90, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        SaveButton = new Button
        {
            Text = "Save",
            Size = new Size(90, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        SaveButton.Click += async (_, _) => await SaveAsync();

        Controls.Add(DialogCancelButton);
        Controls.Add(SaveButton);
        AcceptButton = SaveButton;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        Load += (_, _) =>
        {
            PositionButtons();
            LoadFromEntity();
        };
    }

    private void PositionButtons()
    {
        DialogCancelButton.Location = new Point(ClientSize.Width - DialogCancelButton.Width - 12, ClientSize.Height - DialogCancelButton.Height - 12);
        SaveButton.Location = new Point(DialogCancelButton.Left - SaveButton.Width - 8, DialogCancelButton.Top);
    }

    protected abstract void LoadFromEntity();
    protected abstract bool ValidateInput();
    protected abstract void SaveToEntity();
    protected abstract Task PersistAsync();

    private async Task SaveAsync()
    {
        Validation.Clear();
        if (!ValidateInput())
        {
            return;
        }

        SaveToEntity();
        SaveButton.Enabled = false;
        try
        {
            await PersistAsync();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save record: {ex.Message}", "Save Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SaveButton.Enabled = true;
        }
    }
}
