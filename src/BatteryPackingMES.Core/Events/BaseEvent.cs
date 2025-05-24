using System.Text.Json.Serialization;

namespace BatteryPackingMES.Core.Events;

/// <summary>
/// 领域事件基类
/// </summary>
public abstract class BaseEvent
{
    protected BaseEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        Version = "1.0";
    }

    /// <summary>
    /// 事件唯一标识
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// 事件版本
    /// </summary>
    public string Version { get; protected set; }

    /// <summary>
    /// 事件类型名称
    /// </summary>
    [JsonIgnore]
    public string EventType => GetType().Name;

    /// <summary>
    /// 聚合根ID
    /// </summary>
    public virtual string AggregateId { get; protected set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 相关信息
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 集成事件基类 - 用于跨边界上下文通信
/// </summary>
public abstract class IntegrationEvent : BaseEvent
{
    /// <summary>
    /// 事件来源系统
    /// </summary>
    public string Source { get; set; } = "BatteryPackingMES";

    /// <summary>
    /// 目标系统（可选）
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;
} 