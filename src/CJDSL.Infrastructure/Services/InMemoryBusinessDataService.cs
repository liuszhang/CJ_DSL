using System.Collections.Concurrent;
using System.Text.Json;
using CJDSL.Domain.Interfaces;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// 内存版业务数据服务（开发/演示用，进程重启后数据丢失）
/// </summary>
public class InMemoryBusinessDataService : IBusinessDataService
{
    // key: objectCode -> (id -> record)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BusinessDataRecord>> _store = new();

    public Task<BusinessDataRecord> SaveAsync(string objectCode, string jsonData, CancellationToken ct = default)
        => Task.FromResult(Upsert(objectCode, jsonData, "draft"));

    public Task<BusinessDataRecord> SubmitAsync(string objectCode, string jsonData, CancellationToken ct = default)
        => Task.FromResult(Upsert(objectCode, jsonData, "submitted"));

    public Task<BusinessDataPage> ListAsync(string objectCode, int pageIndex = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var bucket = _store.GetValueOrDefault(objectCode);
        var all = bucket?.Values.OrderByDescending(r => r.CreatedAt).ToList() ?? new List<BusinessDataRecord>();
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 200);

        return Task.FromResult(new BusinessDataPage
        {
            Items = all.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(),
            Total = all.Count,
            PageIndex = pageIndex,
            PageSize = pageSize
        });
    }

    public Task<BusinessDataRecord?> GetAsync(string objectCode, string id, CancellationToken ct = default)
    {
        var record = _store.GetValueOrDefault(objectCode)?.GetValueOrDefault(id);
        return Task.FromResult(record);
    }

    public Task<bool> DeleteAsync(string objectCode, string id, CancellationToken ct = default)
    {
        var removed = _store.GetValueOrDefault(objectCode)?.TryRemove(id, out _) ?? false;
        return Task.FromResult(removed);
    }

    private BusinessDataRecord Upsert(string objectCode, string jsonData, string status)
    {
        var bucket = _store.GetOrAdd(objectCode, _ => new ConcurrentDictionary<string, BusinessDataRecord>());
        var id = BusinessDataJsonHelper.ExtractId(jsonData);

        if (id != null && bucket.TryGetValue(id, out var existing))
        {
            existing.JsonData = jsonData;
            existing.Status = status;
            existing.UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        var record = new BusinessDataRecord
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            ObjectCode = objectCode,
            Status = status,
            JsonData = jsonData,
            CreatedAt = DateTime.UtcNow
        };
        bucket[record.Id] = record;
        return record;
    }
}

/// <summary>
/// 业务数据 JSON 辅助方法
/// </summary>
public static class BusinessDataJsonHelper
{
    /// <summary>从 JSON 文本中提取 id 字段（支持 id / Id）</summary>
    public static string? ExtractId(string jsonData)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonData);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var name in new[] { "id", "Id" })
            {
                if (doc.RootElement.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var value = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }
            }
        }
        catch (JsonException)
        {
            // 非法 JSON 由上层校验，这里不抛出
        }
        return null;
    }
}
