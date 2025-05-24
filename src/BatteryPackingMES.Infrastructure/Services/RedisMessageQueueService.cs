using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Concurrent;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// Redis消息队列服务
/// </summary>
public class RedisMessageQueueService : IMessageQueueService, IDisposable
{
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisMessageQueueService> _logger;
    private readonly string _keyPrefix;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _subscriptions = new();
    private bool _disposed = false;

    public RedisMessageQueueService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisMessageQueueService> logger)
    {
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _logger = logger;
        _keyPrefix = configuration["Redis:KeyPrefix"] ?? "BatteryMES:";
    }

    /// <summary>
    /// 发布消息到指定频道
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="message">消息内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullChannel = RedisChannel.Literal(GetFullChannelName(channel));
            var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var subscribersCount = await _subscriber.PublishAsync(fullChannel, jsonMessage);
            
            _logger.LogInformation(
                "消息已发布到频道 {Channel}，订阅者数量: {SubscribersCount}，消息内容: {Message}",
                fullChannel, subscribersCount, jsonMessage);

            // 同时将消息存储到列表中，以便后续处理
            var listKey = GetMessageListKey(channel);
            await _database.ListLeftPushAsync(listKey, jsonMessage);
            
            // 保留最近1000条消息
            await _database.ListTrimAsync(listKey, 0, 999);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布消息到频道 {Channel} 失败: {Message}", channel, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="handler">消息处理器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullChannel = RedisChannel.Literal(GetFullChannelName(channel));
            var tcs = new TaskCompletionSource<bool>();
            _subscriptions[GetFullChannelName(channel)] = tcs;

            await _subscriber.SubscribeAsync(fullChannel, async (redisChannel, message) =>
            {
                try
                {
                    if (!message.HasValue)
                    {
                        _logger.LogWarning("收到空消息，频道: {Channel}", fullChannel);
                        return;
                    }

                    var messageObject = JsonSerializer.Deserialize<T>(message!, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (messageObject != null)
                    {
                        await handler(messageObject);
                        _logger.LogInformation("成功处理频道 {Channel} 的消息", fullChannel);
                    }
                    else
                    {
                        _logger.LogWarning("无法反序列化消息，频道: {Channel}，内容: {Message}", fullChannel, message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理频道 {Channel} 消息时发生错误: {Message}", fullChannel, ex.Message);
                }
            });

            _logger.LogInformation("成功订阅频道: {Channel}", fullChannel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅频道 {Channel} 失败: {Message}", channel, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <returns></returns>
    public async Task UnsubscribeAsync(string channel)
    {
        try
        {
            var fullChannel = RedisChannel.Literal(GetFullChannelName(channel));
            await _subscriber.UnsubscribeAsync(fullChannel);
            
            if (_subscriptions.TryRemove(GetFullChannelName(channel), out var tcs))
            {
                tcs.SetResult(true);
            }

            _logger.LogInformation("已取消订阅频道: {Channel}", fullChannel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅频道 {Channel} 失败: {Message}", channel, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 取消所有订阅
    /// </summary>
    /// <returns></returns>
    public async Task UnsubscribeAllAsync()
    {
        try
        {
            await _subscriber.UnsubscribeAllAsync();
            
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.SetResult(true);
            }
            _subscriptions.Clear();

            _logger.LogInformation("已取消所有订阅");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消所有订阅失败: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 获取历史消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="count">获取数量</param>
    /// <returns></returns>
    public async Task<List<T>> GetHistoryMessagesAsync<T>(string channel, int count = 100) where T : class
    {
        try
        {
            var listKey = GetMessageListKey(channel);
            var messages = await _database.ListRangeAsync(listKey, 0, count - 1);
            
            var result = new List<T>();
            foreach (var message in messages)
            {
                if (!message.HasValue) continue;
                
                try
                {
                    var messageObject = JsonSerializer.Deserialize<T>(message!, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    if (messageObject != null)
                    {
                        result.Add(messageObject);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "反序列化历史消息失败: {Message}", message);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取频道 {Channel} 历史消息失败: {Message}", channel, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 获取完整频道名称
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <returns></returns>
    private string GetFullChannelName(string channel)
    {
        return $"{_keyPrefix}Channel:{channel}";
    }

    /// <summary>
    /// 获取消息列表键名
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <returns></returns>
    private string GetMessageListKey(string channel)
    {
        return $"{_keyPrefix}Messages:{channel}";
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            UnsubscribeAllAsync().Wait();
            _disposed = true;
        }
    }
} 