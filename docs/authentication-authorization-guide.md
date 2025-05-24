# 认证授权系统指南

## 概述

锂电池包装工序MES系统的认证授权模块提供了完整的用户身份验证和权限管理功能，基于JWT令牌和RBAC（基于角色的访问控制）模型实现。

## 系统架构

### 核心组件

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   认证控制器     │────│   认证服务       │────│   用户仓库       │
│ AuthController  │    │AuthService      │    │UserRepository   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   JWT中间件      │    │   授权策略       │    │   数据库         │
│ JwtMiddleware   │    │Authorization    │    │   MySQL         │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 数据模型

#### 用户实体 (User)
- 基本信息：用户名、邮箱、真实姓名、手机号
- 安全信息：密码哈希、盐值、登录失败次数、账户锁定时间
- 会话信息：最后登录时间、IP地址、刷新令牌

#### 角色实体 (Role)
- 角色名称和描述
- 系统内置角色标识
- 与用户和权限的多对多关系

#### 权限实体 (Permission)
- 权限名称、代码、描述
- 权限模块分组
- 系统内置权限标识

## 认证流程

### 1. 用户登录

```mermaid
sequenceDiagram
    participant C as 客户端
    participant AC as AuthController
    participant AS as AuthService
    participant UR as UserRepository
    participant DB as 数据库

    C->>AC: POST /api/v1.0/auth/login
    AC->>AS: LoginAsync(username, password, ip)
    AS->>UR: GetByUsernameAsync(username)
    UR->>DB: 查询用户信息
    DB-->>UR: 返回用户数据
    UR-->>AS: 返回用户实体
    AS->>AS: 验证密码和账户状态
    AS->>AS: 生成JWT和RefreshToken
    AS->>UR: UpdateAsync(user) 更新登录信息
    AS-->>AC: 返回认证结果
    AC-->>C: 返回令牌和用户信息
```

### 2. 令牌刷新

```mermaid
sequenceDiagram
    participant C as 客户端
    participant AC as AuthController
    participant AS as AuthService
    participant UR as UserRepository

    C->>AC: POST /api/v1.0/auth/refresh
    AC->>AS: RefreshTokenAsync(refreshToken, ip)
    AS->>UR: GetByRefreshTokenAsync(refreshToken)
    AS->>AS: 验证令牌有效性
    AS->>AS: 生成新的JWT和RefreshToken
    AS->>UR: UpdateAsync(user) 更新令牌
    AS-->>AC: 返回新令牌
    AC-->>C: 返回新的令牌信息
```

## 授权策略

### 基于策略的授权

系统定义了多种授权策略：

```csharp
// 角色策略
options.AddPolicy("RequireAdminRole", policy =>
    policy.RequireClaim("role", "Admin"));

options.AddPolicy("RequireOperatorRole", policy =>
    policy.RequireClaim("role", "Operator", "Admin"));

// 权限策略
options.AddPolicy("CanManageUsers", policy =>
    policy.RequireClaim("permission", "users.manage"));

options.AddPolicy("CanManageProduction", policy =>
    policy.RequireClaim("permission", "production.manage"));
```

### 控制器中的使用

```csharp
[Authorize(Policy = "RequireAdminRole")]
public class AdminController : ControllerBase
{
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> CreateUser() { }
}
```

## API版本控制

### 版本策略

系统支持多种版本控制方式：

1. **URL段版本**: `/api/v1.0/auth/login`
2. **请求头版本**: `X-Version: 1.0`
3. **查询参数版本**: `?version=1.0`

### 版本示例

#### V1.0 API
```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ApiResponse<LoginResponse>> Login() { }
}
```

#### V2.0 API (增强版)
```csharp
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ApiResponse<LoginResponseV2>> Login() { }
    
    [HttpPost("refresh-batch")]
    public async Task<ApiResponse<BatchRefreshResponse>> RefreshTokenBatch() { }
}
```

## 安全特性

### 1. 密码安全
- HMAC-SHA256密码哈希
- 随机盐值生成
- 强密码策略验证

### 2. 账户保护
- 登录失败计数
- 自动账户锁定 (5次失败锁定30分钟)
- IP地址跟踪

### 3. 令牌安全
- JWT令牌短期有效 (默认24小时)
- 刷新令牌长期有效 (默认7天)
- 令牌自动轮换

### 4. 会话管理
- 设备指纹识别
- 并发会话控制
- 异常登录检测

## API文档

### 认证端点

#### 用户登录
```http
POST /api/v1.0/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password123"
}
```

**响应**:
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGVzdHJlZnJlc2h0b2tlbg==",
    "tokenExpiry": "2024-01-02T10:00:00Z",
    "user": {
      "id": 1,
      "username": "admin",
      "realName": "系统管理员",
      "email": "admin@example.com"
    }
  }
}
```

#### 刷新令牌
```http
POST /api/v1.0/auth/refresh
Content-Type: application/json

{
  "refreshToken": "dGVzdHJlZnJlc2h0b2tlbg=="
}
```

#### 获取用户信息
```http
GET /api/v1.0/auth/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### V2.0 增强功能

#### 批量刷新令牌
```http
POST /api/v2.0/auth/refresh-batch
Content-Type: application/json

{
  "refreshTokens": [
    "token1",
    "token2",
    "token3"
  ]
}
```

#### 增强的用户信息
```http
GET /api/v2.0/auth/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**响应**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "username": "admin",
    "permissions": ["users.view", "production.manage"],
    "roles": ["Admin"],
    "profileCompletion": 100,
    "securityLevel": "High"
  }
}
```

## 配置选项

### JWT配置
```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "BatteryPackingMES",
    "Audience": "BatteryPackingMES.Api",
    "ExpirationHours": 24
  }
}
```

### 安全配置
```json
{
  "Security": {
    "MaxFailedLoginAttempts": 5,
    "LockoutDurationMinutes": 30,
    "PasswordMinLength": 6,
    "RequireComplexPassword": true
  }
}
```

## 最佳实践

### 1. 令牌处理
- 客户端安全存储JWT令牌
- 自动处理令牌刷新
- 及时清理过期令牌

### 2. 错误处理
- 统一的错误响应格式
- 不泄露敏感信息
- 详细的日志记录

### 3. 性能优化
- 令牌缓存策略
- 数据库连接优化
- 异步操作使用

## 故障排除

### 常见问题

#### 1. 登录失败
- 检查用户名密码
- 确认账户未被锁定
- 验证数据库连接

#### 2. 令牌无效
- 检查令牌格式
- 验证签名密钥
- 确认令牌未过期

#### 3. 权限拒绝
- 检查用户角色
- 验证权限配置
- 确认策略设置

### 日志分析
```
[INFO] User admin logged in successfully from 192.168.1.100
[WARN] Login attempt for locked user: testuser
[ERROR] JWT validation failed: Token expired
```

## 监控指标

### 关键指标
- 登录成功率
- 令牌刷新频率
- 账户锁定次数
- API响应时间

### 告警规则
- 连续登录失败 > 10次
- 令牌验证失败率 > 5%
- API响应时间 > 2秒

## 扩展计划

### 短期
- 多因素认证 (MFA)
- 社交登录集成
- 单点登录 (SSO)

### 长期
- 生物识别认证
- 零信任安全模型
- AI威胁检测 