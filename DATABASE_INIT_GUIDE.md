# 数据库初始化指南

## 📋 概述

本文档介绍如何初始化锂电池包装工序MES系统的数据库。

## 🛠️ 前置条件

1. **MySQL 服务器** 已安装并运行
   - 版本要求：MySQL 5.7+ 或 MySQL 8.0+
   - 端口：3371（可配置）
   - 用户：root（可配置）

2. **MySQL 客户端工具** 已安装
   - `mysql` 命令行客户端
   - `mysqldump` 备份工具

3. **PowerShell** 执行环境
   - Windows PowerShell 5.1+
   - PowerShell Core 6.0+

## 🚀 快速开始

### 方法一：使用简化脚本（推荐）

```powershell
# 在项目根目录执行
powershell -ExecutionPolicy Bypass -File init-database-simple.ps1
```

### 方法二：使用完整脚本

```powershell
# 使用默认配置
powershell -ExecutionPolicy Bypass -File scripts/init-database.ps1

# 自定义配置
powershell -ExecutionPolicy Bypass -File scripts/init-database.ps1 -MySqlHost "localhost" -MySqlPort 3371 -MySqlUser "root" -MySqlPassword "your_password"
```

### 方法三：直接执行 SQL 脚本

```bash
# 使用 MySQL 命令行
mysql -h localhost -P 3371 -u root -p < scripts/init-database.sql
```

## ⚙️ 配置参数

| 参数 | 默认值 | 说明 |
|-----|--------|------|
| MySqlHost | localhost | MySQL 服务器地址 |
| MySqlPort | 3371 | MySQL 端口号 |
| MySqlUser | root | MySQL 用户名 |
| MySqlPassword | root | MySQL 密码 |
| DatabaseName | BatteryPackingMES_Dev | 数据库名称 |
| SkipUserPrompt | false | 跳过用户确认 |

## 📁 脚本说明

### 1. scripts/init-database.sql
- **功能**：核心 SQL 初始化脚本
- **包含**：
  - 数据库创建
  - 表结构定义
  - 索引和约束
  - 种子数据插入
  - 默认用户和权限设置

### 2. scripts/init-database.ps1
- **功能**：PowerShell 自动化脚本
- **特性**：
  - 环境检查
  - 数据库连接测试
  - 自动备份现有数据库
  - SQL 脚本执行
  - 结果验证
  - 配置文件更新

### 3. init-database-simple.ps1
- **功能**：简化版执行脚本
- **特性**：
  - 一键执行
  - 无需用户确认
  - 适合自动化部署

## 🗄️ 数据库结构

初始化后将创建以下核心表：

### 权限管理
- `permissions` - 权限表
- `roles` - 角色表
- `role_permissions` - 角色权限关联表
- `users` - 用户表
- `user_roles` - 用户角色关联表

### 业务核心
- `processes` - 工序表
- `code_generation_rules` - 编码生成规则表
- `code_generation_histories` - 编码生成历史表

### 系统监控
- `system_metrics` - 系统指标表
- `audit_logs` - 审计日志表

## 👤 默认账户

初始化完成后，系统将创建默认管理员账户：

```
用户名: admin
密码: Admin@123
角色: 系统管理员
```

> ⚠️ **重要提醒**：请在生产环境中立即修改默认密码！

## 🔧 故障排除

### 1. MySQL 连接失败
**错误信息**：`数据库连接失败`

**解决方案**：
- 检查 MySQL 服务是否启动
- 验证连接参数（主机、端口、用户名、密码）
- 检查防火墙设置
- 确认网络连通性

### 2. 权限不足
**错误信息**：`Access denied for user`

**解决方案**：
- 确保 MySQL 用户有足够权限
- 尝试使用管理员权限运行脚本
- 检查用户是否被锁定

### 3. 数据库已存在
**错误信息**：`Database exists`

**解决方案**：
- 脚本会自动提示是否备份现有数据库
- 可以选择跳过备份直接覆盖
- 或手动删除现有数据库后重试

### 4. SQL 脚本执行失败
**错误信息**：`SQL script execution failed`

**解决方案**：
- 检查 SQL 脚本文件是否存在且完整
- 验证 MySQL 版本兼容性
- 查看详细错误日志

## 🔍 验证安装

初始化完成后，可以通过以下方式验证：

### 1. 检查数据库和表
```sql
USE BatteryPackingMES_Dev;
SHOW TABLES;
```

### 2. 验证种子数据
```sql
SELECT COUNT(*) FROM users;
SELECT COUNT(*) FROM roles;
SELECT COUNT(*) FROM permissions;
```

### 3. 测试登录
```sql
SELECT Username, RealName FROM users WHERE Username = 'admin';
```

### 4. 启动 API 服务
```bash
cd src/BatteryPackingMES.Api
dotnet run
```

访问 Swagger 文档：`https://localhost:5001/swagger`

## 📝 注意事项

1. **数据安全**
   - 初始化脚本会删除现有数据库
   - 建议在执行前手动备份重要数据
   - 生产环境请使用更强的密码策略

2. **环境隔离**
   - 开发环境和生产环境使用不同的数据库名称
   - 配置文件应根据环境进行调整

3. **权限管理**
   - 默认权限设置适用于开发环境
   - 生产环境应根据实际需求调整权限配置

4. **性能优化**
   - 大数据量环境可能需要调整索引策略
   - 根据实际使用情况优化表结构

## 📞 技术支持

如果在数据库初始化过程中遇到问题，请：

1. 查看日志文件获取详细错误信息
2. 参考故障排除章节
3. 联系技术支持团队 