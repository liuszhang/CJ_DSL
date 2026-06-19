using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;

namespace CJDSL.Application.Dsl;

/// <summary>
/// 生成 DSL 请求 DTO
/// </summary>
public record GenerateDslRequest(
    string MetaObjectCode,
    string Layout,
    UserContext UserContext,
    GenerateOptions? Options = null);

public record GenerateFromNlpRequest(
    string Description,
    UserContext UserContext,
    GenerateOptions? Options = null);

public record AdaptDslRequest(
    DslPage BaseDsl,
    UserContext UserContext,
    Dictionary<string, object>? DataContext = null);

public record DslPageDto(
    string Id,
    string Title,
    string Description,
    string Layout,
    DslPermission? Permission,
    List<DslComponentDto> Components);

public record DslComponentDto(
    string Id,
    string Type,
    string? Label,
    string? FieldName,
    int? Span,
    string? VisibleIf,
    string? DisabledIf,
    string? DataBind);

public record UserContextDto(
    string UserId,
    string UserName,
    List<string> Roles,
    List<string> Permissions,
    string? Department = null,
    string? TenantId = null);

public record ApiResponse(
    bool Success,
    string? Message = null,
    object? Data = null);
