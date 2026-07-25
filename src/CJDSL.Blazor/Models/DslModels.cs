using Microsoft.AspNetCore.Components;
using MudBlazor;

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
/// DSL 事件派发器接口（避免 Models 直接依赖具体实现）
/// </summary>
public interface IDslEventDispatcher
{
    /// <summary>派发事件。返回 false 表示事件被中断/失败（用于中断事件链）</summary>
    Task<bool> DispatchAsync(DslEvent evt, DslComponent component, DslRenderContext context);
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

    /// <summary>页面级事件派发器（由 DslPageRenderer 创建并注入）</summary>
    public IDslEventDispatcher? EventDispatcher { get; set; }

    /// <summary>
    /// 当前所处的 MudDialog 实例（仅当该上下文位于对话框内时非空）。
    /// closeModal Handler 通过它关闭对话框。
    /// </summary>
    public IMudDialogInstance? DialogInstance { get; set; }

    /// <summary>
    /// 数据刷新回调（refresh Handler 触发）。由 DslPageRenderer 注入为 StateHasChanged + 重新加载数据源。
    /// </summary>
    public Func<Task>? OnRefresh { get; set; }

    /// <summary>
    /// 注册表单：以 form 组件的 Id 为 formId，登记其后代中声明了 FieldName 的字段，
    /// 使输入组件的值变更能自动回写对应 FormState。
    /// </summary>
    public FormState RegisterForm(DslComponent formComponent)
    {
        if (!Forms.TryGetValue(formComponent.Id, out var state))
        {
            state = new FormState();
            Forms[formComponent.Id] = state;
        }

        foreach (var descendant in formComponent.Descendants())
        {
            if (!string.IsNullOrEmpty(descendant.FieldName))
                state.RegisterField(descendant.FieldName);
        }
        return state;
    }

    /// <summary>
    /// 写入字段值：同时更新 DataStore（供表达式/模板消费）与所有登记了该字段的 FormState（供 apiCall/submit 提交）。
    /// </summary>
    public void SetFieldValue(string fieldName, object? value)
    {
        if (string.IsNullOrEmpty(fieldName)) return;

        DataStore.Set($"data.{fieldName}", value);

        foreach (var form in Forms.Values)
        {
            if (form.HasField(fieldName))
                form.SetValue(fieldName, value);
        }
    }

    /// <summary>读取字段初始值（优先 FormState，其次 DataStore）</summary>
    public object? GetFieldValue(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return null;

        foreach (var form in Forms.Values)
        {
            var value = form.GetValue(fieldName);
            if (value != null) return value;
        }
        return DataStore.Get($"data.{fieldName}");
    }

    /// <summary>按钮操作回调：接收 Props["action"] 的值</summary>
    public Func<string, Task>? OnAction { get; set; }

    /// <summary>
    /// 静态全局回调，替代 CascadingValue 传递 OnAction。
    /// 在 MAUI BlazorWebView 中 CascadingValue 会导致崩溃，通过此静态字段绕过。
    /// </summary>
    public static Func<string, Task>? GlobalActionHandler { get; set; }
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
    private readonly HashSet<string> _fields = new();

    /// <summary>登记表单包含的字段名（用于字段值变更时精确回写所属表单）</summary>
    public void RegisterField(string fieldName) => _fields.Add(fieldName);

    /// <summary>该表单是否包含指定字段</summary>
    public bool HasField(string fieldName) => _fields.Contains(fieldName);

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
