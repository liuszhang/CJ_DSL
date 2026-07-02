using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class NumberRenderer : IDslComponentRenderer
{
    public string ComponentType => "number";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var textBox = new TextBox
        {
            FontSize = 13,
            Padding = new Thickness(8, 5, 8, 5),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        textBox.PreviewTextInput += (_, e) =>
        {
            var proposed = textBox.Text.Insert(textBox.CaretIndex, e.Text);
            double _ignored;
            e.Handled = !double.TryParse(proposed, NumberStyles.Any, CultureInfo.InvariantCulture, out _ignored);
        };

        if (!string.IsNullOrEmpty(component.FieldName))
        {
            var binding = new Binding($"Data.{component.FieldName}")
            {
                Source = context,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            textBox.SetBinding(TextBox.TextProperty, binding);
        }

        return textBox;
    }
}
