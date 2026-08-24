using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;

namespace CJDSL.Generation.Caching;

/// <summary>
/// 内存缓存实现
/// </summary>
public class InMemoryDslCache : IDslCache
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly object _lock = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
            {
                return Task.FromResult<T?>(entry.Value as T);
            }
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        lock (_lock)
        {
            _cache[key] = new CacheEntry
            {
                Value = value,
                Expiry = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(10))
            };
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _cache.Remove(key);
        }
        return Task.CompletedTask;
    }

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime Expiry { get; set; }
    }
}
