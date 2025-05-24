using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 权限实体
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// 权限名称
    /// </summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 权限代码 (用于程序中判断)
    /// </summary>
    [Required, MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 权限模块
    /// </summary>
    [MaxLength(50)]
    public string? Module { get; set; }

    /// <summary>
    /// 是否系统内置权限
    /// </summary>
    public bool IsSystemPermission { get; set; } = false;

    /// <summary>
    /// 角色权限关联
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
} 