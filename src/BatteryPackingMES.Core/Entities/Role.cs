using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 角色实体
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// 角色名称
    /// </summary>
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否系统内置角色
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// 用户角色关联
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 角色权限关联
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
} 