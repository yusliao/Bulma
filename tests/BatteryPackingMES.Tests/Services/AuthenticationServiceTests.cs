using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BatteryPackingMES.Tests.Services;

/// <summary>
/// 认证服务测试
/// </summary>
public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthenticationService>>();

        // 设置配置模拟
        SetupConfiguration();

        _authService = new AuthenticationService(
            _userRepositoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    private void SetupConfiguration()
    {
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("ThisIsAVeryLongSecretKeyForJwtThatIsAtLeast256BitsLong");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("BatteryPackingMES");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("BatteryPackingMES.Api");
        _configurationMock.Setup(c => c["Jwt:ExpirationHours"]).Returns("24");
    }

    /// <summary>
    /// 测试成功登录
    /// </summary>
    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var ipAddress = "192.168.1.1";

        var salt = _authService.GenerateSalt();
        var passwordHash = _authService.HashPassword(password, salt);

        var user = new User
        {
            Id = 1,
            Username = username,
            PasswordHash = passwordHash,
            Salt = salt,
            IsEnabled = true,
            FailedLoginAttempts = 0
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(username, password, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(username);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.LastLoginTime.HasValue && 
            u.LastLoginIp == ipAddress && 
            u.FailedLoginAttempts == 0)), Times.Once);
    }

    /// <summary>
    /// 测试无效用户名登录
    /// </summary>
    [Fact]
    public async Task LoginAsync_InvalidUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = "nonexistent";
        var password = "password123";
        var ipAddress = "192.168.1.1";

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(username, password, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户名或密码错误");
        result.Token.Should().BeNull();
        result.RefreshToken.Should().BeNull();
    }

    /// <summary>
    /// 测试无效密码登录
    /// </summary>
    [Fact]
    public async Task LoginAsync_InvalidPassword_ShouldReturnFailureAndIncrementFailedAttempts()
    {
        // Arrange
        var username = "testuser";
        var correctPassword = "password123";
        var wrongPassword = "wrongpassword";
        var ipAddress = "192.168.1.1";

        var salt = _authService.GenerateSalt();
        var passwordHash = _authService.HashPassword(correctPassword, salt);

        var user = new User
        {
            Id = 1,
            Username = username,
            PasswordHash = passwordHash,
            Salt = salt,
            IsEnabled = true,
            FailedLoginAttempts = 0
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(username, wrongPassword, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户名或密码错误");

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.FailedLoginAttempts == 1)), Times.Once);
    }

    /// <summary>
    /// 测试账户锁定后登录
    /// </summary>
    [Fact]
    public async Task LoginAsync_AccountLocked_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var ipAddress = "192.168.1.1";

        var user = new User
        {
            Id = 1,
            Username = username,
            IsEnabled = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(30)
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(username, password, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().StartWith("账户已被锁定至");
    }

    /// <summary>
    /// 测试禁用账户登录
    /// </summary>
    [Fact]
    public async Task LoginAsync_DisabledAccount_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var ipAddress = "192.168.1.1";

        var user = new User
        {
            Id = 1,
            Username = username,
            IsEnabled = false
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(username, password, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("账户已被禁用");
    }

    /// <summary>
    /// 测试密码哈希和验证
    /// </summary>
    [Fact]
    public void PasswordHashing_ShouldWorkCorrectly()
    {
        // Arrange
        var password = "mySecretPassword123!";
        var salt = _authService.GenerateSalt();

        // Act
        var hash = _authService.HashPassword(password, salt);
        var isValid = _authService.VerifyPassword(password, hash, salt);
        var isInvalid = _authService.VerifyPassword("wrongPassword", hash, salt);

        // Assert
        hash.Should().NotBeEmpty();
        hash.Should().NotBe(password);
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }

    /// <summary>
    /// 测试盐值生成
    /// </summary>
    [Fact]
    public void GenerateSalt_ShouldGenerateUniqueSalts()
    {
        // Act
        var salt1 = _authService.GenerateSalt();
        var salt2 = _authService.GenerateSalt();

        // Assert
        salt1.Should().NotBeEmpty();
        salt2.Should().NotBeEmpty();
        salt1.Should().NotBe(salt2);
    }

    /// <summary>
    /// 测试刷新令牌成功
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_ValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshToken = "validRefreshToken";
        var ipAddress = "192.168.1.1";

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBe(refreshToken); // 应该生成新的刷新令牌
    }

    /// <summary>
    /// 测试无效刷新令牌
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_InvalidRefreshToken_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = "invalidRefreshToken";
        var ipAddress = "192.168.1.1";

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("无效的刷新令牌");
    }

    /// <summary>
    /// 测试过期的刷新令牌
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_ExpiredRefreshToken_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = "expiredRefreshToken";
        var ipAddress = "192.168.1.1";

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // 已过期
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("无效的刷新令牌");
    }

    /// <summary>
    /// 测试登出功能
    /// </summary>
    [Fact]
    public async Task LogoutAsync_ValidRefreshToken_ShouldClearRefreshToken()
    {
        // Arrange
        var refreshToken = "validRefreshToken";

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        await _authService.LogoutAsync(refreshToken);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.RefreshToken == null && 
            u.RefreshTokenExpiryTime == null)), Times.Once);
    }
} 