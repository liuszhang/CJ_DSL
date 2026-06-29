using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class TabsRenderer : IDslComponentRenderer
{
    public string ComponentType => "tabs";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var tabControl = new TabControl
        {
            FontSize = 13,
        };

        if (component.Children != null)
        {
            foreach (var child in component.Children)
            {
                var tabItem = new TabItem
                {
                    Header = child.Label ?? "Tab",
                };

                var content = new StackPanel();
                if (child.Children != null)
                {
                    foreach (var grandChild in child.Children)
                    {
                        var rendered = DslComponentRenderer.RenderComponent(grandChild, context);
                        if (rendered != null) content.Children.Add(rendered);
                    }
                }

                tabItem.Content = content;
                tabControl.Items.Add(tabItem);
            }
        }

        return tabControl;
    }
}

public class ExpansionRenderer : IDslComponentRenderer
{
    public string ComponentType => "expansion";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var stack = new StackPanel();
        foreach (var child in component.Children ?? Enumerable.Empty<DslComponent>())
        {
            var group = new Expander
            {
                Header = child.Label ?? "",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4),
            };

            var content = new StackPanel();
            if (child.Children != null)
            {
                foreach (var grandChild in child.Children)
                {
                    var rendered = DslComponentRenderer.RenderComponent(grandChild, context);
                    if (rendered != null) content.Children.Add(rendered);
                }
            }

            group.Content = content;
            stack.Children.Add(group);
        }

        return stack;
    }
}
