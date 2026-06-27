using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

public class StackRenderer : IDslComponentRenderer
{
    public string ComponentType => "stack";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;

        builder.OpenComponent<MudBlazor.MudStack>(sequence++);
        builder.AddAttribute(sequence++, "Row", GetComponentBoolProp(component, "Row"));
        builder.AddAttribute(sequence++, "Spacing", GetComponentIntProp(component, "Spacing", 2));
        builder.AddAttribute(sequence++, "Justify", GetJustify(component));
        builder.AddAttribute(sequence++, "AlignItems", GetAlignItems(component));
        builder.AddAttribute(sequence++, "Class", GetComponentStringProp(component, "Class"));

        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(stackBuilder =>
        {
            if (component.Children == null) return;
            var seq = 0;
            foreach (var child in component.Children)
            {
                stackBuilder.OpenComponent<DslComponentRenderer>(seq++);
                stackBuilder.AddAttribute(seq++, "Component", child);
                stackBuilder.CloseComponent();
            }
        }));

        builder.CloseComponent();
    };

    private static bool GetComponentBoolProp(DslComponent component, string key)
    {
        return component.Props?.GetValueOrDefault(key) is bool v && v;
    }

    private static int GetComponentIntProp(DslComponent component, string key, int defaultValue)
    {
        return component.Props?.GetValueOrDefault(key) is int v ? v : defaultValue;
    }

    private static string? GetComponentStringProp(DslComponent component, string key)
    {
        return component.Props?.GetValueOrDefault(key) as string;
    }

    private static MudBlazor.Justify GetJustify(DslComponent component)
    {
        var justifyStr = GetComponentStringProp(component, "Justify");
        return Enum.TryParse<MudBlazor.Justify>(justifyStr, out var j) ? j : MudBlazor.Justify.FlexStart;
    }

    private static MudBlazor.AlignItems GetAlignItems(DslComponent component)
    {
        var alignStr = GetComponentStringProp(component, "AlignItems");
        return Enum.TryParse<MudBlazor.AlignItems>(alignStr, out var a) ? a : MudBlazor.AlignItems.Center;
    }
}
