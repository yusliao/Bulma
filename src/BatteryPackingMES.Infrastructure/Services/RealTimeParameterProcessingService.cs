using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 实时参数处理后台服务
/// </summary>
public class RealTimeParameterProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageQueueService _messageQueue;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RealTimeParameterProcessingService> _logger;
    private readonly IConfiguration _configuration;
    
    private readonly ConcurrentDictionary<string, ParameterWindow> _parameterWindows = new();
    private readonly Timer _aggregationTimer;
    private readonly Timer _alertCheckTimer;
    
    // 配置参数
    private readonly int _aggregationIntervalSeconds;
    private readonly int _alertCheckIntervalSeconds;
    private readonly int _windowSizeMinutes;
    private readonly double _anomalyThreshold;

    public RealTimeParameterProcessingService(
        IServiceProvider serviceProvider,
        IMessageQueueService messageQueue,
        ICacheService cacheService,
        IEventBus eventBus,
        ILogger<RealTimeParameterProcessingService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _messageQueue = messageQueue;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _logger = logger;
        _configuration = configuration;

        // 从配置读取参数
        _aggregationIntervalSeconds = configuration.GetValue<int>("RealTimeProcessing:AggregationIntervalSeconds", 30);
        _alertCheckIntervalSeconds = configuration.GetValue<int>("RealTimeProcessing:AlertCheckIntervalSeconds", 60);
        _windowSizeMinutes = configuration.GetValue<int>("RealTimeProcessing:WindowSizeMinutes", 10);
        _anomalyThreshold = configuration.GetValue<double>("RealTimeProcessing:AnomalyThreshold", 2.0);

        // 初始化定时器
        _aggregationTimer = new Timer(ProcessAggregation, null, Timeout.Infinite, Timeout.Infinite);
        _alertCheckTimer = new Timer(CheckAlerts, null, Timeout.Infinite, Timeout.Infinite);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("实时参数处理服务启动");

        // 订阅参数采集消息
        await _messageQueue.SubscribeAsync<ParameterCollectedMessage>("parameter-collected", ProcessParameterMessage);

        // 启动定时器
        _aggregationTimer.Change(TimeSpan.FromSeconds(_aggregationIntervalSeconds), TimeSpan.FromSeconds(_aggregationIntervalSeconds));
        _alertCheckTimer.Change(TimeSpan.FromSeconds(_alertCheckIntervalSeconds), TimeSpan.FromSeconds(_alertCheckIntervalSeconds));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 清理过期的参数窗口
                await CleanupExpiredWindows();
                
                // 等待一段时间再进行下一次清理
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("实时参数处理服务收到停止信号");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "实时参数处理服务执行过程中发生错误");
        }
    }

    /// <summary>
    /// 处理参数采集消息
    /// </summary>
    private async Task ProcessParameterMessage(ParameterCollectedMessage message)
    {
        try
        {
            var windowKey = $"{message.ProcessId}_{message.ParameterName}";
            
            // 获取或创建参数窗口
            var window = _parameterWindows.GetOrAdd(windowKey, key => new ParameterWindow
            {
                ProcessId = message.ProcessId,
                ParameterName = message.ParameterName,
                WindowStartTime = DateTime.UtcNow.AddMinutes(-_windowSizeMinutes),
                Values = new List<ParameterValue>()
            });

            // 添加新值到窗口
            lock (window.Values)
            {
                window.Values.Add(new ParameterValue
                {
                    Value = message.Value,
                    Timestamp = message.Timestamp,
                    IsQualified = message.IsQualified,
                    EquipmentCode = message.EquipmentCode,
                    BatchNumber = message.BatchNumber
                });

                // 移除过期数据
                var cutoffTime = DateTime.UtcNow.AddMinutes(-_windowSizeMinutes);
                window.Values.RemoveAll(v => v.Timestamp < cutoffTime);
            }

            // 实时异常检测
            await DetectAnomalies(window, message);

            // 更新实时缓存
            await UpdateRealTimeCache(message);

            _logger.LogDebug("处理参数消息: {ProcessId}-{ParameterName}, 值: {Value}", 
                message.ProcessId, message.ParameterName, message.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理参数消息失败: {ProcessId}-{ParameterName}", 
                message.ProcessId, message.ParameterName);
        }
    }

    /// <summary>
    /// 异常检测
    /// </summary>
    private async Task DetectAnomalies(ParameterWindow window, ParameterCollectedMessage message)
    {
        try
        {
            List<decimal> recentValues;
            lock (window.Values)
            {
                if (window.Values.Count < 10) return; // 数据点不足，跳过检测
                recentValues = window.Values.TakeLast(10).Select(v => v.Value).ToList();
            }

            var mean = recentValues.Average();
            var stdDev = CalculateStandardDeviation(recentValues);

            // Z-Score 异常检测
            var zScore = Math.Abs((double)(message.Value - mean) / stdDev);
            
            if (zScore > _anomalyThreshold)
            {
                var anomalyEvent = new ProcessParameterAnomalyDetectedEvent(
                    message.ParameterName,
                    message.Value,
                    mean,
                    stdDev,
                    zScore,
                    message.BatchNumber ?? "",
                    message.EquipmentCode ?? "");

                await _eventBus.PublishAsync(anomalyEvent);

                _logger.LogWarning("检测到参数异常: {ParameterName}, 当前值: {Value}, 均值: {Mean}, Z-Score: {ZScore}",
                    message.ParameterName, message.Value, mean, zScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异常检测失败: {ParameterName}", message.ParameterName);
        }
    }

    /// <summary>
    /// 更新实时缓存
    /// </summary>
    private async Task UpdateRealTimeCache(ParameterCollectedMessage message)
    {
        try
        {
            var cacheKey = $"realtime:parameter:{message.ProcessId}:{message.ParameterName}";
            var realtimeData = new
            {
                message.Value,
                message.Timestamp,
                message.IsQualified,
                message.EquipmentCode,
                message.BatchNumber,
                UpdatedAt = DateTime.UtcNow
            };

            await _cacheService.SetAsync(cacheKey, realtimeData, TimeSpan.FromMinutes(30));

            // 更新最新值列表（用于仪表板显示）
            var latestKey = $"latest:parameters:{message.ProcessId}";
            var latestParameters = await _cacheService.GetAsync<Dictionary<string, object>>(latestKey) 
                                  ?? new Dictionary<string, object>();
            
            latestParameters[message.ParameterName] = realtimeData;
            await _cacheService.SetAsync(latestKey, latestParameters, TimeSpan.FromMinutes(60));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新实时缓存失败: {ParameterName}", message.ParameterName);
        }
    }

    /// <summary>
    /// 数据聚合处理
    /// </summary>
    private async void ProcessAggregation(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<ProductionParameter>>();

            foreach (var kvp in _parameterWindows.ToList())
            {
                var window = kvp.Value;
                ParameterAggregatedData? aggregatedData = null;
                
                lock (window.Values)
                {
                    if (window.Values.Count == 0) continue;

                    aggregatedData = new ParameterAggregatedData
                    {
                        ProcessId = window.ProcessId,
                        ParameterName = window.ParameterName,
                        WindowStartTime = window.WindowStartTime,
                        WindowEndTime = DateTime.UtcNow,
                        DataCount = window.Values.Count,
                        MinValue = window.Values.Min(v => v.Value),
                        MaxValue = window.Values.Max(v => v.Value),
                        AverageValue = window.Values.Average(v => v.Value),
                        StandardDeviation = CalculateStandardDeviation(window.Values.Select(v => v.Value).ToList()),
                        QualifiedCount = window.Values.Count(v => v.IsQualified),
                        QualificationRate = (decimal)window.Values.Count(v => v.IsQualified) / window.Values.Count * 100
                    };
                }

                if (aggregatedData != null)
                {
                    // 发布聚合事件
                    var aggregationEvent = new ParameterAggregationCompletedEvent(aggregatedData);
                    await _eventBus.PublishAsync(aggregationEvent);

                    // 缓存聚合数据
                    var cacheKey = $"aggregated:parameter:{window.ProcessId}:{window.ParameterName}";
                    await _cacheService.SetAsync(cacheKey, aggregatedData, TimeSpan.FromHours(1));
                }
            }

            _logger.LogDebug("参数聚合处理完成，处理窗口数: {Count}", _parameterWindows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据聚合处理失败");
        }
    }

    /// <summary>
    /// 检查预警
    /// </summary>
    private async void CheckAlerts(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<ProductionParameter>>();

            var cutoffTime = DateTime.UtcNow.AddMinutes(-5); // 检查最近5分钟的数据
            
            var unqualifiedParameters = await repository.GetListAsync(
                p => p.CollectTime >= cutoffTime && !p.IsQualified);

            var alertGroups = unqualifiedParameters
                .GroupBy(p => new { p.ProcessId, p.ParameterName })
                .Where(g => g.Count() >= 3) // 连续3次不合格触发预警
                .ToList();

            foreach (var group in alertGroups)
            {
                var alertEvent = new ProcessParameterAlertTriggeredEvent(
                    group.Key.ParameterName,
                    group.Count(),
                    group.Max(p => p.CollectTime),
                    group.OrderByDescending(p => p.CollectTime).First().BatchNumber ?? "",
                    "连续不合格");

                await _eventBus.PublishAsync(alertEvent);
            }

            if (alertGroups.Any())
            {
                _logger.LogWarning("检测到参数预警，预警数量: {Count}", alertGroups.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预警检查失败");
        }
    }

    /// <summary>
    /// 清理过期窗口
    /// </summary>
    private async Task CleanupExpiredWindows()
    {
        try
        {
            var expiredKeys = new List<string>();
            var cutoffTime = DateTime.UtcNow.AddHours(-1); // 1小时未更新的窗口视为过期

            foreach (var kvp in _parameterWindows)
            {
                lock (kvp.Value.Values)
                {
                    if (!kvp.Value.Values.Any() || kvp.Value.Values.Max(v => v.Timestamp) < cutoffTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in expiredKeys)
            {
                _parameterWindows.TryRemove(key, out _);
            }

            if (expiredKeys.Any())
            {
                _logger.LogDebug("清理过期参数窗口: {Count}", expiredKeys.Count);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期窗口失败");
        }
    }

    /// <summary>
    /// 计算标准差
    /// </summary>
    private static double CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2) return 0;

        var mean = values.Average();
        var sumOfSquaredDeviations = values.Sum(v => Math.Pow((double)(v - mean), 2));
        return Math.Sqrt(sumOfSquaredDeviations / (values.Count - 1));
    }

    public override void Dispose()
    {
        _aggregationTimer?.Dispose();
        _alertCheckTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// 参数窗口
/// </summary>
public class ParameterWindow
{
    public long ProcessId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public DateTime WindowStartTime { get; set; }
    public List<ParameterValue> Values { get; set; } = new();
}

/// <summary>
/// 参数值
/// </summary>
public class ParameterValue
{
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsQualified { get; set; }
    public string? EquipmentCode { get; set; }
    public string? BatchNumber { get; set; }
}

/// <summary>
/// 参数采集消息
/// </summary>
public class ParameterCollectedMessage
{
    public long ProcessId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsQualified { get; set; }
    public string? EquipmentCode { get; set; }
    public string? BatchNumber { get; set; }
    public string? Unit { get; set; }
    public decimal? UpperLimit { get; set; }
    public decimal? LowerLimit { get; set; }
}

/// <summary>
/// 参数聚合数据
/// </summary>
public class ParameterAggregatedData
{
    public long ProcessId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public DateTime WindowStartTime { get; set; }
    public DateTime WindowEndTime { get; set; }
    public int DataCount { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal AverageValue { get; set; }
    public double StandardDeviation { get; set; }
    public int QualifiedCount { get; set; }
    public decimal QualificationRate { get; set; }
} 