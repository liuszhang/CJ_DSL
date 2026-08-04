using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Domain.Interfaces;

/// <summary>
/// DSL 安全校验器接口：对 DSL 做沙箱表达式校验、apiCall endpoint 白名单、富文本 XSS 清洗。
/// </summary>
public interface IDslSecurityValidator
{
    /// <summary>
    /// 校验 DSL 是否存在安全风险（恶意表达式、越权 endpoint、未清洗的富文本等）。
    /// 返回安全结果，Errors 非空表示存在致命安全问题，应拦截。
    /// </summary>
    Task<DslSecurityResult> ValidateAsync(DslPage dsl, CancellationToken ct = default);

    /// <summary>
    /// 返回一份清洗后的 DSL 副本（主要清洗 richText 等富文本中的 XSS 脚本）。
    /// 原 DSL 不会被修改。
    /// </summary>
    Task<DslPage> SanitizeAsync(DslPage dsl, CancellationToken ct = default);
}

/// <summary>
/// DSL 安全校验结果
/// </summary>
public class DslSecurityResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>是否存在致命安全问题（应拦截）</summary>
    public bool IsSafe => _errors.Count == 0;

    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<string> Warnings => _warnings;

    public void AddError(string message) => _errors.Add(message);
    public void AddWarning(string message) => _warnings.Add(message);
}
