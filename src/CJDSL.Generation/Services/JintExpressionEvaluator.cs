using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using Jint;

namespace CJDSL.Generation.Services;

/// <summary>
/// Jint 表达式求值引擎
/// </summary>
public class JintExpressionEvaluator : IExpressionEvaluator
{
    public T Evaluate<T>(string expression, IDataContext dataContext)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return default!;

        // 构建 JavaScript 执行上下文（沙箱：超时 3s，不暴露危险宿主对象）
        var engine = DslSecurityValidator.CreateSandboxedEngine();

        // 注入数据到 $ctx 变量
        engine.SetValue("$ctx", dataContext);
        engine.SetValue("today", DateTime.Today);
        engine.SetValue("now", DateTime.Now);

        // 注入辅助函数
        engine.SetValue("hasPermission", (Func<string, bool>)(perm =>
        {
            var perms = dataContext.Get<List<string>>("user.permissions");
            return perms?.Contains(perm) ?? false;
        }));

        engine.SetValue("hasRole", (Func<string, bool>)(role =>
        {
            var roles = dataContext.Get<List<string>>("user.roles");
            return roles?.Contains(role) ?? false;
        }));

        try
        {
            var result = engine.Evaluate(expression);
            var obj = result.ToObject();
            return obj is T typed ? typed : (T)Convert.ChangeType(obj, typeof(T))!;
        }
        catch
        {
            return default!;
        }
    }

    public bool CanEvaluate(string expression)
    {
        try
        {
            DslSecurityValidator.CreateSandboxedEngine().Evaluate(expression);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 数据上下文实现
/// </summary>
public class DslDataContext : IDataContext
{
    private readonly Dictionary<string, object> _data = new();
    private readonly Dictionary<string, object> _user = new();
    private readonly Dictionary<string, object> _row = new();

    public DslDataContext(Dictionary<string, object>? initialData = null, UserContext? user = null)
    {
        if (initialData != null)
        {
            foreach (var kv in initialData) Set($"data.{kv.Key}", kv.Value);
        }
        if (user != null)
        {
            Set("user.id", user.UserId);
            Set("user.name", user.UserName);
            Set("user.roles", user.Roles);
            Set("user.permissions", user.Permissions);
            Set("user.department", user.Department ?? "");
            Set("user.tenantId", user.TenantId ?? "");
        }
    }

    public object? Get(string path)
    {
        if (path.StartsWith("@")) path = path[1..];
        if (path.StartsWith("data.")) path = path[5..];

        var segments = path.Split('.');
        if (segments.Length == 0) return null;

        var root = segments[0] switch
        {
            "user" => GetFromDict(_user, segments[1..]),
            "row" => GetFromDict(_row, segments[1..]),
            _ => GetFromDict(_data, segments)
        };

        return root;
    }

    public T? Get<T>(string path)
    {
        var value = Get(path);
        if (value == null) return default;
        try { return (T)Convert.ChangeType(value, typeof(T))!; } catch { return default; }
    }

    public void Set(string path, object? value)
    {
        if (path.StartsWith("data.")) path = path[5..];

        var segments = path.Split('.');
        if (segments.Length == 0) return;

        var dict = segments[0] switch
        {
            "user" => SetInDict(_user, segments[1..], value),
            "row" => SetInDict(_row, segments[1..], value),
            _ => SetInDict(_data, segments, value)
        };
    }

    public bool Has(string path)
    {
        return Get(path) != null;
    }

    private static object? GetFromDict(Dictionary<string, object> dict, string[] segments)
    {
        if (segments.Length == 0) return null;
        if (!dict.TryGetValue(segments[0], out var current)) return null;
        foreach (var segment in segments[1..])
        {
            if (current is Dictionary<string, object> d && d.TryGetValue(segment, out var next))
            {
                current = next;
            }
            else if (current is not null)
            {
                var prop = current.GetType().GetProperty(segment, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null) current = prop.GetValue(current);
                else return null;
            }
            else return null;
        }
        return current;
    }

    private static bool SetInDict(Dictionary<string, object> dict, string[] segments, object? value)
    {
        if (segments.Length == 0) return false;
        if (segments.Length == 1)
        {
            if (value == null) dict.Remove(segments[0]);
            else dict[segments[0]] = value;
            return true;
        }
        // 简化：不支持嵌套字典的自动创建
        return false;
    }
}
