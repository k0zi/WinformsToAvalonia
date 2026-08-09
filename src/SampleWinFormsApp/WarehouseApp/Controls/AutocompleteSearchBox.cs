using System.Diagnostics.CodeAnalysis;

namespace WarehouseApp.Controls;

public class AutocompleteSearchBox : UserControl
{
    private TextBox _textBox = null!;
    private Form? _popup;
    private ListBox? _popupList;
    private IReadOnlyList<object> _items = [];
    private bool _suppressTextChanged;

    public event EventHandler? SelectedItemChanged;

    public IEnumerable<object> DataSource
    {
        get => _items;
        set => _items = value.ToList();
    }

    public string DisplayMember { get; set; } = string.Empty;

    public object? SelectedItem { get; private set; }

    [AllowNull]
    public override string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? string.Empty;
    }

    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    public AutocompleteSearchBox()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search products..."
        };
        _textBox.TextChanged += TextBox_TextChanged;
        _textBox.KeyDown += TextBox_KeyDown;
        _textBox.LostFocus += (_, _) => ClosePopupSoon();

        Controls.Add(_textBox);
        Size = new Size(220, 24);

        ResumeLayout(false);
        PerformLayout();
    }

    private string GetDisplayText(object item)
    {
        if (string.IsNullOrEmpty(DisplayMember))
        {
            return item.ToString() ?? string.Empty;
        }
        var prop = item.GetType().GetProperty(DisplayMember);
        return prop?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
    }

    private void TextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_suppressTextChanged)
        {
            return;
        }

        var query = _textBox.Text;
        if (string.IsNullOrWhiteSpace(query))
        {
            HidePopup();
            return;
        }

        var matches = _items
            .Where(i => GetDisplayText(i).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(15)
            .ToList();

        if (matches.Count == 0)
        {
            HidePopup();
            return;
        }

        ShowPopup(matches);
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_popupList is null || !_popupList.Visible)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Down:
                if (_popupList.SelectedIndex < _popupList.Items.Count - 1)
                {
                    _popupList.SelectedIndex++;
                }
                e.Handled = true;
                break;
            case Keys.Up:
                if (_popupList.SelectedIndex > 0)
                {
                    _popupList.SelectedIndex--;
                }
                e.Handled = true;
                break;
            case Keys.Enter:
                if (_popupList.SelectedItem is object item)
                {
                    CommitSelection(item);
                }
                e.Handled = true;
                break;
            case Keys.Escape:
                HidePopup();
                e.Handled = true;
                break;
        }
    }

    private void ShowPopup(List<object> matches)
    {
        _popup?.Close();

        _popupList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None
        };
        foreach (var item in matches)
        {
            _popupList.Items.Add(new DisplayWrapper(item, GetDisplayText(item)));
        }
        if (_popupList.Items.Count > 0)
        {
            _popupList.SelectedIndex = 0;
        }
        _popupList.Click += (_, _) =>
        {
            if (_popupList.SelectedItem is DisplayWrapper w)
            {
                CommitSelection(w.Value);
            }
        };

        var screenLocation = Parent?.PointToScreen(new Point(Left, Bottom)) ?? PointToScreen(new Point(0, Height));

        _popup = new Form
        {
            StartPosition = FormStartPosition.Manual,
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            TopMost = true,
            Size = new Size(Width, Math.Min(matches.Count, 6) * 20 + 4)
        };
        _popup.Location = new Point(screenLocation.X, screenLocation.Y);
        _popup.Controls.Add(_popupList);
        _popup.Deactivate += (_, _) => HidePopup();
        _popup.Show(FindForm());
        FindForm()?.Activate();
        _textBox.Focus();
    }

    private void CommitSelection(object item)
    {
        var value = item is DisplayWrapper w ? w.Value : item;
        SelectedItem = value;
        _suppressTextChanged = true;
        _textBox.Text = GetDisplayText(value);
        _suppressTextChanged = false;
        _textBox.SelectionStart = _textBox.Text.Length;
        HidePopup();
        SelectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClosePopupSoon()
    {
        BeginInvoke(new Action(HidePopup));
    }

    private void HidePopup()
    {
        _popup?.Close();
        _popup = null;
        _popupList = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _popup?.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class DisplayWrapper(object value, string display)
    {
        public object Value { get; } = value;
        public override string ToString() => display;
    }
}
