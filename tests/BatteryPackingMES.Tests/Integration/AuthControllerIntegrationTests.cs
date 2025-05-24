using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MySql;
using Testcontainers.Redis;
using Xunit;

namespace BatteryPackingMES.Tests.Integration;

/// <summary>
/// 认证控制器集成测试
/// </summary>
public class AuthControllerIntegrationTests : IClassFixture<AuthControllerIntegrationTests.TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MySqlContainer _mySqlContainer;
    private readonly RedisContainer _redisContainer;

    public AuthControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;

        // 配置测试容器
        _mySqlContainer = new MySqlBuilder()
            .WithDatabase("battery_packing_mes_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithPortBinding(3307, 3306)
            .Build();

        _redisContainer = new RedisBuilder()
            .WithPortBinding(6380, 6379)
            .Build();

        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        // 配置工厂使用测试容器
        _factory.ConfigureTestContainers(
            _mySqlContainer.GetConnectionString(),
            _redisContainer.GetConnectionString());

        // 初始化测试数据
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _mySqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    /// <summary>
    /// 测试用户登录成功
    /// </summary>
    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnSuccessWithToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeEmpty();
        apiResponse.Data.RefreshToken.Should().NotBeEmpty();
        apiResponse.Data.User.Username.Should().Be("admin");
    }

    /// <summary>
    /// 测试用户登录失败
    /// </summary>
    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("用户名或密码错误");
    }

    /// <summary>
    /// 测试令牌刷新
    /// </summary>
    [Fact]
    public async Task RefreshToken_ValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange - 先登录获取令牌
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1.0/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginApiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(loginContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var refreshTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = loginApiResponse!.Data!.RefreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/auth/refresh", refreshTokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<TokenResponse>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeEmpty();
        apiResponse.Data.RefreshToken.Should().NotBeEmpty();
        apiResponse.Data.RefreshToken.Should().NotBe(loginApiResponse.Data.RefreshToken); // 应该是新的刷新令牌
    }

    /// <summary>
    /// 测试获取用户信息（需要认证）
    /// </summary>
    [Fact]
    public async Task GetProfile_WithValidToken_ShouldReturnUserInfo()
    {
        // Arrange - 先登录获取令牌
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1.0/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginApiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(loginContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // 设置认证头
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginApiResponse!.Data!.Token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/auth/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserInfo>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Username.Should().Be("admin");
    }

    /// <summary>
    /// 测试未认证访问受保护的端点
    /// </summary>
    [Fact]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/auth/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // 创建测试用户
        var salt = authService.GenerateSalt();
        var passwordHash = authService.HashPassword("admin123", salt);

        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = passwordHash,
            Salt = salt,
            RealName = "系统管理员",
            Email = "admin@test.com",
            IsEnabled = true,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        await userRepository.AddAsync(adminUser);
    }

    /// <summary>
    /// 测试Web应用程序工厂
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private string? _connectionString;
        private string? _redisConnectionString;

        public void ConfigureTestContainers(string mySqlConnectionString, string redisConnectionString)
        {
            _connectionString = mySqlConnectionString;
            _redisConnectionString = redisConnectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // 这里可以覆盖测试需要的服务配置
                // 例如使用测试数据库连接字符串等
                
                // 设置测试日志级别
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            });

            builder.UseEnvironment("Testing");
        }
    }

    #region DTOs for Testing

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }
        public UserInfo User { get; set; } = new();
    }

    public class UserInfo
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? RealName { get; set; }
        public string? Email { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? ErrorCode { get; set; }
        public long Timestamp { get; set; }
    }

    public class ApiResponse : ApiResponse<object>
    {
    }

    #endregion
} 