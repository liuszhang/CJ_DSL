using System.Windows;
using System.Windows.Controls;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class GridRenderer : IDslComponentRenderer
{
    public string ComponentType => "grid";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var grid = new Grid();
        var columns = component.Children?.Count ?? 0;
        if (columns == 0) columns = 1;

        // 默认 12 列网格
        var totalCols = 12;
        for (var i = 0; i < totalCols; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var currentCol = 0;
        foreach (var child in component.Children ?? Enumerable.Empty<DslComponent>())
        {
            var span = child.Span ?? 12;
            var colSpan = (int)Math.Ceiling(span / (12.0 / totalCols));
            if (colSpan < 1) colSpan = 1;

            if (currentCol + colSpan > totalCols)
                currentCol = 0;

            var rendered = DslComponentRenderer.RenderComponent(child, context);
            if (rendered != null)
            {
                Grid.SetColumn(rendered, currentCol);
                Grid.SetColumnSpan(rendered, colSpan);
                grid.Children.Add(rendered);
            }

            currentCol += colSpan;
        }

        return grid;
    }
}
