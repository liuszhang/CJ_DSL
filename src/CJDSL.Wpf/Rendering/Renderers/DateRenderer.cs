using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class DateRenderer : IDslComponentRenderer
{
    public string ComponentType => "date";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var picker = new DatePicker
        {
            FontSize = 13,
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (!string.IsNullOrEmpty(component.FieldName))
        {
            var binding = new Binding($"Data.{component.FieldName}")
            {
                Source = context,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            picker.SetBinding(DatePicker.SelectedDateProperty, binding);
        }

        return picker;
    }
}

public class TimeRenderer : IDslComponentRenderer
{
    public string ComponentType => "time";

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
