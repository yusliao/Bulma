using BatteryPackingMES.Core.Events;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 事件总线接口
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : BaseEvent;

    /// <summary>
    /// 发布多个事件
    /// </summary>
    Task PublishManyAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : BaseEvent;

    /// <summary>
    /// 订阅事件
    /// </summary>
    Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler) where T : BaseEvent;

    /// <summary>
    /// 取消订阅
    /// </summary>
    Task UnsubscribeAsync<T>() where T : BaseEvent;
}

/// <summary>
/// 事件处理器接口
/// </summary>
/// <typeparam name="T">事件类型</typeparam>
public interface IEventHandler<in T> where T : BaseEvent
{
    /// <summary>
    /// 处理事件
    /// </summary>
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// 事件存储接口
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// 保存事件
    /// </summary>
    Task SaveEventAsync<T>(T @event) where T : BaseEvent;

    /// <summary>
    /// 获取事件流
    /// </summary>
    Task<IEnumerable<BaseEvent>> GetEventsAsync(string aggregateId, int? fromVersion = null);

    /// <summary>
    /// 获取所有事件
    /// </summary>
    Task<IEnumerable<BaseEvent>> GetAllEventsAsync(string eventType, DateTimeOffset? from = null, DateTimeOffset? to = null);
}

/// <summary>
/// 事件处理器注册接口
/// </summary>
public interface IEventHandlerRegistry
{
    /// <summary>
    /// 注册事件处理器
    /// </summary>
    void RegisterHandler<T>(IEventHandler<T> handler) where T : BaseEvent;

    /// <summary>
    /// 获取事件处理器
    /// </summary>
    IEnumerable<IEventHandler<T>> GetHandlers<T>() where T : BaseEvent;

    /// <summary>
    /// 获取所有处理器
    /// </summary>
    IEnumerable<object> GetAllHandlers(Type eventType);
} 