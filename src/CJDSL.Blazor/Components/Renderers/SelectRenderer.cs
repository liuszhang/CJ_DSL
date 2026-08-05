using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Components.Renderers;

public class SelectRenderer : IDslComponentRenderer
{
    public string ComponentType => "select";

    public RenderFragment Render(DslComponent component, DslRenderContext context) => builder =>
    {
        var sequence = 0;
        // 2026-08-05 修复：初始值优先读 FieldName（data.{FieldName} 回显通道），其次 DataBind。
        var stringValue = !string.IsNullOrEmpty(component.FieldName)
            ? context.GetFieldValue(component.FieldName)?.ToString() ?? ""
            : context.DataStore.GetString(component.DataBind) ?? "";

        builder.OpenComponent<MudBlazor.MudSelect<string>>(sequence++);
        builder.AddAttribute(sequence++, "Value", stringValue);
        builder.AddAttribute(sequence++, "ValueChanged", EventCallback.Factory.Create<string>(this, v =>
        {
            // 同 TextRenderer：必须经 SetFieldValue 写入（DataStore + FormState），否则表单提交收集不到。
            context.SetFieldValue(component.FieldName ?? component.Id, v);
        }));
        builder.AddAttribute(sequence++, "Label", component.Label);
        builder.AddAttribute(sequence++, "Required", GetComponentBoolProp(component, "Required"));
        builder.AddAttribute(sequence++, "Clearable", GetComponentBoolProp(component, "Clearable"));
        builder.AddAttribute(sequence++, "Variant", GetVariant(component));
        builder.AddAttribute(sequence++, "Disabled", IsDisabled(component, context));
        builder.AddAttribute(sequence++, "Class", GetComponentStringProp(component, "Class"));

        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(selectBuilder =>
        {
            var items = GetSelectItems(component, context);
            var seq = 0;
            foreach (var item in items)
            {
                selectBuilder.OpenComponent<MudBlazor.MudSelectItem<string>>(seq++);
                selectBuilder.AddAttribute(seq++, "Value", item.Value);
                selectBuilder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(0, item.Label)));
                selectBuilder.CloseComponent();
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

    private static MudBlazor.Variant GetVariant(DslComponent component)
    {
        var variantStr = GetComponentStringProp(component, "Variant");
        return Enum.TryParse<MudBlazor.Variant>(variantStr, out var v) ? v : MudBlazor.Variant.Text;
    }

    private static List<SelectItem> GetSelectItems(DslComponent component, DslRenderContext context)
    {
        // 2026-08-05：内嵌 items（FieldsToDslConverter 生成 props["items"] = [{value,label}...]）
        if (component.Props?.GetValueOrDefault("items") is List<Dictionary<string, object>> propItems)
        {
            return propItems.Select(d => new SelectItem
            {
                Value = d.GetValueOrDefault("value")?.ToString() ?? "",
                Label = d.GetValueOrDefault("label")?.ToString() ?? ""
            }).ToList();
        }
        if (component.DataSource?.Type == "dictionary" || component.DataSource?.Type == "enum")
        {
            return context.DataStore.GetList<SelectItem>($"dict.{component.DataSource.Code}") ?? new List<SelectItem>();
        }
        if (component.DataSource?.Type == "static" && component.DataSource.StaticData != null)
        {
            return component.DataSource.StaticData.Select(d => new SelectItem
            {
                Value = d.GetValueOrDefault("value")?.ToString() ?? "",
                Label = d.GetValueOrDefault("label")?.ToString() ?? ""
            }).ToList();
        }
        return new List<SelectItem>();
    }
}
