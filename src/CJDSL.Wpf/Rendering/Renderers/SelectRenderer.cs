using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class SelectRenderer : IDslComponentRenderer
{
    public string ComponentType => "select";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var comboBox = new ComboBox
        {
            FontSize = 13,
            Padding = new Thickness(4),
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        // 加载选项
        if (component.DataSource?.Type is "enum" or "dictionary" or "static")
        {
            var items = LoadSelectItems(component, context);
            foreach (var item in items)
                comboBox.Items.Add(item);
        }

        if (!string.IsNullOrEmpty(component.FieldName))
        {
            var binding = new Binding($"Data.{component.FieldName}")
            {
                Source = context,
                Mode = BindingMode.TwoWay,
            };
            comboBox.SetBinding(ComboBox.SelectedValueProperty, binding);
        }

        return comboBox;
    }

    private static List<KeyValuePair<string, string>> LoadSelectItems(DslComponent component, DslRenderContext context)
    {
        var result = new List<KeyValuePair<string, string>>();

        if (component.DataSource?.Type == "static" && component.DataSource.StaticData != null)
        {
            foreach (var item in component.DataSource.StaticData)
            {
                var value = item.GetValueOrDefault("value")?.ToString() ?? "";
                var label = item.GetValueOrDefault("label")?.ToString() ?? value;
                result.Add(new KeyValuePair<string, string>(value, label));
            }
        }

        return result;
    }
}
