using System.Text.Json;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.Persistence;

public class SqliteMetaModelRepository : IMetaModelRepository
{
    private readonly IDbContextFactory<CJDSLDbContext> _factory;
    private readonly ILogger<SqliteMetaModelRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public SqliteMetaModelRepository(IDbContextFactory<CJDSLDbContext> factory, ILogger<SqliteMetaModelRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<M1_Object?> GetObjectAsync(string code, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.MetaObjects.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (entity == null) return null;
        return JsonSerializer.Deserialize<M1_Object>(entity.JsonContent, JsonOptions);
    }

    public async Task<M1_Object?> GetObjectByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.MetaObjects.ToListAsync(ct);
        foreach (var entity in entities)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<M1_Object>(entity.JsonContent, JsonOptions);
                if (obj?.Id == id) return obj;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize meta object: {Code}", entity.Code);
            }
        }
        return null;
    }

    public async Task<List<M1_Object>> GetAllObjectsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.MetaObjects.ToListAsync(ct);
        var result = new List<M1_Object>();

        foreach (var entity in entities)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<M1_Object>(entity.JsonContent, JsonOptions);
                if (obj != null) result.Add(obj);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize meta object: {Code}", entity.Code);
            }
        }

        return result;
    }

    public async Task<M1_Object> AddObjectAsync(M1_Object obj, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var json = JsonSerializer.Serialize(obj, JsonOptions);

        db.MetaObjects.Add(new MetaObjectEntity
        {
            Code = obj.Code,
            Name = obj.Name,
            JsonContent = json,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return obj;
    }

    public async Task<M1_Object> UpdateObjectAsync(M1_Object obj, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.MetaObjects.FirstOrDefaultAsync(x => x.Code == obj.Code, ct);
        var json = JsonSerializer.Serialize(obj, JsonOptions);

        if (existing != null)
        {
            existing.JsonContent = json;
            existing.Name = obj.Name;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.MetaObjects.Add(new MetaObjectEntity
            {
                Code = obj.Code,
                Name = obj.Name,
                JsonContent = json,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return obj;
    }

    public async Task<bool> DeleteObjectAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.MetaObjects.ToListAsync(ct);

        foreach (var entity in entities)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<M1_Object>(entity.JsonContent, JsonOptions);
                if (obj?.Id == id)
                {
                    db.MetaObjects.Remove(entity);
                    await db.SaveChangesAsync(ct);
                    return true;
                }
            }
            catch { }
        }

        return false;
    }

    public async Task<M0_Enum?> GetEnumAsync(string code, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.MetaEnums.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (entity == null) return null;
        return JsonSerializer.Deserialize<M0_Enum>(entity.JsonContent, JsonOptions);
    }

    public async Task<List<M0_Enum>> GetAllEnumsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.MetaEnums.ToListAsync(ct);
        var result = new List<M0_Enum>();

        foreach (var entity in entities)
        {
            try
            {
                var en = JsonSerializer.Deserialize<M0_Enum>(entity.JsonContent, JsonOptions);
                if (en != null) result.Add(en);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize meta enum: {Code}", entity.Code);
            }
        }

        return result;
    }

    public async Task<M0_Enum> AddEnumAsync(M0_Enum enumDef, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var json = JsonSerializer.Serialize(enumDef, JsonOptions);

        var existing = await db.MetaEnums.FirstOrDefaultAsync(x => x.Code == enumDef.Code, ct);
        if (existing != null)
        {
            existing.JsonContent = json;
            existing.Name = enumDef.Name;
        }
        else
        {
            db.MetaEnums.Add(new MetaEnumEntity
            {
                Code = enumDef.Code,
                Name = enumDef.Name,
                JsonContent = json,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return enumDef;
    }

    public async Task<M0_DataDictionary?> GetDictionaryAsync(string code, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.MetaDictionaries.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (entity == null) return null;
        return JsonSerializer.Deserialize<M0_DataDictionary>(entity.JsonContent, JsonOptions);
    }

    public async Task<List<M0_DataDictionary>> GetAllDictionariesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.MetaDictionaries.ToListAsync(ct);
        var result = new List<M0_DataDictionary>();

        foreach (var entity in entities)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<M0_DataDictionary>(entity.JsonContent, JsonOptions);
                if (dict != null) result.Add(dict);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize meta dictionary: {Code}", entity.Code);
            }
        }

        return result;
    }
}
