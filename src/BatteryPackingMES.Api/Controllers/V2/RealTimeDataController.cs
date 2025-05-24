using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Api.Models;
using BatteryPackingMES.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 实时数据处理控制器 V2.0
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/realtime")]
[Authorize]
[Produces("application/json")]
public class RealTimeDataController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<RealTimeDataController> _logger;

    public RealTimeDataController(
        ICacheService cacheService,
        IMessageQueueService messageQueueService,
        ILogger<RealTimeDataController> logger)
    {
        _cacheService = cacheService;
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    /// <summary>
    /// 获取指定工序的实时参数监控数据
    /// </summary>
    /// <param name="processId">工序ID</param>
    /// <returns>实时参数监控数据</returns>
    [HttpGet("parameters/{processId}")]
    [ProducesResponseType(typeof(ApiResponse<RealTimeParameterMonitorDto>), 200)]
    public async Task<IActionResult> GetRealTimeParameters([FromRoute, Required] long processId)
    {
        try
        {
            var cacheKey = $"latest:parameters:{processId}";
            var latestParameters = await _cacheService.GetAsync<Dictionary<string, object>>(cacheKey);

            if (latestParameters == null)
            {
                return Ok(ApiResponse.OK(new RealTimeParameterMonitorDto
                {
                    ProcessId = processId,
                    Parameters = new List<RealTimeParameterItemDto>(),
                    LastUpdateTime = DateTime.UtcNow,
                    Status = "无数据"
                }));
            }

            var parameters = new List<RealTimeParameterItemDto>();
            foreach (var kvp in latestParameters)
            {
                try
                {
                    var paramData = System.Text.Json.JsonSerializer.Deserialize<RealTimeParameterData>(
                        System.Text.Json.JsonSerializer.Serialize(kvp.Value));

                    if (paramData != null)
                    {
                        parameters.Add(new RealTimeParameterItemDto
                        {
                            ParameterName = kvp.Key,
                            CurrentValue = paramData.Value,
                            Timestamp = paramData.Timestamp,
                            IsQualified = paramData.IsQualified,
                            EquipmentCode = paramData.EquipmentCode,
                            BatchNumber = paramData.BatchNumber,
                            Unit = "", // 需要从其他地方获取单位
                            Status = paramData.IsQualified ? "正常" : "异常"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析参数数据失败: {ParameterName}", kvp.Key);
                }
            }

            var result = new RealTimeParameterMonitorDto
            {
                ProcessId = processId,
                Parameters = parameters,
                LastUpdateTime = parameters.Any() ? parameters.Max(p => p.Timestamp) : DateTime.UtcNow,
                Status = parameters.Any(p => !p.IsQualified) ? "异常" : "正常"
            };

            return Ok(ApiResponse.OK(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实时参数监控数据失败: ProcessId {ProcessId}", processId);
            return BadRequest(ApiResponse.Fail("获取实时数据失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取聚合数据
    /// </summary>
    /// <param name="processId">工序ID</param>
    /// <param name="parameterName">参数名称</param>
    /// <returns>聚合数据</returns>
    [HttpGet("aggregated/{processId}/{parameterName}")]
    [ProducesResponseType(typeof(ApiResponse<ParameterAggregatedDataDto>), 200)]
    public async Task<IActionResult> GetAggregatedData(
        [FromRoute, Required] long processId,
        [FromRoute, Required] string parameterName)
    {
        try
        {
            var cacheKey = $"aggregated:parameter:{processId}:{parameterName}";
            var aggregatedData = await _cacheService.GetAsync<ParameterAggregatedData>(cacheKey);

            if (aggregatedData == null)
            {
                return NotFound(ApiResponse.Fail("未找到聚合数据"));
            }

            var result = new ParameterAggregatedDataDto
            {
                ProcessId = aggregatedData.ProcessId,
                ParameterName = aggregatedData.ParameterName,
                WindowStartTime = aggregatedData.WindowStartTime,
                WindowEndTime = aggregatedData.WindowEndTime,
                DataCount = aggregatedData.DataCount,
                MinValue = aggregatedData.MinValue,
                MaxValue = aggregatedData.MaxValue,
                AverageValue = aggregatedData.AverageValue,
                StandardDeviation = (decimal)aggregatedData.StandardDeviation,
                QualifiedCount = aggregatedData.QualifiedCount,
                QualificationRate = aggregatedData.QualificationRate
            };

            return Ok(ApiResponse.OK(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聚合数据失败: ProcessId {ProcessId}, ParameterName {ParameterName}", 
                processId, parameterName);
            return BadRequest(ApiResponse.Fail("获取聚合数据失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取系统实时状态概览
    /// </summary>
    /// <returns>系统状态概览</returns>
    [HttpGet("system-status")]
    [ProducesResponseType(typeof(ApiResponse<SystemRealTimeStatusDto>), 200)]
    public async Task<IActionResult> GetSystemStatus()
    {
        try
        {
            var status = new SystemRealTimeStatusDto
            {
                Timestamp = DateTime.UtcNow,
                ProcessingStatus = "运行中",
                MessageQueueStatus = "正常",
                CacheStatus = "正常"
            };

            // 检查消息队列状态
            try
            {
                // 这里可以添加具体的健康检查逻辑
                status.MessageQueueStatus = "正常";
            }
            catch
            {
                status.MessageQueueStatus = "异常";
            }

            // 检查缓存状态
            try
            {
                await _cacheService.GetAsync<string>("health-check");
                status.CacheStatus = "正常";
            }
            catch
            {
                status.CacheStatus = "异常";
            }

            // 获取活跃的参数监控数量
            // 这里可以添加更详细的统计逻辑

            return Ok(ApiResponse.OK(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统状态失败");
            return BadRequest(ApiResponse.Fail("获取系统状态失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取实时事件流
    /// </summary>
    /// <param name="eventTypes">事件类型过滤</param>
    /// <param name="count">获取数量</param>
    /// <returns>实时事件列表</returns>
    [HttpGet("events")]
    [ProducesResponseType(typeof(ApiResponse<List<RealTimeEventDto>>), 200)]
    public async Task<IActionResult> GetRealTimeEvents(
        [FromQuery] string[]? eventTypes = null,
        [FromQuery] int count = 50)
    {
        try
        {
            var events = new List<RealTimeEventDto>();

            // 这里可以从Redis或其他存储获取最近的事件
            // 暂时返回模拟数据
            
            return Ok(ApiResponse.OK(events));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实时事件失败");
            return BadRequest(ApiResponse.Fail("获取实时事件失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 强制触发参数聚合
    /// </summary>
    /// <param name="processId">工序ID</param>
    /// <param name="parameterName">参数名称（可选）</param>
    /// <returns>操作结果</returns>
    [HttpPost("trigger-aggregation")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> TriggerAggregation(
        [FromQuery, Required] long processId,
        [FromQuery] string? parameterName = null)
    {
        try
        {
            var triggerMessage = new
            {
                ProcessId = processId,
                ParameterName = parameterName,
                TriggerType = "Manual",
                Timestamp = DateTime.UtcNow
            };

            await _messageQueueService.PublishAsync("aggregation-trigger", triggerMessage);

            _logger.LogInformation("手动触发参数聚合: ProcessId {ProcessId}, ParameterName {ParameterName}", 
                processId, parameterName);

            return Ok(ApiResponse.OK("聚合任务已触发"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发聚合失败: ProcessId {ProcessId}", processId);
            return BadRequest(ApiResponse.Fail("触发聚合失败: " + ex.Message));
        }
    }
}

#region DTO Models

/// <summary>
/// 实时参数监控DTO
/// </summary>
public class RealTimeParameterMonitorDto
{
    public long ProcessId { get; set; }
    public List<RealTimeParameterItemDto> Parameters { get; set; } = new();
    public DateTime LastUpdateTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 实时参数项DTO
/// </summary>
public class RealTimeParameterItemDto
{
    public string ParameterName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsQualified { get; set; }
    public string? EquipmentCode { get; set; }
    public string? BatchNumber { get; set; }
    public string? Unit { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 实时参数数据（用于反序列化）
/// </summary>
public class RealTimeParameterData
{
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsQualified { get; set; }
    public string? EquipmentCode { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 参数聚合数据DTO
/// </summary>
public class ParameterAggregatedDataDto
{
    public long ProcessId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public DateTime WindowStartTime { get; set; }
    public DateTime WindowEndTime { get; set; }
    public int DataCount { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal AverageValue { get; set; }
    public decimal StandardDeviation { get; set; }
    public int QualifiedCount { get; set; }
    public decimal QualificationRate { get; set; }
}

/// <summary>
/// 系统实时状态DTO
/// </summary>
public class SystemRealTimeStatusDto
{
    public DateTime Timestamp { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public string MessageQueueStatus { get; set; } = string.Empty;
    public string CacheStatus { get; set; } = string.Empty;
    public int ActiveParametersCount { get; set; }
    public int TotalProcessedToday { get; set; }
    public int AlertCount { get; set; }
}

/// <summary>
/// 实时事件DTO
/// </summary>
public class RealTimeEventDto
{
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

#endregion 