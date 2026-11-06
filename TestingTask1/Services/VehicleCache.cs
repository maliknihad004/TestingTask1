
using Microsoft.Extensions.Caching.Memory;
using System.Threading;

namespace ShowroomTask.Services;

public class VehicleCache
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _slidingExpiration;
    private readonly TimeSpan _absoluteExpiration;
    private int _version = 0;

    public VehicleCache(IMemoryCache cache)
        : this(cache, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5))
    {
    }

    public VehicleCache(
        IMemoryCache cache,
        TimeSpan slidingExpiration,
        TimeSpan absoluteExpiration)
    {
        _cache = cache;
        _slidingExpiration = slidingExpiration;
        _absoluteExpiration = absoluteExpiration;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> load)
    {
        int version = Volatile.Read(ref _version);
        string cacheKey = $"vehicle:{version}:{key}";

        if (_cache.TryGetValue(cacheKey, out T? result))
            return result!;

        T value = await load();

        _cache.Set(cacheKey, value, new MemoryCacheEntryOptions
        {
            SlidingExpiration = _slidingExpiration,
            AbsoluteExpirationRelativeToNow = _absoluteExpiration
        });

        return value;
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
    }
}
