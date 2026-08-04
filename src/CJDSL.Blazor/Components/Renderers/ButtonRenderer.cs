using CJDSL.Blazor.Events;
using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CJDSL.Blazor.Components.Renderers;

public class ButtonRenderer : IDslComponentRenderer
{
    public string ComponentType => "button";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;

        builder.OpenComponent<MudBlazor.MudButton>(sequence++);
        builder.AddAttribute(sequence++, "Variant", GetButtonVariant(component));
        builder.AddAttribute(sequence++, "Color", GetColor(component));
        builder.AddAttribute(sequence++, "Size", GetSize(component));
        builder.AddAttribute(sequence++, "Disabled", IsDisabled(component, context));
        builder.AddAttribute(sequence++, "Class", GetComponentStringProp(component, "Class"));
        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(b => b.AddContent(0, component.Label ?? "Button")));
        builder.AddAttribute(sequence++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, async _ =>
        {
            var action = GetComponentStringProp(component, "action");
            if (!string.IsNullOrEmpty(action))
            {
                var handler = context.OnAction ?? DslRenderContext.GlobalActionHandler;
                if (handler != null)
                    await handler.Invoke(action);
            }

            if (component.Events == null) return;
            foreach (var evt in component.Events.Where(e => e.Type == "onClick"))
            {
                // Events are handled by DslComponentRenderer's parent logic
            }
        }));
        builder.CloseComponent();
    };

    private static bool GetComponentBoolProp(DslComponent component, string key)
    {
        return component.Props?.GetValueOrDefault(key) is bool v && v;
    }

    private static string? GetComponentStringProp(DslComponent component, string key)
    {
        return component.Props?.GetValueOrDefault(key) as string;
    }

    private static bool IsDisabled(DslComponent component, DslRenderContext context)
    {
        if (string.IsNullOrEmpty(component.DisabledIf)) return false;
        return context.ExpressionEvaluator.Evaluate<bool>(component.DisabledIf, context.DataStore);
    }

    private static MudBlazor.Variant GetButtonVariant(DslComponent component)
    {
        var variantStr = GetComponentStringProp(component, "Variant");
        return Enum.TryParse<MudBlazor.Variant>(variantStr, out var v) ? v : MudBlazor.Variant.Filled;
    }

    private static MudBlazor.Color GetColor(DslComponent component)
    {
        var colorStr = GetComponentStringProp(component, "Color");
        return Enum.TryParse<MudBlazor.Color>(colorStr, out var c) ? c : MudBlazor.Color.Default;
    }

    private static MudBlazor.Size GetSize(DslComponent component)
    {
        var sizeStr = GetComponentStringProp(component, "Size");
        return Enum.TryParse<MudBlazor.Size>(sizeStr, out var s) ? s : MudBlazor.Size.Medium;
    }
}
