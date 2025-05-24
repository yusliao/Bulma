using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 审计日志实体
/// </summary>
[SugarTable("audit_logs")]
public class AuditLog : BaseEntity
{
    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "操作类型")]
    public string ActionType { get; set; } = string.Empty; // Create, Update, Delete, Read, Login, Logout

    /// <summary>
    /// 实体类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "实体类型")]
    public string? EntityType { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "实体ID")]
    public long? EntityId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "用户ID")]
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "用户名")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// IP地址
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "IP地址")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "用户代理")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "操作描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 旧值（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "旧值")]
    public string? OldValues { get; set; }

    /// <summary>
    /// 新值（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "新值")]
    public string? NewValues { get; set; }

    /// <summary>
    /// 变更字段列表
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "变更字段")]
    public string? ChangedFields { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "操作时间")]
    public DateTime ActionTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "会话ID")]
    public string? SessionId { get; set; }

    /// <summary>
    /// 业务模块
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "业务模块")]
    public string? Module { get; set; }

    /// <summary>
    /// 操作结果
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Display(Name = "操作结果")]
    public string Result { get; set; } = "Success"; // Success, Failed, Warning

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(Length = 2000, IsNullable = true)]
    [Display(Name = "错误信息")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作耗时（毫秒）
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "操作耗时")]
    public long? Duration { get; set; }

    /// <summary>
    /// 风险等级
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Display(Name = "风险等级")]
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High, Critical

    /// <summary>
    /// 合规标记
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "合规标记")]
    public string? ComplianceTag { get; set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? User { get; set; }
}

/// <summary>
/// 审计配置实体
/// </summary>
[SugarTable("audit_configurations")]
public class AuditConfiguration : BaseEntity
{
    /// <summary>
    /// 实体类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "实体类型")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用审计
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用审计")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 审计创建操作
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "审计创建操作")]
    public bool AuditCreate { get; set; } = true;

    /// <summary>
    /// 审计更新操作
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "审计更新操作")]
    public bool AuditUpdate { get; set; } = true;

    /// <summary>
    /// 审计删除操作
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "审计删除操作")]
    public bool AuditDelete { get; set; } = true;

    /// <summary>
    /// 审计查询操作
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "审计查询操作")]
    public bool AuditRead { get; set; } = false;

    /// <summary>
    /// 保留天数
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "保留天数")]
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    /// 排除字段列表（JSON格式）
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "排除字段")]
    public string? ExcludedFields { get; set; }

    /// <summary>
    /// 敏感字段列表（JSON格式）
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "敏感字段")]
    public string? SensitiveFields { get; set; }
} 