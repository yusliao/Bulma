using BatteryPackingMES.Core.Entities;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 用户仓库接口
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// 根据用户名查找用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>用户</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// 根据邮箱查找用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>用户</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// 根据刷新令牌查找用户
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>用户</returns>
    Task<User?> GetByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 获取用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限列表</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(int userId);

    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限代码</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasPermissionAsync(int userId, string permissionCode);
} 