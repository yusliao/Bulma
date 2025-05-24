using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 本地化资源实体
/// </summary>
public class LocalizationResource : BaseEntity
{
    /// <summary>
    /// 资源键
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 语言代码 (如: zh-CN, en-US, ja-JP)
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源值
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 资源分类 (如: Common, Messages, Errors, etc.)
    /// </summary>
    [MaxLength(100)]
    public string Category { get; set; } = "Common";

    /// <summary>
    /// 描述/备注
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否已审核
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// 审核人ID
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>
    /// 翻译人ID
    /// </summary>
    public int? TranslatedBy { get; set; }

    /// <summary>
    /// 翻译时间
    /// </summary>
    public DateTimeOffset? TranslatedAt { get; set; }

    /// <summary>
    /// 是否是自动翻译
    /// </summary>
    public bool IsAutoTranslated { get; set; } = false;

    /// <summary>
    /// 翻译质量评分 (1-5)
    /// </summary>
    public int QualityScore { get; set; } = 0;

    /// <summary>
    /// 导航属性 - 审核人
    /// </summary>
    public virtual User? Approver { get; set; }

    /// <summary>
    /// 导航属性 - 翻译人
    /// </summary>
    public virtual User? Translator { get; set; }
}

/// <summary>
/// 支持的语言配置
/// </summary>
public class SupportedLanguageConfig : BaseEntity
{
    /// <summary>
    /// 语言代码
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// 语言名称（英文）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 语言名称（本地语言）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string NativeName { get; set; } = string.Empty;

    /// <summary>
    /// 国旗图标
    /// </summary>
    [MaxLength(50)]
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// 是否从右到左书写
    /// </summary>
    public bool IsRightToLeft { get; set; } = false;

    /// <summary>
    /// 是否默认语言
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// 翻译完成度百分比
    /// </summary>
    public decimal CompletionPercentage { get; set; } = 0;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTimeOffset LastTranslationUpdate { get; set; }

    /// <summary>
    /// 货币代码
    /// </summary>
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// 日期格式
    /// </summary>
    [MaxLength(50)]
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// 时间格式
    /// </summary>
    [MaxLength(50)]
    public string TimeFormat { get; set; } = "HH:mm:ss";

    /// <summary>
    /// 数字格式
    /// </summary>
    [MaxLength(50)]
    public string NumberFormat { get; set; } = "#,##0.##";
} 