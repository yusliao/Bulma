using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 内存缓存服务（Redis不可用时的备选方案）
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var value) && value is T result)
            {
                _logger.LogDebug("从内存缓存获取数据成功: {Key}", key);
                return Task.FromResult<T?>(result);
            }
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从内存缓存获取数据失败: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }

            _memoryCache.Set(key, value, options);
            _logger.LogDebug("设置内存缓存成功: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置内存缓存失败: {Key}", key);
        }
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var data = await factory();
        await SetAsync(key, data, expiration, cancellationToken);
        return data;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("删除内存缓存成功: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除内存缓存失败: {Key}", key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("内存缓存不支持按模式删除，模式: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("内存缓存不支持动态设置过期时间，键: {Key}", key);
        return Task.FromResult(false);
    }

    public Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("内存缓存不支持获取剩余时间，键: {Key}", key);
        return Task.FromResult<TimeSpan?>(null);
    }
} 