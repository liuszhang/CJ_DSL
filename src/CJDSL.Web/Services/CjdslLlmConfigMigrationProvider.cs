using CJCore.Framework.Abstractions;
using CJCore.Modules.Data;
using CJCore.Modules.LLM.Model;
using CJDSL.Domain.Configuration;
using CJDSL.Generation.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CJDSL.Web.Services;

/// <summary>
/// 模块 J：旧版 system-config.json 中「激活的 LLM 提供商」一次性迁移至 CJCore 数据层（SQLite）。
/// 设计要点：
/// - 仅当 JSON 中存在激活提供商（<see cref="LlmProviderConfig.IsActive"/>）时执行；
///   迁移完成后清空 JSON 中的 LLM 配置，避免后续启动重复覆盖用户在 CJCore「LLM 配置」页面的设定。
/// - 无激活提供商时直接跳过（幂等）。
/// - 运行次序：依赖 <c>LLMSeedDataProvider</c>（Order=100）已创建供应商预设，本迁移 Order=200 在其后执行，
///   因此可基于已存在的供应商（OpenAI/Ollama 等）更新其端点/密钥并将对应模型设为默认。
/// </summary>
public class CjdslLlmConfigMigrationProvider : ISeedDataProvider
{
    private readonly SystemConfigService _systemConfig;
    private readonly ILogger<CjdslLlmConfigMigrationProvider> _logger;

    public CjdslLlmConfigMigrationProvider(
        SystemConfigService systemConfig,
        ILogger<CjdslLlmConfigMigrationProvider> logger)
    {
        _systemConfig = systemConfig;
        _logger = logger;
    }

    public string Name => "CJDSL LLM 配置迁移（system-config.json → CJCore）";
    public int Order => 200;

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var active = _systemConfig.GetLlmConfig().GetActive();
        if (active == null)
        {
            _logger.LogInformation("[CJDSL-LLM-Migrate] 未检测到激活的 LLM 提供商，跳过迁移");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();

        var providerName = MapProviderName(active.Provider);
        var providers = db.Set<LlmProvider>();

        var provider = await providers.FirstOrDefaultAsync(p => p.Name == providerName, ct);
        if (provider == null)
        {
            provider = new LlmProvider
            {
                Name = providerName,
                DisplayName = string.IsNullOrWhiteSpace(active.Name) ? providerName : active.Name,
                ApiBaseUrl = active.BaseUrl,
                ApiKey = active.ApiKey,
                Description = "由旧版 system-config.json 迁移而来",
                SortOrder = 95,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            providers.Add(provider);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            provider.ApiBaseUrl = active.BaseUrl;
            provider.ApiKey = active.ApiKey;
            provider.UpdatedAt = DateTime.UtcNow;
        }

        // 模型：以 JSON 中的 Model 为准，设为默认（缺失则仅迁移供应商端点）
        if (!string.IsNullOrWhiteSpace(active.Model))
        {
            var models = db.Set<LlmModelConfig>();
            var model = await models.FirstOrDefaultAsync(
                m => m.LlmProviderId == provider.Id && m.ModelName == active.Model, ct);
            if (model == null)
            {
                model = new LlmModelConfig
                {
                    LlmProviderId = provider.Id,
                    ModelName = active.Model,
                    DisplayName = active.Model,
                    ModelType = "Chat",
                    Temperature = active.Temperature,
                    MaxTokens = active.MaxTokens,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                models.Add(model);
            }
            else
            {
                model.IsDefault = true;
                model.UpdatedAt = DateTime.UtcNow;
            }

            // 供应商其余模型取消默认，保证仅一个默认
            foreach (var m in models.Where(m => m.LlmProviderId == provider.Id && m.ModelName != active.Model))
                m.IsDefault = false;

            await db.SaveChangesAsync(ct);
        }

        // 迁移完成：清空 JSON 中的 LLM 配置，避免下次启动覆盖用户在 CJCore「LLM 配置」页面的设定
        _systemConfig.UpdateLlmConfig(new LlmConnectionConfig());

        _logger.LogInformation(
            "[CJDSL-LLM-Migrate] 已迁移激活提供商 {Provider}/{Model} 至 CJCore 数据层",
            providerName, active.Model);
    }

    private static string MapProviderName(string oldProvider) => oldProvider.Trim() switch
    {
        "Ollama" => "Ollama",
        "OpenAI" => "OpenAI",
        "AzureOpenAI" => "AzureOpenAI",
        "DeepSeek" => "DeepSeek",
        _ => oldProvider // 自定义名称原样保留（CJCore 以 OpenAI 兼容协议调用）
    };
}
