using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Domain.Interfaces;

/// <summary>
/// DSL 仓储接口
/// </summary>
public interface IDslRepository
{
    Task<DslPage?> GetAsync(string pageCode, string version = "latest", CancellationToken ct = default);
    Task<DslPage> SaveAsync(DslPage dsl, CancellationToken ct = default);
    Task<bool> DeleteAsync(string pageCode, string version, CancellationToken ct = default);
    Task<List<DslPage>> GetAllAsync(CancellationToken ct = default);
}
