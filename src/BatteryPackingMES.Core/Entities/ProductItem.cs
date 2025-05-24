using SqlSugar;
using System.ComponentModel.DataAnnotations;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 产品项实体 - 单个产品追溯
/// </summary>
[SugarTable("product_items")]
public class ProductItem : BaseEntity
{
    /// <summary>
    /// 产品序列号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "产品序列号")]
    [Required(ErrorMessage = "产品序列号不能为空")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 产品条码
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "产品条码")]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 批次ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "批次ID")]
    public long BatchId { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "批次号")]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 产品型号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "产品型号")]
    public string? ProductModel { get; set; }

    /// <summary>
    /// 产品类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "产品类型")]
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// 生产日期
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "生产日期")]
    public DateTime ProductionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 产品项状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "产品项状态")]
    public ProductItemStatus ItemStatus { get; set; } = ProductItemStatus.Created;

    /// <summary>
    /// 质量等级
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "质量等级")]
    public QualityGrade QualityGrade { get; set; } = QualityGrade.Ungraded;

    /// <summary>
    /// 质量备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "质量备注")]
    public string? QualityNotes { get; set; }

    /// <summary>
    /// 父级产品序列号（用于组装关系）
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "父级序列号")]
    public string? ParentSerialNumber { get; set; }

    /// <summary>
    /// 组装位置（在父级产品中的位置）
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "组装位置")]
    public string? AssemblyPosition { get; set; }

    /// <summary>
    /// 生产开始时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "生产开始时间")]
    public DateTime? ProductionStartTime { get; set; }

    /// <summary>
    /// 生产完成时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "生产完成时间")]
    public DateTime? ProductionEndTime { get; set; }

    /// <summary>
    /// 当前工序ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "当前工序ID")]
    public long? CurrentProcessId { get; set; }

    /// <summary>
    /// 当前工作站
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "当前工作站")]
    public string? CurrentWorkstation { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "操作员ID")]
    public long? OperatorId { get; set; }

    /// <summary>
    /// 最后检测时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "最后检测时间")]
    public DateTime? LastInspectionTime { get; set; }

    /// <summary>
    /// 检测结果（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "检测结果")]
    public string? InspectionResults { get; set; }

    /// <summary>
    /// 客户编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "客户编码")]
    public string? CustomerCode { get; set; }

    /// <summary>
    /// 客户订单号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "客户订单号")]
    public string? CustomerOrderNumber { get; set; }

    /// <summary>
    /// 出货时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "出货时间")]
    public DateTime? ShippedTime { get; set; }

    /// <summary>
    /// 出货批次号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "出货批次号")]
    public string? ShippingBatchNumber { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 是否返工品
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否返工品")]
    public bool IsReworked { get; set; } = false;

    /// <summary>
    /// 返工次数
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "返工次数")]
    public int ReworkCount { get; set; } = 0;

    /// <summary>
    /// 导航属性 - 生产批次
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(BatchId))]
    public ProductionBatch? ProductionBatch { get; set; }

    /// <summary>
    /// 导航属性 - 当前工序
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(CurrentProcessId))]
    public Process? CurrentProcess { get; set; }

    /// <summary>
    /// 导航属性 - 操作员
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(OperatorId))]
    public User? Operator { get; set; }
} 