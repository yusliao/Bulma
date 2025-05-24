using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Diagnostics;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 指标服务实现
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly IRepository<SystemMetric> _metricRepository;
    private readonly ILogger<MetricsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _serviceName;
    private readonly string _environment;

    public MetricsService(
        IRepository<SystemMetric> metricRepository,
        ILogger<MetricsService> logger,
        IConfiguration configuration)
    {
        _metricRepository = metricRepository;
        _logger = logger;
        _configuration = configuration;
        _serviceName = _configuration["ServiceName"] ?? "BatteryPackingMES";
        _environment = _configuration["Environment"] ?? "Development";
    }

    /// <summary>
    /// 记录指标
    /// </summary>
    public async Task RecordMetricAsync(string metricName, double value, string metricType = "gauge", Dictionary<string, string>? tags = null)
    {
        try
        {
            var metric = new SystemMetric
            {
                MetricName = metricName,
                MetricType = metricType,
                Value = value,
                MeasuredAt = DateTime.UtcNow,
                ServiceName = _serviceName,
                Environment = _environment,
                Tags = tags != null ? JsonSerializer.Serialize(tags) : null
            };

            await _metricRepository.AddAsync(metric);
            _logger.LogDebug("记录指标: {MetricName} = {Value} ({MetricType})", metricName, value, metricType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录指标失败: {MetricName}", metricName);
        }
    }

    /// <summary>
    /// 记录计数器指标
    /// </summary>
    public async Task IncrementCounterAsync(string metricName, double increment = 1, Dictionary<string, string>? tags = null)
    {
        await RecordMetricAsync(metricName, increment, "counter", tags);
    }

    /// <summary>
    /// 记录计时器指标
    /// </summary>
    public async Task RecordTimingAsync(string metricName, double duration, Dictionary<string, string>? tags = null)
    {
        await RecordMetricAsync(metricName, duration, "timer", tags);
    }

    /// <summary>
    /// 获取指标数据
    /// </summary>
    public async Task<List<SystemMetric>> GetMetricsAsync(string metricName, DateTime startTime, DateTime endTime)
    {
        try
        {
            var metrics = await _metricRepository.GetByConditionAsync(m => 
                m.MetricName == metricName && 
                m.MeasuredAt >= startTime && 
                m.MeasuredAt <= endTime);
            
            return metrics.OrderBy(m => m.MeasuredAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取指标数据失败: {MetricName}", metricName);
            return new List<SystemMetric>();
        }
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    public async Task<Dictionary<string, object>> GetSystemHealthAsync()
    {
        var healthStatus = new Dictionary<string, object>();

        try
        {
            // CPU使用率
            var cpuProcess = System.Diagnostics.Process.GetCurrentProcess();
            var cpuUsage = cpuProcess.TotalProcessorTime.TotalMilliseconds;
            
            // 内存使用情况
            var memoryUsage = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;
            
            // 线程数
            var threadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            
            // 运行时间
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

            healthStatus["status"] = "healthy";
            healthStatus["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            healthStatus["service"] = _serviceName;
            healthStatus["environment"] = _environment;
            healthStatus["uptime_seconds"] = uptime.TotalSeconds;
            healthStatus["memory_usage_bytes"] = memoryUsage;
            healthStatus["working_set_bytes"] = workingSet;
            healthStatus["thread_count"] = threadCount;
            
            // 记录健康检查指标
            await RecordMetricAsync("system.health.memory_usage", memoryUsage);
            await RecordMetricAsync("system.health.working_set", workingSet);
            await RecordMetricAsync("system.health.thread_count", threadCount);
            await RecordMetricAsync("system.health.uptime", uptime.TotalSeconds);

            // 最近错误统计
            var recentErrors = await GetRecentErrorCountAsync();
            healthStatus["recent_errors"] = recentErrors;
            
            if (recentErrors > 100)
            {
                healthStatus["status"] = "degraded";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统健康状态失败");
            healthStatus["status"] = "unhealthy";
            healthStatus["error"] = ex.Message;
        }

        return healthStatus;
    }

    /// <summary>
    /// 记录API请求指标
    /// </summary>
    public async Task RecordApiRequestAsync(string endpoint, string method, int statusCode, double duration)
    {
        var tags = new Dictionary<string, string>
        {
            ["endpoint"] = endpoint,
            ["method"] = method,
            ["status_code"] = statusCode.ToString()
        };

        await RecordTimingAsync("api.request.duration", duration, tags);
        await IncrementCounterAsync("api.request.count", 1, tags);

        // 记录错误率
        if (statusCode >= 400)
        {
            await IncrementCounterAsync("api.request.errors", 1, tags);
        }

        // 记录响应时间分级
        var latencyCategory = duration switch
        {
            < 100 => "fast",
            < 500 => "medium",
            < 2000 => "slow",
            _ => "very_slow"
        };

        tags["latency_category"] = latencyCategory;
        await IncrementCounterAsync("api.request.latency_category", 1, tags);
    }

    /// <summary>
    /// 获取最近错误数量
    /// </summary>
    private async Task<int> GetRecentErrorCountAsync()
    {
        try
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var errorMetrics = await _metricRepository.GetByConditionAsync(m => 
                m.MetricName.Contains("error") && 
                m.MeasuredAt >= last24Hours);
            
            return errorMetrics.Count();
        }
        catch
        {
            return 0;
        }
    }
} 