using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class TextDisplayRenderer : IDslComponentRenderer
{
    public string ComponentType => "textDisplay";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var textBlock = new TextBlock
        {
            Text = component.Label ?? "",
            TextWrapping = TextWrapping.Wrap,
        };

        if (component.Props?.TryGetValue("Typo", out var typo) == true)
        {
            switch (typo.ToString())
            {
                case "h4":
                    textBlock.FontSize = 24;
                    textBlock.FontWeight = FontWeights.Bold;
                    break;
                case "h5":
                    textBlock.FontSize = 20;
                    textBlock.FontWeight = FontWeights.SemiBold;
                    break;
                case "h6":
                    textBlock.FontSize = 17;
                    textBlock.FontWeight = FontWeights.SemiBold;
                    break;
                case "body1":
                    textBlock.FontSize = 14;
                    break;
                case "body2":
                    textBlock.FontSize = 13;
                    break;
                case "caption":
                    textBlock.FontSize = 11;
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                    break;
                default:
                    textBlock.FontSize = 14;
                    break;
            }
        }
        else
        {
            textBlock.FontSize = 14;
        }

        if (!string.IsNullOrEmpty(component.DataBind))
        {
            textBlock.Text = context.DataStore.GetString(component.DataBind) ?? component.Label ?? "";
        }

        return textBlock;
    }
}
