using System.Text.Json;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.Persistence;

public class SqliteDslRepository : IDslRepository
{
    private readonly IDbContextFactory<CJDSLDbContext> _factory;
    private readonly ILogger<SqliteDslRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public SqliteDslRepository(IDbContextFactory<CJDSLDbContext> factory, ILogger<SqliteDslRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<DslPage?> GetAsync(string pageCode, string version = "latest", CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.DslPages
            .FirstOrDefaultAsync(x => x.PageCode == pageCode, ct);

        if (entity == null) return null;

        return JsonSerializer.Deserialize<DslPage>(entity.JsonContent, JsonOptions);
    }

    public async Task<DslPage> SaveAsync(DslPage dsl, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var json = JsonSerializer.Serialize(dsl, JsonOptions);
        var now = DateTime.UtcNow;

        var existing = await db.DslPages.FirstOrDefaultAsync(x => x.PageCode == dsl.Id, ct);
        if (existing != null)
        {
            existing.JsonContent = json;
            existing.Title = dsl.Title;
            existing.Layout = dsl.Layout;
            existing.UpdatedAt = now;
        }
        else
        {
            db.DslPages.Add(new DslPageEntity
            {
                PageCode = dsl.Id,
                Title = dsl.Title,
                Layout = dsl.Layout,
                JsonContent = json,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
        return dsl;
    }

    public async Task<bool> DeleteAsync(string pageCode, string version, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.DslPages.FirstOrDefaultAsync(x => x.PageCode == pageCode, ct);
        if (entity == null) return false;

        db.DslPages.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<DslPage>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.DslPages.ToListAsync(ct);
        var result = new List<DslPage>();

        foreach (var entity in entities)
        {
            try
            {
                var page = JsonSerializer.Deserialize<DslPage>(entity.JsonContent, JsonOptions);
                if (page != null) result.Add(page);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize DSL page: {PageCode}", entity.PageCode);
            }
        }

        return result;
    }
}
