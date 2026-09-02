using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

/// <summary>
/// flow 渲染器：将溯源路径 DSL（nodes/edges/eliminated）渲染为结构化 hop 链。
/// 实际渲染与交互状态由 FlowView.razor 承载（选中高亮、事件派发）。
/// </summary>
public class FlowRenderer : IDslComponentRenderer
{
    public string ComponentType => "flow";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;
        builder.OpenComponent<FlowView>(sequence++);
        builder.AddAttribute(sequence++, "Component", component);
        builder.AddAttribute(sequence++, "Context", context);
        builder.CloseComponent();
    };
}
