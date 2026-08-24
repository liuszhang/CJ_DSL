using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;

namespace CJDSL.Generation.Interfaces;

/// <summary>
/// CJDSL 生成能力高层门面。
/// 各产品（CJOEM / CJOntology / ABWork / DA.DSH.PA 等）引用 CJDSL.Generation 后，
/// 仅需注入本接口即可在自身进程内本地生成 DSL（规则 + LLM 双路、自动降级、后处理 + 安全清洗），
/// 不必依赖集中 HTTP 服务，也不必直接派发 MediatR 命令。
/// </summary>
public interface IDslGenerationService
{
    /// <summary>基于自然语言生成 DSL（默认走 LLM，未配置 LLM 时自动降级模板正则解析）。</summary>
    Task<Result<DslPage>> GenerateFromNlpAsync(
        string description,
        UserContext user,
        GenerateOptions? options = null,
        CancellationToken ct = default);

    /// <summary>基于元模型生成 DSL（form/list/detail/dashboard）。</summary>
    Task<Result<DslPage>> GenerateFromMetaAsync(
        string metaObjectCode,
        string layout,
        UserContext user,
        GenerateOptions? options = null,
        CancellationToken ct = default);

    /// <summary>基于当前上下文动态调整（适配）已有 DSL。</summary>
    Task<Result<DslPage>> AdaptAsync(
        DslPage baseDsl,
        UserContext user,
        Dictionary<string, object>? dataContext = null,
        string? provider = null,
        CancellationToken ct = default);
}
