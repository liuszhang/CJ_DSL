using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class TextAreaRenderer : IDslComponentRenderer
{
    public string ComponentType => "textarea";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var lines = 3;
        if (component.Props?.TryGetValue("Lines", out var l) == true && l is int linesVal)
            lines = linesVal;

        var textBox = new TextBox
        {
            FontSize = 13,
            Padding = new Thickness(8, 5, 8, 5),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = lines * 24,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        if (component.Props?.TryGetValue("MaxLength", out var ml) == true && ml is int maxLen)
            textBox.MaxLength = maxLen;

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
