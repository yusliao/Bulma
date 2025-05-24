using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Concurrent;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 基于Redis的事件总线实现
/// </summary>
public class EventBusService : IEventBus, IDisposable
{
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly IEventStore _eventStore;
    private readonly IEventHandlerRegistry _handlerRegistry;
    private readonly ILogger<EventBusService> _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _subscriptions = new();
    private bool _disposed = false;

    public EventBusService(
        IConnectionMultiplexer redis,
        IEventStore eventStore,
        IEventHandlerRegistry handlerRegistry,
        ILogger<EventBusService> logger)
    {
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _eventStore = eventStore;
        _handlerRegistry = handlerRegistry;
        _logger = logger;
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        try
        {
            var eventType = typeof(T).Name;
            var channel = RedisChannel.Literal(GetChannelName(eventType));
            
            var eventJson = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // 发布到Redis频道
            await _subscriber.PublishAsync(channel, eventJson);

            // 保存到事件存储
            await _eventStore.SaveEventAsync(@event);

            // 记录发布的事件统计
            await RecordEventMetrics(eventType, "published");

            _logger.LogInformation("事件已发布: {EventType}, ID: {EventId}, 聚合ID: {AggregateId}", 
                eventType, @event.EventId, @event.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布事件失败: {EventType}, ID: {EventId}", typeof(T).Name, @event.EventId);
            throw;
        }
    }

    /// <summary>
    /// 发布多个事件
    /// </summary>
    public async Task PublishManyAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        var eventList = events.ToList();
        if (!eventList.Any()) return;

        var tasks = eventList.Select(e => PublishAsync(e, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogInformation("批量发布事件完成: {EventType}, 数量: {Count}", typeof(T).Name, eventList.Count);
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    public async Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler) where T : BaseEvent
    {
        var eventType = typeof(T).Name;
        var channel = RedisChannel.Literal(GetChannelName(eventType));

        await _subscriber.SubscribeAsync(channel, async (redisChannel, message) =>
        {
            try
            {
                var @event = JsonSerializer.Deserialize<T>(message!, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (@event != null)
                {
                    // 调用自定义处理器
                    await handler(@event, CancellationToken.None);

                    // 调用注册的处理器
                    var handlers = _handlerRegistry.GetHandlers<T>();
                    foreach (var registeredHandler in handlers)
                    {
                        await registeredHandler.HandleAsync(@event);
                    }

                    // 记录处理的事件统计
                    await RecordEventMetrics(eventType, "processed");

                    _logger.LogInformation("事件已处理: {EventType}, ID: {EventId}", eventType, @event.EventId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理事件失败: {EventType}, 消息: {Message}", eventType, message);
                
                // 可以在这里实现重试机制或死信队列
                await HandleFailedEvent(eventType, message!, ex);
            }
        });

        _logger.LogInformation("已订阅事件: {EventType}, 频道: {Channel}", eventType, channel);
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public async Task UnsubscribeAsync<T>() where T : BaseEvent
    {
        var eventType = typeof(T).Name;
        var channel = RedisChannel.Literal(GetChannelName(eventType));

        await _subscriber.UnsubscribeAsync(channel);
        _logger.LogInformation("已取消订阅事件: {EventType}, 频道: {Channel}", eventType, channel);
    }

    /// <summary>
    /// 获取频道名称
    /// </summary>
    private static string GetChannelName(string eventType)
    {
        return $"events:{eventType.ToLowerInvariant()}";
    }

    /// <summary>
    /// 记录事件指标
    /// </summary>
    private async Task RecordEventMetrics(string eventType, string action)
    {
        var key = $"metrics:events:{eventType}:{action}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // 增加计数器
        await _database.StringIncrementAsync($"{key}:count");
        
        // 记录时间戳
        await _database.SortedSetAddAsync($"{key}:timeline", timestamp, timestamp);
        
        // 设置过期时间为30天
        await _database.KeyExpireAsync($"{key}:count", TimeSpan.FromDays(30));
        await _database.KeyExpireAsync($"{key}:timeline", TimeSpan.FromDays(30));
    }

    /// <summary>
    /// 处理失败的事件
    /// </summary>
    private async Task HandleFailedEvent(string eventType, string message, Exception exception)
    {
        var failedEvent = new
        {
            EventType = eventType,
            Message = message,
            Exception = exception.ToString(),
            FailedAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        var failedEventJson = JsonSerializer.Serialize(failedEvent);
        var deadLetterKey = $"events:failed:{eventType}";
        
        // 存储到死信队列
        await _database.ListLeftPushAsync(deadLetterKey, failedEventJson);
        
        // 限制死信队列长度
        await _database.ListTrimAsync(deadLetterKey, 0, 999);
        
        _logger.LogWarning("事件已添加到死信队列: {EventType}", eventType);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// 事件处理器注册服务
/// </summary>
public class EventHandlerRegistry : IEventHandlerRegistry
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    private readonly ILogger<EventHandlerRegistry> _logger;

    public EventHandlerRegistry(ILogger<EventHandlerRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterHandler<T>(IEventHandler<T> handler) where T : BaseEvent
    {
        var eventType = typeof(T);
        _handlers.AddOrUpdate(eventType, 
            _ => new List<object> { handler },
            (_, existing) => 
            {
                existing.Add(handler);
                return existing;
            });

        _logger.LogInformation("已注册事件处理器: {EventType} -> {HandlerType}", 
            eventType.Name, handler.GetType().Name);
    }

    public IEnumerable<IEventHandler<T>> GetHandlers<T>() where T : BaseEvent
    {
        var eventType = typeof(T);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            return handlers.Cast<IEventHandler<T>>();
        }
        return Enumerable.Empty<IEventHandler<T>>();
    }

    public IEnumerable<object> GetAllHandlers(Type eventType)
    {
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            return handlers;
        }
        return Enumerable.Empty<object>();
    }
} 