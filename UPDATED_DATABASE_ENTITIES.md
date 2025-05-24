# 数据库实体更新总结

本文档总结了 BatteryPackingMES 系统中新增的实体和对应的数据库表结构。

## 新增实体概览

### 1. 仓库管理模块

#### 1.1 仓库表 (`warehouses`)
- **实体类**: `Warehouse`
- **功能**: 管理仓库基本信息
- **主要字段**: 
  - 仓库编码 (`WarehouseCode`)
  - 仓库名称 (`WarehouseName`)
  - 仓库类型 (`WarehouseType`)
  - 管理员ID (`ManagerId`)
  - 仓库面积 (`Area`)

#### 1.2 库位表 (`warehouse_locations`)
- **实体类**: `WarehouseLocation`
- **功能**: 管理仓库内具体存储位置
- **主要字段**:
  - 库位编码 (`LocationCode`)
  - 区域/巷道/货架信息 (`ZoneCode`, `AisleCode`, `RackCode`)
  - 容量信息 (`MaxCapacity`, `UsedCapacity`)
  - 库位状态 (`LocationStatus`)

#### 1.3 库存记录表 (`inventory_records`)
- **实体类**: `InventoryRecord`
- **功能**: 记录库存物料信息
- **主要字段**:
  - 产品编码 (`ProductCode`)
  - 批次号 (`BatchNumber`)
  - 数量信息 (`Quantity`, `AvailableQuantity`)
  - 质量状态 (`QualityStatus`)

#### 1.4 库存事务表 (`inventory_transactions`)
- **实体类**: `InventoryTransaction`
- **功能**: 记录库存变动事务
- **主要字段**:
  - 事务单号 (`TransactionNumber`)
  - 事务类型 (`TransactionType`)
  - 数量变更 (`QuantityBefore`, `QuantityAfter`)

#### 1.5 库存报警表 (`inventory_alerts`)
- **实体类**: `InventoryAlert`
- **功能**: 库存异常报警
- **主要字段**:
  - 报警类型 (`AlertType`)
  - 阈值 (`Threshold`)
  - 当前数量 (`CurrentQuantity`)

### 2. 产品追溯模块

#### 2.1 产品追溯记录表 (`product_traceability`)
- **实体类**: `ProductTraceability`
- **功能**: 记录产品全生命周期追溯信息
- **主要字段**:
  - 产品序列号 (`SerialNumber`)
  - 工序步骤 (`ProcessStep`)
  - 操作类型 (`OperationType`)
  - 质量检查点标识 (`IsQualityCheckpoint`)
  - 测试参数 (`TestParameters`)

### 3. 本地化资源模块

#### 3.1 本地化资源表 (`localization_resources`)
- **实体类**: `LocalizationResource`
- **功能**: 多语言资源管理
- **主要字段**:
  - 资源键 (`Key`)
  - 语言代码 (`LanguageCode`)
  - 资源值 (`Value`)
  - 翻译质量评分 (`QualityScore`)

#### 3.2 支持的语言配置表 (`supported_language_configs`)
- **实体类**: `SupportedLanguageConfig`
- **功能**: 系统支持的语言配置
- **主要字段**:
  - 语言代码 (`LanguageCode`)
  - 本地化名称 (`NativeName`)
  - 格式化配置 (`DateFormat`, `TimeFormat`, `NumberFormat`)

### 4. 数据验证模块

#### 4.1 数据验证规则表 (`data_validation_rules`)
- **实体类**: `DataValidationRule`
- **功能**: 定义数据验证规则
- **主要字段**:
  - 规则名称 (`RuleName`)
  - 实体类型 (`EntityType`)
  - 验证表达式 (`ValidationExpression`)
  - 错误消息 (`ErrorMessage`)

#### 4.2 数据验证结果表 (`data_validation_results`)
- **实体类**: `DataValidationResult`
- **功能**: 记录验证结果
- **主要字段**:
  - 验证状态 (`Status`)
  - 验证时间 (`ValidatedAt`)
  - 验证值 (`ValidatedValue`)

### 5. 生产参数模块

#### 5.1 生产参数记录表 (`production_parameter`)
- **实体类**: `ProductionParameter`
- **功能**: 记录生产过程参数
- **主要字段**:
  - 参数名称 (`ParameterName`)
  - 参数值 (`ParameterValue`)
  - 上下限值 (`UpperLimit`, `LowerLimit`)
  - 合格标识 (`IsQualified`)

#### 5.2 高频参数数据表 (`high_frequency_parameter`)
- **实体类**: `HighFrequencyParameter`
- **功能**: 存储高频采集的参数数据（分表存储）
- **特性**: 按月分表提升性能

### 6. 工艺路线模块

#### 6.1 工艺路线表 (`process_route`)
- **实体类**: `ProcessRoute`
- **功能**: 定义产品工艺路线
- **主要字段**:
  - 路线编码 (`RouteCode`)
  - 产品类型 (`ProductType`)
  - 版本号 (`VersionNumber`)
  - 路线配置 (`RouteConfig` - JSON格式)

#### 6.2 工艺路线步骤表 (`process_route_step`)
- **实体类**: `ProcessRouteStep`
- **功能**: 定义工艺路线的具体步骤
- **主要字段**:
  - 工艺路线ID (`ProcessRouteId`)
  - 工序ID (`ProcessId`)
  - 步骤序号 (`StepOrder`)
  - 步骤配置 (`StepConfig` - JSON格式)

## 新增权限

为了支持新增功能，在权限系统中添加了以下权限：

### 仓库管理权限
- `warehouses.view` - 仓库查看
- `warehouses.manage` - 仓库管理
- `inventory.manage` - 库存管理

### 工艺管理权限
- `routes.view` - 工艺路线查看
- `routes.manage` - 工艺路线管理

### 系统管理权限
- `localization.manage` - 本地化管理
- `validation.manage` - 数据验证管理

### 生产管理权限
- `parameters.view` - 生产参数查看
- `parameters.manage` - 生产参数管理

### 设备管理权限
- `equipments.view` - 设备查看
- `equipments.manage` - 设备管理

## 角色权限分配

### 系统管理员
- 拥有所有权限（包括新增的21个权限）

### 操作员
- 新增权限：仓库查看、库存管理、工艺路线查看、生产参数查看/管理、设备查看

### 质量员
- 新增权限：仓库查看、工艺路线查看、生产参数查看、设备查看

### 查看者
- 新增权限：仓库查看、工艺路线查看、生产参数查看、设备查看

## 初始化数据

### 默认仓库数据
- 原材料仓库 (WH001)
- 半成品仓库 (WH002)
- 成品仓库 (WH003)
- 不良品仓库 (WH004)

### 支持的语言
- 简体中文 (zh-CN) - 默认语言
- 英语 (en-US)
- 日语 (ja-JP)
- 韩语 (ko-KR)

### 默认工艺路线
- 电芯标准工艺路线 (RT-CELL-001)
- 模组标准工艺路线 (RT-MOD-001)
- Pack标准工艺路线 (RT-PACK-001)

### 数据验证规则
- 批次号格式验证
- 序列号唯一性验证
- 用户名长度验证
- 邮箱格式验证

## 数据库脚本更新内容

1. **新增表结构**: 15个新表
2. **权限扩展**: 新增11个权限
3. **角色权限**: 更新所有角色的权限分配
4. **初始化数据**: 提供完整的测试数据
5. **索引优化**: 为新表添加必要的索引

## 注意事项

1. **分表策略**: `high_frequency_parameter` 表采用按月分表策略
2. **JSON字段**: 多个表使用JSON格式存储配置信息
3. **外键关系**: 建议在生产环境中根据需要启用外键约束
4. **性能优化**: 为高频查询字段添加了合适的索引
5. **数据完整性**: 通过数据验证规则确保数据质量

## 升级说明

执行 `scripts/init-database.sql` 脚本将：
1. 创建所有新增表结构
2. 插入权限和角色数据
3. 初始化基础配置数据
4. 创建必要的索引

建议在测试环境中先验证脚本的正确性，然后再应用到生产环境。 