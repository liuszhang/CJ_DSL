using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class PaperRenderer : IDslComponentRenderer
{
    public string ComponentType => "paper";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var border = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
        };

        var stack = new StackPanel();
        foreach (var child in component.Children ?? Enumerable.Empty<DslComponent>())
        {
            var rendered = DslComponentRenderer.RenderComponent(child, context);
            if (rendered != null) stack.Children.Add(rendered);
        }

        border.Child = stack;
        return border;
    }
}
