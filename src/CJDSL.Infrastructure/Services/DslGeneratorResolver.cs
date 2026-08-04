using CJCore.LLM.Abstractions;
using CJDSL.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// DSL 生成器解析器实现。
/// - provider = "llm"：若数据库中已配置默认 LLM 模型则返回 <see cref="LlmDslGenerator"/>，
///   否则记录警告并降级为 <see cref="TemplateDslGenerator"/>。
/// - provider = "template" 或其他：返回 <see cref="TemplateDslGenerator"/>。
/// 模块 J：可用性判断收敛到 CJCore —— 通过 ILLMClient.IsAvailableAsync（DB 默认模型配置）。
/// </summary>
public class DslGeneratorResolver : IDslGeneratorResolver
{
    private readonly TemplateDslGenerator _templateGenerator;
    private readonly LlmDslGenerator _llmGenerator;
    private readonly ILLMClient _llmClient;
    private readonly ILogger<DslGeneratorResolver> _logger;

    public DslGeneratorResolver(
        TemplateDslGenerator templateGenerator,
        LlmDslGenerator llmGenerator,
        ILLMClient llmClient,
        ILogger<DslGeneratorResolver> logger)
    {
        _templateGenerator = templateGenerator;
        _llmGenerator = llmGenerator;
        _llmClient = llmClient;
        _logger = logger;
    }

    public IDslGenerator Resolve(string? provider, string defaultProvider = DslGeneratorProviders.Template)
    {
        var effective = string.IsNullOrWhiteSpace(provider) ? defaultProvider : provider.Trim().ToLowerInvariant();

        if (effective == DslGeneratorProviders.Llm)
        {
            // 同步等待可接受：仅一次轻量 DB 查询（默认模型配置）
            if (_llmClient.IsAvailableAsync().GetAwaiter().GetResult())
            {
                return _llmGenerator;
            }

            _logger.LogWarning(
                "请求使用 LLM 生成 DSL，但数据库中未配置默认 LLM 模型（请在「LLM 配置」页面设置），已降级为模板生成器");
            return _templateGenerator;
        }

        return _templateGenerator;
    }
}
