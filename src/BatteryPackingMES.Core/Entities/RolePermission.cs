namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 角色权限关联实体
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// 关联的角色
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// 关联的权限
    /// </summary>
    public virtual Permission Permission { get; set; } = null!;
} 