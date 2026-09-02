// flow 渲染器类型定义（对齐《CJDSL三端Flow渲染器接口定义方案》4.1）
//   溯源路径结构化图：nodes/edges 有向链 + eliminated 灰化虚线分组 + 可选点击高亮。

/** 溯源路径节点（对齐 RootCausePathHop） */
export interface FlowNode {
  id: string;
  hop: number;
  node: string;
  type: string;
  note?: string;
  evidenceStrength?: number;
  pathConfidence?: number;
  instanceId?: string;
}

/** 溯源路径边（相邻 hop 关系） */
export interface FlowEdge {
  source: string;
  target: string;
  relation: string;
}

/** 已排除候选分支（对齐 RootCauseBranchEliminated） */
export interface FlowEliminatedBranch {
  candidate: string;
  candidateType: string;
  reason: string;
  strength?: number;
}

/** type="flow" 节点 props 契约 */
export interface FlowProps {
  nodes: FlowNode[];
  edges: FlowEdge[];
  eliminated?: FlowEliminatedBranch[];
  highlightOnClick?: boolean;
  layout?: "horizontal" | "vertical";
}

export const FLOW_LAYOUTS = ["horizontal", "vertical"] as const;
