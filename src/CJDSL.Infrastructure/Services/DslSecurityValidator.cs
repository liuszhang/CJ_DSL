using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using Ganss.Xss;
using Jint;
using Jint.Runtime;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// DSL 安全校验器实现：
/// 1. 表达式沙箱：可见性/禁用表达式在带超时（3s）的 Jint 引擎中求值，阻断 while(true) 等死循环；
/// 2. apiCall / DataSource endpoint 白名单：禁止绝对外链（除非命中 CJDSL:Security:AllowedEndpoints 配置）；
/// 3. 富文本 XSS 清洗：使用 HtmlSanitizer 清洗 richText 内容。
/// </summary>
public class DslSecurityValidator : IDslSecurityValidator
{
    private readonly HtmlSanitizer _sanitizer = new();
    private readonly List<string> _allowedEndpointPrefixes;

    public DslSecurityValidator(IConfiguration? configuration = null)
    {
        _allowedEndpointPrefixes = configuration
            ?.GetSection("CJDSL:Security:AllowedEndpoints").Get<List<string>>()
            ?? new List<string>();
    }

    public Task<DslSecurityResult> ValidateAsync(DslPage dsl, CancellationToken ct = default)
    {
        var result = new DslSecurityResult();

        // 1. 表达式沙箱校验（可见性 / 禁用）
        foreach (var comp in dsl.GetAllComponents())
        {
            if (!string.IsNullOrWhiteSpace(comp.VisibleIf))
                CheckExpression(comp.VisibleIf, comp.Id, "VisibleIf", result);
            if (!string.IsNullOrWhiteSpace(comp.DisabledIf))
                CheckExpression(comp.DisabledIf, comp.Id, "DisabledIf", result);
        }

        // 2. apiCall / DataSource endpoint 白名单
        foreach (var comp in dsl.GetAllComponents())
        {
            if (comp.Events != null)
            {
                foreach (var evt in comp.Events)
                {
                    if (evt.Handler == DslHandlers.ApiCall)
                    {
                        var ep = evt.Params?.GetValueOrDefault("endpoint")?.ToString();
                        if (!IsEndpointAllowed(ep))
                            result.AddError($"组件 {comp.Id} 的 apiCall 指向未授权 endpoint: {ep}");
                    }
                }
            }

            if (comp.DataSource != null && !IsEndpointAllowed(comp.DataSource.Endpoint))
                result.AddError($"组件 {comp.Id} 的 DataSource 指向未授权 endpoint: {comp.DataSource.Endpoint}");
        }

        if (dsl.PageEvents != null)
        {
            foreach (var pe in dsl.PageEvents)
            {
                if (pe.Handler == DslHandlers.ApiCall)
                {
                    var ep = pe.Params?.GetValueOrDefault("endpoint")?.ToString();
                    if (!IsEndpointAllowed(ep))
                        result.AddError($"页面事件 apiCall 指向未授权 endpoint: {ep}");
                }
            }
        }

        // 3. 富文本 XSS 检查（输出前会被清洗，这里给出告警）
        foreach (var comp in dsl.GetAllComponents())
        {
            if (comp.Type == "richText")
            {
                var html = comp.Props?.GetValueOrDefault("Content")?.ToString() ?? comp.Label ?? "";
                if (ContainsDangerousHtml(html))
                    result.AddWarning($"组件 {comp.Id} 的 richText 含未清洗的脚本内容，将在输出前被清洗");
            }
        }

        return Task.FromResult(result);
    }

    public Task<DslPage> SanitizeAsync(DslPage dsl, CancellationToken ct = default)
    {
        // 通过 JSON 深拷贝得到独立副本，避免修改原 DSL
        var clone = JsonSerializer.Deserialize<DslPage>(JsonSerializer.Serialize(dsl)) ?? dsl;
        SanitizeComponents(clone.Components);
        return Task.FromResult(clone);
    }

    private void SanitizeComponents(List<DslComponent>? components)
    {
        if (components == null) return;
        foreach (var c in components)
        {
            if (c.Type == "richText")
            {
                // 注意：JSON 深拷贝后 Props 值为 JsonElement 而非 string，需同时兼容
                if (c.Props != null && c.Props.TryGetValue("Content", out var raw))
                {
                    var html = raw switch
                    {
                        string s => s,
                        JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
                        _ => null
                    };
                    if (html != null)
                        c.Props["Content"] = _sanitizer.Sanitize(html);
                }
                if (!string.IsNullOrEmpty(c.Label))
                    c.Label = _sanitizer.Sanitize(c.Label!);
            }
            SanitizeComponents(c.Children);
        }
    }

    private static void CheckExpression(string expr, string compId, string kind, DslSecurityResult result)
    {
        try
        {
            var engine = CreateSandboxedEngine();
            engine.Evaluate(expr);
        }
        catch (JavaScriptException jex)
        {
            // 语法错误 / 沙箱拦截（含超时）均视为风险
            result.AddError($"组件 {compId} 的 {kind} 表达式校验未通过（可能被沙箱拦截）: {jex.Message}");
        }
        catch (Exception ex)
        {
            result.AddError($"组件 {compId} 的 {kind} 表达式存在安全风险: {ex.Message}");
        }
    }

    private bool IsEndpointAllowed(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return true; // 空 endpoint 不限制

        // 相对路径（以 / 开头，非协议相对 //）视为同源，允许
        if (endpoint.StartsWith("/") && !endpoint.StartsWith("//")) return true;

        // 协议相对 URL（//host/path）视为外部，禁止
        if (endpoint.StartsWith("//")) return false;

        // 绝对 URI：需命中白名单前缀
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            return _allowedEndpointPrefixes.Any(p =>
                endpoint.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        // 其他形式（如纯相对片段）放行
        return true;
    }

    private static bool ContainsDangerousHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return false;
        return html.Contains("<script", StringComparison.OrdinalIgnoreCase)
            || html.Contains("onerror=", StringComparison.OrdinalIgnoreCase)
            || html.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 创建带超时与最小全局对象的沙箱引擎：阻断死循环，不暴露 System/IO/Net 等危险宿主对象。
    /// </summary>
    internal static Engine CreateSandboxedEngine()
    {
        var engine = new Engine(opts =>
        {
            opts.TimeoutInterval(TimeSpan.FromSeconds(3)); // 表达式求值超时 3s，阻断 while(true)
            opts.MaxStatements(10_000);                    // 双保险：限制语句数
        });

        // 注入与运行时一致的只读全局，避免合法表达式（如 user.role）被误判
        engine.SetValue("$store", new DslDataContext());
        engine.SetValue("today", DateTime.Today);
        engine.SetValue("now", DateTime.Now);
        engine.SetValue("hasPermission", (Func<string, bool>)(_ => false));
        engine.SetValue("hasRole", (Func<string, bool>)(_ => false));
        return engine;
    }
}
