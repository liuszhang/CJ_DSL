using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using CJDSL.Domain.Shared;
using MediatR;

namespace CJDSL.Application.Dsl.Commands;

/// <summary>
/// 基于自然语言生成 DSL 命令
/// </summary>
public record GenerateDslFromNlpCommand(
    string Description,
    UserContext UserContext,
    GenerateOptions? Options = null) : IRequest<Result<DslPage>>;

public class GenerateDslFromNlpCommandHandler : IRequestHandler<GenerateDslFromNlpCommand, Result<DslPage>>
{
    private readonly IDslGenerator _dslGenerator;

    public GenerateDslFromNlpCommandHandler(IDslGenerator dslGenerator)
    {
        _dslGenerator = dslGenerator;
    }

    public async Task<Result<DslPage>> Handle(GenerateDslFromNlpCommand request, CancellationToken ct)
    {
        try
        {
            var dsl = await _dslGenerator.GenerateFromNlpAsync(
                request.Description,
                request.UserContext,
                request.Options ?? new GenerateOptions(),
                ct);

            return Result.Success(dsl);
        }
        catch (Exception ex)
        {
            return Result.Failure<DslPage>($"NLP 生成失败: {ex.Message}", "Dsl.NlpGenerationFailed");
        }
    }
}
