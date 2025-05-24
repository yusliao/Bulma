using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 系统指标实体
/// </summary>
[SugarTable("system_metrics")]
public class SystemMetric : BaseEntity
{
    /// <summary>
    /// 指标名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "指标名称")]
    [Required(ErrorMessage = "指标名称不能为空")]
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// 指标类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "指标类型")]
    public string MetricType { get; set; } = string.Empty;

    /// <summary>
    /// 指标值
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "指标值")]
    public double Value { get; set; }

    /// <summary>
    /// 测量时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "测量时间")]
    public DateTime MeasuredAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 标签（JSON格式）
    /// </summary>
    [SugarColumn(Length = 2000, IsNullable = true)]
    [Display(Name = "标签")]
    public string? Tags { get; set; }

    /// <summary>
    /// 服务名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "服务名称")]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 环境标识
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "环境标识")]
    public string Environment { get; set; } = string.Empty;
} 