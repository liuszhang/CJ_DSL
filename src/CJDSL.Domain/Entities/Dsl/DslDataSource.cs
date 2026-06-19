namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// DSL 数据源配置
/// </summary>
public class DslDataSource
{
    /// <summary>
    /// 数据源类型：api, dictionary, enum, static
    /// </summary>
    public string Type { get; set; } = "api";

    /// <summary>
    /// API 端点（Type=api 时必填）
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// 字典/枚举编码（Type=dictionary/enum 时必填）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 静态数据（Type=static 时必填）
    /// </summary>
    public List<Dictionary<string, object>>? StaticData { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public Dictionary<string, object>? Params { get; set; }

    /// <summary>
    /// 搜索参数名（用于 autocomplete）
    /// </summary>
    public string? SearchParam { get; set; }

    /// <summary>
    /// 分页配置
    /// </summary>
    public DslPagination? Pagination { get; set; }

    /// <summary>
    /// 是否服务端分页
    /// </summary>
    public bool ServerSide { get; set; } = false;

    /// <summary>
    /// 数据映射路径（如 "data.items" 用于从嵌套响应中提取数据）
    /// </summary>
    public string? DataPath { get; set; }
}

public class DslPagination
{
    public string PageParam { get; set; } = "pageIndex";
    public string SizeParam { get; set; } = "pageSize";
    public int DefaultSize { get; set; } = 20;
}
