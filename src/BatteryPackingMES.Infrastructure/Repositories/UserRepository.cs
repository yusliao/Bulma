using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Infrastructure.Data;
using SqlSugar;

namespace BatteryPackingMES.Infrastructure.Repositories;

/// <summary>
/// 用户仓库实现
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(MESDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据用户名查找用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>用户</returns>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<User>()
            .Where(u => u.Username == username && !u.IsDeleted)
            .FirstAsync();
    }

    /// <summary>
    /// 根据邮箱查找用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>用户</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<User>()
            .Where(u => u.Email == email && !u.IsDeleted)
            .FirstAsync();
    }

    /// <summary>
    /// 根据刷新令牌查找用户
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>用户</returns>
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<User>()
            .Where(u => u.RefreshToken == refreshToken && !u.IsDeleted)
            .FirstAsync();
    }

    /// <summary>
    /// 获取用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限列表</returns>
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(int userId)
    {
        var db = _dbContext.GetSlaveDb();
        var permissions = await db.Queryable<User>()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .InnerJoin<UserRole>((u, ur) => u.Id == ur.UserId)
            .InnerJoin<Role>((u, ur, r) => ur.RoleId == r.Id && !r.IsDeleted)
            .InnerJoin<RolePermission>((u, ur, r, rp) => r.Id == rp.RoleId)
            .InnerJoin<Permission>((u, ur, r, rp, p) => rp.PermissionId == p.Id && !p.IsDeleted)
            .Select((u, ur, r, rp, p) => p.Code)
            .ToListAsync();

        return permissions.Distinct();
    }

    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限代码</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        var db = _dbContext.GetSlaveDb();
        var hasPermission = await db.Queryable<User>()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .InnerJoin<UserRole>((u, ur) => u.Id == ur.UserId)
            .InnerJoin<Role>((u, ur, r) => ur.RoleId == r.Id && !r.IsDeleted)
            .InnerJoin<RolePermission>((u, ur, r, rp) => r.Id == rp.RoleId)
            .InnerJoin<Permission>((u, ur, r, rp, p) => rp.PermissionId == p.Id && !p.IsDeleted)
            .Where((u, ur, r, rp, p) => p.Code == permissionCode)
            .AnyAsync();

        return hasPermission;
    }
} 