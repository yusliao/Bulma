using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 参数异常检测事件处理器
/// </summary>
public class ParameterAnomalyEventHandler : IEventHandler<ProcessParameterAnomalyDetectedEvent>
{
    private readonly ILogger<ParameterAnomalyEventHandler> _logger;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public ParameterAnomalyEventHandler(
        ILogger<ParameterAnomalyEventHandler> logger,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(ProcessParameterAnomalyDetectedEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("处理参数异常事件: 参数 {ParameterName}, 当前值 {CurrentValue}, Z-Score {ZScore}, 异常级别 {AnomalyLevel}",
                eventData.ParameterName, eventData.CurrentValue, eventData.ZScore, eventData.AnomalyLevel);

            // 缓存异常信息
            var anomalyCacheKey = $"anomaly:parameter:{eventData.ParameterName}:{DateTime.UtcNow:yyyyMMddHH}";
            var anomalyInfo = new
            {
                eventData.ParameterName,
                eventData.CurrentValue,
                eventData.ExpectedMean,
                eventData.ZScore,
                eventData.AnomalyLevel,
                eventData.BatchNumber,
                eventData.EquipmentCode,
                Timestamp = eventData.OccurredOn
            };

            await _cacheService.SetAsync(anomalyCacheKey, anomalyInfo, TimeSpan.FromHours(24));

            // 如果是严重异常，发送警报通知
            if (eventData.AnomalyLevel == "Critical" || eventData.AnomalyLevel == "High")
            {
                var alertMessage = new
                {
                    Type = "ParameterAnomaly",
                    Severity = eventData.AnomalyLevel,
                    Message = $"参数 {eventData.ParameterName} 检测到异常，当前值 {eventData.CurrentValue}，Z-Score: {eventData.ZScore:F2}",
                    Data = anomalyInfo
                };

                await _messageQueueService.PublishAsync("alert-notifications", alertMessage, cancellationToken);
            }

            // 更新异常统计
            await UpdateAnomalyStatistics(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理参数异常事件失败: {ParameterName}", eventData.ParameterName);
        }
    }

    private async Task UpdateAnomalyStatistics(ProcessParameterAnomalyDetectedEvent eventData)
    {
        try
        {
            var statsKey = $"stats:anomaly:{eventData.ParameterName}:daily:{DateTime.UtcNow:yyyyMMdd}";
            var currentStats = await _cacheService.GetAsync<AnomalyStatistics>(statsKey) ?? new AnomalyStatistics();

            currentStats.TotalCount++;
            switch (eventData.AnomalyLevel)
            {
                case "Critical":
                    currentStats.CriticalCount++;
                    break;
                case "High":
                    currentStats.HighCount++;
                    break;
                case "Medium":
                    currentStats.MediumCount++;
                    break;
                default:
                    currentStats.LowCount++;
                    break;
            }

            currentStats.LastAnomalyTime = eventData.OccurredOn.DateTime;

            await _cacheService.SetAsync(statsKey, currentStats, TimeSpan.FromDays(7));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新异常统计失败: {ParameterName}", eventData.ParameterName);
        }
    }
}

/// <summary>
/// 参数预警事件处理器
/// </summary>
public class ParameterAlertEventHandler : IEventHandler<ProcessParameterAlertTriggeredEvent>
{
    private readonly ILogger<ParameterAlertEventHandler> _logger;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public ParameterAlertEventHandler(
        ILogger<ParameterAlertEventHandler> logger,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(ProcessParameterAlertTriggeredEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("处理参数预警事件: 参数 {ParameterName}, 预警次数 {AlertCount}, 严重程度 {Severity}",
                eventData.ParameterName, eventData.AlertCount, eventData.Severity);

            // 缓存预警信息
            var alertCacheKey = $"alert:parameter:{eventData.ParameterName}:{DateTime.UtcNow:yyyyMMddHH}";
            var alertInfo = new
            {
                eventData.ParameterName,
                eventData.AlertCount,
                eventData.LastAlertTime,
                eventData.BatchNumber,
                eventData.AlertReason,
                eventData.Severity,
                Timestamp = eventData.OccurredOn
            };

            await _cacheService.SetAsync(alertCacheKey, alertInfo, TimeSpan.FromHours(24));

            // 发送预警通知
            var notificationMessage = new
            {
                Type = "ParameterAlert",
                Severity = eventData.Severity,
                Message = $"参数 {eventData.ParameterName} 触发预警，连续 {eventData.AlertCount} 次异常",
                Data = alertInfo
            };

            await _messageQueueService.PublishAsync("alert-notifications", notificationMessage, cancellationToken);

            // 更新预警统计
            await UpdateAlertStatistics(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理参数预警事件失败: {ParameterName}", eventData.ParameterName);
        }
    }

    private async Task UpdateAlertStatistics(ProcessParameterAlertTriggeredEvent eventData)
    {
        try
        {
            var statsKey = $"stats:alert:{eventData.ParameterName}:daily:{DateTime.UtcNow:yyyyMMdd}";
            var currentStats = await _cacheService.GetAsync<AlertStatistics>(statsKey) ?? new AlertStatistics();

            currentStats.TotalAlerts++;
            switch (eventData.Severity)
            {
                case "Critical":
                    currentStats.CriticalAlerts++;
                    break;
                case "High":
                    currentStats.HighAlerts++;
                    break;
                case "Medium":
                    currentStats.MediumAlerts++;
                    break;
                default:
                    currentStats.LowAlerts++;
                    break;
            }

            currentStats.LastAlertTime = eventData.OccurredOn.DateTime;

            await _cacheService.SetAsync(statsKey, currentStats, TimeSpan.FromDays(7));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新预警统计失败: {ParameterName}", eventData.ParameterName);
        }
    }
}

/// <summary>
/// 参数聚合完成事件处理器
/// </summary>
public class ParameterAggregationEventHandler : IEventHandler<ParameterAggregationCompletedEvent>
{
    private readonly ILogger<ParameterAggregationEventHandler> _logger;
    private readonly ICacheService _cacheService;

    public ParameterAggregationEventHandler(
        ILogger<ParameterAggregationEventHandler> logger,
        ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(ParameterAggregationCompletedEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("处理参数聚合完成事件: {AggregateId}", eventData.AggregateId);

            // 缓存聚合结果的摘要信息
            var data = eventData.AggregatedData as ParameterAggregatedData;
            if (data != null)
            {
                var summaryKey = $"summary:aggregation:{data.ProcessId}:{data.ParameterName}";
                var summary = new
                {
                    data.ProcessId,
                    data.ParameterName,
                    data.DataCount,
                    data.AverageValue,
                    data.QualificationRate,
                    LastAggregationTime = eventData.OccurredOn
                };

                await _cacheService.SetAsync(summaryKey, summary, TimeSpan.FromHours(2));

                _logger.LogInformation("参数聚合完成: 工序 {ProcessId}, 参数 {ParameterName}, 数据量 {DataCount}, 合格率 {QualificationRate:F1}%",
                    data.ProcessId, data.ParameterName, data.DataCount, data.QualificationRate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理参数聚合完成事件失败: {AggregateId}", eventData.AggregateId);
        }
    }
}

/// <summary>
/// 异常统计信息
/// </summary>
public class AnomalyStatistics
{
    public int TotalCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public DateTime LastAnomalyTime { get; set; }
}

/// <summary>
/// 预警统计信息
/// </summary>
public class AlertStatistics
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int MediumAlerts { get; set; }
    public int LowAlerts { get; set; }
    public DateTime LastAlertTime { get; set; }
} 