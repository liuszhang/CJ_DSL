using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

/// <summary>
/// 组件渲染器接口 - 每种 DSL 组件类型对应一个渲染器
/// </summary>
public interface IDslComponentRenderer
{
    /// <summary>支持的组件类型</summary>
    string ComponentType { get; }

    /// <summary>渲染组件</summary>
    RenderFragment Render(DslComponent component, DslRenderContext context);
}

/// <summary>
/// 渲染器注册表 - 管理所有组件渲染器
/// </summary>
public class DslRendererRegistry
{
    private readonly Dictionary<string, IDslComponentRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IDslComponentRenderer renderer)
    {
        _renderers[renderer.ComponentType] = renderer;
    }

    public IDslComponentRenderer? Get(string componentType)
    {
        return _renderers.TryGetValue(componentType, out var renderer) ? renderer : null;
    }

    public bool HasRenderer(string componentType) => _renderers.ContainsKey(componentType);

    public IReadOnlyCollection<string> SupportedTypes => _renderers.Keys.ToList().AsReadOnly();
}
