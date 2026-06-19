namespace CJDSL.Domain.Configuration;

/// <summary>
/// LLM 连接配置
/// </summary>
public class LlmConnectionConfig
{
    public string Provider { get; set; } = "OpenAI";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public float Temperature { get; set; } = 0.3f;
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 120;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// 本体（元模型）数据源配置
/// </summary>
public class OntologySourceConfig
{
    public string SourceType { get; set; } = "InMemory"; // InMemory, Database, File, Api
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
    public OntologySourceConfig Ontology { get; set; } = new();
}
