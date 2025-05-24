using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 盐值
    /// </summary>
    [Required]
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(50)]
    public string? RealName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录IP
    /// </summary>
    [MaxLength(45)] // IPv6 最大长度
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 登录失败次数
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// 账户锁定到期时间
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// 用户角色关联
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
} 