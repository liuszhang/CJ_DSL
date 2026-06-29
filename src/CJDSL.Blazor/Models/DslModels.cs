using Microsoft.AspNetCore.Components;

namespace CJDSL.Blazor.Models;

/// <summary>
/// 目标渲染平台
/// </summary>
public enum TargetPlatform
{
    Web,
    Wpf,
    Maui,
    React,
    Vue
}

/// <summary>
/// DSL 渲染上下文
/// </summary>
public class DslRenderContext
{
    public DslPage Page { get; set; } = null!;
    public DslDataStore DataStore { get; } = new();
    public UserContext User { get; set; } = null!;
    public IExpressionEvaluator ExpressionEvaluator { get; set; } = null!;
    public TargetPlatform TargetPlatform { get; set; } = TargetPlatform.Web;
    public Dictionary<string, FormState> Forms { get; } = new();
    public object? RowData { get; set; }
    public Dictionary<string, object> ComponentRefs { get; } = new();
    public DslRenderContext? Parent { get; set; }
}

/// <summary>
/// 客户端 DSL 数据存储
/// </summary>
public class DslDataStore
{
    private readonly Dictionary<string, object> _data = new();
    public event EventHandler<DataChangedEventArgs>? DataChanged;

    public void Set(string path, object? value)
    {
        var oldValue = _data.GetValueOrDefault(path);
        if (value == null) _data.Remove(path);
        else _data[path] = value;
        DataChanged?.Invoke(this, new DataChangedEventArgs(path, oldValue, value));
    }

    public object? Get(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (path.StartsWith('@')) path = path[1..];
        var segments = path.Split('.');
        var current = _data.GetValueOrDefault(segments[0]);
        foreach (var segment in segments[1..])
        {
            if (current == null) return null;
            current = GetProperty(current, segment);
        }
        return current;
    }

    public T? Get<T>(string path) => Get(path) is T value ? value : default;
    public string? GetString(string path) => Get(path)?.ToString();
    public List<T>? GetList<T>(string path) => Get(path) as List<T>;

    public void Merge(Dictionary<string, object> data)
    {
        foreach (var kv in data) Set(kv.Key, kv.Value);
    }

    private static object? GetProperty(object target, string propertyName)
    {
        if (target is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object && jsonElement.TryGetProperty(propertyName, out var jsonProp))
                return jsonProp;
            return null;
        }
        var propInfo = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        return propInfo?.GetValue(target);
    }
}

public class DataChangedEventArgs : EventArgs
{
    public string Path { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public DataChangedEventArgs(string path, object? oldValue, object? newValue)
    {
        Path = path; OldValue = oldValue; NewValue = newValue;
    }
}

/// <summary>
/// 表单状态
/// </summary>
public class FormState
{
    private readonly Dictionary<string, object> _values = new();

    public void SetValue(string fieldName, object? value)
    {
        if (value == null) _values.Remove(fieldName);
        else _values[fieldName] = value;
    }

    public object? GetValue(string fieldName) => _values.GetValueOrDefault(fieldName);
    public Dictionary<string, object> GetValues() => new(_values);
    public void Reset() => _values.Clear();
}

/// <summary>
/// 选择项
/// </summary>
public class SelectItem
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;
}

/// <summary>
/// 用户上下文（客户端版本）
/// </summary>
public class UserContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public string? Department { get; set; }
    public string? TenantId { get; set; }

    public bool HasRole(string role) => Roles.Contains(role);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}

/// <summary>
/// 表达式求值器接口（客户端版本）
/// </summary>
public interface IExpressionEvaluator
{
    T Evaluate<T>(string expression, DslDataStore dataStore);
    bool CanEvaluate(string expression);
}

/// <summary>
/// DSL 页面
/// </summary>
public class DslPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Layout { get; set; } = "form";
    public TargetPlatform TargetPlatform { get; set; } = TargetPlatform.Web;
    public List<DslComponent> Components { get; set; } = new();
    public List<DslPageEvent>? PageEvents { get; set; }
}

public class DslPageEvent
{
    public string Type { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
}

public class DslComponent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "text";
    public Dictionary<string, object>? Props { get; set; }
    public List<DslComponent>? Children { get; set; }
    public string? DataBind { get; set; }
    public string? Label { get; set; }
    public string? FieldName { get; set; }
    public int? Span { get; set; } = 12;
    public string? VisibleIf { get; set; }
    public string? DisabledIf { get; set; }
    public List<DslEvent>? Events { get; set; }
    public DslDataSource? DataSource { get; set; }
    public List<DslValidationRule>? ValidationRules { get; set; }
    public DslStyle? Style { get; set; }
    public string? HelpText { get; set; }

    public IEnumerable<DslComponent> Descendants()
    {
        yield return this;
        if (Children == null) yield break;
        foreach (var child in Children.SelectMany(c => c.Descendants()))
            yield return child;
    }
}

public class DslEvent
{
    public string Type { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
    public DslConfirm? Confirm { get; set; }
    public int? DebounceMs { get; set; }
}

public class DslConfirm
{
    public string Title { get; set; } = "确认";
    public string Message { get; set; } = string.Empty;
    public string ConfirmText { get; set; } = "确认";
    public string CancelText { get; set; } = "取消";
}

public class DslDataSource
{
    public string Type { get; set; } = "api";
    public string? Endpoint { get; set; }
    public string Method { get; set; } = "GET";
    public string? Code { get; set; }
    public List<Dictionary<string, object>>? StaticData { get; set; }
    public Dictionary<string, object>? Params { get; set; }
    public string? SearchParam { get; set; }
    public DslPagination? Pagination { get; set; }
    public bool ServerSide { get; set; } = false;
    public string? DataPath { get; set; }
}

public class DslPagination
{
    public string PageParam { get; set; } = "pageIndex";
    public string SizeParam { get; set; } = "pageSize";
    public int DefaultSize { get; set; } = 20;
}

public class DslValidationRule
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Pattern { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string? Expression { get; set; }
}

public class DslStyle
{
    public string? Class { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public string? Margin { get; set; }
    public string? Padding { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
}

public class DslPermission
{
    public List<string>? RequiredRoles { get; set; }
    public List<string>? RequiredPermissions { get; set; }
}
