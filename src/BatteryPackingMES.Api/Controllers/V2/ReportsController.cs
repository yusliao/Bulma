using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 报表控制器
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
// [Authorize] // 暂时注释掉用于测试
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// 生成生产日报
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="lineCode">产线编号</param>
    /// <returns></returns>
    [HttpGet("production-daily")]
    public async Task<IActionResult> GetProductionDailyReportAsync([FromQuery] DateTime date, [FromQuery] string? lineCode = null)
    {
        try
        {
            var report = await _reportService.GenerateProductionDailyReportAsync(date, lineCode);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成生产日报失败");
            return StatusCode(500, new { success = false, message = "生成报表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 生成质量统计报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="productModel">产品型号</param>
    /// <returns></returns>
    [HttpGet("quality-statistics")]
    public async Task<IActionResult> GetQualityStatisticsReportAsync(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        [FromQuery] string? productModel = null)
    {
        try
        {
            var report = await _reportService.GenerateQualityStatisticsReportAsync(startDate, endDate, productModel);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成质量统计报表失败");
            return StatusCode(500, new { success = false, message = "生成报表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 生成设备效率报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="equipmentCode">设备编号</param>
    /// <returns></returns>
    [HttpGet("equipment-efficiency")]
    public async Task<IActionResult> GetEquipmentEfficiencyReportAsync(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        [FromQuery] string? equipmentCode = null)
    {
        try
        {
            var report = await _reportService.GenerateEquipmentEfficiencyReportAsync(startDate, endDate, equipmentCode);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成设备效率报表失败");
            return StatusCode(500, new { success = false, message = "生成报表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 生成异常分析报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="alarmLevel">告警级别</param>
    /// <returns></returns>
    [HttpGet("exception-analysis")]
    public async Task<IActionResult> GetExceptionAnalysisReportAsync(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        [FromQuery] string? alarmLevel = null)
    {
        try
        {
            var report = await _reportService.GenerateExceptionAnalysisReportAsync(startDate, endDate, alarmLevel);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成异常分析报表失败");
            return StatusCode(500, new { success = false, message = "生成报表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 生成产能趋势报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="groupBy">分组方式</param>
    /// <returns></returns>
    [HttpGet("production-trend")]
    public async Task<IActionResult> GetProductionTrendReportAsync(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        [FromQuery] ReportGroupBy groupBy = ReportGroupBy.Day)
    {
        try
        {
            var report = await _reportService.GenerateProductionTrendReportAsync(startDate, endDate, groupBy);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成产能趋势报表失败");
            return StatusCode(500, new { success = false, message = "生成报表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 导出生产日报到Excel
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="lineCode">产线编号</param>
    /// <returns></returns>
    [HttpGet("production-daily/export/excel")]
    public async Task<IActionResult> ExportProductionDailyReportToExcelAsync([FromQuery] DateTime date, [FromQuery] string? lineCode = null)
    {
        try
        {
            var report = await _reportService.GenerateProductionDailyReportAsync(date, lineCode);
            var fileName = $"生产日报_{date:yyyy-MM-dd}_{lineCode ?? "全部"}.xlsx";
            var fileContent = await _reportService.ExportToExcelAsync(report, fileName);

            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出生产日报Excel失败");
            return StatusCode(500, new { success = false, message = "导出失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 导出质量统计报表到PDF
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="productModel">产品型号</param>
    /// <returns></returns>
    [HttpGet("quality-statistics/export/pdf")]
    public async Task<IActionResult> ExportQualityStatisticsReportToPdfAsync(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        [FromQuery] string? productModel = null)
    {
        try
        {
            var report = await _reportService.GenerateQualityStatisticsReportAsync(startDate, endDate, productModel);
            var fileName = $"质量统计报表_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.pdf";
            var fileContent = await _reportService.ExportToPdfAsync(report, fileName);

            return File(fileContent, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出质量统计报表PDF失败");
            return StatusCode(500, new { success = false, message = "导出失败", error = ex.Message });
        }
    }
} 