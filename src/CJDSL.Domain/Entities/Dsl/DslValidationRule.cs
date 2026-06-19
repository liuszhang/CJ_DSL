namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// DSL 验证规则
/// </summary>
public class DslValidationRule
{
    /// <summary>
    /// 规则类型：required, regex, minLength, maxLength, min, max, email, phone, custom
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 正则表达式（Type=regex 时必填）
    /// </summary>
    public string? Pattern { get; set; }

    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }

    /// <summary>
    /// 自定义验证函数（Type=custom 时必填）
    /// </summary>
    public string? Expression { get; set; }
}
