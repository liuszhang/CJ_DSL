using CJDSL.Application.Dsl;
using CJDSL.Application.Dsl.Commands;
using CJDSL.Application.Dsl.Queries;
using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using CJDSL.Domain.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CJDSL.Api.Endpoints;

public static class DslEndpoints
{
    public static IEndpointRouteBuilder MapDslEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dsl")
            .WithTags("DSL")
            .WithOpenApi();

        // 基于元模型生成 DSL（?provider=llm|template，默认 template）
        group.MapPost("/generate", async (
            GenerateDslRequest request,
            [FromQuery] string? provider,
            IMediator mediator,
            IDslSecurityValidator security,
            CancellationToken ct) =>
        {
            var options = request.Options ?? new GenerateOptions();
            if (!string.IsNullOrWhiteSpace(provider))
                options.Provider = provider;

            var command = new GenerateDslCommand(
                request.MetaObjectCode,
                request.Layout,
                request.UserContext,
                options);
            var result = await mediator.Send(command, ct);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error, code = result.ErrorCode });

            var generated = await security.SanitizeAsync(result.Value, ct);
            var sec = await security.ValidateAsync(generated, ct);
            return sec.IsSafe
                ? Results.Ok(generated)
                : Results.BadRequest(new { error = "DSL 安全校验未通过", details = sec.Errors, code = "Dsl.Security" });
        });

        // 基于自然语言生成 DSL（默认走 LLM，未配置 LLM 时降级模板解析）
        group.MapPost("/generate-from-nlp", async (
            GenerateFromNlpRequest request,
            [FromQuery] string? provider,
            IMediator mediator,
            IDslSecurityValidator security,
            CancellationToken ct) =>
        {
            var options = request.Options ?? new GenerateOptions();
            if (!string.IsNullOrWhiteSpace(provider))
                options.Provider = provider;

            var command = new GenerateDslFromNlpCommand(
                request.Description,
                request.UserContext,
                options);
            var result = await mediator.Send(command, ct);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error, code = result.ErrorCode });

            var generated = await security.SanitizeAsync(result.Value, ct);
            var sec = await security.ValidateAsync(generated, ct);
            return sec.IsSafe
                ? Results.Ok(generated)
                : Results.BadRequest(new { error = "DSL 安全校验未通过", details = sec.Errors, code = "Dsl.Security" });
        });

        // 基于当前上下文动态调整 DSL（默认走 LLM，未配置 LLM 时降级规则适配）
        group.MapPost("/adapt", async (
            AdaptDslRequest request,
            [FromQuery] string? provider,
            IMediator mediator,
            IDslSecurityValidator security,
            CancellationToken ct) =>
        {
            var command = new AdaptDslCommand(
                request.BaseDsl,
                request.UserContext,
                request.DataContext,
                provider);
            var result = await mediator.Send(command, ct);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error, code = result.ErrorCode });

            var generated = await security.SanitizeAsync(result.Value, ct);
            var sec = await security.ValidateAsync(generated, ct);
            return sec.IsSafe
                ? Results.Ok(generated)
                : Results.BadRequest(new { error = "DSL 安全校验未通过", details = sec.Errors, code = "Dsl.Security" });
        });

        // 获取页面 DSL
        group.MapGet("/page/{pageCode}", async (
            string pageCode,
            [FromQuery] string? role,
            [FromQuery] string? device,
            [FromQuery] TargetPlatform? platform,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetDslQuery(pageCode, role, device, platform);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error, code = result.ErrorCode });
        });

        // 验证 DSL 语法 + 安全
        group.MapPost("/validate", async (
            DslPage dsl,
            IDslValidator validator,
            IDslSecurityValidator security) =>
        {
            var result = await validator.ValidateAsync(dsl);
            var sec = await security.ValidateAsync(dsl);
            result.Errors.AddRange(sec.Errors);
            result.Warnings.AddRange(sec.Warnings);
            return Results.Ok(result);
        });

        return app;
    }
}
