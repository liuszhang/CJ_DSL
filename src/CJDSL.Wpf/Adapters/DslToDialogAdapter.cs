using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Adapters;

/// <summary>
/// 将 CJDSL DslPage 转换为简化的 DialogDefinition（兼容 ABWork 的 DynamicDialogService）
/// </summary>
public static class DslToDialogAdapter
{
    /// <summary>
    /// 从 DslPage 提取表单字段，构建 DialogField 列表
    /// </summary>
    public static List<DialogFieldAdapter> ExtractFields(DslPage page)
    {
        var fields = new List<DialogFieldAdapter>();
        foreach (var component in page.GetAllComponents())
        {
            if (string.IsNullOrEmpty(component.FieldName)) continue;
            if (component.Type is "card" or "grid" or "stack" or "divider" or "tabs" or "form" or "button") continue;

            fields.Add(new DialogFieldAdapter
            {
                Key = component.FieldName,
                Label = component.Label ?? component.FieldName,
                FieldType = MapFieldType(component.Type),
                Required = component.Props?.TryGetValue("Required", out var r) == true && r is true,
                Placeholder = component.Props?.TryGetValue("Placeholder", out var p) == true ? p?.ToString() : null,
                VisibleIf = component.VisibleIf,
                Options = ExtractOptions(component),
            });
        }
        return fields;
    }

    /// <summary>
    /// 从 DslPage 提取按钮定义
    /// </summary>
    public static List<DialogButtonAdapter> ExtractButtons(DslPage page)
    {
        var buttons = new List<DialogButtonAdapter>();
        foreach (var component in page.GetAllComponents())
        {
            if (component.Type != "button") continue;
            if (component.Events == null || component.Events.Count == 0) continue;

            var primaryEvent = component.Events.FirstOrDefault(e => e.Type == "onClick");
            if (primaryEvent == null) continue;

            buttons.Add(new DialogButtonAdapter
            {
                Label = component.Label ?? "Button",
                Action = primaryEvent.Handler switch
                {
                    "submit" or "apiCall" => "Submit",
                    "reset" => "Cancel",
                    _ => "Custom",
                },
                CustomTag = primaryEvent.Handler,
            });
        }
        return buttons;
    }

    private static string MapFieldType(string componentType) => componentType switch
    {
        "text" => "TextBox",
        "textarea" => "TextArea",
        "number" => "Number",
        "date" or "datetime" => "DatePicker",
        "select" or "autocomplete" => "Select",
        "switch" or "checkbox" => "CheckBox",
        _ => "TextBox",
    };

    private static List<KeyValuePair<string, string>>? ExtractOptions(DslComponent component)
    {
        if (component.DataSource?.Type == "static" && component.DataSource.StaticData != null)
        {
            return component.DataSource.StaticData
                .Select(d => new KeyValuePair<string, string>(
                    d.GetValueOrDefault("value")?.ToString() ?? "",
                    d.GetValueOrDefault("label")?.ToString() ?? ""))
                .ToList();
        }
        return null;
    }
}

public class DialogFieldAdapter
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string FieldType { get; set; } = "TextBox";
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? VisibleIf { get; set; }
    public List<KeyValuePair<string, string>>? Options { get; set; }
}

public class DialogButtonAdapter
{
    public string Label { get; set; } = "";
    public string Action { get; set; } = "Submit";
    public string? CustomTag { get; set; }
}
