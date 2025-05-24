namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 用户角色关联实体
/// </summary>
public class UserRole : BaseEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 关联的用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 关联的角色
    /// </summary>
    public virtual Role Role { get; set; } = null!;
} 