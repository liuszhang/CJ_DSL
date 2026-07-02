using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

public class TextDisplayRenderer : IDslComponentRenderer
{
    public string ComponentType => "textDisplay";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;
        var displayText = component.DataBind != null
            ? context.DataStore.GetString(component.DataBind) ?? component.Label ?? ""
            : component.Label ?? "";

        builder.OpenComponent<MudBlazor.MudText>(sequence++);
        builder.AddAttribute(sequence++, "Typo", GetTypo(component));
        builder.AddAttribute(sequence++, "Class", GetComponentStringProp(component, "Class"));
        builder.AddAttribute(sequence++, "Color", GetColor(component));
        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(b => b.AddContent(0, displayText)));
        builder.CloseComponent();
    };

    private static string? GetComponentStringProp(DslComponent component, string key)
    {
        return component.Props?.GetValueOrDefault(key) as string;
    }

    private static MudBlazor.Typo GetTypo(DslComponent component)
    {
        var typoStr = GetComponentStringProp(component, "Typo");
        return Enum.TryParse<MudBlazor.Typo>(typoStr, out var t) ? t : MudBlazor.Typo.body1;
    }

    private static MudBlazor.Color GetColor(DslComponent component)
    {
        var colorStr = GetComponentStringProp(component, "Color");
        return Enum.TryParse<MudBlazor.Color>(colorStr, out var c) ? c : MudBlazor.Color.Default;
    }
}
