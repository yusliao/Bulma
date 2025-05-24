using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 生产参数记录
/// </summary>
[SugarTable("production_parameter")]
public class ProductionParameter : BaseEntity
{
    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "批次号")]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 工序ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "工序ID")]
    public long ProcessId { get; set; }

    /// <summary>
    /// 参数名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "参数名称")]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    [Display(Name = "参数值")]
    public string ParameterValue { get; set; } = string.Empty;

    /// <summary>
    /// 参数单位
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "参数单位")]
    public string? Unit { get; set; }

    /// <summary>
    /// 上限值
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "上限值")]
    public decimal? UpperLimit { get; set; }

    /// <summary>
    /// 下限值
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "下限值")]
    public decimal? LowerLimit { get; set; }

    /// <summary>
    /// 是否合格
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否合格")]
    public bool IsQualified { get; set; } = true;

    /// <summary>
    /// 采集时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "采集时间")]
    public DateTime CollectTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 设备编号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "设备编号")]
    public string? EquipmentCode { get; set; }

    /// <summary>
    /// 操作员
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "操作员")]
    public string? Operator { get; set; }

    /// <summary>
    /// 关联的工序信息
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ProcessId))]
    public Process? Process { get; set; }
}

/// <summary>
/// 高频参数数据（按月分表）
/// </summary>
[SugarTable("high_frequency_parameter")]
[SplitTable(SplitType.Month)]
public class HighFrequencyParameter
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 工序ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long ProcessId { get; set; }

    /// <summary>
    /// 参数名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string ParameterValue { get; set; } = string.Empty;

    /// <summary>
    /// 采集时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CollectTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 设备编号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? EquipmentCode { get; set; }
} 