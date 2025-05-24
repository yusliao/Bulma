# 实时参数处理系统使用指南

## 概述

本系统完善了锂电池包装MES系统的参数采集实时处理逻辑，提供了全面的实时数据处理、异常检测、预警管理和数据聚合功能。

## 主要功能特性

### 1. 实时参数处理服务 (`RealTimeParameterProcessingService`)

**核心功能：**
- ✅ 实时参数数据采集和处理
- ✅ 基于滑动窗口的数据管理
- ✅ Z-Score算法异常检测
- ✅ 自动预警触发机制
- ✅ 参数数据聚合统计
- ✅ 过期数据清理机制

**关键特性：**
- 支持多工序并行处理
- 可配置的异常检测阈值
- 自动数据分片和存储
- 内存高效的窗口管理

### 2. 增强的API接口

#### 参数采集API (ProcessParameterController)
```http
POST /api/v2.0/process-parameters/batch-record
```
- 自动发布参数采集消息到实时处理队列
- 支持批量参数记录
- 实时质量判定和验证

#### 实时数据API (RealTimeDataController)
```http
GET  /api/v2.0/realtime/parameters/{processId}     # 获取实时参数监控
GET  /api/v2.0/realtime/aggregated/{processId}/{parameterName}  # 获取聚合数据
GET  /api/v2.0/realtime/system-status             # 系统状态监控
POST /api/v2.0/realtime/trigger-aggregation       # 手动触发聚合
```

### 3. 事件驱动架构

**新增事件类型：**
- `ProcessParameterAnomalyDetectedEvent` - 参数异常检测事件
- `ProcessParameterAlertTriggeredEvent` - 参数预警触发事件
- `ParameterAggregationCompletedEvent` - 参数聚合完成事件

**事件处理器：**
- `ParameterAnomalyEventHandler` - 异常事件处理
- `ParameterAlertEventHandler` - 预警事件处理
- `ParameterAggregationEventHandler` - 聚合事件处理

### 4. 高性能缓存系统

**缓存策略：**
- 实时参数数据：30分钟过期
- 聚合数据：1小时过期
- 异常统计：24小时过期
- 预警统计：7天过期

## 系统架构

```
参数采集 → 消息队列 → 实时处理服务 → 事件发布 → 事件处理器
    ↓           ↓            ↓           ↓          ↓
  数据库    Redis缓存    滑动窗口    异常检测    预警通知
```

## 配置参数

在 `appsettings.json` 中配置：

```json
{
  "RealTimeProcessing": {
    "AggregationIntervalSeconds": 30,    // 聚合间隔（秒）
    "AlertCheckIntervalSeconds": 60,     // 预警检查间隔（秒）
    "WindowSizeMinutes": 10,             // 滑动窗口大小（分钟）
    "AnomalyThreshold": 2.0,             // 异常检测阈值（Z-Score）
    "EnableRealTimeProcessing": true,    // 启用实时处理
    "MaxParameterWindows": 1000,         // 最大参数窗口数
    "DataRetentionHours": 24             // 数据保留时间（小时）
  }
}
```

## 部署和启动

### 1. 服务注册

系统会自动注册实时处理服务：

```csharp
// 在 Program.cs 中自动注册
if (builder.Configuration.GetValue<bool>("RealTimeProcessing:EnableRealTimeProcessing", true))
{
    builder.Services.AddHostedService<RealTimeParameterProcessingService>();
}
```

### 2. 依赖服务

确保以下服务正常运行：
- ✅ Redis (消息队列和缓存)
- ✅ MySQL (数据持久化)
- ✅ 事件总线服务

### 3. 启动验证

检查服务启动日志：
```
[INFO] 实时参数处理服务启动
[INFO] 已订阅事件: parameter-collected
[INFO] 聚合定时器已启动，间隔: 30秒
[INFO] 预警检查定时器已启动，间隔: 60秒
```

## 测试和验证

### 1. 运行测试脚本

```powershell
.\test_realtime_processing.ps1 -ProcessId 1 -ParameterName "温度" -TestDuration 300
```

**测试功能：**
- 📊 模拟参数数据发送
- 🔥 异常数据生成
- 📈 实时数据监控
- 📊 聚合数据验证
- ⚠️ 预警触发测试

### 2. API测试示例

#### 发送参数数据
```bash
curl -X POST "https://localhost:5001/api/v2.0/process-parameters/batch-record" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "parameters": [{
      "processId": 1,
      "parameterName": "温度",
      "parameterValue": 22.5,
      "parameterUnit": "℃",
      "upperLimit": 25.0,
      "lowerLimit": 20.0,
      "batchNumber": "BATCH001"
    }]
  }'
```

#### 获取实时数据
```bash
curl "https://localhost:5001/api/v2.0/realtime/parameters/1" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## 监控和运维

### 1. 性能监控

**关键指标：**
- 消息处理吞吐量
- 异常检测准确率
- 缓存命中率
- 内存使用情况

### 2. 日志监控

**重要日志事件：**
```
[WARN] 检测到参数异常: 温度, 当前值: 28.5, Z-Score: 2.3
[WARN] 参数预警: 温度, 连续 3 次异常
[INFO] 参数聚合完成: 工序 1, 参数 温度, 合格率 95.2%
```

### 3. 故障排查

**常见问题：**

1. **消息处理延迟**
   - 检查Redis连接状态
   - 验证消息队列配置
   - 监控内存使用

2. **异常检测误报**
   - 调整异常检测阈值
   - 增加数据窗口大小
   - 检查基础数据质量

3. **聚合数据不准确**
   - 验证时间窗口配置
   - 检查数据清理逻辑
   - 确认计算算法正确性

## 扩展和定制

### 1. 自定义异常检测算法

```csharp
// 可以扩展不同的异常检测方法
public interface IAnomalyDetector
{
    Task<bool> DetectAnomalyAsync(IEnumerable<decimal> values, decimal currentValue);
}
```

### 2. 添加新的预警规则

```csharp
// 自定义预警条件
public class CustomAlertRule : IAlertRule
{
    public bool ShouldTriggerAlert(ParameterWindow window)
    {
        // 实现自定义预警逻辑
        return false;
    }
}
```

### 3. 扩展数据输出

```csharp
// 自定义数据导出器
public class CustomDataExporter : IDataExporter
{
    public async Task ExportAsync(ParameterAggregatedData data)
    {
        // 实现自定义导出逻辑
    }
}
```

## 性能优化建议

### 1. 内存优化
- 定期清理过期窗口数据
- 限制最大窗口数量
- 使用对象池减少GC压力

### 2. 处理优化
- 异步处理非关键操作
- 批量处理提升效率
- 合理设置处理间隔

### 3. 存储优化
- 使用Redis分片存储
- 定期清理历史数据
- 优化缓存键命名规则

## 安全考虑

1. **接口安全**
   - JWT认证和授权
   - API访问频率限制
   - 输入数据验证

2. **数据安全**
   - 敏感参数加密存储
   - 审计日志记录
   - 数据备份策略

3. **系统安全**
   - 服务间通信加密
   - 访问权限控制
   - 异常行为监控

## 总结

完善后的实时参数处理系统提供了：

✅ **高性能** - 异步处理，低延迟响应  
✅ **高可靠** - 事件驱动，故障容错  
✅ **高扩展** - 模块化设计，易于扩展  
✅ **易运维** - 完善监控，详细日志  
✅ **易测试** - 提供测试工具和文档  

系统已准备好用于生产环境部署。 