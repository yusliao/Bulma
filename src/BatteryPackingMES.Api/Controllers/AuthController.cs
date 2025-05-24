using System.ComponentModel.DataAnnotations;
using BatteryPackingMES.Api.Models;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BatteryPackingMES.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[ApiVersion("1.0")]
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
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录结果</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var result = await _authService.LoginAsync(request.Username, request.Password, ipAddress);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.Fail(result.ErrorMessage ?? "登录失败"));
        }

        var response = new LoginResponse
        {
            Token = result.Token!,
            RefreshToken = result.RefreshToken!,
            TokenExpiry = result.TokenExpiry!.Value,
            User = new UserInfo
            {
                Id = result.User!.Id,
                Username = result.User.Username,
                RealName = result.User.RealName,
                Email = result.User.Email
            }
        };

        return Ok(ApiResponse<LoginResponse>.Ok(response, "登录成功"));
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <param name="request">刷新令牌请求</param>
    /// <returns>新的令牌</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.Fail(result.ErrorMessage ?? "刷新令牌失败"));
        }

        var response = new TokenResponse
        {
            Token = result.Token!,
            RefreshToken = result.RefreshToken!,
            TokenExpiry = result.TokenExpiry!.Value
        };

        return Ok(ApiResponse<TokenResponse>.Ok(response, "令牌刷新成功"));
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <param name="request">登出请求</param>
    /// <returns>登出结果</returns>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] LogoutRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse.Ok("登出成功"));
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns>用户信息</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetProfile()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
        {
            return Unauthorized(ApiResponse.Fail("无效的用户令牌"));
        }

        var userInfo = new UserInfo
        {
            Id = userId,
            Username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value ?? "",
            RealName = User.Claims.FirstOrDefault(c => c.Type == "realName")?.Value,
            Email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
        };

        return Ok(ApiResponse<UserInfo>.Ok(userInfo, "获取用户信息成功"));
    }
}

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
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
}

/// <summary>
/// 刷新令牌请求
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 登出请求
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
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
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// 令牌响应
/// </summary>
public class TokenResponse
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
}

/// <summary>
/// 用户信息
/// </summary>
public class UserInfo
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
} 