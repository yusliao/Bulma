using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 产品追溯记录
/// </summary>
[SugarTable("product_traceability")]
public class ProductTraceability : BaseEntity
{
    /// <summary>
    /// 产品项ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "产品项ID")]
    public long ProductItemId { get; set; }

    /// <summary>
    /// 产品序列号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "产品序列号")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "批次号")]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 工序步骤
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "工序步骤")]
    public string ProcessStep { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "操作类型")]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作结果
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "操作结果")]
    public string? OperationResult { get; set; }

    /// <summary>
    /// 操作员
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "操作员")]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// 工作站代码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "工作站代码")]
    public string? WorkstationCode { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "操作时间")]
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 测试参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "测试参数")]
    public string? TestParameters { get; set; }

    /// <summary>
    /// 质量参数
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "质量参数")]
    public string? QualityParameters { get; set; }

    /// <summary>
    /// 环境数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "环境数据")]
    public string? EnvironmentalData { get; set; }

    /// <summary>
    /// 是否质量检查点
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否质量检查点")]
    public bool IsQualityCheckpoint { get; set; } = false;

    /// <summary>
    /// 设备编号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "设备编号")]
    public string? EquipmentCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 产品项
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ProductItemId))]
    public ProductItem? ProductItem { get; set; }
} 