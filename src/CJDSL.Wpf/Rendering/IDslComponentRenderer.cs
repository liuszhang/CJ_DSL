using System.Windows;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering;

/// <summary>
/// WPF 组件渲染器接口 — 每种 DSL 组件类型对应一个渲染器
/// </summary>
public interface IDslComponentRenderer
{
    /// <summary>支持的组件类型</summary>
    string ComponentType { get; }

    /// <summary>将 DSL 组件渲染为 WPF 控件</summary>
    FrameworkElement Render(DslComponent component, DslRenderContext context);
}

/// <summary>
/// WPF 渲染器注册表
/// </summary>
public class DslRendererRegistry
{
    private readonly Dictionary<string, IDslComponentRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IDslComponentRenderer renderer) => _renderers[renderer.ComponentType] = renderer;
    public IDslComponentRenderer? Get(string componentType) => _renderers.TryGetValue(componentType, out var r) ? r : null;
    public bool HasRenderer(string componentType) => _renderers.ContainsKey(componentType);
    public IReadOnlyCollection<string> SupportedTypes => _renderers.Keys.ToList().AsReadOnly();
}
