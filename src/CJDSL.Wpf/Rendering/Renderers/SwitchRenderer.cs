using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class SwitchRenderer : IDslComponentRenderer
{
    public string ComponentType => "switch";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var checkBox = new CheckBox
        {
            Content = component.Label,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (!string.IsNullOrEmpty(component.FieldName))
        {
            var binding = new Binding($"Data.{component.FieldName}")
            {
                Source = context,
                Mode = BindingMode.TwoWay,
            };
            checkBox.SetBinding(ToggleButton.IsCheckedProperty, binding);
        }

        return checkBox;
    }
}

public class CheckboxRenderer : IDslComponentRenderer
{
    public string ComponentType => "checkbox";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var checkBox = new CheckBox
        {
            Content = component.Label,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (!string.IsNullOrEmpty(component.FieldName))
        {
            var binding = new Binding($"Data.{component.FieldName}")
            {
                Source = context,
                Mode = BindingMode.TwoWay,
            };
            checkBox.SetBinding(ToggleButton.IsCheckedProperty, binding);
        }

        return checkBox;
    }
}
