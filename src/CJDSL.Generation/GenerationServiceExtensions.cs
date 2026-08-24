using CJCore.LLM.LLMClient;
using CJCore.LLM.Structured;
using CJDSL.Domain.Interfaces;
using CJDSL.Generation.Caching;
using CJDSL.Generation.Commands;
using CJDSL.Generation.Configuration;
using CJDSL.Generation.Interfaces;
using CJDSL.Generation.LLM;
using CJDSL.Generation.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CJDSL.Generation;

/// <summary>
/// CJDSL.Generation 依赖注入扩展。
/// 集中封装「规则 + LLM + 后处理 + 验证」全部生成能力，委托 CJCore 提供 LLM 传输/结构化输出。
/// 各产品（CJOEM / CJOntology / ABWork 等）直接引用本库并调用 <see cref="IDslGenerationService"/> 即可本地生成 DSL，
/// 不再各自 AddCJDSLInfrastructure() 或依赖独立 HTTP 生成服务。
/// </summary>
public static class GenerationServiceExtensions
{
    public static IServiceCollection AddCJDSLGeneration(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // 生成器
        services.AddSingleton<TemplateDslGenerator>();
        services.AddSingleton<IDslGenerator>(sp => sp.GetRequiredService<TemplateDslGenerator>());
        services.AddScoped<LlmDslGenerator>();
        // 生成器解析器：按 provider（template|llm）动态选择，LLM 未配置时自动降级模板并告警
        services.AddScoped<IDslGeneratorResolver, DslGeneratorResolver>();

        // 缓存
        services.AddSingleton<IDslCache, InMemoryDslCache>();

        // 表达式引擎
        services.AddSingleton<IExpressionEvaluator, JintExpressionEvaluator>();

        // 验证器
        services.AddSingleton<IDslValidator, DslSemanticValidator>();
        // 安全校验器（表达式沙箱 / endpoint 白名单 / 富文本清洗）
        services.AddSingleton<IDslSecurityValidator, DslSecurityValidator>();

        // 系统配置服务
        services.AddSingleton<SystemConfigService>();

        // LLM Prompt 构建器
        services.AddSingleton<IDslPromptBuilder, DslPromptBuilder>();

        // ★ 模块 J：LLM 客户端栈收敛到 CJCore
        // - ILLMConfigReader(DB) + ILLMChatService：CJCore 官方注册入口
        // - ILLMClient → DbConfiguredLLMClient：每次调用前从 DB 读默认模型配置
        // - IStructuredLLMClient：强类型结构化输出（LlmDslGenerator 消费）
        services.AddHttpClient();
        services.AddLLMChatService();
        services.AddHttpClient<CJCore.LLM.Abstractions.ILLMClient, LLM.DbConfiguredLLMClient>();
        services.AddCJCoreStructuredLLM();

        // MediatR（注册本程序集内的生成命令 handler）
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GenerationServiceExtensions).Assembly));

        // 高层门面：各产品直接调用，无需触碰 MediatR / 命令细节
        services.AddScoped<IDslGenerationService, DslGenerationService>();

        return services;
    }
}
