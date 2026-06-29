using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class TextRenderer : IDslComponentRenderer
{
    public string ComponentType => "text";

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

        if (component.Props?.TryGetValue("MaxLength", out var ml) == true && ml is int maxLen)
            textBox.MaxLength = maxLen;

        if (component.Props?.TryGetValue("ReadOnly", out var ro) == true && ro is bool readOnly)
            textBox.IsReadOnly = readOnly;

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
        else if (!string.IsNullOrEmpty(component.DataBind))
        {
            var binding = new Binding(component.DataBind)
            {
                Source = context.DataStore,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            textBox.SetBinding(TextBox.TextProperty, binding);
        }

        return textBox;
    }
}
