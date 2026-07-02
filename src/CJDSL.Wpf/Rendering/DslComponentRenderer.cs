using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering;

/// <summary>
/// WPF 递归组件渲染器 — 根据 DslComponent.Type 选择对应渲染器或内置控件
/// </summary>
public static class DslComponentRenderer
{
    private static DslRendererRegistry? _registry;

    public static void Initialize(DslRendererRegistry registry) => _registry = registry;

    /// <summary>
    /// 渲染单个 DSL 组件为 WPF FrameworkElement
    /// </summary>
    public static FrameworkElement? RenderComponent(DslComponent component, DslRenderContext context)
    {
        // 可见性检查
        if (!CheckVisibility(component, context))
            return null;

        // 尝试注册表
        if (_registry?.Get(component.Type) is IDslComponentRenderer renderer)
            return renderer.Render(component, context);

        // 内置 fallback
        return component.Type switch
        {
            "form" => RenderForm(component, context),
            "list" => RenderList(component, context),
            "listItem" => RenderListItem(component, context),
            _ => RenderUnknown(component),
        };
    }

    private static bool CheckVisibility(DslComponent component, DslRenderContext context)
    {
        if (string.IsNullOrEmpty(component.VisibleIf)) return true;
        // 简单表达式求值: "key == value"
        return EvaluateSimpleExpression(component.VisibleIf, context);
    }

    private static bool EvaluateSimpleExpression(string expression, DslRenderContext context)
    {
        expression = expression.Trim();

        // 处理 && 和 || 组合
        if (expression.Contains("&&"))
        {
            return expression.Split("&&", StringSplitOptions.RemoveEmptyEntries)
                .All(part => EvaluateSimpleExpression(part.Trim(), context));
        }
        if (expression.Contains("||"))
        {
            return expression.Split("||", StringSplitOptions.RemoveEmptyEntries)
                .Any(part => EvaluateSimpleExpression(part.Trim(), context));
        }

        // 简单比较: key == value, key != value
        var ops = new[] { "==", "!=", ">", "<", ">=", "<=" };
        foreach (var op in ops)
        {
            var idx = expression.IndexOf(op, StringComparison.Ordinal);
            if (idx > 0)
            {
                var left = expression[..idx].Trim().Trim('\'', '"');
                var right = expression[(idx + op.Length)..].Trim().Trim('\'', '"');
                var value = context.DataStore.GetString(left) ?? "";

                return op switch
                {
                    "==" => string.Equals(value, right, StringComparison.OrdinalIgnoreCase),
                    "!=" => !string.Equals(value, right, StringComparison.OrdinalIgnoreCase),
                    _ => true,
                };
            }
        }

        // 布尔值
        if (bool.TryParse(expression, out var b)) return b;

        return true;
    }

    private static FrameworkElement RenderForm(DslComponent component, DslRenderContext context)
    {
        var stack = new StackPanel();
        foreach (var child in component.Children ?? Enumerable.Empty<DslComponent>())
        {
            var rendered = RenderComponent(child, context);
            if (rendered != null) stack.Children.Add(rendered);
        }
        return stack;
    }

    private static FrameworkElement RenderList(DslComponent component, DslRenderContext context)
    {
        var list = new ListBox
        {
            BorderThickness = new Thickness(0),
            FontSize = 13,
        };
        foreach (var child in component.Children ?? Enumerable.Empty<DslComponent>())
        {
            var rendered = RenderComponent(child, context);
            if (rendered != null) list.Items.Add(rendered);
        }
        return list;
    }

    private static FrameworkElement RenderListItem(DslComponent component, DslRenderContext context)
    {
        var textBlock = new TextBlock
        {
            Text = component.Label ?? "",
            Padding = new Thickness(8, 4, 8, 4),
        };
        return textBlock;
    }

    private static FrameworkElement RenderUnknown(DslComponent component)
    {
        return new TextBlock
        {
            Text = $"[未识别: {component.Type}]",
            Foreground = Brushes.Gray,
            FontSize = 12,
            FontStyle = FontStyles.Italic,
        };
    }
}
