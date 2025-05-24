using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 增强版事件总线服务 - 支持重试、死信队列、批量处理
/// </summary>
public class EnhancedEventBusService : IEventBus, IDisposable
{
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly IEventStore _eventStore;
    private readonly IEventHandlerRegistry _handlerRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnhancedEventBusService> _logger;
    private readonly IConfiguration _configuration;
    
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _subscriptions = new();
    private readonly ConcurrentQueue<FailedEvent> _retryQueue = new();
    private readonly Timer _retryTimer;
    private readonly Timer _metricsTimer;
    
    private readonly int _maxRetryAttempts;
    private readonly TimeSpan _retryInterval;
    private readonly TimeSpan _deadLetterRetention;
    private readonly int _batchSize;
    private bool _disposed = false;

    public EnhancedEventBusService(
        IConnectionMultiplexer redis,
        IEventStore eventStore,
        IEventHandlerRegistry handlerRegistry,
        IServiceProvider serviceProvider,
        ILogger<EnhancedEventBusService> logger,
        IConfiguration configuration)
    {
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _eventStore = eventStore;
        _handlerRegistry = handlerRegistry;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;

        // 从配置读取参数
        _maxRetryAttempts = _configuration.GetValue<int>("EventBus:MaxRetryAttempts", 3);
        _retryInterval = TimeSpan.FromSeconds(_configuration.GetValue<int>("EventBus:RetryIntervalSeconds", 30));
        _deadLetterRetention = TimeSpan.FromDays(_configuration.GetValue<int>("EventBus:DeadLetterRetentionDays", 7));
        _batchSize = _configuration.GetValue<int>("EventBus:BatchSize", 10);

        // 启动后台处理器
        _retryTimer = new Timer(ProcessRetryQueue, null, TimeSpan.FromSeconds(10), _retryInterval);
        _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

        _logger.LogInformation("增强版事件总线已启动，重试间隔: {RetryInterval}, 最大重试次数: {MaxRetries}, 批量大小: {BatchSize}",
            _retryInterval, _maxRetryAttempts, _batchSize);
    }

    /// <summary>
    /// 发布单个事件
    /// </summary>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        try
        {
            // 预处理事件
            await PreprocessEvent(@event);

            var eventType = typeof(T).Name;
            var channel = RedisChannel.Literal(GetChannelName(eventType));
            
            var eventJson = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // 并行处理：发布到Redis和保存到存储
            var publishTask = _subscriber.PublishAsync(channel, eventJson);
            var storeTask = _eventStore.SaveEventAsync(@event);
            var metricsTask = RecordEventMetrics(eventType, "published");

            await Task.WhenAll(publishTask, storeTask, metricsTask);

            _logger.LogInformation("事件发布成功: {EventType}, ID: {EventId}, 聚合ID: {AggregateId}, 订阅者: {SubscriberCount}", 
                eventType, @event.EventId, @event.AggregateId, publishTask.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布事件失败: {EventType}, ID: {EventId}", typeof(T).Name, @event.EventId);
            
            // 添加到死信队列
            await AddToDeadLetterQueue(@event, ex);
            throw;
        }
    }

    /// <summary>
    /// 批量发布事件
    /// </summary>
    public async Task PublishManyAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        var eventList = events.ToList();
        if (!eventList.Any()) return;

        var batches = eventList.Chunk(_batchSize);
        var tasks = new List<Task>();

        foreach (var batch in batches)
        {
            tasks.Add(ProcessEventBatch(batch, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("批量发布事件完成: {EventType}, 总数: {Count}, 批次数: {BatchCount}", 
                typeof(T).Name, eventList.Count, tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量发布事件失败: {EventType}, 总数: {Count}", typeof(T).Name, eventList.Count);
            throw;
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    public async Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler) where T : BaseEvent
    {
        var eventType = typeof(T).Name;
        var channel = RedisChannel.Literal(GetChannelName(eventType));
        var cancellationTokenSource = new CancellationTokenSource();
        
        _subscriptions[eventType] = cancellationTokenSource;

        await _subscriber.SubscribeAsync(channel, async (redisChannel, message) =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                var @event = JsonSerializer.Deserialize<T>(message!, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (@event != null)
                {
                    // 并行处理：自定义处理器和注册处理器
                    var handlerTask = ExecuteHandlerSafely(() => handler(@event, cancellationTokenSource.Token), eventType, @event.EventId);
                    var registeredHandlersTask = ExecuteRegisteredHandlers(@event, cancellationTokenSource.Token);

                    await Task.WhenAll(handlerTask, registeredHandlersTask);

                    // 记录处理指标
                    stopwatch.Stop();
                    await RecordProcessingMetrics(eventType, stopwatch.ElapsedMilliseconds, true);

                    _logger.LogDebug("事件处理完成: {EventType}, ID: {EventId}, 耗时: {ElapsedMs}ms", 
                        eventType, @event.EventId, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await RecordProcessingMetrics(eventType, stopwatch.ElapsedMilliseconds, false);
                
                _logger.LogError(ex, "处理事件失败: {EventType}, 消息: {Message}", eventType, message);
                
                // 添加到重试队列
                await AddToRetryQueue(eventType, message!, ex);
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

        if (_subscriptions.TryRemove(eventType, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        await _subscriber.UnsubscribeAsync(channel);
        _logger.LogInformation("已取消订阅事件: {EventType}, 频道: {Channel}", eventType, channel);
    }

    /// <summary>
    /// 预处理事件
    /// </summary>
    private async Task PreprocessEvent<T>(T @event) where T : BaseEvent
    {
        // 添加元数据
        @event.Metadata["PublishedAt"] = DateTimeOffset.UtcNow;
        @event.Metadata["MachineName"] = Environment.MachineName;
        @event.Metadata["ProcessId"] = Environment.ProcessId;

        // 验证事件数据
        if (string.IsNullOrEmpty(@event.AggregateId))
        {
            _logger.LogWarning("事件缺少聚合ID: {EventType}, ID: {EventId}", typeof(T).Name, @event.EventId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理事件批次
    /// </summary>
    private async Task ProcessEventBatch<T>(IEnumerable<T> batch, CancellationToken cancellationToken) where T : BaseEvent
    {
        var batchList = batch.ToList();
        var tasks = batchList.Select(e => PublishAsync(e, cancellationToken));
        
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批次处理失败，批次大小: {BatchSize}", batchList.Count);
            throw;
        }
    }

    /// <summary>
    /// 执行注册的事件处理器
    /// </summary>
    private async Task ExecuteRegisteredHandlers<T>(T @event, CancellationToken cancellationToken) where T : BaseEvent
    {
        var handlers = _handlerRegistry.GetHandlers<T>();
        var handlerTasks = handlers.Select(handler => 
            ExecuteHandlerSafely(() => handler.HandleAsync(@event, cancellationToken), typeof(T).Name, @event.EventId));

        if (handlerTasks.Any())
        {
            await Task.WhenAll(handlerTasks);
        }
    }

    /// <summary>
    /// 安全执行事件处理器
    /// </summary>
    private async Task ExecuteHandlerSafely(Func<Task> handlerExecution, string eventType, Guid eventId)
    {
        try
        {
            await handlerExecution();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理器执行失败: {EventType}, EventId: {EventId}", eventType, eventId);
            // 不重新抛出异常，防止影响其他处理器
        }
    }

    /// <summary>
    /// 添加到重试队列
    /// </summary>
    private async Task AddToRetryQueue(string eventType, string message, Exception exception)
    {
        var failedEvent = new FailedEvent
        {
            EventType = eventType,
            Message = message,
            Exception = exception.ToString(),
            FailedAt = DateTimeOffset.UtcNow,
            RetryCount = 0,
            NextRetryAt = DateTimeOffset.UtcNow.Add(_retryInterval)
        };

        _retryQueue.Enqueue(failedEvent);
        await RecordEventMetrics(eventType, "failed");
        
        _logger.LogWarning("事件已添加到重试队列: {EventType}, 重试时间: {NextRetryAt}", 
            eventType, failedEvent.NextRetryAt);
    }

    /// <summary>
    /// 添加到死信队列
    /// </summary>
    private async Task AddToDeadLetterQueue<T>(T @event, Exception exception) where T : BaseEvent
    {
        var deadLetterEvent = new
        {
            EventId = @event.EventId,
            EventType = @event.EventType,
            AggregateId = @event.AggregateId,
            EventData = JsonSerializer.Serialize(@event),
            Exception = exception.ToString(),
            DeadAt = DateTimeOffset.UtcNow,
            OriginalOccurredOn = @event.OccurredOn
        };

        var deadLetterJson = JsonSerializer.Serialize(deadLetterEvent);
        var deadLetterKey = $"events:deadletter:{@event.EventType}";
        
        await _database.ListLeftPushAsync(deadLetterKey, deadLetterJson);
        await _database.KeyExpireAsync(deadLetterKey, _deadLetterRetention);
        
        await RecordEventMetrics(@event.EventType, "deadletter");
        
        _logger.LogError("事件已添加到死信队列: {EventType}, ID: {EventId}", @event.EventType, @event.EventId);
    }

    /// <summary>
    /// 处理重试队列
    /// </summary>
    private async void ProcessRetryQueue(object? state)
    {
        var processedCount = 0;
        var retryList = new List<FailedEvent>();

        // 收集需要重试的事件
        while (_retryQueue.TryDequeue(out var failedEvent) && processedCount < _batchSize)
        {
            if (DateTimeOffset.UtcNow >= failedEvent.NextRetryAt)
            {
                retryList.Add(failedEvent);
                processedCount++;
            }
            else
            {
                // 重新入队等待
                _retryQueue.Enqueue(failedEvent);
                break;
            }
        }

        // 处理重试
        foreach (var failedEvent in retryList)
        {
            try
            {
                if (failedEvent.RetryCount >= _maxRetryAttempts)
                {
                    // 超过最大重试次数，移到死信队列
                    await MoveToDeadLetterQueue(failedEvent);
                    continue;
                }

                // 重新发布事件
                var channel = RedisChannel.Literal(GetChannelName(failedEvent.EventType));
                await _subscriber.PublishAsync(channel, failedEvent.Message);

                await RecordEventMetrics(failedEvent.EventType, "retried");
                
                _logger.LogInformation("事件重试成功: {EventType}, 重试次数: {RetryCount}", 
                    failedEvent.EventType, failedEvent.RetryCount + 1);
            }
            catch (Exception ex)
            {
                // 重试失败，增加重试次数后重新入队
                failedEvent.RetryCount++;
                failedEvent.NextRetryAt = DateTimeOffset.UtcNow.Add(_retryInterval);
                failedEvent.Exception = ex.ToString();
                
                _retryQueue.Enqueue(failedEvent);
                
                _logger.LogWarning(ex, "事件重试失败: {EventType}, 重试次数: {RetryCount}", 
                    failedEvent.EventType, failedEvent.RetryCount);
            }
        }

        if (processedCount > 0)
        {
            _logger.LogDebug("重试队列处理完成，处理数量: {ProcessedCount}, 队列剩余: {QueueCount}", 
                processedCount, _retryQueue.Count);
        }
    }

    /// <summary>
    /// 移动到死信队列
    /// </summary>
    private async Task MoveToDeadLetterQueue(FailedEvent failedEvent)
    {
        var deadLetterData = new
        {
            failedEvent.EventType,
            failedEvent.Message,
            failedEvent.Exception,
            failedEvent.FailedAt,
            failedEvent.RetryCount,
            DeadAt = DateTimeOffset.UtcNow,
            Reason = "超过最大重试次数"
        };

        var deadLetterJson = JsonSerializer.Serialize(deadLetterData);
        var deadLetterKey = $"events:deadletter:{failedEvent.EventType}";
        
        await _database.ListLeftPushAsync(deadLetterKey, deadLetterJson);
        await _database.KeyExpireAsync(deadLetterKey, _deadLetterRetention);
        
        await RecordEventMetrics(failedEvent.EventType, "deadletter");
        
        _logger.LogError("事件已移至死信队列: {EventType}, 重试次数: {RetryCount}", 
            failedEvent.EventType, failedEvent.RetryCount);
    }

    /// <summary>
    /// 记录事件指标
    /// </summary>
    private async Task RecordEventMetrics(string eventType, string action)
    {
        var key = $"metrics:events:{eventType}:{action}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        await Task.WhenAll(
            _database.StringIncrementAsync($"{key}:count"),
            _database.SortedSetAddAsync($"{key}:timeline", timestamp, timestamp),
            _database.KeyExpireAsync($"{key}:count", TimeSpan.FromDays(30)),
            _database.KeyExpireAsync($"{key}:timeline", TimeSpan.FromDays(30))
        );
    }

    /// <summary>
    /// 记录处理指标
    /// </summary>
    private async Task RecordProcessingMetrics(string eventType, long elapsedMs, bool success)
    {
        var statusKey = success ? "success" : "error";
        var key = $"metrics:processing:{eventType}:{statusKey}";
        
        await Task.WhenAll(
            _database.StringIncrementAsync($"{key}:count"),
            _database.ListLeftPushAsync($"{key}:duration", elapsedMs),
            _database.ListTrimAsync($"{key}:duration", 0, 999), // 保留最近1000次
            _database.KeyExpireAsync($"{key}:count", TimeSpan.FromDays(7)),
            _database.KeyExpireAsync($"{key}:duration", TimeSpan.FromDays(7))
        );
    }

    /// <summary>
    /// 收集指标
    /// </summary>
    private async void CollectMetrics(object? state)
    {
        try
        {
            var metrics = new
            {
                Timestamp = DateTimeOffset.UtcNow,
                RetryQueueSize = _retryQueue.Count,
                ActiveSubscriptions = _subscriptions.Count,
                SystemInfo = new
                {
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    WorkingSet = GC.GetTotalMemory(false)
                }
            };

            var metricsJson = JsonSerializer.Serialize(metrics);
            await _database.ListLeftPushAsync("metrics:eventbus:system", metricsJson);
            await _database.ListTrimAsync("metrics:eventbus:system", 0, 1439); // 保留24小时数据(每5分钟一次)
            
            _logger.LogDebug("系统指标已收集: 重试队列 {RetryQueueSize}, 活跃订阅 {ActiveSubscriptions}", 
                _retryQueue.Count, _subscriptions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集系统指标失败");
        }
    }

    /// <summary>
    /// 获取频道名称
    /// </summary>
    private static string GetChannelName(string eventType)
    {
        return $"events:{eventType.ToLowerInvariant()}";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _retryTimer?.Dispose();
            _metricsTimer?.Dispose();
            
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Cancel();
                subscription.Dispose();
            }
            
            _subscriptions.Clear();
            _disposed = true;
            
            _logger.LogInformation("增强版事件总线已释放资源");
        }
    }
}

/// <summary>
/// 失败事件信息
/// </summary>
public class FailedEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Exception { get; set; } = string.Empty;
    public DateTimeOffset FailedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset NextRetryAt { get; set; }
} 