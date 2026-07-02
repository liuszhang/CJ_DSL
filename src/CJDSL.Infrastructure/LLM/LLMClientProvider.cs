using System.Collections.Concurrent;
using CJDSL.Domain.Configuration;
using CJDSL.Domain.Interfaces;
using CJDSL.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.LLM;

/// <summary>
/// LLM 客户端工厂 - 根据当前启用的提供商创建客户端
/// </summary>
public interface ILLMClientProvider
{
    ILLMClient GetClient();
    IReadOnlyList<LlmProviderConfig> GetAllProviders();
    LlmProviderConfig? GetActiveProvider();
}

public class LLMClientProvider : ILLMClientProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SystemConfigService _configService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, ILLMClient> _clientCache = new();

    public LLMClientProvider(
        IHttpClientFactory httpClientFactory,
        SystemConfigService configService,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }

    public ILLMClient GetClient()
    {
        var config = _configService.GetLlmConfig();
        var active = config.GetActive();

        if (active == null)
        {
            throw new InvalidOperationException("No active LLM provider configured. Please enable one in LLM settings.");
        }

        return CreateClient(active);
    }

    public IReadOnlyList<LlmProviderConfig> GetAllProviders()
    {
        return _configService.GetLlmConfig().Providers.AsReadOnly();
    }

    public LlmProviderConfig? GetActiveProvider()
    {
        return _configService.GetLlmConfig().GetActive();
    }

    private ILLMClient CreateClient(LlmProviderConfig providerConfig)
    {
        var cacheKey = $"{providerConfig.Provider}:{providerConfig.Name}:{providerConfig.BaseUrl}";

        return _clientCache.GetOrAdd(cacheKey, _ =>
        {
            var httpClient = _httpClientFactory.CreateClient($"LLM_{providerConfig.Name}");
            var loggerType = providerConfig.Provider switch
            {
                "Ollama" => typeof(OllamaClient),
                _ => typeof(OpenAIClient)
            };

            var logger = (ILogger)Activator.CreateInstance(
                typeof(ILogger<>).MakeGenericType(loggerType),
                _serviceProvider.GetRequiredService<ILoggerFactory>(),
                providerConfig.Name)!;

            return providerConfig.Provider switch
            {
                "Ollama" => new OllamaClient(httpClient, providerConfig, (ILogger<OllamaClient>)logger),
                _ => new OpenAIClient(httpClient, providerConfig, (ILogger<OpenAIClient>)logger)
            };
        });
    }
}
