using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Text.Json;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 事件存储服务实现
/// </summary>
public class EventStoreService : IEventStore
{
    private readonly ISqlSugarClient _sqlSugar;
    private readonly ILogger<EventStoreService> _logger;

    public EventStoreService(ISqlSugarClient sqlSugar, ILogger<EventStoreService> logger)
    {
        _sqlSugar = sqlSugar;
        _logger = logger;
    }

    /// <summary>
    /// 保存事件到存储
    /// </summary>
    public async Task SaveEventAsync<T>(T @event) where T : BaseEvent
    {
        try
        {
            var eventData = new EventStoreEntity
            {
                EventId = @event.EventId.ToString(),
                EventType = @event.EventType,
                AggregateId = @event.AggregateId,
                EventData = JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                Metadata = JsonSerializer.Serialize(@event.Metadata),
                OccurredOn = @event.OccurredOn.DateTime,
                Version = @event.Version,
                UserId = @event.UserId
            };

            await _sqlSugar.Insertable(eventData).ExecuteCommandAsync();
            
            _logger.LogDebug("事件已保存到存储: {EventType}, ID: {EventId}, 聚合ID: {AggregateId}", 
                @event.EventType, @event.EventId, @event.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存事件失败: {EventType}, ID: {EventId}", typeof(T).Name, @event.EventId);
            throw;
        }
    }

    /// <summary>
    /// 获取聚合根的事件流
    /// </summary>
    public async Task<IEnumerable<BaseEvent>> GetEventsAsync(string aggregateId, int? fromVersion = null)
    {
        try
        {
            var query = _sqlSugar.Queryable<EventStoreEntity>()
                .Where(e => e.AggregateId == aggregateId)
                .OrderBy(e => e.OccurredOn);

            if (fromVersion.HasValue)
            {
                query = query.Where(e => e.Id > fromVersion.Value);
            }

            var eventEntities = await query.ToListAsync();
            var events = new List<BaseEvent>();

            foreach (var entity in eventEntities)
            {
                try
                {
                    var eventType = Type.GetType($"BatteryPackingMES.Core.Events.{entity.EventType}");
                    if (eventType != null)
                    {
                        var deserializedEvent = JsonSerializer.Deserialize(entity.EventData!, eventType, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }) as BaseEvent;

                        if (deserializedEvent != null)
                        {
                            events.Add(deserializedEvent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "反序列化事件失败: {EventType}, ID: {EventId}", entity.EventType, entity.EventId);
                }
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取事件流失败: 聚合ID {AggregateId}", aggregateId);
            throw;
        }
    }

    /// <summary>
    /// 获取指定类型和时间范围的所有事件
    /// </summary>
    public async Task<IEnumerable<BaseEvent>> GetAllEventsAsync(string eventType, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        try
        {
            var query = _sqlSugar.Queryable<EventStoreEntity>()
                .Where(e => e.EventType == eventType)
                .OrderBy(e => e.OccurredOn);

            if (from.HasValue)
            {
                query = query.Where(e => e.OccurredOn >= from.Value.DateTime);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.OccurredOn <= to.Value.DateTime);
            }

            var eventEntities = await query.ToListAsync();
            var events = new List<BaseEvent>();

            foreach (var entity in eventEntities)
            {
                try
                {
                    var type = Type.GetType($"BatteryPackingMES.Core.Events.{entity.EventType}");
                    if (type != null)
                    {
                        var deserializedEvent = JsonSerializer.Deserialize(entity.EventData!, type, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }) as BaseEvent;

                        if (deserializedEvent != null)
                        {
                            events.Add(deserializedEvent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "反序列化事件失败: {EventType}, ID: {EventId}", entity.EventType, entity.EventId);
                }
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取事件列表失败: 事件类型 {EventType}", eventType);
            throw;
        }
    }

    /// <summary>
    /// 获取事件统计信息
    /// </summary>
    public async Task<EventStatistics> GetEventStatisticsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        try
        {
            var stats = await _sqlSugar.Queryable<EventStoreEntity>()
                .Where(e => e.OccurredOn >= from.DateTime && e.OccurredOn <= to.DateTime)
                .GroupBy(e => e.EventType)
                .Select(g => new { EventType = g.EventType, Count = SqlFunc.AggregateCount(g.EventType) })
                .ToListAsync();

            var total = await _sqlSugar.Queryable<EventStoreEntity>()
                .Where(e => e.OccurredOn >= from.DateTime && e.OccurredOn <= to.DateTime)
                .CountAsync();

            return new EventStatistics
            {
                TotalEvents = total,
                EventTypeCounts = stats.ToDictionary(s => s.EventType, s => s.Count),
                Period = new { From = from, To = to }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取事件统计失败");
            throw;
        }
    }
}

/// <summary>
/// 事件存储实体
/// </summary>
[SugarTable("event_store")]
public class EventStoreEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 36, IsNullable = false)]
    public string EventId { get; set; } = string.Empty;

    [SugarColumn(Length = 255, IsNullable = false)]
    public string EventType { get; set; } = string.Empty;

    [SugarColumn(Length = 255, IsNullable = false)]
    public string AggregateId { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "JSON", IsNullable = false)]
    public string EventData { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "JSON", IsNullable = true)]
    public string? Metadata { get; set; }

    [SugarColumn(IsNullable = false)]
    public DateTime OccurredOn { get; set; }

    [SugarColumn(Length = 10, IsNullable = false)]
    public string Version { get; set; } = "1.0";

    [SugarColumn(IsNullable = true)]
    public int? UserId { get; set; }

    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 事件统计信息
/// </summary>
public class EventStatistics
{
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventTypeCounts { get; set; } = new();
    public object? Period { get; set; }
} 