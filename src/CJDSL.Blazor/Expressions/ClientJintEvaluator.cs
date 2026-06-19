using CJDSL.Blazor.Models;

namespace CJDSL.Blazor.Expressions;

/// <summary>
/// 客户端 Jint 表达式求值器
/// </summary>
public class ClientJintEvaluator : IExpressionEvaluator
{
    public T Evaluate<T>(string expression, DslDataStore dataStore)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return default!;

        try
        {
            var engine = new Jint.Engine();
            engine.SetValue("$store", dataStore);
            engine.SetValue("today", DateTime.Today);
            engine.SetValue("now", DateTime.Now);
            engine.SetValue("hasPermission", (Func<string, bool>)(perm =>
            {
                var perms = dataStore.Get<List<string>>("user.permissions");
                return perms?.Contains(perm) ?? false;
            }));
            engine.SetValue("hasRole", (Func<string, bool>)(role =>
            {
                var roles = dataStore.Get<List<string>>("user.roles");
                return roles?.Contains(role) ?? false;
            }));

            var result = engine.Evaluate(expression);
            var obj = result.ToObject();
            if (obj is T typed) return typed;
            return (T)Convert.ChangeType(obj, typeof(T))!;
        }
        catch
        {
            return default!;
        }
    }

    public bool CanEvaluate(string expression)
    {
        try { new Jint.Engine().Evaluate(expression); return true; } catch { return false; }
    }
}
