using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms WebBrowser: Avalonia ships no webview, and the community ones
/// (Avalonia.WebView, WebView2 interop) are platform-specific extra dependencies this tool
/// deliberately does not add to every generated project. This is a visible placeholder that
/// keeps the control's footprint in the layout and shows the <see cref="Url"/> the designer
/// had configured.
/// </summary>
/// <remarks>
/// To make it real, add a webview package and replace this class - the converted View's
/// element name and x:Name stay valid, so only this file changes.
/// </remarks>
public class WebBrowserFallback : UserControl
{
    /// <remarks>
    /// Avalonia resolves a control's theme by its <em>concrete</em> type, so a subclass of a
    /// templated control finds no theme and gets no template - it renders as nothing at all,
    /// not as an unstyled box. Measured: without this the fallback was absent from the window
    /// while compiling, starting and passing every test.
    /// </remarks>
    protected override Type StyleKeyOverride => typeof(UserControl);

    public static readonly StyledProperty<string?> UrlProperty =
        AvaloniaProperty.Register<WebBrowserFallback, string?>(nameof(Url));

    private readonly TextBlock _urlText;

    public WebBrowserFallback()
    {
        _urlText = new TextBlock
        {
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };

        var caption = new TextBlock
        {
            Text = "WebBrowser (no Avalonia webview)",
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Content = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Background = Brushes.WhiteSmoke,
            Padding = new Thickness(8),
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 4,
                Children = { caption, _urlText },
            },
        };

        UpdateUrlText();
    }

    /// <summary>The WinForms WebBrowser.Url the designer configured, shown as placeholder text.</summary>
    public string? Url
    {
        get => GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == UrlProperty)
        {
            UpdateUrlText();
        }
    }

    private void UpdateUrlText() => _urlText.Text = string.IsNullOrEmpty(Url) ? "(no Url set)" : Url;
}
