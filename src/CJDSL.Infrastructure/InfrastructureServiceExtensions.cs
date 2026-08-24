using CJDSL.Domain.Interfaces;
using CJDSL.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CJDSL.Infrastructure;

/// <summary>
/// 持久化层依赖注入扩展。
/// 仅负责仓储 / EF / 业务数据；「规则 + LLM + 后处理 + 验证」生成能力已迁至 CJDSL.Generation（见 <see cref="CJDSL.Generation.GenerationServiceExtensions"/>）。
/// CJDSL.Web 以 <c>AddCJDSLGeneration()</c> + <c>AddCJDSLPersistence(configuration)</c> 组合装配。
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddCJDSLPersistence(this IServiceCollection services)
        => services.AddCJDSLPersistence(null);

    public static IServiceCollection AddCJDSLPersistence(this IServiceCollection services, IConfiguration? configuration)
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

        return services;
    }
}
