using System.Text.Json;
using CJDSL.Domain.Configuration;

namespace CJDSL.Infrastructure.Configuration;

/// <summary>
/// 系统配置管理服务（内存 + JSON 文件持久化）
/// </summary>
public class SystemConfigService
{
    private SystemConfig _config;
    private readonly string _configPath;
    private readonly object _lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public event Action? ConfigChanged;

    public SystemConfigService(string? configDir = null)
    {
        var dir = configDir ?? Path.Combine(AppContext.BaseDirectory, "config");
        Directory.CreateDirectory(dir);
        _configPath = Path.Combine(dir, "system-config.json");
        _config = LoadConfig();
    }

    public SystemConfig GetConfig()
    {
        lock (_lock)
        {
            return _config;
        }
    }

    public LlmConnectionConfig GetLlmConfig()
    {
        lock (_lock)
        {
            return _config.Llm;
        }
    }

    public OntologySourceConfig GetOntologyConfig()
    {
        lock (_lock)
        {
            return _config.Ontology;
        }
    }

    public void UpdateLlmConfig(LlmConnectionConfig llm)
    {
        lock (_lock)
        {
            _config.Llm = llm;
            SaveConfig();
        }
        ConfigChanged?.Invoke();
    }

    public void UpdateOntologyConfig(OntologySourceConfig ontology)
    {
        lock (_lock)
        {
            _config.Ontology = ontology;
            SaveConfig();
        }
        ConfigChanged?.Invoke();
    }

    public void UpdateConfig(SystemConfig config)
    {
        lock (_lock)
        {
            _config = config;
            SaveConfig();
        }
        ConfigChanged?.Invoke();
    }

    private SystemConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<SystemConfig>(json, JsonOptions) ?? new SystemConfig();
            }
        }
        catch
        {
            // 配置文件损坏时返回默认配置
        }
        return new SystemConfig();
    }

    private void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // 写入失败时静默处理
        }
    }
}
