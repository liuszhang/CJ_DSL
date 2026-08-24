using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using CJCore.Abstractions;
using MediatR;

namespace CJDSL.Generation.Commands;

/// <summary>
/// 基于自然语言生成 DSL 命令
/// </summary>
public record GenerateDslFromNlpCommand(
    string Description,
    UserContext UserContext,
    GenerateOptions? Options = null) : IRequest<Result<DslPage>>;

public class GenerateDslFromNlpCommandHandler : IRequestHandler<GenerateDslFromNlpCommand, Result<DslPage>>
{
    private readonly IDslGeneratorResolver _generatorResolver;

    public GenerateDslFromNlpCommandHandler(IDslGeneratorResolver generatorResolver)
    {
        _generatorResolver = generatorResolver;
    }

    public async Task<Result<DslPage>> Handle(GenerateDslFromNlpCommand request, CancellationToken ct)
    {
        try
        {
            // NLP 链路默认走 LLM（未配置 LLM 时由 Resolver 自动降级为模板正则解析）
            var generator = _generatorResolver.Resolve(request.Options?.Provider, DslGeneratorProviders.Llm);

            var dsl = await generator.GenerateFromNlpAsync(
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
