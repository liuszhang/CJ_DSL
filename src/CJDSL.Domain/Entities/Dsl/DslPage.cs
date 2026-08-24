using System.Text.Json.Serialization;
using CJDSL.Domain;

namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// CJDSL 页面根节点
/// </summary>
public class DslPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 页面布局模式：form, list, detail, dashboard, custom
    /// </summary>
    public string Layout { get; set; } = "form";

    /// <summary>
    /// 目标渲染平台
    /// </summary>
    public TargetPlatform TargetPlatform { get; set; } = TargetPlatform.Web;

    /// <summary>
    /// 推荐渲染器包标识（仅作提示，前端可按需选择）。
    /// React 平台输出由 @cj/cjdsl-react 消费；Web 由 CJDSL.Blazor 消费；Wpf 由 CJDSL.Wpf 消费。
    /// </summary>
    public string? RendererHint { get; set; }

    public DslDataSource? DataSource { get; set; }
    public DslPermission? Permission { get; set; }
    public DslResponsive? Responsive { get; set; }
    public List<DslComponent> Components { get; set; } = new();
    public List<DslPageEvent>? PageEvents { get; set; }
    public DslStyle? Style { get; set; }

    /// <summary>
    /// 获取组件树中所有组件（扁平化）
    /// </summary>
    public IEnumerable<DslComponent> GetAllComponents()
    {
        return Components.SelectMany(c => c.Descendants());
    }
}

public class DslPageEvent
{
    public string Type { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
}
