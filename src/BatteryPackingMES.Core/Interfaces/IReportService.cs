using BatteryPackingMES.Core.Models.Reports;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 报表服务接口
/// </summary>
public interface IReportService
{
    /// <summary>
    /// 生成生产日报
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="lineCode">产线编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<ProductionDailyReport> GenerateProductionDailyReportAsync(DateTime date, string? lineCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成质量统计报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="productModel">产品型号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<QualityStatisticsReport> GenerateQualityStatisticsReportAsync(DateTime startDate, DateTime endDate, string? productModel = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成设备效率报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="equipmentCode">设备编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<EquipmentEfficiencyReport> GenerateEquipmentEfficiencyReportAsync(DateTime startDate, DateTime endDate, string? equipmentCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成异常分析报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="alarmLevel">告警级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<ExceptionAnalysisReport> GenerateExceptionAnalysisReportAsync(DateTime startDate, DateTime endDate, string? alarmLevel = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成产能趋势报表
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="groupBy">分组方式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<ProductionTrendReport> GenerateProductionTrendReportAsync(DateTime startDate, DateTime endDate, ReportGroupBy groupBy = ReportGroupBy.Day, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出报表到Excel
    /// </summary>
    /// <typeparam name="T">报表类型</typeparam>
    /// <param name="report">报表数据</param>
    /// <param name="fileName">文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<byte[]> ExportToExcelAsync<T>(T report, string fileName, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 导出报表到PDF
    /// </summary>
    /// <typeparam name="T">报表类型</typeparam>
    /// <param name="report">报表数据</param>
    /// <param name="fileName">文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<byte[]> ExportToPdfAsync<T>(T report, string fileName, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 获取报表缓存
    /// </summary>
    /// <typeparam name="T">报表类型</typeparam>
    /// <param name="reportType">报表类型</param>
    /// <param name="parameters">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<T?> GetCachedReportAsync<T>(string reportType, Dictionary<string, object> parameters, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 设置报表缓存
    /// </summary>
    /// <typeparam name="T">报表类型</typeparam>
    /// <param name="reportType">报表类型</param>
    /// <param name="parameters">参数</param>
    /// <param name="report">报表数据</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task SetCachedReportAsync<T>(string reportType, Dictionary<string, object> parameters, T report, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
} 