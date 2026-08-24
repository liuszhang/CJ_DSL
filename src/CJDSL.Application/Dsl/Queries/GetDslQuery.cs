using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using CJCore.Abstractions;
using MediatR;

namespace CJDSL.Application.Dsl.Queries;

/// <summary>
/// 获取页面 DSL 查询
/// </summary>
public record GetDslQuery(
    string PageCode,
    string? Role = null,
    string? Device = null,
    TargetPlatform? Platform = null) : IRequest<Result<DslPage>>;

public class GetDslQueryHandler : IRequestHandler<GetDslQuery, Result<DslPage>>
{
    private readonly IDslRepository _dslRepo;
    private readonly IDslCache _cache;

    public GetDslQueryHandler(IDslRepository dslRepo, IDslCache cache)
    {
        _dslRepo = dslRepo;
        _cache = cache;
    }

    public async Task<Result<DslPage>> Handle(GetDslQuery request, CancellationToken ct)
    {
        var cacheKey = $"dsl:page:{request.PageCode}:{request.Role ?? "all"}:{request.Device ?? "desktop"}:{request.Platform?.ToString() ?? "Web"}";
        
        var cached = await _cache.GetAsync<DslPage>(cacheKey, ct);
        if (cached != null) return Result.Success(cached);

        var dsl = await _dslRepo.GetAsync(request.PageCode, "latest", ct);
        if (dsl == null) return Result.Failure<DslPage>("DSL 页面未找到", "Dsl.NotFound");

        await _cache.SetAsync(cacheKey, dsl, TimeSpan.FromMinutes(5), ct);
        return Result.Success(dsl);
    }
}
