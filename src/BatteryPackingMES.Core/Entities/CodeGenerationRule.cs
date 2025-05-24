using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 编码生成规则实体
/// </summary>
[SugarTable("code_generation_rules")]
public class CodeGenerationRule : BaseEntity
{
    /// <summary>
    /// 规则名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "规则名称")]
    [Required(ErrorMessage = "规则名称不能为空")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 编码类型 (BatchCode, SerialNumber, Barcode)
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "编码类型")]
    public string CodeType { get; set; } = string.Empty;

    /// <summary>
    /// 产品类型 (Cell, Module, Pack, Pallet)
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "产品类型")]
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// 编码前缀
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "编码前缀")]
    public string? Prefix { get; set; }

    /// <summary>
    /// 编码后缀
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "编码后缀")]
    public string? Suffix { get; set; }

    /// <summary>
    /// 日期格式 (如: yyyyMMdd, yyyy-MM-dd)
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "日期格式")]
    public string? DateFormat { get; set; }

    /// <summary>
    /// 序号长度
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "序号长度")]
    public int SequenceLength { get; set; } = 4;

    /// <summary>
    /// 序号起始值
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "序号起始值")]
    public int StartNumber { get; set; } = 1;

    /// <summary>
    /// 是否按日重置序号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否按日重置")]
    public bool ResetDaily { get; set; } = true;

    /// <summary>
    /// 是否按月重置序号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否按月重置")]
    public bool ResetMonthly { get; set; } = false;

    /// <summary>
    /// 是否按年重置序号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否按年重置")]
    public bool ResetYearly { get; set; } = false;

    /// <summary>
    /// 当前序号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "当前序号")]
    public int CurrentSequence { get; set; } = 0;

    /// <summary>
    /// 最后生成日期
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "最后生成日期")]
    public DateTime? LastGeneratedDate { get; set; }

    /// <summary>
    /// 编码模板 (如: {Prefix}{Date}{Sequence}{Suffix})
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    [Display(Name = "编码模板")]
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// 验证规则 (正则表达式)
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "验证规则")]
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 规则描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "规则描述")]
    public string? Description { get; set; }
}

/// <summary>
/// 编码生成历史记录
/// </summary>
[SugarTable("code_generation_history")]
public class CodeGenerationHistory : BaseEntity
{
    /// <summary>
    /// 规则ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "规则ID")]
    public long RuleId { get; set; }

    /// <summary>
    /// 生成的编码
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "生成编码")]
    public string GeneratedCode { get; set; } = string.Empty;

    /// <summary>
    /// 编码类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "编码类型")]
    public string CodeType { get; set; } = string.Empty;

    /// <summary>
    /// 关联实体类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "关联实体类型")]
    public string? EntityType { get; set; }

    /// <summary>
    /// 关联实体ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "关联实体ID")]
    public long? EntityId { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "生成时间")]
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 生成用户ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "生成用户ID")]
    public long? GeneratedBy { get; set; }

    /// <summary>
    /// 生成源 (Manual, Auto, Import)
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "生成源")]
    public string GenerationSource { get; set; } = "Auto";

    /// <summary>
    /// 是否已使用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否已使用")]
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// 使用时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "使用时间")]
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// 导航属性 - 生成规则
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(RuleId))]
    public CodeGenerationRule? Rule { get; set; }

    /// <summary>
    /// 导航属性 - 生成用户
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(GeneratedBy))]
    public User? GeneratedByUser { get; set; }
} 