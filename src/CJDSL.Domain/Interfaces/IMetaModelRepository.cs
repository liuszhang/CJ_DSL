using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;

namespace CJDSL.Domain.Interfaces;

/// <summary>
/// 元模型仓储接口
/// </summary>
public interface IMetaModelRepository
{
    Task<M1_Object?> GetObjectAsync(string code, CancellationToken ct = default);
    Task<M1_Object?> GetObjectByIdAsync(string id, CancellationToken ct = default);
    Task<List<M1_Object>> GetAllObjectsAsync(CancellationToken ct = default);
    Task<M1_Object> AddObjectAsync(M1_Object obj, CancellationToken ct = default);
    Task<M1_Object> UpdateObjectAsync(M1_Object obj, CancellationToken ct = default);
    Task<bool> DeleteObjectAsync(string id, CancellationToken ct = default);

    Task<M0_Enum?> GetEnumAsync(string code, CancellationToken ct = default);
    Task<List<M0_Enum>> GetAllEnumsAsync(CancellationToken ct = default);
    Task<M0_Enum> AddEnumAsync(M0_Enum enumDef, CancellationToken ct = default);

    Task<M0_DataDictionary?> GetDictionaryAsync(string code, CancellationToken ct = default);
    Task<List<M0_DataDictionary>> GetAllDictionariesAsync(CancellationToken ct = default);
}

/// <summary>
/// DSL 生成器接口
/// </summary>
public interface IDslGenerator
{
    Task<DslPage> GenerateFormAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default);
    Task<DslPage> GenerateListAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default);
    Task<DslPage> GenerateDetailAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default);
    Task<DslPage> GenerateFromNlpAsync(string description, UserContext user, GenerateOptions options, CancellationToken ct = default);
    Task<DslPage> GenerateDashboardAsync(M4_Scene scene, GenerateOptions options, CancellationToken ct = default);
    Task<DslPage> AdaptAsync(DslPage baseDsl, UserContext user, DataContext data, CancellationToken ct = default);
}

/// <summary>
/// DSL 缓存接口
/// </summary>
public interface IDslCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// DSL 验证器接口
/// </summary>
public interface IDslValidator
{
    Task<DslValidationResult> ValidateAsync(DslPage dsl, CancellationToken ct = default);
}

public class DslValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 表达式求值器接口
/// </summary>
public interface IExpressionEvaluator
{
    T Evaluate<T>(string expression, IDataContext dataContext);
    bool CanEvaluate(string expression);
}

public interface IDataContext
{
    object? Get(string path);
    T? Get<T>(string path);
    void Set(string path, object? value);
    bool Has(string path);
}
