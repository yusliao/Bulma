using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Api.Models;

/// <summary>
/// 质量检测结果DTO
/// </summary>
public class QualityInspectionResultDto
{
    /// <summary>
    /// 序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 质量等级
    /// </summary>
    public QualityGrade QualityGrade { get; set; }

    /// <summary>
    /// 质量备注
    /// </summary>
    public string? QualityNotes { get; set; }

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime InspectionTime { get; set; }

    /// <summary>
    /// 检查员
    /// </summary>
    public string Inspector { get; set; } = string.Empty;

    /// <summary>
    /// 追溯记录ID
    /// </summary>
    public long TraceabilityId { get; set; }
}

/// <summary>
/// 产品追溯DTO
/// </summary>
public class ProductTraceabilityDto
{
    /// <summary>
    /// 产品信息
    /// </summary>
    public ProductInfoDto ProductInfo { get; set; } = new();

    /// <summary>
    /// 批次信息
    /// </summary>
    public BatchInfoDto? BatchInfo { get; set; }

    /// <summary>
    /// 追溯记录列表
    /// </summary>
    public List<TraceabilityRecordDto> TraceabilityRecords { get; set; } = new();
}

/// <summary>
/// 产品信息DTO
/// </summary>
public class ProductInfoDto
{
    /// <summary>
    /// 序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 条码
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 产品类型
    /// </summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// 生产日期
    /// </summary>
    public DateTime ProductionDate { get; set; }

    /// <summary>
    /// 质量等级
    /// </summary>
    public QualityGrade QualityGrade { get; set; }

    /// <summary>
    /// 质量备注
    /// </summary>
    public string? QualityNotes { get; set; }

    /// <summary>
    /// 产品状态
    /// </summary>
    public ProductItemStatus ItemStatus { get; set; }
}

/// <summary>
/// 批次信息DTO
/// </summary>
public class BatchInfoDto
{
    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 产品类型
    /// </summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// 计划数量
    /// </summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// 实际数量
    /// </summary>
    public int ActualQuantity { get; set; }

    /// <summary>
    /// 批次状态
    /// </summary>
    public BatchStatus BatchStatus { get; set; }

    /// <summary>
    /// 工作订单
    /// </summary>
    public string? WorkOrder { get; set; }

    /// <summary>
    /// 客户订单
    /// </summary>
    public string? CustomerOrder { get; set; }
}

/// <summary>
/// 追溯记录DTO
/// </summary>
public class TraceabilityRecordDto
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 工序步骤
    /// </summary>
    public string ProcessStep { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作结果
    /// </summary>
    public string? OperationResult { get; set; }

    /// <summary>
    /// 操作员
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// 工作站代码
    /// </summary>
    public string? WorkstationCode { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; }

    /// <summary>
    /// 测试参数
    /// </summary>
    public string? TestParameters { get; set; }

    /// <summary>
    /// 质量参数
    /// </summary>
    public string? QualityParameters { get; set; }

    /// <summary>
    /// 环境数据
    /// </summary>
    public string? EnvironmentalData { get; set; }

    /// <summary>
    /// 是否质量检查点
    /// </summary>
    public bool IsQualityCheckpoint { get; set; }
}

/// <summary>
/// 批次质量统计DTO
/// </summary>
public class BatchQualityStatisticsDto
{
    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 总产品数
    /// </summary>
    public int TotalProducts { get; set; }

    /// <summary>
    /// A级产品数
    /// </summary>
    public int GradeACount { get; set; }

    /// <summary>
    /// B级产品数
    /// </summary>
    public int GradeBCount { get; set; }

    /// <summary>
    /// C级产品数
    /// </summary>
    public int GradeCCount { get; set; }

    /// <summary>
    /// 不合格产品数
    /// </summary>
    public int DefectiveCount { get; set; }

    /// <summary>
    /// 未分级产品数
    /// </summary>
    public int UngradedCount { get; set; }

    /// <summary>
    /// 质量合格率（%）
    /// </summary>
    public double QualityRate { get; set; }

    /// <summary>
    /// 不合格率（%）
    /// </summary>
    public double DefectiveRate { get; set; }

    /// <summary>
    /// 完成率（%）
    /// </summary>
    public double CompletionRate { get; set; }
}

/// <summary>
/// 不合格品处理结果DTO
/// </summary>
public class DefectiveHandlingResultDto
{
    /// <summary>
    /// 序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 处理方式
    /// </summary>
    public DefectiveHandlingAction HandlingAction { get; set; }

    /// <summary>
    /// 新状态
    /// </summary>
    public ProductItemStatus NewStatus { get; set; }

    /// <summary>
    /// 处理人员
    /// </summary>
    public string HandledBy { get; set; } = string.Empty;

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTime HandledTime { get; set; }

    /// <summary>
    /// 追溯记录ID
    /// </summary>
    public long TraceabilityId { get; set; }
}

/// <summary>
/// 质量报表DTO
/// </summary>
public class QualityReportDto
{
    /// <summary>
    /// 报表期间
    /// </summary>
    public ReportPeriodDto ReportPeriod { get; set; } = new();

    /// <summary>
    /// 质量汇总
    /// </summary>
    public QualitySummaryDto Summary { get; set; } = new();

    /// <summary>
    /// 等级分布
    /// </summary>
    public Dictionary<QualityGrade, int> GradeDistribution { get; set; } = new();

    /// <summary>
    /// 每日趋势
    /// </summary>
    public List<DailyQualityTrendDto> DailyTrends { get; set; } = new();
}

/// <summary>
/// 报表期间DTO
/// </summary>
public class ReportPeriodDto
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 产品类型
    /// </summary>
    public string? ProductType { get; set; }
}

/// <summary>
/// 质量汇总DTO
/// </summary>
public class QualitySummaryDto
{
    /// <summary>
    /// 总产品数
    /// </summary>
    public int TotalProducts { get; set; }

    /// <summary>
    /// 合格产品数
    /// </summary>
    public int QualifiedProducts { get; set; }

    /// <summary>
    /// 不合格产品数
    /// </summary>
    public int DefectiveProducts { get; set; }

    /// <summary>
    /// 质量合格率（%）
    /// </summary>
    public double QualityRate { get; set; }

    /// <summary>
    /// 不合格率（%）
    /// </summary>
    public double DefectiveRate { get; set; }
}

/// <summary>
/// 每日质量趋势DTO
/// </summary>
public class DailyQualityTrendDto
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 总产品数
    /// </summary>
    public int TotalProducts { get; set; }

    /// <summary>
    /// 合格产品数
    /// </summary>
    public int QualifiedProducts { get; set; }

    /// <summary>
    /// 不合格产品数
    /// </summary>
    public int DefectiveProducts { get; set; }

    /// <summary>
    /// 质量合格率（%）
    /// </summary>
    public double QualityRate { get; set; }
} 