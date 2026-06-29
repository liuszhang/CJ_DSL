using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class DividerRenderer : IDslComponentRenderer
{
    public string ComponentType => "divider";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            Margin = new Thickness(0, 8, 0, 8),
        };
    }
}
