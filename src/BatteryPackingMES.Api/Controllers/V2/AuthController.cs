using System.ComponentModel.DataAnnotations;
using BatteryPackingMES.Api.Models;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 认证控制器 V2.0
/// 增强版本，包含更多功能和改进的响应格式
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录 V2.0 - 增强版本
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录结果</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseV2>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 429)] // 新增：速率限制
    public async Task<ActionResult<ApiResponse<LoginResponseV2>>> Login([FromBody] LoginRequestV2 request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        
        var result = await _authService.LoginAsync(request.Username, request.Password, ipAddress);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.Fail(result.ErrorMessage ?? "登录失败"));
        }

        var response = new LoginResponseV2
        {
            Token = result.Token!,
            RefreshToken = result.RefreshToken!,
            TokenExpiry = result.TokenExpiry!.Value,
            User = new UserInfoV2
            {
                Id = result.User!.Id,
                Username = result.User.Username,
                RealName = result.User.RealName,
                Email = result.User.Email,
                LastLoginTime = result.User.LastLoginTime,
                LastLoginIp = result.User.LastLoginIp
            },
            // V2.0 新增字段
            SessionInfo = new SessionInfo
            {
                LoginTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = GenerateDeviceFingerprint(Request)
            }
        };

        return Ok(ApiResponse<LoginResponseV2>.Ok(response, "登录成功"));
    }

    /// <summary>
    /// 批量刷新令牌 V2.0 - 新功能
    /// </summary>
    /// <param name="request">批量刷新请求</param>
    /// <returns>刷新结果</returns>
    [HttpPost("refresh-batch")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BatchRefreshResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<BatchRefreshResponse>>> RefreshTokenBatch([FromBody] BatchRefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var results = new List<RefreshResult>();

        foreach (var refreshToken in request.RefreshTokens)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
            results.Add(new RefreshResult
            {
                OriginalRefreshToken = refreshToken,
                Success = result.Success,
                NewToken = result.Token,
                NewRefreshToken = result.RefreshToken,
                ErrorMessage = result.ErrorMessage
            });
        }

        var response = new BatchRefreshResponse
        {
            Results = results,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success)
        };

        return Ok(ApiResponse<BatchRefreshResponse>.Ok(response, "批量刷新完成"));
    }

    /// <summary>
    /// 获取当前用户详细信息 V2.0 - 增强版本
    /// </summary>
    /// <returns>用户详细信息</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileV2>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<ActionResult<ApiResponse<UserProfileV2>>> GetProfile()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
        {
            return Unauthorized(ApiResponse.Fail("无效的用户令牌"));
        }

        // V2.0 返回更详细的用户信息
        var userProfile = new UserProfileV2
        {
            Id = userId,
            Username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value ?? "",
            RealName = User.Claims.FirstOrDefault(c => c.Type == "realName")?.Value,
            Email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
            // V2.0 新增字段
            Permissions = await GetUserPermissions(userId),
            Roles = await GetUserRoles(userId),
            LastActivity = DateTime.UtcNow,
            ProfileCompletion = CalculateProfileCompletion(User.Claims),
            SecurityLevel = GetSecurityLevel(User.Claims)
        };

        return Ok(ApiResponse<UserProfileV2>.Ok(userProfile, "获取用户信息成功"));
    }

    /// <summary>
    /// 生成设备指纹
    /// </summary>
    /// <param name="request">HTTP请求</param>
    /// <returns>设备指纹</returns>
    private static string GenerateDeviceFingerprint(HttpRequest request)
    {
        var fingerprint = $"{request.Headers.UserAgent}|{request.Headers.AcceptLanguage}|{request.Headers.AcceptEncoding}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fingerprint))[..16];
    }

    /// <summary>
    /// 获取用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限列表</returns>
    private async Task<List<string>> GetUserPermissions(long userId)
    {
        // 实际实现中会从用户仓库获取权限
        return new List<string> { "users.view", "processes.view", "production.view" };
    }

    /// <summary>
    /// 获取用户角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>角色列表</returns>
    private async Task<List<string>> GetUserRoles(long userId)
    {
        // 实际实现中会从用户仓库获取角色
        return new List<string> { "Operator" };
    }

    /// <summary>
    /// 计算用户资料完整度
    /// </summary>
    /// <param name="claims">用户声明</param>
    /// <returns>完整度百分比</returns>
    private static int CalculateProfileCompletion(IEnumerable<System.Security.Claims.Claim> claims)
    {
        var completion = 50; // 基础分
        if (!string.IsNullOrEmpty(claims.FirstOrDefault(c => c.Type == "realName")?.Value))
            completion += 25;
        if (!string.IsNullOrEmpty(claims.FirstOrDefault(c => c.Type == "email")?.Value))
            completion += 25;
        return completion;
    }

    /// <summary>
    /// 获取安全级别
    /// </summary>
    /// <param name="claims">用户声明</param>
    /// <returns>安全级别</returns>
    private static string GetSecurityLevel(IEnumerable<System.Security.Claims.Claim> claims)
    {
        // 简化的安全级别计算
        return "Standard";
    }
}

#region V2.0 DTOs

/// <summary>
/// 登录请求 V2.0
/// </summary>
public class LoginRequestV2
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 记住登录状态（V2.0 新增）
    /// </summary>
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// 客户端类型（V2.0 新增）
    /// </summary>
    public string? ClientType { get; set; }
}

/// <summary>
/// 登录响应 V2.0
/// </summary>
public class LoginResponseV2
{
    /// <summary>
    /// JWT令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime TokenExpiry { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfoV2 User { get; set; } = new();

    /// <summary>
    /// 会话信息（V2.0 新增）
    /// </summary>
    public SessionInfo SessionInfo { get; set; } = new();
}

/// <summary>
/// 用户信息 V2.0
/// </summary>
public class UserInfoV2
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 最后登录时间（V2.0 新增）
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录IP（V2.0 新增）
    /// </summary>
    public string? LastLoginIp { get; set; }
}

/// <summary>
/// 会话信息（V2.0 新增）
/// </summary>
public class SessionInfo
{
    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime LoginTime { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 用户代理
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// 设备指纹
    /// </summary>
    public string DeviceFingerprint { get; set; } = string.Empty;
}

/// <summary>
/// 批量刷新令牌请求（V2.0 新增）
/// </summary>
public class BatchRefreshTokenRequest
{
    /// <summary>
    /// 刷新令牌列表
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "至少需要一个刷新令牌")]
    public List<string> RefreshTokens { get; set; } = new();
}

/// <summary>
/// 批量刷新响应（V2.0 新增）
/// </summary>
public class BatchRefreshResponse
{
    /// <summary>
    /// 刷新结果列表
    /// </summary>
    public List<RefreshResult> Results { get; set; } = new();

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailureCount { get; set; }
}

/// <summary>
/// 刷新结果（V2.0 新增）
/// </summary>
public class RefreshResult
{
    /// <summary>
    /// 原始刷新令牌
    /// </summary>
    public string OriginalRefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 新的JWT令牌
    /// </summary>
    public string? NewToken { get; set; }

    /// <summary>
    /// 新的刷新令牌
    /// </summary>
    public string? NewRefreshToken { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 用户档案 V2.0（V2.0 新增）
/// </summary>
public class UserProfileV2
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// 资料完整度百分比
    /// </summary>
    public int ProfileCompletion { get; set; }

    /// <summary>
    /// 安全级别
    /// </summary>
    public string SecurityLevel { get; set; } = string.Empty;
}

#endregion 