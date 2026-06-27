using System.Text.Json;
using System.Text.Json.Serialization;

namespace CJDSL.Domain.Interfaces;

/// <summary>
/// LLM 客户端接口（支持多提供商）
/// </summary>
public interface ILLMClient
{
    /// <summary>服务提供商名称</summary>
    string Provider { get; }

    /// <summary>检查服务可用性</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>发送生成请求</summary>
    Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken ct = default);

    /// <summary>流式生成（可选）</summary>
    IAsyncEnumerable<string> GenerateStreamAsync(LLMRequest request, CancellationToken ct = default);
}

/// <summary>
/// LLM 生成请求
/// </summary>
public class LLMRequest
{
    /// <summary>系统提示词（角色设定）</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>用户提示词</summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>温度（0-2，越高越创造性）</summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>最大输出 Token 数</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>是否强制 JSON 输出</summary>
    public bool JsonMode { get; set; } = true;

    /// <summary>输出格式 schema（JSON Mode 时使用）</summary>
    public string? JsonSchema { get; set; }

    /// <summary>重试次数</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>超时（秒）</summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// LLM 生成响应
/// </summary>
public class LLMResponse
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>生成的文本（原始 JSON 字符串）</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>解析后的 JSON 元素</summary>
    public JsonElement? ParsedJson { get; set; }

    /// <summary>使用的 Token 数</summary>
    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    /// <summary>耗时毫秒</summary>
    public long ElapsedMs { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>服务提供商</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>模型名称</summary>
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// LLM 生成响应
/// </summary>
public class LLMConfig
{
    /// <summary>提供商类型</summary>
    public string Provider { get; set; } = "OpenAI"; // OpenAI, Ollama, Local

    /// <summary>API 基础地址</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    /// <summary>API Key</summary>
    public string? ApiKey { get; set; }

    /// <summary>模型名称</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>默认温度</summary>
    public float DefaultTemperature { get; set; } = 0.3f;

    /// <summary>默认超时（秒）</summary>
    public int DefaultTimeoutSeconds { get; set; } = 120;

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>优先级（越小越优先）</summary>
    public int Priority { get; set; } = 0;
}
