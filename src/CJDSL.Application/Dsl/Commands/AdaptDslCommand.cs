using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using CJDSL.Domain.Shared;
using MediatR;

namespace CJDSL.Application.Dsl.Commands;

/// <summary>
/// 适配 DSL 命令 - 根据用户上下文动态调整 DSL
/// </summary>
public record AdaptDslCommand(
    DslPage BaseDsl,
    UserContext UserContext,
    Dictionary<string, object>? DataContext = null) : IRequest<Result<DslPage>>;

public class AdaptDslCommandHandler : IRequestHandler<AdaptDslCommand, Result<DslPage>>
{
    private readonly IDslGenerator _dslGenerator;

    public AdaptDslCommandHandler(IDslGenerator dslGenerator)
    {
        _dslGenerator = dslGenerator;
    }

    public async Task<Result<DslPage>> Handle(AdaptDslCommand request, CancellationToken ct)
    {
        try
        {
            var dataContext = new DataContext();
            if (request.DataContext != null)
            {
                foreach (var kv in request.DataContext)
                    dataContext.Values[kv.Key] = kv.Value;
            }

            var adapted = await _dslGenerator.AdaptAsync(
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
