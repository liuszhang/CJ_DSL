namespace CJDSL.Domain;

/// <summary>
/// 生成选项
/// </summary>
public class GenerateOptions
{
    public string Layout { get; set; } = "form";
    public List<string> Roles { get; set; } = new();
    public UserPreference Preference { get; set; } = new();
    public string DeviceType { get; set; } = "Desktop";
    public Dictionary<string, object>? DataContext { get; set; }
    public float Temperature { get; set; } = 0.3f;
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
