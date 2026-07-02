namespace CJDSL.Domain.Configuration;

/// <summary>
/// 单个 LLM 提供商配置
/// </summary>
public class LlmProviderConfig
{
    /// <summary>显示名称（如 "GPT-4o"、"本地 Ollama"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>提供商类型：OpenAI / Ollama</summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>API 基础地址</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    /// <summary>API Key（Ollama 可为空）</summary>
    public string? ApiKey { get; set; }

    /// <summary>模型名称</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>温度</summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>最大 Token 数</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>超时时间（秒）</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>是否为当前启用的提供商（同时只能有一个）</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// LLM 连接配置（多提供商，单启用）
/// </summary>
public class LlmConnectionConfig
{
    /// <summary>提供商列表</summary>
    public List<LlmProviderConfig> Providers { get; set; } = new();

    /// <summary>获取当前启用的提供商</summary>
    public LlmProviderConfig? GetActive()
    {
        return Providers.FirstOrDefault(p => p.IsActive);
    }
}

/// <summary>
/// DSL 生成提示词配置
/// </summary>
public class DslPromptConfig
{
    /// <summary>系统提示词 - 定义 LLM 角色和 CJDSL Schema 规范，留空使用内置默认值</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>表单生成提示词模板，留空使用内置默认值</summary>
    public string FormPromptTemplate { get; set; } = string.Empty;

    /// <summary>列表生成提示词模板，留空使用内置默认值</summary>
    public string ListPromptTemplate { get; set; } = string.Empty;

    /// <summary>自然语言生成提示词模板，留空使用内置默认值</summary>
    public string NlpPromptTemplate { get; set; } = string.Empty;
}

/// <summary>
/// 本体（元模型）数据源配置
/// </summary>
public class OntologySourceConfig
{
    public string SourceType { get; set; } = "InMemory";
    public string? ConnectionString { get; set; }
    public string? FilePath { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public bool AutoSync { get; set; } = false;
    public int SyncIntervalMinutes { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// 系统全局配置
/// </summary>
public class SystemConfig
{
    public LlmConnectionConfig Llm { get; set; } = new();
    public DslPromptConfig DslPrompt { get; set; } = new();
    public OntologySourceConfig Ontology { get; set; } = new();
}
