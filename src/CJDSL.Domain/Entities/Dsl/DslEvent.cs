namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// DSL 事件定义
/// </summary>
public class DslEvent
{
    /// <summary>
    /// 事件类型：onClick, onChange, onSubmit, onLoad, onRowClick, onSearch...
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 处理器名称：预定义行为或自定义 API 调用
    /// </summary>
    public string Handler { get; set; } = string.Empty;

    /// <summary>
    /// 处理器参数
    /// </summary>
    public Dictionary<string, object>? Params { get; set; }

    /// <summary>
    /// 执行前确认对话框配置
    /// </summary>
    public DslConfirm? Confirm { get; set; }

    /// <summary>
    /// 防抖/节流（毫秒）
    /// </summary>
    public int? DebounceMs { get; set; }
}

public class DslConfirm
{
    public string Title { get; set; } = "确认";
    public string Message { get; set; } = string.Empty;
    public string ConfirmText { get; set; } = "确认";
    public string CancelText { get; set; } = "取消";
}
