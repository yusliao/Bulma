using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 工艺路线
/// </summary>
[SugarTable("process_route")]
public class ProcessRoute : BaseEntity
{
    /// <summary>
    /// 路线编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "路线编码")]
    public string RouteCode { get; set; } = string.Empty;

    /// <summary>
    /// 路线名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "路线名称")]
    public string RouteName { get; set; } = string.Empty;

    /// <summary>
    /// 产品类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "产品类型")]
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// 路线描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "路线描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 版本号
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Display(Name = "版本号")]
    public string VersionNumber { get; set; } = "1.0";

    /// <summary>
    /// 路线配置（JSON格式，包含工序节点和连接关系）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "路线配置")]
    public string? RouteConfig { get; set; }
}

/// <summary>
/// 工艺路线步骤
/// </summary>
[SugarTable("process_route_step")]
public class ProcessRouteStep : BaseEntity
{
    /// <summary>
    /// 工艺路线ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "工艺路线ID")]
    public long ProcessRouteId { get; set; }

    /// <summary>
    /// 工序ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "工序ID")]
    public long ProcessId { get; set; }

    /// <summary>
    /// 步骤序号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "步骤序号")]
    public int StepOrder { get; set; }

    /// <summary>
    /// 是否必需
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否必需")]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 步骤参数配置（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "步骤参数配置")]
    public string? StepConfig { get; set; }

    /// <summary>
    /// 关联的工序信息
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ProcessId))]
    public Process? Process { get; set; }
} 