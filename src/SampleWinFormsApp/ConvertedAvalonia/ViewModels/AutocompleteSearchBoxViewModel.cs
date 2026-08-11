using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for AutocompleteSearchBox (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class AutocompleteSearchBoxViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal Form? _popup;

    internal ListBox? _popupList;

    internal IReadOnlyList<object> _items = [];

    internal bool _suppressTextChanged;

    internal string GetDisplayText(object item)
        {
            if (string.IsNullOrEmpty(DisplayMember))
            {
                return item.ToString() ?? string.Empty;
            }
            var prop = item.GetType().GetProperty(DisplayMember);
            return prop?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
        }

    internal void ShowPopup(List<object> matches)
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

    internal void CommitSelection(object item)
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

    internal void ClosePopupSoon()
        {
            BeginInvoke(new Action(HidePopup));
        }

    internal void HidePopup()
        {
            _popup?.Close();
            _popup = null;
            _popupList = null;
        }

    public string ToString() => display;

}
