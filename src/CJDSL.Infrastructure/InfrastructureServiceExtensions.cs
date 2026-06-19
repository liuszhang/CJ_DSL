using CJDSL.Domain.Interfaces;
using CJDSL.Infrastructure.Caching;
using CJDSL.Infrastructure.Configuration;
using CJDSL.Infrastructure.LLM;
using CJDSL.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CJDSL.Infrastructure;

/// <summary>
/// Infrastructure 层依赖注入扩展
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddCJDSLInfrastructure(this IServiceCollection services)
    {
        // 仓储
        services.AddSingleton<IMetaModelRepository, InMemoryMetaModelRepository>();
        services.AddSingleton<IDslRepository, InMemoryDslRepository>();

        // 生成器
        services.AddSingleton<IDslGenerator, TemplateDslGenerator>();
        services.AddSingleton<LlmDslGenerator>();

        // 缓存
        services.AddSingleton<IDslCache, InMemoryDslCache>();

        // 表达式引擎
        services.AddSingleton<IExpressionEvaluator, JintExpressionEvaluator>();

        // 验证器
        services.AddSingleton<IDslValidator, DslSemanticValidator>();

        // 系统配置服务
        services.AddSingleton<SystemConfigService>();

        // LLM 响应解析器
        services.AddSingleton<IDslResponseParser, DslResponseParser>();

        // LLM Prompt 构建器
        services.AddSingleton<IDslPromptBuilder, DslPromptBuilder>();

        // LLM 客户端（OpenAI）
        services.AddHttpClient<ILLMClient, OpenAIClient>();

        // HTTP 客户端
        services.AddHttpClient("DslApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
