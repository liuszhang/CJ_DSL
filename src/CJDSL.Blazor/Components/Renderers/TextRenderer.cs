using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

public class TextRenderer : IDslComponentRenderer
{
    public string ComponentType => "text";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;
        var stringValue = context.DataStore.GetString(component.DataBind) ?? "";

        builder.OpenComponent<MudBlazor.MudTextField<string>>(sequence++);
        builder.AddAttribute(sequence++, "Value", stringValue);
        builder.AddAttribute(sequence++, "ValueChanged", EventCallback.Factory.Create<string>(this, v =>
        {
            context.DataStore.Set(component.FieldName ?? component.Id, v);
        }));
        builder.AddAttribute(sequence++, "Label", component.Label);
        builder.AddAttribute(sequence++, "Required", GetComponentBoolProp(component, "Required"));
        builder.AddAttribute(sequence++, "ReadOnly", GetComponentBoolProp(component, "ReadOnly"));
        builder.AddAttribute(sequence++, "Variant", GetVariant(component));
        builder.AddAttribute(sequence++, "Placeholder", GetComponentStringProp(component, "Placeholder"));
        builder.AddAttribute(sequence++, "Disabled", IsDisabled(component, context));
        builder.AddAttribute(sequence++, "Class", GetComponentStringProp(component, "Class"));
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

    private static MudBlazor.Variant GetVariant(DslComponent component)
    {
        var variantStr = GetComponentStringProp(component, "Variant");
        return Enum.TryParse<MudBlazor.Variant>(variantStr, out var v) ? v : MudBlazor.Variant.Text;
    }
}
