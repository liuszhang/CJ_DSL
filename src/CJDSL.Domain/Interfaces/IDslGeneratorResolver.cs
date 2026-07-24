namespace CJDSL.Domain.Interfaces;

/// <summary>
/// DSL 生成器提供方常量
/// </summary>
public static class DslGeneratorProviders
{
    public const string Template = "template";
    public const string Llm = "llm";
}

/// <summary>
/// DSL 生成器解析器 - 根据 provider 参数选择模板生成器或 LLM 生成器。
/// 当请求 LLM 但未配置任何可用的 LLM 提供商时，自动降级为模板生成器并记录警告。
/// </summary>
public interface IDslGeneratorResolver
{
    /// <summary>
    /// 解析生成器。
    /// </summary>
    /// <param name="provider">"template" | "llm"，为空时使用 defaultProvider</param>
    /// <param name="defaultProvider">provider 为空时的默认值</param>
    IDslGenerator Resolve(string? provider, string defaultProvider = DslGeneratorProviders.Template);
}
