using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;

namespace All_In_One_WinForms.Controls;

/// <summary>
/// Fallback for WinForms PropertyGrid: Avalonia has no built-in property editor. This is a
/// deliberately small reflection-based stand-in - set <see cref="SelectedObject"/> and it
/// lists the object's public readable properties as name/value rows, editing the ones whose
/// type it can round-trip through a TypeConverter (string, numbers, bool, enums).
/// </summary>
/// <remarks>
/// It is not a full PropertyGrid: no category grouping, no nested/expandable objects, no
/// custom UITypeEditors, no design-time attributes beyond [Browsable(false)] and
/// [ReadOnly(true)]. If you need those, replace this control with a community Avalonia
/// PropertyGrid package - everything else in the converted View stays as it is.
/// </remarks>
public class PropertyGridFallback : UserControl
{
    /// <remarks>
    /// Avalonia resolves a control's theme by its <em>concrete</em> type, so a subclass of a
    /// templated control finds no theme and gets no template - it renders as nothing at all,
    /// not as an unstyled box. Measured: without this the fallback was absent from the window
    /// while compiling, starting and passing every test.
    /// </remarks>
    protected override Type StyleKeyOverride => typeof(UserControl);

    public static readonly StyledProperty<object?> SelectedObjectProperty =
        AvaloniaProperty.Register<PropertyGridFallback, object?>(nameof(SelectedObject));

    private readonly Grid _rows;

    public PropertyGridFallback()
    {
        _rows = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Content = new ScrollViewer { Content = _rows };
    }

    /// <summary>The object whose properties are listed - the WinForms PropertyGrid.SelectedObject equivalent.</summary>
    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedObjectProperty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _rows.Children.Clear();
        _rows.RowDefinitions.Clear();

        var target = SelectedObject;
        if (target is null)
        {
            return;
        }

        var rowIndex = 0;
        foreach (var property in GetBrowsableProperties(target.GetType()))
        {
            _rows.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var label = new TextBlock
            {
                Text = property.Name,
                Margin = new Thickness(4, 3, 12, 3),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(label, rowIndex);
            Grid.SetColumn(label, 0);
            _rows.Children.Add(label);

            var editor = CreateEditor(target, property);
            Grid.SetRow(editor, rowIndex);
            Grid.SetColumn(editor, 1);
            _rows.Children.Add(editor);

            rowIndex++;
        }
    }

    private static IEnumerable<PropertyInfo> GetBrowsableProperties(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead)
            {
                continue;
            }

            if (property.GetCustomAttribute<BrowsableAttribute>() is { Browsable: false })
            {
                continue;
            }

            yield return property;
        }
    }

    private static Control CreateEditor(object target, PropertyInfo property)
    {
        var value = SafeGetValue(target, property);
        var text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
        var isReadOnly = !property.CanWrite
            || property.GetCustomAttribute<ReadOnlyAttribute>() is { IsReadOnly: true }
            || !CanConvertFromString(property.PropertyType);

        if (isReadOnly)
        {
            return new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 3, 4, 3),
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        var box = new TextBox { Text = text, Margin = new Thickness(0, 1, 4, 1) };
        box.LostFocus += (_, _) => TryWriteBack(target, property, box.Text);
        return box;
    }

    private static object? SafeGetValue(object target, PropertyInfo property)
    {
        try
        {
            return property.GetValue(target);
        }
        catch (TargetInvocationException)
        {
            // A property that throws is a PropertyGrid fact of life (WinForms shows the
            // exception text in the cell); listing the row is more useful than failing.
            return null;
        }
    }

    private static bool CanConvertFromString(Type type) =>
        TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));

    private static void TryWriteBack(object target, PropertyInfo property, string? text)
    {
        try
        {
            var converted = TypeDescriptor.GetConverter(property.PropertyType)
                .ConvertFromString(null, CultureInfo.CurrentCulture, text ?? "");
            property.SetValue(target, converted);
        }
        catch (Exception)
        {
            // Same reason as SafeGetValue: an unparseable edit must not take the app down.
        }
    }
}
