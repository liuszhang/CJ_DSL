using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class ButtonRenderer : IDslComponentRenderer
{
    public string ComponentType => "button";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var button = new Button
        {
            Content = component.Label ?? "Button",
            MinWidth = 80,
            Height = 32,
            FontSize = 13,
            Margin = new Thickness(4, 0, 4, 0),
            Padding = new Thickness(16, 0, 16, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            BorderThickness = new Thickness(0),
        };

        // 样式映射
        if (component.Props?.TryGetValue("Variant", out var variant) == true)
        {
            var v = variant.ToString();
            if (v == "Filled")
            {
                button.Background = new SolidColorBrush(Color.FromRgb(66, 165, 245));
                button.Foreground = Brushes.White;
            }
        }

        if (component.Props?.TryGetValue("Color", out var color) == true)
        {
            button.Background = ParseColor(color.ToString() ?? "Default") ?? button.Background;
        }

        // 事件处理
        if (component.Events != null)
        {
            button.Click += async (_, _) =>
            {
                foreach (var evt in component.Events.Where(e => e.Type == "onClick"))
                {
                    await HandleEventAsync(evt, component, context);
                }
            };
        }

        return button;
    }

    private async Task HandleEventAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        switch (evt.Handler)
        {
            case "setValue":
                if (evt.Params?.ContainsKey("path") == true && evt.Params?.ContainsKey("value") == true)
                    context.DataStore.Set(evt.Params["path"]?.ToString()!, evt.Params["value"]);
                break;
            case "refresh":
                context.RequestRender();
                break;
            default:
                break;
        }
        await Task.CompletedTask;
    }

    private static Brush? ParseColor(string colorName) => colorName switch
    {
        "Primary" => new SolidColorBrush(Color.FromRgb(66, 165, 245)),
        "Secondary" => new SolidColorBrush(Color.FromRgb(120, 120, 120)),
        "Success" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
        "Error" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
        "Warning" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
        "Info" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
        _ => null,
    };
}
