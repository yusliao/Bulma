using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>认证结果</returns>
    public async Task<AuthenticationResult> LoginAsync(string username, string password, string ipAddress)
    {
        try
        {
            // 查找用户
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent username: {Username}", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "用户名或密码错误"
                };
            }

            // 检查账户状态
            if (!user.IsEnabled)
            {
                _logger.LogWarning("Login attempt for disabled user: {Username}", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "账户已被禁用"
                };
            }

            // 检查账户锁定
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt for locked user: {Username}", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"账户已被锁定至 {user.LockoutEnd.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                };
            }

            // 验证密码
            if (!VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                // 增加失败次数
                user.FailedLoginAttempts++;
                
                // 锁定账户（5次失败后锁定30分钟）
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning("User {Username} locked due to too many failed login attempts", username);
                }

                await _userRepository.UpdateAsync(user);

                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "用户名或密码错误"
                };
            }

            // 登录成功，重置失败次数
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginTime = DateTime.UtcNow;
            user.LastLoginIp = ipAddress;

            // 生成令牌
            var jwtToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Username} logged in successfully from {IpAddress}", username, ipAddress);

            return new AuthenticationResult
            {
                Success = true,
                User = user,
                Token = jwtToken,
                RefreshToken = refreshToken,
                TokenExpiry = DateTime.UtcNow.AddHours(int.Parse(_configuration["Jwt:ExpirationHours"] ?? "24"))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", username);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "登录过程中发生错误"
            };
        }
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>认证结果</returns>
    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        try
        {
            var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "无效的刷新令牌"
                };
            }

            // 生成新令牌
            var jwtToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userRepository.UpdateAsync(user);

            return new AuthenticationResult
            {
                Success = true,
                User = user,
                Token = jwtToken,
                RefreshToken = newRefreshToken,
                TokenExpiry = DateTime.UtcNow.AddHours(int.Parse(_configuration["Jwt:ExpirationHours"] ?? "24"))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "刷新令牌过程中发生错误"
            };
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns></returns>
    public async Task LogoutAsync(string refreshToken)
    {
        try
        {
            var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _userRepository.UpdateAsync(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    /// <summary>
    /// 验证令牌
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>用户信息</returns>
    public async Task<User?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");

            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                return await _userRepository.GetByIdAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
        }

        return null;
    }

    /// <summary>
    /// 生成密码哈希
    /// </summary>
    /// <param name="password">密码</param>
    /// <param name="salt">盐值</param>
    /// <returns>密码哈希</returns>
    public string HashPassword(string password, string salt)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 生成盐值
    /// </summary>
    /// <returns>盐值</returns>
    public string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">密码</param>
    /// <param name="hash">密码哈希</param>
    /// <param name="salt">盐值</param>
    /// <returns>是否匹配</returns>
    public bool VerifyPassword(string password, string hash, string salt)
    {
        var computedHash = HashPassword(password, salt);
        return computedHash == hash;
    }

    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    /// <param name="user">用户</param>
    /// <returns>JWT令牌</returns>
    private string GenerateJwtToken(User user)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("realName", user.RealName ?? ""),
                new Claim("email", user.Email ?? "")
            }),
            Expires = DateTime.UtcNow.AddHours(int.Parse(_configuration["Jwt:ExpirationHours"] ?? "24")),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    /// <returns>刷新令牌</returns>
    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
} 