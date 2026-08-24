using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using CJDSL.Generation.Commands;
using MediatR;

namespace CJDSL.Generation;

/// <summary>
/// <see cref="IDslGenerationService"/> 默认实现：派发 MediatR 生成命令并做安全清洗（后处理）。
/// 命令 handler 已完成「规则/LLM 解析 + 自动降级」；此处统一负责输出前的安全清洗（richText XSS 等）。
/// </summary>
public class DslGenerationService : IDslGenerationService
{
    private readonly IMediator _mediator;
    private readonly IDslSecurityValidator _security;

    public DslGenerationService(IMediator mediator, IDslSecurityValidator security)
    {
        _mediator = mediator;
        _security = security;
    }

    public async Task<Result<DslPage>> GenerateFromNlpAsync(
        string description, UserContext user, GenerateOptions? options = null, CancellationToken ct = default)
    {
        var command = new GenerateDslFromNlpCommand(description, user, options);
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess) return result;

        var sanitized = await _security.SanitizeAsync(result.Value, ct);
        return Result.Success(sanitized);
    }

    public async Task<Result<DslPage>> GenerateFromMetaAsync(
        string metaObjectCode, string layout, UserContext user, GenerateOptions? options = null, CancellationToken ct = default)
    {
        var command = new GenerateDslCommand(metaObjectCode, layout, user, options);
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess) return result;

        var sanitized = await _security.SanitizeAsync(result.Value, ct);
        return Result.Success(sanitized);
    }

    public async Task<Result<DslPage>> AdaptAsync(
        DslPage baseDsl, UserContext user, Dictionary<string, object>? dataContext = null, string? provider = null, CancellationToken ct = default)
    {
        var command = new AdaptDslCommand(baseDsl, user, dataContext, provider);
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess) return result;

        var sanitized = await _security.SanitizeAsync(result.Value, ct);
        return Result.Success(sanitized);
    }
}
