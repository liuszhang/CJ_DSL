using CJDSL.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.Persistence;

/// <summary>
/// SQLite 版业务数据服务 - 通用宽表（BusinessData）持久化任意元模型对象的数据。
/// 首次使用时自动确保 BusinessData 表存在（兼容旧库：EnsureCreated 对已存在的库不建新表，
/// 因此额外执行 CREATE TABLE IF NOT EXISTS 兜底）。
/// </summary>
public class SqliteBusinessDataService : IBusinessDataService
{
    private readonly IDbContextFactory<CJDSLDbContext> _factory;
    private readonly ILogger<SqliteBusinessDataService> _logger;
    private volatile bool _tableEnsured;
    private readonly SemaphoreSlim _ensureLock = new(1, 1);

    public SqliteBusinessDataService(
        IDbContextFactory<CJDSLDbContext> factory,
        ILogger<SqliteBusinessDataService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<BusinessDataRecord> SaveAsync(string objectCode, string jsonData, CancellationToken ct = default)
        => await UpsertAsync(objectCode, jsonData, "draft", ct);

    public async Task<BusinessDataRecord> SubmitAsync(string objectCode, string jsonData, CancellationToken ct = default)
        => await UpsertAsync(objectCode, jsonData, "submitted", ct);

    public async Task<BusinessDataPage> ListAsync(string objectCode, int pageIndex = 1, int pageSize = 20, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.BusinessData.AsNoTracking().Where(x => x.ObjectCode == objectCode);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToRecord(x))
            .ToListAsync(ct);

        return new BusinessDataPage { Items = items, Total = total, PageIndex = pageIndex, PageSize = pageSize };
    }

    public async Task<BusinessDataRecord?> GetAsync(string objectCode, string id, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.BusinessData.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ObjectCode == objectCode && x.Id == id, ct);
        return entity == null ? null : ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(string objectCode, string id, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.BusinessData
            .FirstOrDefaultAsync(x => x.ObjectCode == objectCode && x.Id == id, ct);
        if (entity == null) return false;
        db.BusinessData.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<BusinessDataRecord> UpsertAsync(string objectCode, string jsonData, string status, CancellationToken ct)
    {
        await EnsureTableAsync(ct);
        await using var db = await _factory.CreateDbContextAsync(ct);

        var id = BusinessDataJsonHelper.ExtractId(jsonData);
        var now = DateTime.UtcNow;

        BusinessDataEntity? entity = null;
        if (id != null)
        {
            entity = await db.BusinessData
                .FirstOrDefaultAsync(x => x.ObjectCode == objectCode && x.Id == id, ct);
        }

        if (entity != null)
        {
            entity.JsonData = jsonData;
            entity.Status = status;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new BusinessDataEntity
            {
                Id = id ?? Guid.NewGuid().ToString("N"),
                ObjectCode = objectCode,
                Status = status,
                JsonData = jsonData,
                CreatedAt = now
            };
            db.BusinessData.Add(entity);
        }

        await db.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    private async Task EnsureTableAsync(CancellationToken ct)
    {
        if (_tableEnsured) return;
        await _ensureLock.WaitAsync(ct);
        try
        {
            if (_tableEnsured) return;

            await using var db = await _factory.CreateDbContextAsync(ct);
            // 新库：建全部表；旧库：EnsureCreated 为 no-op
            await db.Database.EnsureCreatedAsync(ct);
            // 旧库兜底：单独确保 BusinessData 表存在
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "BusinessData" (
                    "ObjectCode" TEXT NOT NULL,
                    "Id" TEXT NOT NULL,
                    "Status" TEXT NOT NULL,
                    "JsonData" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NULL,
                    CONSTRAINT "PK_BusinessData" PRIMARY KEY ("ObjectCode", "Id")
                );
                CREATE INDEX IF NOT EXISTS "IX_BusinessData_ObjectCode" ON "BusinessData" ("ObjectCode");
                """, ct);

            _tableEnsured = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化 BusinessData 表失败");
            throw;
        }
        finally
        {
            _ensureLock.Release();
        }
    }

    private static BusinessDataRecord ToRecord(BusinessDataEntity e) => new()
    {
        Id = e.Id,
        ObjectCode = e.ObjectCode,
        Status = e.Status,
        JsonData = e.JsonData,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
