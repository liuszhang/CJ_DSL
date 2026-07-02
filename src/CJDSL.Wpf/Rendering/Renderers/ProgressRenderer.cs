using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class ProgressRenderer : IDslComponentRenderer
{
    public string ComponentType => "progress";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var value = 0.5;
        if (component.Props?.TryGetValue("Value", out var v) == true && v is double dv)
            value = dv;

        var max = 100.0;
        if (component.Props?.TryGetValue("Max", out var m) == true && m is double dm)
            max = dm;

        var ratio = max > 0 ? value / max : 0;

        var progressBar = new ProgressBar
        {
            Height = 4,
            Minimum = 0,
            Maximum = 100,
            Value = ratio * 100,
            Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            Foreground = new SolidColorBrush(Color.FromRgb(66, 165, 245)),
        };

        return progressBar;
    }
}
