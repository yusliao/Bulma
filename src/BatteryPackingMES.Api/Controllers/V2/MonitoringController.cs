using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 监控管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
[Authorize]
public class MonitoringController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly IDataValidationService _dataValidationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IMetricsService metricsService,
        IDataValidationService dataValidationService,
        IAuditService auditService,
        ILogger<MonitoringController> logger)
    {
        _metricsService = metricsService;
        _dataValidationService = dataValidationService;
        _auditService = auditService;
        _logger = logger;
    }

    #region 系统监控

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    /// <returns></returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSystemHealth()
    {
        try
        {
            var healthStatus = await _metricsService.GetSystemHealthAsync();
            return Ok(new { Success = true, Data = healthStatus });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统健康状态失败");
            return StatusCode(500, new { Success = false, Message = "获取系统健康状态失败" });
        }
    }

    /// <summary>
    /// 获取指标数据
    /// </summary>
    /// <param name="metricName">指标名称</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(
        [Required] string metricName,
        [Required] DateTime startTime,
        [Required] DateTime endTime)
    {
        try
        {
            var metrics = await _metricsService.GetMetricsAsync(metricName, startTime, endTime);
            return Ok(new { Success = true, Data = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取指标数据失败: {MetricName}", metricName);
            return StatusCode(500, new { Success = false, Message = "获取指标数据失败" });
        }
    }

    /// <summary>
    /// 记录自定义指标
    /// </summary>
    /// <param name="request">指标请求</param>
    /// <returns></returns>
    [HttpPost("metrics")]
    public async Task<IActionResult> RecordMetric([FromBody] MetricRequest request)
    {
        try
        {
            await _metricsService.RecordMetricAsync(request.MetricName, request.Value, request.MetricType, request.Tags);
            
            // 记录审计日志
            await _auditService.LogAsync("RecordMetric", description: $"记录指标: {request.MetricName} = {request.Value}");
            
            return Ok(new { Success = true, Message = "指标记录成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录指标失败: {MetricName}", request.MetricName);
            return StatusCode(500, new { Success = false, Message = "记录指标失败" });
        }
    }

    #endregion

    #region 数据验证

    /// <summary>
    /// 获取数据质量报告
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    [HttpGet("data-quality/report")]
    public async Task<IActionResult> GetDataQualityReport(
        string? entityType = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        try
        {
            var report = await _dataValidationService.GetDataQualityReportAsync(entityType, startTime, endTime);
            return Ok(new { Success = true, Data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据质量报告失败");
            return StatusCode(500, new { Success = false, Message = "获取数据质量报告失败" });
        }
    }

    /// <summary>
    /// 获取验证结果
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    [HttpGet("data-quality/results")]
    public async Task<IActionResult> GetValidationResults(
        string? entityType = null,
        long? entityId = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        try
        {
            var results = await _dataValidationService.GetValidationResultsAsync(entityType, entityId, startTime, endTime);
            return Ok(new { Success = true, Data = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验证结果失败");
            return StatusCode(500, new { Success = false, Message = "获取验证结果失败" });
        }
    }

    /// <summary>
    /// 运行数据质量检查
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns></returns>
    [HttpPost("data-quality/check")]
    public async Task<IActionResult> RunDataQualityCheck(string? entityType = null)
    {
        try
        {
            await _dataValidationService.RunDataQualityJobAsync(entityType);
            
            // 记录审计日志
            await _auditService.LogAsync("DataQualityCheck", description: $"运行数据质量检查: {entityType ?? "全部"}");
            
            return Ok(new { Success = true, Message = "数据质量检查已启动" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "运行数据质量检查失败");
            return StatusCode(500, new { Success = false, Message = "运行数据质量检查失败" });
        }
    }

    /// <summary>
    /// 添加验证规则
    /// </summary>
    /// <param name="rule">验证规则</param>
    /// <returns></returns>
    [HttpPost("data-quality/rules")]
    public async Task<IActionResult> AddValidationRule([FromBody] DataValidationRule rule)
    {
        try
        {
            var createdRule = await _dataValidationService.AddValidationRuleAsync(rule);
            
            // 记录审计日志
            await _auditService.LogCreateAsync(createdRule, "添加数据验证规则");
            
            return Ok(new { Success = true, Data = createdRule });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加验证规则失败");
            return StatusCode(500, new { Success = false, Message = "添加验证规则失败" });
        }
    }

    #endregion

    #region 审计日志

    /// <summary>
    /// 获取审计日志
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="actionType">操作类型</param>
    /// <param name="userId">用户ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns></returns>
    [HttpGet("audit/logs")]
    public async Task<IActionResult> GetAuditLogs(
        string? entityType = null,
        long? entityId = null,
        string? actionType = null,
        long? userId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int pageIndex = 1,
        int pageSize = 50)
    {
        try
        {
            var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
                entityType, entityId, actionType, userId, startTime, endTime, pageIndex, pageSize);

            return Ok(new 
            { 
                Success = true, 
                Data = logs,
                Pagination = new 
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审计日志失败");
            return StatusCode(500, new { Success = false, Message = "获取审计日志失败" });
        }
    }

    /// <summary>
    /// 获取实体审计历史
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <returns></returns>
    [HttpGet("audit/entity-history")]
    public async Task<IActionResult> GetEntityAuditHistory(
        [Required] string entityType,
        [Required] long entityId)
    {
        try
        {
            var history = await _auditService.GetEntityAuditHistoryAsync(entityType, entityId);
            return Ok(new { Success = true, Data = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实体审计历史失败: {EntityType}:{EntityId}", entityType, entityId);
            return StatusCode(500, new { Success = false, Message = "获取实体审计历史失败" });
        }
    }

    /// <summary>
    /// 获取审计统计报告
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    [HttpGet("audit/report")]
    public async Task<IActionResult> GetAuditReport(
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        try
        {
            var report = await _auditService.GetAuditReportAsync(startTime, endTime);
            return Ok(new { Success = true, Data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审计统计报告失败");
            return StatusCode(500, new { Success = false, Message = "获取审计统计报告失败" });
        }
    }

    /// <summary>
    /// 导出审计日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="entityType">实体类型</param>
    /// <param name="format">导出格式</param>
    /// <returns></returns>
    [HttpGet("audit/export")]
    public async Task<IActionResult> ExportAuditLogs(
        [Required] DateTime startTime,
        [Required] DateTime endTime,
        string? entityType = null,
        string format = "CSV")
    {
        try
        {
            var fileBytes = await _auditService.ExportAuditLogsAsync(startTime, endTime, entityType, format);
            
            // 记录审计日志
            await _auditService.LogAsync("ExportAuditLogs", description: $"导出审计日志: {startTime:yyyy-MM-dd} 到 {endTime:yyyy-MM-dd}");
            
            var fileName = $"audit_logs_{startTime:yyyyMMdd}_{endTime:yyyyMMdd}.{format.ToLower()}";
            var contentType = format.ToUpper() switch
            {
                "CSV" => "text/csv",
                "EXCEL" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "JSON" => "application/json",
                _ => "application/octet-stream"
            };

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出审计日志失败");
            return StatusCode(500, new { Success = false, Message = "导出审计日志失败" });
        }
    }

    #endregion
}

/// <summary>
/// 指标请求模型
/// </summary>
public class MetricRequest
{
    /// <summary>
    /// 指标名称
    /// </summary>
    [Required(ErrorMessage = "指标名称不能为空")]
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// 指标值
    /// </summary>
    [Required(ErrorMessage = "指标值不能为空")]
    public double Value { get; set; }

    /// <summary>
    /// 指标类型
    /// </summary>
    public string MetricType { get; set; } = "gauge";

    /// <summary>
    /// 标签
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }
} 