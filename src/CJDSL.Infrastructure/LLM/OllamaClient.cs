using System.Text.Json;
using System.Text.Json.Serialization;
using CJDSL.Domain.Configuration;
using CJDSL.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.LLM;

/// <summary>
/// Ollama 本地 LLM 客户端
/// </summary>
public class OllamaClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderConfig _providerConfig;
    private readonly ILogger<OllamaClient> _logger;

    public string Provider => "Ollama";

    public OllamaClient(HttpClient httpClient, LlmProviderConfig providerConfig, ILogger<OllamaClient> logger)
    {
        _httpClient = httpClient;
        _providerConfig = providerConfig;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(providerConfig.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(providerConfig.TimeoutSeconds);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama service availability check failed for {Name}", _providerConfig.Name);
            return false;
        }
    }

    public async Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var maxRetries = request.MaxRetries;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var payload = new
                {
                    model = _providerConfig.Model,
                    system = request.SystemPrompt,
                    prompt = request.UserPrompt,
                    stream = false,
                    options = new
                    {
                        temperature = request.Temperature
                    },
                    format = request.JsonMode ? "json" : null
                };

                using var requestContent = new StringContent(
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
                    System.Text.Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync("api/generate", requestContent, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ollama API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return new LLMResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"API error: {response.StatusCode} - {responseBody}",
                        Provider = Provider,
                        Model = _providerConfig.Model
                    };
                }

                var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseBody);
                var rawText = result?.Response ?? string.Empty;
                rawText = CleanJsonResponse(rawText);

                JsonElement? parsed = null;
                try { parsed = JsonSerializer.Deserialize<JsonElement>(rawText); } catch { }

                sw.Stop();

                return new LLMResponse
                {
                    IsSuccess = !string.IsNullOrEmpty(rawText),
                    RawText = rawText,
                    ParsedJson = parsed,
                    PromptTokens = result?.PromptEvalCount,
                    CompletionTokens = result?.EvalCount,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Provider = Provider,
                    Model = _providerConfig.Model
                };
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Ollama request failed, retry {Attempt}...", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        sw.Stop();
        return new LLMResponse
        {
            IsSuccess = false,
            ErrorMessage = $"Request failed (retried {maxRetries} times): {lastException?.Message}",
            Provider = Provider,
            Model = _providerConfig.Model,
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        LLMRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var payload = new
        {
            model = _providerConfig.Model,
            system = request.SystemPrompt,
            prompt = request.UserPrompt,
            stream = true,
            options = new { temperature = request.Temperature }
        };

        using var requestContent = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("api/generate", requestContent, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaGenerateResponse? chunk = null;
            try { chunk = JsonSerializer.Deserialize<OllamaGenerateResponse>(line); } catch { }

            if (!string.IsNullOrEmpty(chunk?.Response)) yield return chunk.Response;
        }
    }

    private static string CleanJsonResponse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return rawText;
        rawText = rawText.Trim();
        if (rawText.StartsWith("```json")) rawText = rawText["```json".Length..];
        if (rawText.StartsWith("```")) rawText = rawText[3..];
        if (rawText.EndsWith("```")) rawText = rawText[..^3];
        return rawText.Trim();
    }
}

#pragma warning disable CS8618
public class OllamaGenerateResponse
{
    [JsonPropertyName("response")] public string Response { get; set; }
    [JsonPropertyName("done")] public bool Done { get; set; }
    [JsonPropertyName("prompt_eval_count")] public int? PromptEvalCount { get; set; }
    [JsonPropertyName("eval_count")] public int? EvalCount { get; set; }
}
#pragma warning restore CS8618
