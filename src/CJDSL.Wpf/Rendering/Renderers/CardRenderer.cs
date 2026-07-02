using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class CardRenderer : IDslComponentRenderer
{
    public string ComponentType => "card";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var border = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8),
        };

        if (component.Props?.TryGetValue("Elevation", out var elevation) == true && elevation is int e)
        {
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = e * 4,
                ShadowDepth = e,
                Opacity = 0.15,
                Color = Colors.Black,
            };
        }

        var stack = new StackPanel();
        foreach (var child in component.Children ?? Enumerable.Empty<DslComponent>())
        {
            var rendered = context.ElementRefs.ContainsKey(child.Id)
                ? context.ElementRefs[child.Id]
                : DslComponentRenderer.RenderComponent(child, context);
            if (rendered != null)
                stack.Children.Add(rendered);
        }

        border.Child = stack;
        return border;
    }
}
