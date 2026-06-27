using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CJDSL.Domain.Configuration;
using CJDSL.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.LLM;

/// <summary>
/// OpenAI 兼容 API 客户端
/// </summary>
public class OpenAIClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderConfig _providerConfig;
    private readonly ILogger<OpenAIClient> _logger;

    public string Provider => "OpenAI";

    public OpenAIClient(HttpClient httpClient, LlmProviderConfig providerConfig, ILogger<OpenAIClient> logger)
    {
        _httpClient = httpClient;
        _providerConfig = providerConfig;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(providerConfig.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(providerConfig.TimeoutSeconds);

        if (!string.IsNullOrEmpty(providerConfig.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", providerConfig.ApiKey);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("models", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI service availability check failed for {Name}", _providerConfig.Name);
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
                    messages = new[]
                    {
                        new { role = "system", content = request.SystemPrompt },
                        new { role = "user", content = request.UserPrompt }
                    },
                    temperature = request.Temperature,
                    max_tokens = request.MaxTokens,
                    response_format = request.JsonMode ? new { type = "json_object" } : null
                };

                using var requestContent = new StringContent(
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
                    System.Text.Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync("chat/completions", requestContent, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return new LLMResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"API error: {response.StatusCode} - {responseBody}",
                        Provider = Provider,
                        Model = _providerConfig.Model
                    };
                }

                var result = JsonSerializer.Deserialize<OpenAIChatCompletion>(responseBody);
                var rawText = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
                rawText = CleanJsonResponse(rawText);

                JsonElement? parsed = null;
                try { parsed = JsonSerializer.Deserialize<JsonElement>(rawText); } catch { }

                sw.Stop();

                return new LLMResponse
                {
                    IsSuccess = !string.IsNullOrEmpty(rawText),
                    RawText = rawText,
                    ParsedJson = parsed,
                    PromptTokens = result?.Usage?.PromptTokens,
                    CompletionTokens = result?.Usage?.CompletionTokens,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Provider = Provider,
                    Model = _providerConfig.Model
                };
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "OpenAI request failed, retry {Attempt}...", attempt + 1);
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
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            },
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = true
        };

        using var requestContent = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("chat/completions", requestContent, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            OpenAIStreamChunk? chunk = null;
            try { chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(data); } catch { }

            var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content)) yield return content;
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
public class OpenAIChatCompletion
{
    [JsonPropertyName("choices")] public List<Choice> Choices { get; set; }
    [JsonPropertyName("usage")] public UsageInfo Usage { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")] public Message Message { get; set; }
    [JsonPropertyName("finish_reason")] public string FinishReason { get; set; }
}

public class Message
{
    [JsonPropertyName("role")] public string Role { get; set; }
    [JsonPropertyName("content")] public string Content { get; set; }
}

public class UsageInfo
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
}

public class OpenAIStreamChunk
{
    [JsonPropertyName("choices")] public List<StreamChoice> Choices { get; set; }
}

public class StreamChoice
{
    [JsonPropertyName("delta")] public Delta Delta { get; set; }
}

public class Delta
{
    [JsonPropertyName("content")] public string Content { get; set; }
}
#pragma warning restore CS8618
