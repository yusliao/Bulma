using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// Redis缓存服务
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _keyPrefix;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
        _logger = logger;
        _keyPrefix = configuration["Redis:KeyPrefix"] ?? "BatteryMES:";
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// 获取缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var value = await _database.StringGetAsync(fullKey);
            
            if (!value.HasValue)
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            _logger.LogDebug("从缓存获取数据成功: {Key}", fullKey);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从缓存获取数据失败: {Key}, 错误: {Message}", key, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var success = await _database.StringSetAsync(fullKey, jsonValue, expiration);
            
            if (success)
            {
                _logger.LogDebug("设置缓存成功: {Key}, 过期时间: {Expiration}", fullKey, expiration);
            }
            else
            {
                _logger.LogWarning("设置缓存失败: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存失败: {Key}, 错误: {Message}", key, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 获取或设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">数据工厂方法</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // 先尝试从缓存获取
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // 缓存不存在，从工厂方法获取数据
            var data = await factory();
            
            // 设置到缓存
            await SetAsync(key, data, expiration, cancellationToken);
            
            _logger.LogDebug("从工厂方法获取数据并缓存: {Key}", key);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取或设置缓存失败: {Key}, 错误: {Message}", key, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var success = await _database.KeyDeleteAsync(fullKey);
            
            if (success)
            {
                _logger.LogDebug("删除缓存成功: {Key}", fullKey);
            }
            else
            {
                _logger.LogDebug("缓存不存在或删除失败: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除缓存失败: {Key}, 错误: {Message}", key, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 删除匹配模式的缓存
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPattern = GetFullKey(pattern);
            var keys = _server.Keys(pattern: fullPattern).ToArray();
            
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogInformation("删除匹配模式的缓存成功: {Pattern}, 删除数量: {Count}", fullPattern, keys.Length);
            }
            else
            {
                _logger.LogDebug("没有找到匹配模式的缓存: {Pattern}", fullPattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除匹配模式的缓存失败: {Pattern}, 错误: {Message}", pattern, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var exists = await _database.KeyExistsAsync(fullKey);
            _logger.LogDebug("检查缓存存在性: {Key}, 结果: {Exists}", fullKey, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存存在性失败: {Key}, 错误: {Message}", key, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 设置缓存过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var success = await _database.KeyExpireAsync(fullKey, expiration);
            _logger.LogDebug("设置缓存过期时间: {Key}, 过期时间: {Expiration}, 结果: {Success}", fullKey, expiration, success);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存过期时间失败: {Key}, 错误: {Message}", key, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 获取缓存剩余过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var ttl = await _database.KeyTimeToLiveAsync(fullKey);
            _logger.LogDebug("获取缓存剩余时间: {Key}, TTL: {TTL}", fullKey, ttl);
            return ttl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存剩余时间失败: {Key}, 错误: {Message}", key, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 获取完整缓存键
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns></returns>
    private string GetFullKey(string key)
    {
        return $"{_keyPrefix}Cache:{key}";
    }
} 