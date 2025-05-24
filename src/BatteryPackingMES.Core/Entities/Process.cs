using SqlSugar;
using System.ComponentModel.DataAnnotations;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 工序信息
/// </summary>
[SugarTable("process")]
public class Process : BaseEntity
{
    /// <summary>
    /// 工序编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "工序编码")]
    public string ProcessCode { get; set; } = string.Empty;

    /// <summary>
    /// 工序名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "工序名称")]
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 工序类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "工序类型")]
    public ProcessType ProcessType { get; set; }

    /// <summary>
    /// 工序描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "工序描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 标准工时（分钟）
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "标准工时")]
    public decimal StandardTime { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "排序号")]
    public int SortOrder { get; set; }

    /// <summary>
    /// 工艺参数配置（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "工艺参数配置")]
    public string? ParameterConfig { get; set; }
} 