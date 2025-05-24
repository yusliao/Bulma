namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 缓存服务接口
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 获取缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 获取或设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">数据工厂方法</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除匹配模式的缓存
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置缓存过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取缓存剩余过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default);
} 