using System.Windows;
using System.Windows.Controls;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class StackRenderer : IDslComponentRenderer
{
    public string ComponentType => "stack";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var isRow = component.Props?.TryGetValue("Row", out var row) == true && row is true;

        var panel = new StackPanel
        {
            Orientation = isRow ? Orientation.Horizontal : Orientation.Vertical,
        };

        if (component.Props?.TryGetValue("Spacing", out var spacing) == true && spacing is int sp)
            panel.Margin = new Thickness(0, 0, 0, sp);

        foreach (var child in component.Children ?? Enumerable.Empty<DslComponent>())
        {
            var rendered = DslComponentRenderer.RenderComponent(child, context);
            if (rendered != null)
                panel.Children.Add(rendered);
        }

        return panel;
    }
}
