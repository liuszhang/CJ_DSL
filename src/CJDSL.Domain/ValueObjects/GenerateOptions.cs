using System.Text.Json.Serialization;

namespace CJDSL.Domain;

/// <summary>
/// 目标渲染平台
/// </summary>
[JsonConverter(typeof(DomainTargetPlatformConverter))]
public enum TargetPlatform
{
    Web,
    Wpf,
    Maui,
    React,
    Vue
}

/// <summary>
/// TargetPlatform 的 JSON 转换器（字符串/数字均可，未知值回退 Web）。
/// </summary>
public sealed class DomainTargetPlatformConverter : FlexibleEnumConverter<TargetPlatform>
{
}

/// <summary>
/// 生成选项
/// </summary>
public class GenerateOptions
{
    public string Layout { get; set; } = "form";
    public List<string> Roles { get; set; } = new();
    public UserPreference Preference { get; set; } = new();
    public string DeviceType { get; set; } = "Desktop";
    public TargetPlatform TargetPlatform { get; set; } = TargetPlatform.Web;
    public Dictionary<string, object>? DataContext { get; set; }
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// 生成器提供方："template"（模板）| "llm"（大模型）。
    /// 为空时由各链路使用自身默认值（元模型生成默认 template，NLP/Adapt 默认 llm）。
    /// </summary>
    public string? Provider { get; set; }
}

public class UserPreference
{
    public string Density { get; set; } = "comfortable"; // compact, comfortable, spacious
    public string Theme { get; set; } = "light"; // light, dark, system
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// 用户上下文
/// </summary>
public class UserContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public string? Department { get; set; }
    public string? TenantId { get; set; }
}

/// <summary>
/// 数据上下文
/// </summary>
public class DataContext
{
    public Dictionary<string, object> Values { get; set; } = new();
}
