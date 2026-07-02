using System.Windows;
using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering;

/// <summary>
/// WPF 渲染上下文 — 贯穿整个渲染树，携带数据、事件和平台信息
/// </summary>
public class DslRenderContext
{
    public DslPage Page { get; set; } = null!;
    public DslDataStore DataStore { get; } = new();
    public TargetPlatform TargetPlatform { get; set; } = TargetPlatform.Wpf;
    public Dictionary<string, FrameworkElement> ElementRefs { get; } = new();
    public DslRenderContext? Parent { get; set; }

    public event EventHandler? RenderRequested;
    public void RequestRender() => RenderRequested?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// WPF 侧 DSL 数据存储
/// </summary>
public class DslDataStore
{
    private readonly Dictionary<string, object?> _data = new();
    public event EventHandler<DataChangedEventArgs>? DataChanged;

    public void Set(string path, object? value)
    {
        var oldValue = _data.GetValueOrDefault(path);
        if (value is null) _data.Remove(path);
        else _data[path] = value;
        DataChanged?.Invoke(this, new DataChangedEventArgs(path, oldValue, value));
    }

    public object? Get(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (path.StartsWith('@')) path = path[1..];
        var segments = path.Split('.');
        if (!_data.TryGetValue(segments[0], out var current)) return null;
        foreach (var segment in segments[1..])
        {
            if (current is null) return null;
            current = GetProperty(current, segment);
        }
        return current;
    }

    public T? Get<T>(string path) => Get(path) is T v ? v : default;
    public string? GetString(string path) => Get(path)?.ToString();
    public List<T>? GetList<T>(string path) => Get(path) as List<T>;

    public void Merge(Dictionary<string, object?> data)
    {
        foreach (var kv in data) Set(kv.Key, kv.Value);
    }

    public IReadOnlyDictionary<string, object?> Snapshot() => _data;

    private static object? GetProperty(object target, string propertyName)
    {
        if (target is System.Text.Json.JsonElement je)
        {
            if (je.ValueKind == System.Text.Json.JsonValueKind.Object && je.TryGetProperty(propertyName, out var jp))
                return jp;
            return null;
        }
        var pi = target.GetType().GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        return pi?.GetValue(target);
    }
}

public class DataChangedEventArgs(string path, object? oldValue, object? newValue) : EventArgs
{
    public string Path { get; } = path;
    public object? OldValue { get; } = oldValue;
    public object? NewValue { get; } = newValue;
}
