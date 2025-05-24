using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 数据验证规则实体
/// </summary>
[SugarTable("data_validation_rules")]
public class DataValidationRule : BaseEntity
{
    /// <summary>
    /// 规则名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "规则名称")]
    [Required(ErrorMessage = "规则名称不能为空")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "实体类型")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 字段名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "字段名称")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 验证类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "验证类型")]
    public string ValidationType { get; set; } = string.Empty;

    /// <summary>
    /// 验证表达式
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = false)]
    [Display(Name = "验证表达式")]
    public string ValidationExpression { get; set; } = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = false)]
    [Display(Name = "错误消息")]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Display(Name = "严重程度")]
    public string Severity { get; set; } = "Error"; // Error, Warning, Info

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 规则描述
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "规则描述")]
    public string? Description { get; set; }
}

/// <summary>
/// 数据验证结果实体
/// </summary>
[SugarTable("data_validation_results")]
public class DataValidationResult : BaseEntity
{
    /// <summary>
    /// 验证规则ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "验证规则ID")]
    public long RuleId { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "实体ID")]
    public long EntityId { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "实体类型")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 验证状态
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Display(Name = "验证状态")]
    public string Status { get; set; } = string.Empty; // Passed, Failed, Warning

    /// <summary>
    /// 验证消息
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "验证消息")]
    public string? Message { get; set; }

    /// <summary>
    /// 验证时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "验证时间")]
    public DateTime ValidatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 验证值
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "验证值")]
    public string? ValidatedValue { get; set; }

    /// <summary>
    /// 导航属性 - 验证规则
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public DataValidationRule? Rule { get; set; }
} 