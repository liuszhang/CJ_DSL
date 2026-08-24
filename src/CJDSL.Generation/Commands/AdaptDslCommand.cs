using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using CJDSL.Domain.Shared;
using MediatR;

namespace CJDSL.Generation.Commands;

/// <summary>
/// 适配 DSL 命令 - 根据用户上下文动态调整 DSL
/// </summary>
public record AdaptDslCommand(
    DslPage BaseDsl,
    UserContext UserContext,
    Dictionary<string, object>? DataContext = null,
    string? Provider = null) : IRequest<Result<DslPage>>;

public class AdaptDslCommandHandler : IRequestHandler<AdaptDslCommand, Result<DslPage>>
{
    private readonly IDslGeneratorResolver _generatorResolver;

    public AdaptDslCommandHandler(IDslGeneratorResolver generatorResolver)
    {
        _generatorResolver = generatorResolver;
    }

    public async Task<Result<DslPage>> Handle(AdaptDslCommand request, CancellationToken ct)
    {
        try
        {
            // 适配链路默认走 LLM（未配置 LLM 时由 Resolver 自动降级为模板规则适配）
            var generator = _generatorResolver.Resolve(request.Provider, DslGeneratorProviders.Llm);

            var dataContext = new DataContext();
            if (request.DataContext != null)
            {
                foreach (var kv in request.DataContext)
                    dataContext.Values[kv.Key] = kv.Value;
            }

            var adapted = await generator.AdaptAsync(
                request.BaseDsl,
                request.UserContext,
                dataContext,
                ct);

            return Result.Success(adapted);
        }
        catch (Exception ex)
        {
            return Result.Failure<DslPage>($"DSL 适配失败: {ex.Message}", "Dsl.AdaptFailed");
        }
    }
}
