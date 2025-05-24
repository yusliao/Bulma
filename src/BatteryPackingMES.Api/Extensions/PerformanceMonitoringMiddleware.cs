using BatteryPackingMES.Core.Interfaces;
using System.Diagnostics;

namespace BatteryPackingMES.Api.Extensions;

/// <summary>
/// 性能监控中间件
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMetricsService metricsService)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = context.Request.Path.ToString();
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            // 记录API请求指标
            try
            {
                await metricsService.RecordApiRequestAsync(endpoint, method, statusCode, duration);

                // 记录详细的性能日志
                if (duration > 2000) // 超过2秒的慢请求
                {
                    _logger.LogWarning("慢请求检测: {Method} {Endpoint} 耗时 {Duration}ms, 状态码: {StatusCode}",
                        method, endpoint, duration, statusCode);
                }
                else if (duration > 5000) // 超过5秒的超慢请求
                {
                    _logger.LogError("超慢请求检测: {Method} {Endpoint} 耗时 {Duration}ms, 状态码: {StatusCode}",
                        method, endpoint, duration, statusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录API性能指标失败: {Method} {Endpoint}", method, endpoint);
            }
        }
    }
}

/// <summary>
/// 性能监控中间件扩展
/// </summary>
public static class PerformanceMonitoringMiddlewareExtensions
{
    /// <summary>
    /// 使用性能监控中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
    }
} 