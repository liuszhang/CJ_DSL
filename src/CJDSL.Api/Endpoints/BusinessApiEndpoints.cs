using System.Text.Json;
using CJDSL.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CJDSL.Api.Endpoints;

/// <summary>
/// 元模型驱动的通用业务数据端点。
/// 路由约定与模板生成器生成的 apiCall 一致：
///   POST /api/{objectCode}/save    保存草稿
///   POST /api/{objectCode}/submit  提交
///   GET  /api/{objectCode}/list    分页查询
///   GET  /api/{objectCode}/item/{id}  单条查询
///   DELETE /api/{objectCode}/item/{id} 删除
/// 请求/响应体为动态 JSON，按元模型对象编码适配，无需为每个业务对象手写端点。
/// </summary>
public static class BusinessApiEndpoints
{
    // 元模型对象编码白名单格式：字母开头，仅含字母/数字/下划线（防路径滥用；"dsl" 为保留前缀）
    private static bool IsValidObjectCode(string code) =>
        !string.Equals(code, "dsl", StringComparison.OrdinalIgnoreCase)
        && System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Za-z][A-Za-z0-9_]{0,99}$");

    public static IEndpointRouteBuilder MapBusinessApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{objectCode}")
            .WithTags("BusinessData")
            .WithOpenApi();

        // 保存草稿
        group.MapPost("/save", async (
            string objectCode,
            JsonElement body,
            IBusinessDataService service,
            CancellationToken ct) =>
        {
            if (!IsValidObjectCode(objectCode))
                return Results.BadRequest(new { error = $"非法对象编码: {objectCode}" });
            if (body.ValueKind != JsonValueKind.Object)
                return Results.BadRequest(new { error = "请求体必须是 JSON 对象" });

            // 使用 JsonSerializer 序列化（而非 GetRawText）以正确处理含中文等多字节 UTF-8 内容
            var record = await service.SaveAsync(objectCode, JsonSerializer.Serialize(body), ct);
            return Results.Ok(ToResponse(record));
        });

        // 提交
        group.MapPost("/submit", async (
            string objectCode,
            JsonElement body,
            IBusinessDataService service,
            CancellationToken ct) =>
        {
            if (!IsValidObjectCode(objectCode))
                return Results.BadRequest(new { error = $"非法对象编码: {objectCode}" });
            if (body.ValueKind != JsonValueKind.Object)
                return Results.BadRequest(new { error = "请求体必须是 JSON 对象" });

            var record = await service.SubmitAsync(objectCode, JsonSerializer.Serialize(body), ct);
            return Results.Ok(ToResponse(record));
        });

        // 分页列表
        group.MapGet("/list", async (
            string objectCode,
            IBusinessDataService service,
            CancellationToken ct,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20) =>
        {
            if (!IsValidObjectCode(objectCode))
                return Results.BadRequest(new { error = $"非法对象编码: {objectCode}" });

            var page = await service.ListAsync(
                objectCode,
                pageIndex <= 0 ? 1 : pageIndex,
                pageSize <= 0 ? 20 : pageSize,
                ct);

            return Results.Ok(new
            {
                items = page.Items.Select(ToResponse),
                total = page.Total,
                pageIndex = page.PageIndex,
                pageSize = page.PageSize
            });
        });

        // 单条查询
        group.MapGet("/item/{id}", async (
            string objectCode,
            string id,
            IBusinessDataService service,
            CancellationToken ct) =>
        {
            if (!IsValidObjectCode(objectCode))
                return Results.BadRequest(new { error = $"非法对象编码: {objectCode}" });

            var record = await service.GetAsync(objectCode, id, ct);
            return record == null
                ? Results.NotFound(new { error = $"未找到记录: {objectCode}/{id}" })
                : Results.Ok(ToResponse(record));
        });

        // 删除
        group.MapDelete("/item/{id}", async (
            string objectCode,
            string id,
            IBusinessDataService service,
            CancellationToken ct) =>
        {
            if (!IsValidObjectCode(objectCode))
                return Results.BadRequest(new { error = $"非法对象编码: {objectCode}" });

            var removed = await service.DeleteAsync(objectCode, id, ct);
            return removed
                ? Results.Ok(new { deleted = true })
                : Results.NotFound(new { error = $"未找到记录: {objectCode}/{id}" });
        });

        return app;
    }

    private static object ToResponse(BusinessDataRecord record)
    {
        // data 反序列化为动态 JSON 返回，前端可直接消费
        JsonElement data;
        try
        {
            using var doc = JsonDocument.Parse(record.JsonData);
            data = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var doc = JsonDocument.Parse("{}");
            data = doc.RootElement.Clone();
        }

        return new
        {
            id = record.Id,
            objectCode = record.ObjectCode,
            status = record.Status,
            data,
            createdAt = record.CreatedAt,
            updatedAt = record.UpdatedAt
        };
    }
}
