using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace CJDSL.Blazor.Components.Renderers;

public class CardRenderer : IDslComponentRenderer
{
    public string ComponentType => "card";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;

        builder.OpenComponent<MudBlazor.MudCard>(sequence++);
        builder.AddAttribute(sequence++, "Elevation", GetComponentIntProp(component, "Elevation", 1));
        builder.AddAttribute(sequence++, "Class", GetComponentStringProp(component, "Class"));

        if (!string.IsNullOrEmpty(component.Label))
        {
            builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(cardBuilder =>
            {
                cardBuilder.OpenComponent<MudBlazor.MudCardHeader>(0);
                cardBuilder.AddAttribute(1, "CardHeaderContent", (RenderFragment)(headerBuilder =>
                {
                    headerBuilder.OpenComponent<MudBlazor.MudText>(0);
                    headerBuilder.AddAttribute(1, "Typo", MudBlazor.Typo.h6);
                    headerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, component.Label)));
                    headerBuilder.CloseComponent();
                }));
                cardBuilder.CloseComponent();

                cardBuilder.OpenComponent<MudBlazor.MudCardContent>(2);
                cardBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(contentBuilder =>
                {
                    RenderChildren(contentBuilder, component, context);
                }));
                cardBuilder.CloseComponent();
            }));
        }
        else
        {
            builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(contentBuilder =>
            {
                contentBuilder.OpenComponent<MudBlazor.MudCardContent>(0);
                contentBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    RenderChildren(innerBuilder, component, context);
                }));
                contentBuilder.CloseComponent();
            }));
        }

        builder.CloseComponent();
    };

    private static void RenderChildren(RenderTreeBuilder builder, DslComponent component, DslRenderContext context)
    {
        if (component.Children == null) return;
        var seq = 0;
        foreach (var child in component.Children)
        {
            builder.OpenComponent<DslComponentRenderer>(seq++);
            builder.AddAttribute(seq++, "Component", child);
            builder.CloseComponent();
        }
    }

    private static int GetComponentIntProp(DslComponent component, string key, int defaultValue)
    {
        return component.Props?.GetValueOrDefault(key) is int v ? v : defaultValue;
    }

    private static string? GetComponentStringProp(DslComponent component, string key)
    {
        return component.Props?.GetValueOrDefault(key) as string;
    }
}
