using BatteryPackingMES.Core.Entities;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>认证结果</returns>
    Task<AuthenticationResult> LoginAsync(string username, string password, string ipAddress);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>认证结果</returns>
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, string ipAddress);

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns></returns>
    Task LogoutAsync(string refreshToken);

    /// <summary>
    /// 验证令牌
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>用户信息</returns>
    Task<User?> ValidateTokenAsync(string token);

    /// <summary>
    /// 生成密码哈希
    /// </summary>
    /// <param name="password">密码</param>
    /// <param name="salt">盐值</param>
    /// <returns>密码哈希</returns>
    string HashPassword(string password, string salt);

    /// <summary>
    /// 生成盐值
    /// </summary>
    /// <returns>盐值</returns>
    string GenerateSalt();

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">密码</param>
    /// <param name="hash">密码哈希</param>
    /// <param name="salt">盐值</param>
    /// <returns>是否匹配</returns>
    bool VerifyPassword(string password, string hash, string salt);
}

/// <summary>
/// 认证结果
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// JWT令牌
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? TokenExpiry { get; set; }
} 