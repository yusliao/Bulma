# 环境配置文档

## 概述
锂电池包装工序MES系统支持多环境配置，包括开发(Development)、预发布(Staging)和生产(Production)环境。

## 配置文件说明

### 1. 基础配置文件
- `appsettings.json` - 通用配置模板
- `appsettings.Development.json` - 开发环境配置
- `appsettings.Staging.json` - 预发布环境配置
- `appsettings.Production.json` - 生产环境配置

### 2. 环境变量
生产和预发布环境使用环境变量进行配置，格式：`${VARIABLE_NAME}`

## 必需的环境变量

### 数据库配置
```bash
MYSQL_MASTER_HOST=your_mysql_master_host
MYSQL_SLAVE_HOST=your_mysql_slave_host
MYSQL_PORT=3306
MYSQL_DATABASE=BatteryPackingMES
MYSQL_USER=battery_mes
MYSQL_PASSWORD=your_mysql_password
```

### Redis配置
```bash
REDIS_CONNECTION_STRING=redis_host:6379,password=redis_password
REDIS_PASSWORD=your_redis_password
```

### InfluxDB配置
```bash
INFLUXDB_URL=http://influxdb_host:8086
INFLUXDB_TOKEN=your_influxdb_token
INFLUXDB_ORG=your_organization
INFLUXDB_BUCKET=your_bucket_name
```

### JWT安全配置
```bash
JWT_SECRET_KEY=your_very_long_and_secure_jwt_secret_key_minimum_32_characters
```

## 环境特性对比

| 功能 | Development | Staging | Production |
|------|-------------|---------|-----------|
| Swagger文档 | ✅ 启用 | ✅ 启用 | ❌ 禁用 |
| 详细错误 | ✅ 启用 | ❌ 禁用 | ❌ 禁用 |
| 性能日志 | ✅ 启用 | ✅ 启用 | ❌ 禁用 |
| 敏感数据日志 | ✅ 启用 | ❌ 禁用 | ❌ 禁用 |
| JWT过期时间 | 8小时 | 12小时 | 24小时 |
| 日志级别 | Debug | Information | Warning |
| SSL要求 | 可选 | 必需 | 必需 |
| 缓存时间 | 30分钟 | 2小时 | 4小时 |

## 配置示例

### 开发环境启动
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/BatteryPackingMES.Api
```

### 预发布环境部署
```bash
export ASPNETCORE_ENVIRONMENT=Staging
export MYSQL_PASSWORD=staging_password
export REDIS_PASSWORD=staging_redis_password
export JWT_SECRET_KEY=staging_jwt_secret
docker-compose -f docker-compose.staging.yml up -d
```

### 生产环境部署
```bash
export ASPNETCORE_ENVIRONMENT=Production
export MYSQL_MASTER_HOST=prod-mysql-master
export MYSQL_SLAVE_HOST=prod-mysql-slave
export MYSQL_PASSWORD=prod_password
export REDIS_CONNECTION_STRING=prod-redis:6379,password=prod_redis_password
export JWT_SECRET_KEY=production_jwt_secret_key_very_long_and_secure
docker-compose -f docker-compose.production.yml up -d
```

## 安全配置建议

### 1. 密码强度
- MySQL密码：至少16位，包含大小写字母、数字和特殊字符
- Redis密码：至少12位随机字符
- JWT密钥：至少32位随机字符

### 2. 网络安全
- 生产环境强制使用HTTPS
- 配置防火墙规则
- 使用SSL证书

### 3. 日志安全
- 生产环境禁用敏感数据日志
- 设置日志轮转和保留策略
- 监控异常访问

## 健康检查配置

系统支持健康检查端点，可用于负载均衡器和监控系统：

- `/health` - 基础健康检查
- `/health/ready` - 就绪检查（包含数据库连接）
- `/health/live` - 存活检查

### 监控指标
- 数据库连接状态
- Redis连接状态
- InfluxDB连接状态
- 应用程序状态

## 故障排除

### 常见问题
1. **数据库连接失败**
   - 检查连接字符串格式
   - 验证用户权限
   - 确认网络连通性

2. **Redis连接超时**
   - 检查Redis服务状态
   - 验证密码配置
   - 调整超时参数

3. **JWT验证失败**
   - 确认密钥长度
   - 检查时钟同步
   - 验证issuer和audience

### 日志查看
```bash
# 实时查看应用日志
docker logs -f battery-mes-api

# 查看特定时间段日志
docker logs battery-mes-api --since="2024-01-01T00:00:00" --until="2024-01-01T23:59:59"
```

## 性能优化建议

### 数据库优化
- 配置适当的连接池大小
- 启用查询缓存
- 设置合理的超时时间

### 缓存优化
- 调整Redis内存配置
- 设置合适的过期时间
- 监控缓存命中率

### 应用优化
- 启用响应压缩
- 配置静态文件缓存
- 优化序列化配置 