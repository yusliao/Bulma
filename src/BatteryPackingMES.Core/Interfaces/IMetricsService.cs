using BatteryPackingMES.Core.Entities;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 指标服务接口
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// 记录指标
    /// </summary>
    /// <param name="metricName">指标名称</param>
    /// <param name="value">指标值</param>
    /// <param name="metricType">指标类型</param>
    /// <param name="tags">标签</param>
    /// <returns></returns>
    Task RecordMetricAsync(string metricName, double value, string metricType = "gauge", Dictionary<string, string>? tags = null);

    /// <summary>
    /// 记录计数器指标
    /// </summary>
    /// <param name="metricName">指标名称</param>
    /// <param name="increment">增量（默认为1）</param>
    /// <param name="tags">标签</param>
    /// <returns></returns>
    Task IncrementCounterAsync(string metricName, double increment = 1, Dictionary<string, string>? tags = null);

    /// <summary>
    /// 记录计时器指标
    /// </summary>
    /// <param name="metricName">指标名称</param>
    /// <param name="duration">持续时间（毫秒）</param>
    /// <param name="tags">标签</param>
    /// <returns></returns>
    Task RecordTimingAsync(string metricName, double duration, Dictionary<string, string>? tags = null);

    /// <summary>
    /// 获取指标数据
    /// </summary>
    /// <param name="metricName">指标名称</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<List<SystemMetric>> GetMetricsAsync(string metricName, DateTime startTime, DateTime endTime);

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetSystemHealthAsync();

    /// <summary>
    /// 记录API请求指标
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="duration">响应时间</param>
    /// <returns></returns>
    Task RecordApiRequestAsync(string endpoint, string method, int statusCode, double duration);
} 