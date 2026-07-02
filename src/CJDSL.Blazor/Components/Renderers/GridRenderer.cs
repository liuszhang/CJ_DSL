using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

public class GridRenderer : IDslComponentRenderer
{
    public string ComponentType => "grid";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;

        builder.OpenComponent<MudBlazor.MudGrid>(sequence++);
        builder.AddAttribute(sequence++, "Class", GetComponentStringProp(component, "Class"));

        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(gridBuilder =>
        {
            if (component.Children == null) return;
            var seq = 0;
            foreach (var child in component.Children)
            {
                var span = child.Span ?? 12;
                gridBuilder.OpenComponent<MudBlazor.MudItem>(seq++);
                gridBuilder.AddAttribute(seq++, "xs", (MudBlazor.Breakpoint)12);
                gridBuilder.AddAttribute(seq++, "sm", (MudBlazor.Breakpoint)Math.Min(span, 12));
                gridBuilder.AddAttribute(seq++, "md", (MudBlazor.Breakpoint)Math.Min(span, 12));
                gridBuilder.AddAttribute(seq++, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<DslComponentRenderer>(0);
                    itemBuilder.AddAttribute(1, "Component", child);
                    itemBuilder.CloseComponent();
                }));
                gridBuilder.CloseComponent();
            }
        }));

        builder.CloseComponent();
    };

    private static string? GetComponentStringProp(DslComponent component, string key)
    {
        return component.Props?.GetValueOrDefault(key) as string;
    }
}
