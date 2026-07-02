using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

public class DividerRenderer : IDslComponentRenderer
{
    public string ComponentType => "divider";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        builder.OpenComponent<MudBlazor.MudDivider>(0);
        builder.AddAttribute(1, "Class", component.Props?.GetValueOrDefault("Class") as string);
        builder.CloseComponent();
    };
}
