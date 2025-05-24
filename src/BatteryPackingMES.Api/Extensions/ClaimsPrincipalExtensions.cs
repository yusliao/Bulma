using System.Security.Claims;

namespace BatteryPackingMES.Api.Extensions;

/// <summary>
/// ClaimsPrincipal 扩展方法
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 获取用户ID
    /// </summary>
    /// <param name="principal">用户主体</param>
    /// <returns>用户ID</returns>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// 获取用户ID（长整型）
    /// </summary>
    /// <param name="principal">用户主体</param>
    /// <returns>用户ID</returns>
    public static long? GetUserIdAsLong(this ClaimsPrincipal principal)
    {
        var userIdStr = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdStr, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// 获取用户名
    /// </summary>
    /// <param name="principal">用户主体</param>
    /// <returns>用户名</returns>
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Name)?.Value ?? principal?.Identity?.Name;
    }
} 