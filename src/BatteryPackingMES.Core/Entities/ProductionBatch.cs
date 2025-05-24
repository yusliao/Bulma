using SqlSugar;
using System.ComponentModel.DataAnnotations;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 生产批次
/// </summary>
[SugarTable("production_batch")]
public class ProductionBatch : BaseEntity
{
    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "批次号")]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 产品类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "产品类型")]
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// 产品编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "产品编码")]
    public string? ProductCode { get; set; }

    /// <summary>
    /// 产品名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "产品名称")]
    public string? ProductName { get; set; }

    /// <summary>
    /// 工艺路线ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "工艺路线ID")]
    public long? ProcessRouteId { get; set; }

    /// <summary>
    /// 计划数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "计划数量")]
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// 实际数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "实际数量")]
    public int ActualQuantity { get; set; } = 0;

    /// <summary>
    /// 已完成数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "已完成数量")]
    public int CompletedQuantity { get; set; } = 0;

    /// <summary>
    /// 合格数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "合格数量")]
    public int QualifiedQuantity { get; set; } = 0;

    /// <summary>
    /// 批次状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "批次状态")]
    public BatchStatus BatchStatus { get; set; } = BatchStatus.Created;

    /// <summary>
    /// 优先级
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "优先级")]
    public BatchPriority Priority { get; set; } = BatchPriority.Normal;

    /// <summary>
    /// 计划开始时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "计划开始时间")]
    public DateTime? PlannedStartTime { get; set; }

    /// <summary>
    /// 计划结束时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "计划结束时间")]
    public DateTime? PlannedEndTime { get; set; }

    /// <summary>
    /// 实际开始时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "实际开始时间")]
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// 实际结束时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "实际结束时间")]
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// 工作订单
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "工作订单")]
    public string? WorkOrder { get; set; }

    /// <summary>
    /// 客户订单
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "客户订单")]
    public string? CustomerOrder { get; set; }

    /// <summary>
    /// 产品规格
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "产品规格")]
    public string? ProductSpecification { get; set; }

    /// <summary>
    /// 质量要求
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "质量要求")]
    public string? QualityRequirements { get; set; }

    /// <summary>
    /// 质量结果
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "质量结果")]
    public string? QualityResult { get; set; }

    /// <summary>
    /// 当前工序ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "当前工序ID")]
    public long? CurrentProcessId { get; set; }

    /// <summary>
    /// 当前操作员
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "当前操作员")]
    public string? CurrentOperator { get; set; }

    /// <summary>
    /// 当前工作站
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "当前工作站")]
    public string? CurrentWorkstation { get; set; }

    /// <summary>
    /// 加工备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "加工备注")]
    public string? ProcessingNotes { get; set; }

    /// <summary>
    /// 完成备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "完成备注")]
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// 状态原因
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    [Display(Name = "状态原因")]
    public string? StatusReason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 关联的工艺路线
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ProcessRouteId))]
    public ProcessRoute? ProcessRoute { get; set; }
} 