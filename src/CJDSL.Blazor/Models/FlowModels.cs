namespace CJDSL.Blazor.Models;

/// <summary>
/// 溯源路径节点（对齐 RootCausePathHop）。
/// </summary>
public class FlowNode
{
    /// <summary>节点 id：hop-{hop} 或 InstanceId</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>hop 序号（主链顺序，从 0 开始）</summary>
    public int Hop { get; set; }

    /// <summary>节点编码，如 FM-ENG-003</summary>
    public string Node { get; set; } = string.Empty;

    /// <summary>FaultPhenomenon / TelemetrySegment / TextLog / FaultMode</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>节点说明（展示时截断 ≤30 字）</summary>
    public string? Note { get; set; }

    /// <summary>证据强度 0-1</summary>
    public double? EvidenceStrength { get; set; }

    /// <summary>路径置信度 0-1</summary>
    public double? PathConfidence { get; set; }

    /// <summary>实例 Id（点击事件携带，不直接展示）</summary>
    public Guid? InstanceId { get; set; }
}

/// <summary>
/// 溯源路径边（相邻 hop 关系）。
/// </summary>
public class FlowEdge
{
    /// <summary>源节点 id（hop-{hop}）</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>目标节点 id（hop-{hop}）</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>关系名，如 evidence_of / causes</summary>
    public string Relation { get; set; } = string.Empty;
}

/// <summary>
/// 已排除候选分支（对齐 RootCauseBranchEliminated）。
/// </summary>
public class FlowEliminatedBranch
{
    /// <summary>候选节点编码，如 FM-ENG-004</summary>
    public string Candidate { get; set; } = string.Empty;

    /// <summary>候选类型（FaultMode 等）</summary>
    public string CandidateType { get; set; } = string.Empty;

    /// <summary>排除原因</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>候选证据强度 0-1</summary>
    public double? Strength { get; set; }
}
