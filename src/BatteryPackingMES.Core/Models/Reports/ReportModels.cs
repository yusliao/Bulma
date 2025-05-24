namespace BatteryPackingMES.Core.Models.Reports;

/// <summary>
/// 报表分组方式
/// </summary>
public enum ReportGroupBy
{
    Hour,
    Day,
    Week,
    Month,
    Quarter,
    Year
}

/// <summary>
/// 报表基类
/// </summary>
public abstract class BaseReport
{
    /// <summary>
    /// 报表ID
    /// </summary>
    public string ReportId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 报表名称
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 生成用户
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>
    /// 报表周期开始时间
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 报表周期结束时间
    /// </summary>
    public DateTime EndDate { get; set; }
}

/// <summary>
/// 生产日报
/// </summary>
public class ProductionDailyReport : BaseReport
{
    public ProductionDailyReport()
    {
        ReportName = "生产日报";
    }

    /// <summary>
    /// 产线编号
    /// </summary>
    public string? LineCode { get; set; }

    /// <summary>
    /// 计划产量
    /// </summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// 实际产量
    /// </summary>
    public int ActualQuantity { get; set; }

    /// <summary>
    /// 良品数量
    /// </summary>
    public int QualifiedQuantity { get; set; }

    /// <summary>
    /// 不良品数量
    /// </summary>
    public int DefectiveQuantity { get; set; }

    /// <summary>
    /// 达成率
    /// </summary>
    public decimal CompletionRate => PlannedQuantity > 0 ? (decimal)ActualQuantity / PlannedQuantity * 100 : 0;

    /// <summary>
    /// 良品率
    /// </summary>
    public decimal QualifiedRate => ActualQuantity > 0 ? (decimal)QualifiedQuantity / ActualQuantity * 100 : 0;

    /// <summary>
    /// 按小时统计
    /// </summary>
    public List<HourlyProduction> HourlyProductions { get; set; } = new();

    /// <summary>
    /// 按产品型号统计
    /// </summary>
    public List<ProductModelProduction> ProductModelProductions { get; set; } = new();
}

/// <summary>
/// 小时产量统计
/// </summary>
public class HourlyProduction
{
    /// <summary>
    /// 小时
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// 产量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 良品数量
    /// </summary>
    public int QualifiedQuantity { get; set; }
}

/// <summary>
/// 产品型号产量统计
/// </summary>
public class ProductModelProduction
{
    /// <summary>
    /// 产品型号
    /// </summary>
    public string ProductModel { get; set; } = string.Empty;

    /// <summary>
    /// 计划数量
    /// </summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// 实际数量
    /// </summary>
    public int ActualQuantity { get; set; }

    /// <summary>
    /// 良品数量
    /// </summary>
    public int QualifiedQuantity { get; set; }
}

/// <summary>
/// 质量统计报表
/// </summary>
public class QualityStatisticsReport : BaseReport
{
    public QualityStatisticsReport()
    {
        ReportName = "质量统计报表";
    }

    /// <summary>
    /// 产品型号
    /// </summary>
    public string? ProductModel { get; set; }

    /// <summary>
    /// 总检测数量
    /// </summary>
    public int TotalInspected { get; set; }

    /// <summary>
    /// 良品数量
    /// </summary>
    public int QualifiedQuantity { get; set; }

    /// <summary>
    /// 不良品数量
    /// </summary>
    public int DefectiveQuantity { get; set; }

    /// <summary>
    /// 总良品率
    /// </summary>
    public decimal OverallQualifiedRate => TotalInspected > 0 ? (decimal)QualifiedQuantity / TotalInspected * 100 : 0;

    /// <summary>
    /// 不良类型统计
    /// </summary>
    public List<DefectTypeStatistics> DefectTypeStatistics { get; set; } = new();

    /// <summary>
    /// 按日期统计
    /// </summary>
    public List<DailyQualityStatistics> DailyQualityStatistics { get; set; } = new();
}

/// <summary>
/// 不良类型统计
/// </summary>
public class DefectTypeStatistics
{
    /// <summary>
    /// 不良代码
    /// </summary>
    public string DefectCode { get; set; } = string.Empty;

    /// <summary>
    /// 不良描述
    /// </summary>
    public string DefectDescription { get; set; } = string.Empty;

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 占比
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// 日质量统计
/// </summary>
public class DailyQualityStatistics
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 检测数量
    /// </summary>
    public int InspectedQuantity { get; set; }

    /// <summary>
    /// 良品数量
    /// </summary>
    public int QualifiedQuantity { get; set; }

    /// <summary>
    /// 良品率
    /// </summary>
    public decimal QualifiedRate => InspectedQuantity > 0 ? (decimal)QualifiedQuantity / InspectedQuantity * 100 : 0;
}

/// <summary>
/// 设备效率报表
/// </summary>
public class EquipmentEfficiencyReport : BaseReport
{
    public EquipmentEfficiencyReport()
    {
        ReportName = "设备效率报表";
    }

    /// <summary>
    /// 设备编号
    /// </summary>
    public string? EquipmentCode { get; set; }

    /// <summary>
    /// 平均OEE
    /// </summary>
    public decimal AverageOEE { get; set; }

    /// <summary>
    /// 平均可用率
    /// </summary>
    public decimal AverageAvailability { get; set; }

    /// <summary>
    /// 平均性能率
    /// </summary>
    public decimal AveragePerformance { get; set; }

    /// <summary>
    /// 平均质量率
    /// </summary>
    public decimal AverageQuality { get; set; }

    /// <summary>
    /// 设备效率详情
    /// </summary>
    public List<EquipmentEfficiencyDetail> EquipmentDetails { get; set; } = new();
}

/// <summary>
/// 设备效率详情
/// </summary>
public class EquipmentEfficiencyDetail
{
    /// <summary>
    /// 设备编号
    /// </summary>
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>
    /// 运行时间（分钟）
    /// </summary>
    public int RunningTime { get; set; }

    /// <summary>
    /// 停机时间（分钟）
    /// </summary>
    public int DownTime { get; set; }

    /// <summary>
    /// 可用率
    /// </summary>
    public decimal Availability { get; set; }

    /// <summary>
    /// 性能率
    /// </summary>
    public decimal Performance { get; set; }

    /// <summary>
    /// 质量率
    /// </summary>
    public decimal Quality { get; set; }

    /// <summary>
    /// OEE
    /// </summary>
    public decimal OEE => Availability * Performance * Quality / 10000;
}

/// <summary>
/// 异常分析报表
/// </summary>
public class ExceptionAnalysisReport : BaseReport
{
    public ExceptionAnalysisReport()
    {
        ReportName = "异常分析报表";
    }

    /// <summary>
    /// 告警级别
    /// </summary>
    public string? AlarmLevel { get; set; }

    /// <summary>
    /// 总异常数量
    /// </summary>
    public int TotalExceptions { get; set; }

    /// <summary>
    /// 已解决数量
    /// </summary>
    public int ResolvedExceptions { get; set; }

    /// <summary>
    /// 未解决数量
    /// </summary>
    public int UnresolvedExceptions { get; set; }

    /// <summary>
    /// 平均解决时间（分钟）
    /// </summary>
    public decimal AverageResolutionTime { get; set; }

    /// <summary>
    /// 异常类型统计
    /// </summary>
    public List<ExceptionTypeStatistics> ExceptionTypeStatistics { get; set; } = new();

    /// <summary>
    /// 设备异常统计
    /// </summary>
    public List<EquipmentExceptionStatistics> EquipmentExceptionStatistics { get; set; } = new();
}

/// <summary>
/// 异常类型统计
/// </summary>
public class ExceptionTypeStatistics
{
    /// <summary>
    /// 异常类型
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 占比
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// 平均解决时间
    /// </summary>
    public decimal AverageResolutionTime { get; set; }
}

/// <summary>
/// 设备异常统计
/// </summary>
public class EquipmentExceptionStatistics
{
    /// <summary>
    /// 设备编号
    /// </summary>
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>
    /// 异常数量
    /// </summary>
    public int ExceptionCount { get; set; }

    /// <summary>
    /// 停机时间（分钟）
    /// </summary>
    public int DownTime { get; set; }
}

/// <summary>
/// 产能趋势报表
/// </summary>
public class ProductionTrendReport : BaseReport
{
    public ProductionTrendReport()
    {
        ReportName = "产能趋势报表";
    }

    /// <summary>
    /// 分组方式
    /// </summary>
    public ReportGroupBy GroupBy { get; set; }

    /// <summary>
    /// 趋势数据
    /// </summary>
    public List<ProductionTrendData> TrendData { get; set; } = new();

    /// <summary>
    /// 平均产量
    /// </summary>
    public decimal AverageProduction { get; set; }

    /// <summary>
    /// 最高产量
    /// </summary>
    public int MaxProduction { get; set; }

    /// <summary>
    /// 最低产量
    /// </summary>
    public int MinProduction { get; set; }

    /// <summary>
    /// 产量增长率
    /// </summary>
    public decimal GrowthRate { get; set; }
}

/// <summary>
/// 产能趋势数据
/// </summary>
public class ProductionTrendData
{
    /// <summary>
    /// 时间点
    /// </summary>
    public DateTime TimePoint { get; set; }

    /// <summary>
    /// 时间标签
    /// </summary>
    public string TimeLabel { get; set; } = string.Empty;

    /// <summary>
    /// 计划产量
    /// </summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// 实际产量
    /// </summary>
    public int ActualQuantity { get; set; }

    /// <summary>
    /// 良品数量
    /// </summary>
    public int QualifiedQuantity { get; set; }

    /// <summary>
    /// 达成率
    /// </summary>
    public decimal CompletionRate => PlannedQuantity > 0 ? (decimal)ActualQuantity / PlannedQuantity * 100 : 0;

    /// <summary>
    /// 良品率
    /// </summary>
    public decimal QualifiedRate => ActualQuantity > 0 ? (decimal)QualifiedQuantity / ActualQuantity * 100 : 0;
} 