namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// 响应式断点配置
/// </summary>
public class DslResponsive
{
    public Dictionary<string, BreakpointConfig>? Breakpoints { get; set; }
}

public class BreakpointConfig
{
    public int Columns { get; set; } = 1;
    public string ComponentSize { get; set; } = "Medium";
}
