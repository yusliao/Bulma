namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 消息队列服务接口
/// </summary>
public interface IMessageQueueService
{
    /// <summary>
    /// 发布消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="message">消息内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="handler">消息处理器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 取消订阅
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <returns></returns>
    Task UnsubscribeAsync(string channel);

    /// <summary>
    /// 取消所有订阅
    /// </summary>
    /// <returns></returns>
    Task UnsubscribeAllAsync();
} 