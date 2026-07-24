using CJDSL.Domain.Interfaces;
using CJDSL.Infrastructure.LLM;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// DSL 生成器解析器实现。
/// - provider = "llm"：若已配置启用的 LLM 提供商则返回 <see cref="LlmDslGenerator"/>，
///   否则记录警告并降级为 <see cref="TemplateDslGenerator"/>。
/// - provider = "template" 或其他：返回 <see cref="TemplateDslGenerator"/>。
/// </summary>
public class DslGeneratorResolver : IDslGeneratorResolver
{
    private readonly TemplateDslGenerator _templateGenerator;
    private readonly LlmDslGenerator _llmGenerator;
    private readonly ILLMClientProvider _llmClientProvider;
    private readonly ILogger<DslGeneratorResolver> _logger;

    public DslGeneratorResolver(
        TemplateDslGenerator templateGenerator,
        LlmDslGenerator llmGenerator,
        ILLMClientProvider llmClientProvider,
        ILogger<DslGeneratorResolver> logger)
    {
        _templateGenerator = templateGenerator;
        _llmGenerator = llmGenerator;
        _llmClientProvider = llmClientProvider;
        _logger = logger;
    }

    public IDslGenerator Resolve(string? provider, string defaultProvider = DslGeneratorProviders.Template)
    {
        var effective = string.IsNullOrWhiteSpace(provider) ? defaultProvider : provider.Trim().ToLowerInvariant();

        if (effective == DslGeneratorProviders.Llm)
        {
            if (_llmClientProvider.GetActiveProvider() != null)
            {
                return _llmGenerator;
            }

            _logger.LogWarning(
                "请求使用 LLM 生成 DSL，但未配置任何启用的 LLM 提供商（config/system-config.json 的 Llm.Providers），已降级为模板生成器");
            return _templateGenerator;
        }

        return _templateGenerator;
    }
}
