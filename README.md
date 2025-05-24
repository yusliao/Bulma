# 锂电池包装工序MES系统

## 项目简介
锂电池包装工序MES系统是专为锂电池制造企业设计的制造执行系统，聚焦于电芯包装、模组包装、Pack包装和栈板包装等关键工序的全流程数字化管理。

## 核心功能
- **工序管理**：电芯/模组/Pack/栈板包装工序定义与配置
- **工艺路线**：可视化拖拽式工艺路线设计
- **参数处理**：高频工艺参数采集与存储
- **数据追溯**：完整生产批次追溯能力
- **实时监控**：生产状态可视化看板

## 技术栈
- **后端**：.NET 8 WebAPI
- **数据库**：MySQL 8（主从架构）
- **ORM**：SqlSugar
- **认证**：JWT
- **缓存**：Redis
- **时序数据**：InfluxDB
- **API文档**：Swagger/OpenAPI
- **日志**：Serilog

## 项目结构
```
BatteryPackingMES/
├── src/
│   ├── BatteryPackingMES.Api/          # Web API层
│   │   ├── Controllers/                # 控制器
│   │   ├── Models/                     # API模型
│   │   ├── appsettings.json           # 基础配置
│   │   ├── appsettings.Development.json # 开发环境配置
│   │   ├── appsettings.Staging.json   # 预发布环境配置
│   │   ├── appsettings.Production.json # 生产环境配置
│   │   └── Program.cs                  # 启动配置
│   ├── BatteryPackingMES.Core/         # 核心领域层
│   │   ├── Entities/                   # 实体模型
│   │   ├── Enums/                      # 枚举类型
│   │   └── Interfaces/                 # 接口定义
│   └── BatteryPackingMES.Infrastructure/ # 基础设施层
│       ├── Data/                       # 数据访问
│       └── Repositories/               # 仓储实现
├── scripts/                            # 脚本文件
│   ├── start.sh                       # 快速启动脚本
│   ├── deploy.sh                      # 多环境部署脚本
│   └── mysql-init.sql                 # 数据库初始化脚本
├── docs/                               # 文档
│   └── environment-configuration.md   # 环境配置文档
├── docker-compose.yml                  # 开发环境Docker编排
├── docker-compose.staging.yml          # 预发布环境Docker编排
├── Dockerfile                          # Docker镜像
├── README.md                          # 项目文档
└── BatteryPackingMES.sln              # 解决方案文件
```

## 快速开始

### 环境准备
1. 安装 .NET 8 SDK
2. 安装 Docker 和 Docker Compose
3. 安装 MySQL 8.0+ (可选，可使用Docker)
4. 安装 Redis 6.0+ (可选，可使用Docker)

### 多环境支持

系统支持三种环境配置：

| 环境 | 配置文件 | 特性 |
|------|----------|------|
| **开发环境** | `appsettings.Development.json` | Swagger启用、详细日志、调试信息 |
| **预发布环境** | `appsettings.Staging.json` | 生产级配置、监控、健康检查 |
| **生产环境** | `appsettings.Production.json` | 高安全性、性能优化、完整监控 |

### 环境变量配置

#### 开发环境
开发环境使用本地配置，无需设置环境变量。

#### 预发布/生产环境
创建对应的环境变量文件：

```bash
# .env.staging 或 .env.production
MYSQL_ROOT_PASSWORD=your_root_password
MYSQL_PASSWORD=your_mysql_password
REDIS_PASSWORD=your_redis_password
INFLUXDB_PASSWORD=your_influxdb_password
INFLUXDB_TOKEN=your_influxdb_token
JWT_SECRET_KEY=your_jwt_secret_key_minimum_32_characters
GRAFANA_ADMIN_PASSWORD=your_grafana_password
```

### 一键部署

使用多环境部署脚本：

```bash
# 给脚本执行权限
chmod +x scripts/deploy.sh

# 启动开发环境
./scripts/deploy.sh dev start

# 启动预发布环境
./scripts/deploy.sh staging start

# 启动生产环境
./scripts/deploy.sh prod start
```

### 手动部署

#### 1. 开发环境
```bash
# 克隆项目
git clone <repository-url>
cd BatteryPackingMES

# 恢复依赖包
dotnet restore

# 构建项目
dotnet build

# 启动开发环境
docker-compose up -d

# 或本地运行
cd src/BatteryPackingMES.Api
dotnet run
```

#### 2. 预发布环境
```bash
# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Staging
export MYSQL_PASSWORD=staging_password
export REDIS_PASSWORD=staging_redis_password
export JWT_SECRET_KEY=staging_jwt_secret_key

# 启动预发布环境
docker-compose -f docker-compose.staging.yml up -d
```

#### 3. 生产环境
```bash
# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Production
export MYSQL_MASTER_HOST=prod-mysql-master
export MYSQL_SLAVE_HOST=prod-mysql-slave
export MYSQL_PASSWORD=prod_password
export JWT_SECRET_KEY=production_jwt_secret_key

# 启动生产环境
docker-compose -f docker-compose.production.yml up -d
```

## 环境特性对比

### 开发环境 (Development)
- ✅ Swagger API文档
- ✅ 详细错误信息
- ✅ 性能日志记录
- ✅ 敏感数据日志
- 🔧 JWT过期时间：8小时
- 📊 日志级别：Debug
- 🌐 CORS：允许所有本地端口

### 预发布环境 (Staging)
- ✅ Swagger API文档
- ❌ 详细错误信息
- ✅ 性能日志记录
- ❌ 敏感数据日志
- 🔧 JWT过期时间：12小时
- 📊 日志级别：Information
- 🔒 SSL：必需
- 🌐 CORS：指定域名

### 生产环境 (Production)
- ❌ Swagger API文档
- ❌ 详细错误信息
- ❌ 性能日志记录
- ❌ 敏感数据日志
- 🔧 JWT过期时间：24小时
- 📊 日志级别：Warning
- 🔒 SSL：必需
- 🛡️ 安全策略：HSTS、CSP
- 🚦 速率限制：启用

## 访问地址

### 开发环境
- 🚀 API文档：http://localhost:60163/swagger
- 📊 Grafana监控：http://localhost:3000 (admin/admin123)
- 💾 MySQL数据库：localhost:3306 (root/123456)
- 🔥 Redis缓存：localhost:6379
- 📈 InfluxDB：http://localhost:8086

### 生产环境
- 🌐 API服务：https://api.battery-mes.com
- 📊 监控面板：https://monitoring.battery-mes.com
- 🎯 管理后台：https://admin.battery-mes.com

## API文档

### 主要API端点

#### 工序管理
- `GET /api/Process` - 获取所有工序
- `GET /api/Process/{id}` - 根据ID获取工序
- `GET /api/Process/paged` - 分页获取工序列表
- `POST /api/Process` - 创建工序
- `PUT /api/Process/{id}` - 更新工序
- `DELETE /api/Process/{id}` - 删除工序

#### 生产批次管理
- `GET /api/ProductionBatch` - 获取所有生产批次
- `GET /api/ProductionBatch/{id}` - 根据ID获取生产批次
- `GET /api/ProductionBatch/paged` - 分页获取生产批次列表
- `POST /api/ProductionBatch` - 创建生产批次
- `PUT /api/ProductionBatch/{id}` - 更新生产批次
- `POST /api/ProductionBatch/{id}/start` - 启动生产批次
- `POST /api/ProductionBatch/{id}/complete` - 完成生产批次
- `DELETE /api/ProductionBatch/{id}` - 删除生产批次

## 核心特性

### 1. 工序类型支持
- 电芯包装 (CellPacking)
- 模组包装 (ModulePacking)  
- Pack包装 (PackPacking)
- 栈板包装 (PalletPacking)

### 2. 批次状态管理
- 已计划 (Planned)
- 进行中 (InProgress)
- 暂停 (Paused)
- 已完成 (Completed)
- 已取消 (Cancelled)

### 3. 数据分表策略
- 高频参数数据按月自动分表
- 历史数据归档管理
- 查询性能优化

### 4. 全局过滤器
- 软删除过滤
- 乐观锁并发控制
- 统一时间戳管理

## 运维管理

### 日志管理
```bash
# 查看应用日志
./scripts/deploy.sh dev logs

# 查看特定服务日志
./scripts/deploy.sh prod logs mysql

# 查看服务状态
./scripts/deploy.sh staging status
```

### 数据备份
```bash
# 备份MySQL数据
docker exec battery-mes-mysql mysqldump -u root -p BatteryPackingMES > backup.sql

# 备份Redis数据
docker exec battery-mes-redis redis-cli BGSAVE
```

### 监控指标
- 应用程序健康状态
- 数据库连接池状态
- Redis缓存命中率
- API响应时间
- 系统资源使用情况

## 性能优化建议
1. 为高频查询添加合适索引
2. 配置合理的连接池大小
3. 启用查询缓存
4. 批量操作使用事务
5. 合理设置缓存过期时间
6. 监控并优化慢查询

## 安全建议
1. 定期更新依赖包
2. 使用强密码策略
3. 启用SSL/TLS加密
4. 配置防火墙规则
5. 定期备份数据
6. 监控异常访问

## 故障排除

### 常见问题
1. **数据库连接失败**
   - 检查MySQL服务状态
   - 验证连接字符串
   - 确认网络连通性

2. **Redis连接超时**
   - 检查Redis服务状态
   - 验证密码配置
   - 调整超时参数

3. **JWT验证失败**
   - 确认密钥配置
   - 检查token过期时间
   - 验证issuer和audience

详细的环境配置说明请参考：[环境配置文档](docs/environment-configuration.md)

## 许可证
MIT License

## 贡献
欢迎提交Issue和Pull Request来改进这个项目。

## 联系方式
- 项目维护者：开发团队
- 邮箱：dev@battery-mes.com
- 技术支持：support@battery-mes.com 