using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class ChipRenderer : IDslComponentRenderer
{
    public string ComponentType => "chip";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(224, 234, 246)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 4, 12, 4),
            Margin = new Thickness(0, 0, 4, 0),
        };

        var text = new TextBlock
        {
            Text = component.Label ?? "",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
        };

        border.Child = text;
        return border;
    }
}

public class AvatarRenderer : IDslComponentRenderer
{
    public string ComponentType => "avatar";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var ellipse = new Ellipse
        {
            Width = 40,
            Height = 40,
            Fill = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
        };

        var text = new TextBlock
        {
            Text = component.Label ?? "",
            FontSize = 16,
            FontWeight = FontWeights.Medium,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var grid = new Grid();
        grid.Children.Add(ellipse);
        grid.Children.Add(text);

        return grid;
    }
}
