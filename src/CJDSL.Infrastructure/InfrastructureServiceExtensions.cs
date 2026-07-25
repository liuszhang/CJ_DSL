using CJCore.LLM.LLMClient;
using CJCore.LLM.Structured;
using CJDSL.Domain.Interfaces;
using CJDSL.Infrastructure.Caching;
using CJDSL.Infrastructure.Configuration;
using CJDSL.Infrastructure.LLM;
using CJDSL.Infrastructure.Persistence;
using CJDSL.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CJDSL.Infrastructure;

/// <summary>
/// Infrastructure 层依赖注入扩展
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddCJDSLInfrastructure(this IServiceCollection services)
    {
        return services.AddCJDSLInfrastructure(null);
    }

    public static IServiceCollection AddCJDSLInfrastructure(this IServiceCollection services, IConfiguration? configuration)
    {
        var useSqlite = configuration?.GetValue<bool>("CJDSL:Persistence:UseSqlite") ?? false;

        if (useSqlite)
        {
            var connectionString = configuration?.GetValue<string>("CJDSL:Persistence:SqliteConnectionString")
                ?? "Data Source=cjdsl.db";

            services.AddDbContextFactory<CJDSLDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddSingleton<IDslRepository, SqliteDslRepository>();
            services.AddSingleton<IMetaModelRepository, SqliteMetaModelRepository>();
            services.AddSingleton<IBusinessDataService, SqliteBusinessDataService>();
        }
        else
        {
            services.AddSingleton<IMetaModelRepository, InMemoryMetaModelRepository>();
            services.AddSingleton<IDslRepository, InMemoryDslRepository>();
            services.AddSingleton<IBusinessDataService, InMemoryBusinessDataService>();
        }

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

        // HTTP 客户端
        services.AddHttpClient("DslApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
