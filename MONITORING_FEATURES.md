# BatteryPackingMES 监控增强功能

本文档介绍了 BatteryPackingMES 系统新增的三个重要功能：监控增强、数据验证和审计日志。

## 1. 监控增强 - 可观测性

### 功能概述
提供全面的系统监控和性能指标收集，确保系统的可观测性和运行状态监控。

### 核心组件

#### 1.1 系统指标收集
- **SystemMetric 实体**: 存储各类系统指标数据
- **IMetricsService 接口**: 定义指标收集和查询的契约
- **MetricsService 实现**: 提供指标记录、查询和系统健康检查

#### 1.2 性能监控中间件
- **PerformanceMonitoringMiddleware**: 自动记录 API 请求的性能指标
- 监控指标包括：
  - 响应时间
  - 状态码分布
  - 错误率统计
  - 请求量统计
  - 慢请求检测

#### 1.3 系统健康检查
- CPU 使用率监控
- 内存使用情况
- 线程数统计
- 系统运行时间
- 错误统计

### API 接口

```http
# 获取系统健康状态
GET /api/v2.0/monitoring/health

# 获取指标数据
GET /api/v2.0/monitoring/metrics?metricName=api.request.duration&startTime=2024-01-01&endTime=2024-01-02

# 记录自定义指标
POST /api/v2.0/monitoring/metrics
{
  "metricName": "custom.metric",
  "value": 100.5,
  "metricType": "gauge",
  "tags": {
    "environment": "production",
    "service": "battery-mes"
  }
}
```

## 2. 数据验证 - 数据质量

### 功能概述
提供灵活的数据质量检查和验证规则管理，确保数据的准确性和完整性。

### 核心组件

#### 2.1 验证规则管理
- **DataValidationRule 实体**: 定义数据验证规则
- **DataValidationResult 实体**: 存储验证结果
- **IDataValidationService 接口**: 数据验证服务契约
- **DataValidationService 实现**: 验证逻辑实现

#### 2.2 支持的验证类型
- **NotNull**: 非空验证
- **NotEmpty**: 非空字符串验证
- **Range**: 数值范围验证
- **Regex**: 正则表达式验证
- **Custom**: 自定义验证逻辑

#### 2.3 数据质量报告
- 验证通过率统计
- 按实体类型分组统计
- 时间趋势分析
- 数据质量评分

### API 接口

```http
# 获取数据质量报告
GET /api/v2.0/monitoring/data-quality/report?entityType=ProductionBatch&startTime=2024-01-01

# 获取验证结果
GET /api/v2.0/monitoring/data-quality/results?entityType=User&entityId=123

# 运行数据质量检查
POST /api/v2.0/monitoring/data-quality/check?entityType=ProductionBatch

# 添加验证规则
POST /api/v2.0/monitoring/data-quality/rules
{
  "ruleName": "用户名长度验证",
  "entityType": "User",
  "fieldName": "UserName",
  "validationType": "Range",
  "validationExpression": "{\"min\": 3, \"max\": 50}",
  "errorMessage": "用户名长度必须在3-50个字符之间",
  "severity": "Error",
  "isEnabled": true
}
```

## 3. 审计日志 - 合规要求

### 功能概述
提供完整的操作审计轨迹，满足合规要求和安全监控需求。

### 核心组件

#### 3.1 审计日志记录
- **AuditLog 实体**: 存储详细的操作记录
- **AuditConfiguration 实体**: 审计配置管理
- **IAuditService 接口**: 审计服务契约
- **AuditService 实现**: 审计日志记录和查询

#### 3.2 审计内容
- 操作类型（创建、更新、删除、登录等）
- 用户信息（用户ID、用户名）
- 操作时间和IP地址
- 实体变更前后的值
- 变更字段列表
- 风险等级评估

#### 3.3 风险等级评估
- **Low**: 一般操作
- **Medium**: 敏感操作（用户管理、权限变更）
- **High**: 高风险操作（删除操作）
- **Critical**: 关键操作

### API 接口

```http
# 获取审计日志
GET /api/v2.0/monitoring/audit/logs?entityType=User&actionType=Update&startTime=2024-01-01

# 获取实体审计历史
GET /api/v2.0/monitoring/audit/entity-history?entityType=User&entityId=123

# 获取审计统计报告
GET /api/v2.0/monitoring/audit/report?startTime=2024-01-01&endTime=2024-01-31

# 导出审计日志
GET /api/v2.0/monitoring/audit/export?startTime=2024-01-01&endTime=2024-01-31&format=CSV
```

## 使用示例

### 1. 在业务代码中记录审计日志

```csharp
// 记录实体创建
await _auditService.LogCreateAsync(newUser, "创建新用户");

// 记录实体更新
await _auditService.LogUpdateAsync(oldUser, updatedUser, "更新用户信息");

// 记录实体删除
await _auditService.LogDeleteAsync(user, "删除用户");

// 记录自定义操作
await _auditService.LogAsync("CustomAction", "User", userId, "执行自定义操作");
```

### 2. 记录自定义指标

```csharp
// 记录业务指标
await _metricsService.RecordMetricAsync("production.batch.count", batchCount);

// 记录计数器
await _metricsService.IncrementCounterAsync("api.calls", 1, new Dictionary<string, string>
{
    ["endpoint"] = "/api/users",
    ["method"] = "POST"
});

// 记录计时器
await _metricsService.RecordTimingAsync("database.query.duration", queryDuration);
```

### 3. 数据验证

```csharp
// 验证单个实体
var validationResults = await _dataValidationService.ValidateEntityAsync(user);

// 验证实体列表
var batchResults = await _dataValidationService.ValidateEntitiesAsync(users);

// 检查验证结果
foreach (var result in validationResults.Where(r => r.Status == "Failed"))
{
    _logger.LogWarning("数据验证失败: {Message}", result.Message);
}
```

## 配置说明

### 1. 在 Program.cs 中注册服务

```csharp
// 配置监控增强服务
builder.Services.AddScoped<IMetricsService, MetricsService>();

// 配置数据验证服务
builder.Services.AddScoped<IDataValidationService, DataValidationService>();

// 配置审计日志服务
builder.Services.AddScoped<IAuditService, AuditService>();

// 添加性能监控中间件
app.UsePerformanceMonitoring();
```

### 2. 数据库表结构

系统会自动创建以下数据表：
- `system_metrics`: 系统指标数据
- `data_validation_rules`: 数据验证规则
- `data_validation_results`: 验证结果
- `audit_logs`: 审计日志
- `audit_configurations`: 审计配置

## 监控仪表板建议

建议集成以下监控工具：
1. **Grafana**: 用于指标可视化和告警
2. **ELK Stack**: 用于日志分析和搜索
3. **Prometheus**: 用于指标收集和存储
4. **Jaeger**: 用于分布式链路追踪

## 最佳实践

1. **指标命名规范**: 使用层次化命名，如 `service.module.metric_name`
2. **审计日志保留**: 根据合规要求设置合适的保留期限
3. **数据验证规则**: 定期审查和更新验证规则
4. **性能监控**: 设置合理的告警阈值
5. **安全考虑**: 敏感数据在审计日志中应当脱敏处理

## 故障排查

### 常见问题
1. **指标数据丢失**: 检查数据库连接和存储配置
2. **审计日志过多**: 调整审计配置，排除不必要的操作
3. **验证规则失效**: 检查规则表达式的正确性
4. **性能影响**: 监控中间件的执行时间，必要时优化

### 日志级别配置
```json
{
  "Logging": {
    "LogLevel": {
      "BatteryPackingMES.Infrastructure.Services.MetricsService": "Information",
      "BatteryPackingMES.Infrastructure.Services.AuditService": "Information",
      "BatteryPackingMES.Infrastructure.Services.DataValidationService": "Information"
    }
  }
}
```

通过这些功能，BatteryPackingMES 系统现在具备了企业级的监控、数据质量管理和审计能力，为系统的稳定运行和合规管理提供了强有力的支持。 