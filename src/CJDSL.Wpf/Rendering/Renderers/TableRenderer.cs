using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering.Renderers;

public class TableRenderer : IDslComponentRenderer
{
    public string ComponentType => "table";

    public FrameworkElement Render(DslComponent component, DslRenderContext context)
    {
        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(1),
            FontSize = 13,
            RowBackground = Brushes.White,
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
        };

        // 解析 columns
        if (component.Props?.TryGetValue("columns", out var colsObj) == true &&
            colsObj is List<Dictionary<string, object>> columns)
        {
            foreach (var col in columns)
            {
                var header = col.GetValueOrDefault("title")?.ToString() ?? "";
                var dataIndex = col.GetValueOrDefault("dataIndex")?.ToString() ?? "";
                if (string.IsNullOrEmpty(dataIndex)) continue;

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = header,
                    Binding = new Binding($"[{dataIndex}]"),
                    MinWidth = 60,
                });
            }
        }

        // 数据绑定
        if (!string.IsNullOrEmpty(component.DataBind))
        {
            var binding = new Binding(component.DataBind)
            {
                Source = context.DataStore,
            };
            dataGrid.SetBinding(ItemsControl.ItemsSourceProperty, binding);
        }

        return dataGrid;
    }
}
