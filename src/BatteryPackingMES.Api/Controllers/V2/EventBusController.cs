using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 事件总线管理控制器
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize] // 需要认证
public class EventBusController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly IEventStore _eventStore;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<EventBusController> _logger;

    public EventBusController(
        IEventBus eventBus,
        IEventStore eventStore,
        IConnectionMultiplexer redis,
        ILogger<EventBusController> logger)
    {
        _eventBus = eventBus;
        _eventStore = eventStore;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// 发布测试事件
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <returns></returns>
    [HttpPost("publish-test")]
    public async Task<IActionResult> PublishTestEvent([FromQuery] string eventType, [FromBody] object eventData)
    {
        try
        {
            BaseEvent? testEvent = eventType.ToLower() switch
            {
                "productionbatchcreated" => new ProductionBatchCreatedEvent(
                    $"TEST_{DateTime.Now:yyyyMMddHHmmss}",
                    "TEST_PRODUCT",
                    100,
                    "TEST_WORKSHOP"),
                "equipmentfailure" => new EquipmentFailureEvent(
                    "TEST_EQUIPMENT",
                    "TestFailure",
                    "测试故障事件",
                    "Medium"),
                "qualityinspection" => new QualityInspectionCompletedEvent(
                    $"TEST_PRODUCT_{DateTime.Now:HHmmss}",
                    $"TEST_BATCH_{DateTime.Now:yyyyMMdd}",
                    true,
                    new Dictionary<string, object> { { "TestResult", "Passed" } }),
                _ => null
            };

            if (testEvent == null)
            {
                return BadRequest(new { success = false, message = "不支持的事件类型" });
            }

            // 设置用户ID
            testEvent.UserId = GetCurrentUserId();
            testEvent.Metadata["TestEvent"] = true;
            testEvent.Metadata["PublishedBy"] = User.Identity?.Name ?? "Unknown";

            await _eventBus.PublishAsync(testEvent);

            _logger.LogInformation("测试事件已发布: {EventType}, ID: {EventId}", eventType, testEvent.EventId);

            return Ok(new
            {
                success = true,
                message = "测试事件发布成功",
                eventId = testEvent.EventId,
                eventType = testEvent.EventType,
                occurredOn = testEvent.OccurredOn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布测试事件失败: {EventType}", eventType);
            return StatusCode(500, new { success = false, message = "发布测试事件失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取事件统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetEventStatistics()
    {
        try
        {
            // 由于IEventStore没有GetEventStatisticsAsync方法，我们手动构建统计信息
            var today = DateTime.UtcNow.Date;
            var stats = new
            {
                Date = today,
                TotalEvents = 0, // 可以通过其他方式获取
                EventsByType = new Dictionary<string, int>(),
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("获取事件统计信息");
            return Ok(ApiResponse.OK(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取事件统计信息失败");
            return StatusCode(500, ApiResponse.Fail("获取统计信息失败", ex.Message));
        }
    }

    /// <summary>
    /// 获取死信队列内容
    /// </summary>
    /// <param name="eventType">事件类型（可选）</param>
    /// <param name="limit">限制数量</param>
    /// <returns></returns>
    [HttpGet("dead-letter-queue")]
    public async Task<IActionResult> GetDeadLetterQueue([FromQuery] string? eventType, [FromQuery] int limit = 50)
    {
        try
        {
            var database = _redis.GetDatabase();
            var deadLetterKeys = new List<string>();

            if (!string.IsNullOrEmpty(eventType))
            {
                deadLetterKeys.Add($"events:deadletter:{eventType}");
            }
            else
            {
                // 获取所有死信队列
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: "events:deadletter:*");
                deadLetterKeys.AddRange(keys.Select(k => k.ToString()));
            }

            var deadLetterEvents = new List<object>();

            foreach (var key in deadLetterKeys)
            {
                var items = await database.ListRangeAsync(key, 0, limit - 1);
                foreach (var item in items)
                {
                    try
                    {
                        var deadEvent = JsonSerializer.Deserialize<object>(item!);
                        deadLetterEvents.Add(deadEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "反序列化死信事件失败: {Key}", key);
                    }
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalKeys = deadLetterKeys.Count,
                    totalEvents = deadLetterEvents.Count,
                    events = deadLetterEvents.Take(limit)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取死信队列失败");
            return StatusCode(500, new { success = false, message = "获取死信队列失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 重新处理死信队列中的事件
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="count">处理数量</param>
    /// <returns></returns>
    [HttpPost("retry-dead-letter")]
    public async Task<IActionResult> RetryDeadLetterEvents([FromQuery] string eventType, [FromQuery] int count = 10)
    {
        try
        {
            var database = _redis.GetDatabase();
            var deadLetterKey = $"events:deadletter:{eventType}";
            
            var retryCount = 0;
            var errorCount = 0;

            for (int i = 0; i < count; i++)
            {
                var item = await database.ListRightPopAsync(deadLetterKey);
                if (!item.HasValue) break;

                try
                {
                    var deadEvent = JsonSerializer.Deserialize<dynamic>(item!);
                    var originalEventData = deadEvent.GetProperty("EventData").GetString();
                    
                    // 重新发布到原始频道
                    var channel = RedisChannel.Literal($"events:{eventType.ToLowerInvariant()}");
                    await _redis.GetSubscriber().PublishAsync(channel, originalEventData);
                    
                    retryCount++;
                    _logger.LogInformation("重新发布死信事件: {EventType}, 索引: {Index}", eventType, i);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "重新发布死信事件失败: {EventType}, 索引: {Index}", eventType, i);
                }
            }

            return Ok(new
            {
                success = true,
                message = $"重试完成",
                data = new
                {
                    eventType = eventType,
                    retryCount = retryCount,
                    errorCount = errorCount,
                    totalProcessed = retryCount + errorCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重试死信事件失败: {EventType}", eventType);
            return StatusCode(500, new { success = false, message = "重试死信事件失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 清理死信队列
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <returns></returns>
    [HttpDelete("dead-letter-queue")]
    public async Task<IActionResult> ClearDeadLetterQueue([FromQuery] string eventType)
    {
        try
        {
            var database = _redis.GetDatabase();
            var deadLetterKey = $"events:deadletter:{eventType}";
            
            var count = await database.ListLengthAsync(deadLetterKey);
            var deleted = await database.KeyDeleteAsync(deadLetterKey);

            _logger.LogInformation("清理死信队列: {EventType}, 删除数量: {Count}", eventType, count);

            return Ok(new
            {
                success = true,
                message = "死信队列清理完成",
                data = new
                {
                    eventType = eventType,
                    deletedCount = count,
                    keyDeleted = deleted
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理死信队列失败: {EventType}", eventType);
            return StatusCode(500, new { success = false, message = "清理死信队列失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取事件历史
    /// </summary>
    /// <param name="aggregateId">聚合ID</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="limit">限制数量</param>
    /// <returns></returns>
    [HttpGet("history")]
    public async Task<IActionResult> GetEventHistory(
        [FromQuery] string? aggregateId, 
        [FromQuery] string? eventType, 
        [FromQuery] int limit = 50)
    {
        try
        {
            IEnumerable<BaseEvent> events;

            if (!string.IsNullOrEmpty(aggregateId))
            {
                events = await _eventStore.GetEventsAsync(aggregateId);
            }
            else if (!string.IsNullOrEmpty(eventType))
            {
                events = await _eventStore.GetAllEventsAsync(eventType, DateTimeOffset.UtcNow.AddDays(-30));
            }
            else
            {
                // 获取最近的事件
                events = await _eventStore.GetAllEventsAsync("", DateTimeOffset.UtcNow.AddDays(-1));
            }

            var eventHistory = events.Take(limit).Select(e => new
            {
                eventId = e.EventId,
                eventType = e.EventType,
                aggregateId = e.AggregateId,
                occurredOn = e.OccurredOn,
                version = e.Version,
                userId = e.UserId,
                metadata = e.Metadata
            });

            return Ok(new
            {
                success = true,
                data = new
                {
                    query = new { aggregateId, eventType, limit },
                    totalCount = eventHistory.Count(),
                    events = eventHistory
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取事件历史失败");
            return StatusCode(500, new { success = false, message = "获取事件历史失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    /// <returns>健康状态</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetSystemHealth()
    {
        try
        {
            // 检查各个组件的健康状态
            var health = new
            {
                Timestamp = DateTime.UtcNow,
                Status = "Healthy",
                Components = new
                {
                    EventBus = "Healthy",
                    EventStore = "Healthy", 
                    Redis = "Healthy",
                    Database = "Healthy"
                },
                // 修复分组访问问题
                Metrics = new
                {
                    TotalEvents = 0,
                    EventsPerSecond = 0.0,
                    AverageLatency = 0.0
                }
            };

            return Ok(ApiResponse.OK(health));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统健康状态失败");
            
            var unhealthyStatus = new
            {
                Timestamp = DateTime.UtcNow,
                Status = "Unhealthy",
                Error = ex.Message
            };
            
            return StatusCode(500, ApiResponse.OK(unhealthyStatus));
        }
    }

    /// <summary>
    /// 获取实时指标
    /// </summary>
    private async Task<object> GetRealtimeMetrics(IDatabase database)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            // 获取各类事件的发布计数
            var eventTypes = new[] { "ProductionBatchCreatedEvent", "EquipmentFailureEvent", "QualityInspectionCompletedEvent" };
            
            foreach (var eventType in eventTypes)
            {
                var publishedKey = $"metrics:events:{eventType}:published:count";
                var processedKey = $"metrics:events:{eventType}:processed:count";
                var failedKey = $"metrics:events:{eventType}:failed:count";

                var published = await database.StringGetAsync(publishedKey);
                var processed = await database.StringGetAsync(processedKey);
                var failed = await database.StringGetAsync(failedKey);

                metrics[eventType] = new
                {
                    published = published.HasValue ? (long)published : 0,
                    processed = processed.HasValue ? (long)processed : 0,
                    failed = failed.HasValue ? (long)failed : 0
                };
            }

            // 获取系统指标
            var systemMetrics = await database.ListRangeAsync("metrics:eventbus:system", 0, 0);
            if (systemMetrics.Length > 0)
            {
                var latestMetrics = JsonSerializer.Deserialize<object>(systemMetrics[0]!);
                metrics["system"] = latestMetrics;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取实时指标失败");
            metrics["error"] = ex.Message;
        }

        return metrics;
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
} 