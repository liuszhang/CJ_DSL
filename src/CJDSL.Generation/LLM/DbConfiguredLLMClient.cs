using System.Runtime.CompilerServices;
using CJCore.LLM.Abstractions;
using CJCore.LLM.LLMClient;
using Microsoft.Extensions.Logging;

namespace CJDSL.Generation.LLM;

/// <summary>
/// 模块 J（LLM 收敛到 CJCore）：数据库配置驱动的 LLM 客户端。
/// 每次调用前经 <see cref="ILLMConfigReader"/>（DB）读取默认模型的 Endpoint/ApiKey/Model
/// 填入 <see cref="ChatRequest"/>，再委托 CJCore 通用 <see cref="LLMClient"/>（OpenAI 兼容格式，
/// 自带重试/流式/工具调用）。供 StructuredLLMClient 等上层组件透明使用。
/// </summary>
public class DbConfiguredLLMClient : ILLMClient
{
    private readonly ILLMConfigReader _configReader;
    private readonly LLMClient _inner;
    private readonly ILogger<DbConfiguredLLMClient> _logger;

    public string Provider => "CJCoreDb";

    public DbConfiguredLLMClient(
        ILLMConfigReader configReader,
        HttpClient httpClient,
        ILogger<DbConfiguredLLMClient> logger)
    {
        _configReader = configReader;
        _inner = new LLMClient(httpClient);
        _logger = logger;
    }

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default)
    {
        if (!await FillFromDbAsync(request, ct))
        {
            return new ChatResponse
            {
                IsSuccess = false,
                Error = "未配置默认 LLM 模型，请在「LLM 配置」页面设置默认模型"
            };
        }

        return await _inner.CompleteAsync(request, ct);
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!await FillFromDbAsync(request, ct))
            throw new InvalidOperationException("未配置默认 LLM 模型，请在「LLM 配置」页面设置默认模型");

        await foreach (var chunk in _inner.CompleteStreamingAsync(request, ct))
            yield return chunk;
    }

    public async IAsyncEnumerable<ChatStreamChunk> CompleteStreamingWithToolsAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!await FillFromDbAsync(request, ct))
            throw new InvalidOperationException("未配置默认 LLM 模型，请在「LLM 配置」页面设置默认模型");

        await foreach (var chunk in _inner.CompleteStreamingWithToolsAsync(request, ct))
            yield return chunk;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var (endpoint, _, model) = await _configReader.GetDefaultModelConfigAsync(ct);
            return !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(model);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 LLM 默认模型配置失败");
            return false;
        }
    }

    /// <summary>从 DB 读取默认模型配置补全请求。已显式指定 Endpoint 的请求不覆盖。返回是否可用。</summary>
    private async Task<bool> FillFromDbAsync(ChatRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.Endpoint) && !string.IsNullOrEmpty(request.Model))
            return true;

        var (endpoint, apiKey, model) = await _configReader.GetDefaultModelConfigAsync(ct);
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(model))
        {
            _logger.LogWarning("数据库中未配置默认 LLM 模型");
            return false;
        }

        request.Endpoint ??= endpoint;
        request.ApiKey ??= apiKey;
        if (string.IsNullOrEmpty(request.Model))
            request.Model = model;
        return true;
    }
}
