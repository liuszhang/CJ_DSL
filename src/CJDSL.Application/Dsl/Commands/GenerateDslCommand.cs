using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain.Interfaces;
using CJDSL.Domain.Shared;
using MediatR;

namespace CJDSL.Application.Dsl.Commands;

/// <summary>
/// 基于元模型生成 DSL 命令
/// </summary>
public record GenerateDslCommand(
    string MetaObjectCode,
    string Layout,
    UserContext UserContext,
    GenerateOptions? Options = null) : IRequest<Result<DslPage>>;

public class GenerateDslCommandHandler : IRequestHandler<GenerateDslCommand, Result<DslPage>>
{
    private readonly IMetaModelRepository _metaModelRepo;
    private readonly IDslGenerator _dslGenerator;
    private readonly IDslCache _cache;

    public GenerateDslCommandHandler(
        IMetaModelRepository metaModelRepo,
        IDslGenerator dslGenerator,
        IDslCache cache)
    {
        _metaModelRepo = metaModelRepo;
        _dslGenerator = dslGenerator;
        _cache = cache;
    }

    public async Task<Result<DslPage>> Handle(GenerateDslCommand request, CancellationToken ct)
    {
        // 构建缓存键
        var cacheKey = $"dsl:{request.MetaObjectCode}:{request.Layout}:{string.Join(",", request.UserContext.Roles)}:{request.Options?.DeviceType ?? "Desktop"}:{request.Options?.TargetPlatform ?? TargetPlatform.Web}";

        // 尝试命中缓存
        var cached = await _cache.GetAsync<DslPage>(cacheKey, ct);
        if (cached != null) return Result.Success(cached);

        // 加载元模型
        var metaObject = await _metaModelRepo.GetObjectAsync(request.MetaObjectCode, ct);
        if (metaObject == null)
            return Result.Failure<DslPage>($"未找到元对象: {request.MetaObjectCode}", "MetaObject.NotFound");

        // 根据布局类型生成 DSL
        DslPage dsl = request.Layout.ToLower() switch
        {
            "list" => await _dslGenerator.GenerateListAsync(metaObject, request.Options ?? new GenerateOptions(), ct),
            "detail" => await _dslGenerator.GenerateDetailAsync(metaObject, request.Options ?? new GenerateOptions(), ct),
            "dashboard" => await _dslGenerator.GenerateDashboardAsync(null!, request.Options ?? new GenerateOptions(), ct),
            _ => await _dslGenerator.GenerateFormAsync(metaObject, request.Options ?? new GenerateOptions(), ct)
        };

        // 缓存结果
        await _cache.SetAsync(cacheKey, dsl, TimeSpan.FromMinutes(10), ct);

        return Result.Success(dsl);
    }
}
