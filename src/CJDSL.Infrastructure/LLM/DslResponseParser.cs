using System.Text.Json;
using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Infrastructure.LLM;

/// <summary>
/// LLM 响应解析器 - 将 LLM 输出的 JSON 文本解析为 DslPage
/// </summary>
public interface IDslResponseParser
{
    DslPage? Parse(string json);
    DslPage? Parse(JsonElement element);
}

public class DslResponseParser : IDslResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DslPage? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        json = CleanJsonResponse(json);

        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return Parse(element);
        }
        catch
        {
            return null;
        }
    }

    public DslPage? Parse(JsonElement element)
    {
        try
        {
            var page = new DslPage
            {
                Id = GetString(element, "id") ?? Guid.NewGuid().ToString("N"),
                Title = GetString(element, "title") ?? "Untitled",
                Description = GetString(element, "description") ?? "",
                Layout = GetString(element, "layout") ?? "form",
                TargetPlatform = ParseTargetPlatform(GetString(element, "targetPlatform"))
            };

            if (element.TryGetProperty("components", out var components) && components.ValueKind == JsonValueKind.Array)
            {
                page.Components = ParseComponents(components);
            }

            if (element.TryGetProperty("dataSource", out var ds) && ds.ValueKind == JsonValueKind.Object)
            {
                page.DataSource = ParseDataSource(ds);
            }

            if (element.TryGetProperty("permission", out var perm) && perm.ValueKind == JsonValueKind.Object)
            {
                page.Permission = ParsePermission(perm);
            }

            return page;
        }
        catch
        {
            return null;
        }
    }

    private List<DslComponent> ParseComponents(JsonElement array)
    {
        var components = new List<DslComponent>();
        foreach (var item in array.EnumerateArray())
        {
            components.Add(ParseComponent(item));
        }
        return components;
    }

    private DslComponent ParseComponent(JsonElement element)
    {
        var component = new DslComponent
        {
            Id = GetString(element, "id") ?? Guid.NewGuid().ToString("N"),
            Type = GetString(element, "type") ?? "text",
            Label = GetString(element, "label"),
            FieldName = GetString(element, "fieldName"),
            DataBind = GetString(element, "dataBind"),
            Span = GetInt(element, "span") ?? 12,
            VisibleIf = GetString(element, "visibleIf"),
            DisabledIf = GetString(element, "disabledIf"),
            HelpText = GetString(element, "helpText")
        };

        if (element.TryGetProperty("props", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            component.Props = ParseProps(props);
        }

        if (element.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            component.Children = ParseComponents(children);
        }

        if (element.TryGetProperty("events", out var events) && events.ValueKind == JsonValueKind.Array)
        {
            component.Events = ParseEvents(events);
        }

        if (element.TryGetProperty("dataSource", out var ds) && ds.ValueKind == JsonValueKind.Object)
        {
            component.DataSource = ParseDataSource(ds);
        }

        if (element.TryGetProperty("validationRules", out var rules) && rules.ValueKind == JsonValueKind.Array)
        {
            component.ValidationRules = ParseValidationRules(rules);
        }

        if (element.TryGetProperty("style", out var style) && style.ValueKind == JsonValueKind.Object)
        {
            component.Style = ParseStyle(style);
        }

        return component;
    }

    private Dictionary<string, object> ParseProps(JsonElement element)
    {
        var props = new Dictionary<string, object>();
        foreach (var prop in element.EnumerateObject())
        {
            props[prop.Name] = ParseValue(prop.Value);
        }
        return props;
    }

    private object ParseValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => "",
            JsonValueKind.Array => ParseArray(element),
            JsonValueKind.Object => ParseProps(element),
            _ => element.ToString()
        };
    }

    private object ParseArray(JsonElement element)
    {
        var list = new List<object>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(ParseValue(item));
        }
        return list;
    }

    private List<DslEvent> ParseEvents(JsonElement array)
    {
        var events = new List<DslEvent>();
        foreach (var item in array.EnumerateArray())
        {
            events.Add(new DslEvent
            {
                Type = GetString(item, "type") ?? "",
                Handler = GetString(item, "handler") ?? "",
                DebounceMs = GetInt(item, "debounceMs"),
                Params = item.TryGetProperty("params", out var p) && p.ValueKind == JsonValueKind.Object
                    ? ParseProps(p) : null,
                Confirm = item.TryGetProperty("confirm", out var c) && c.ValueKind == JsonValueKind.Object
                    ? ParseConfirm(c) : null
            });
        }
        return events;
    }

    private DslConfirm ParseConfirm(JsonElement element)
    {
        return new DslConfirm
        {
            Title = GetString(element, "title") ?? "确认",
            Message = GetString(element, "message") ?? "",
            ConfirmText = GetString(element, "confirmText") ?? "确认",
            CancelText = GetString(element, "cancelText") ?? "取消"
        };
    }

    private DslDataSource ParseDataSource(JsonElement element)
    {
        return new DslDataSource
        {
            Type = GetString(element, "type") ?? "api",
            Endpoint = GetString(element, "endpoint"),
            Method = GetString(element, "method") ?? "GET",
            Code = GetString(element, "code"),
            SearchParam = GetString(element, "searchParam"),
            ServerSide = GetBool(element, "serverSide"),
            DataPath = GetString(element, "dataPath")
        };
    }

    private DslValidationRule ParseValidationRule(JsonElement element)
    {
        return new DslValidationRule
        {
            Type = GetString(element, "type") ?? "",
            Message = GetString(element, "message") ?? "",
            Pattern = GetString(element, "pattern"),
            MinLength = GetInt(element, "minLength"),
            MaxLength = GetInt(element, "maxLength"),
            Min = GetInt(element, "min"),
            Max = GetInt(element, "max"),
            Expression = GetString(element, "expression")
        };
    }

    private List<DslValidationRule> ParseValidationRules(JsonElement array)
    {
        var rules = new List<DslValidationRule>();
        foreach (var item in array.EnumerateArray())
        {
            rules.Add(ParseValidationRule(item));
        }
        return rules;
    }

    private DslPermission ParsePermission(JsonElement element)
    {
        var permission = new DslPermission();
        if (element.TryGetProperty("requiredRoles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            permission.RequiredRoles = new List<string>();
            foreach (var r in roles.EnumerateArray()) permission.RequiredRoles.Add(r.GetString() ?? "");
        }
        if (element.TryGetProperty("requiredPermissions", out var perms) && perms.ValueKind == JsonValueKind.Array)
        {
            permission.RequiredPermissions = new List<string>();
            foreach (var p in perms.EnumerateArray()) permission.RequiredPermissions.Add(p.GetString() ?? "");
        }
        return permission;
    }

    private DslStyle ParseStyle(JsonElement element)
    {
        return new DslStyle
        {
            Class = GetString(element, "class"),
            Color = GetString(element, "color"),
            BackgroundColor = GetString(element, "backgroundColor"),
            Margin = GetString(element, "margin"),
            Padding = GetString(element, "padding"),
            Width = GetString(element, "width"),
            Height = GetString(element, "height")
        };
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() : null;
    }

    private static int? GetInt(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32() : null;
    }

    private static bool GetBool(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.True;
    }

    private static string CleanJsonResponse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return rawText;
        rawText = rawText.Trim();
        if (rawText.StartsWith("```json")) rawText = rawText["```json".Length..];
        if (rawText.StartsWith("```")) rawText = rawText[3..];
        if (rawText.EndsWith("```")) rawText = rawText[..^3];
        return rawText.Trim();
    }

    private static TargetPlatform ParseTargetPlatform(string? value)
    {
        if (string.IsNullOrEmpty(value)) return TargetPlatform.Web;
        return Enum.TryParse<TargetPlatform>(value, true, out var result) ? result : TargetPlatform.Web;
    }
}
