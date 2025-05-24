-- ========================================
-- 锂电池包装工序MES系统 - 数据库初始化脚本
-- ========================================

-- 1. 创建数据库
DROP DATABASE IF EXISTS `BatteryPackingMES_Dev`;
CREATE DATABASE `BatteryPackingMES_Dev` 
  DEFAULT CHARACTER SET utf8mb4 
  DEFAULT COLLATE utf8mb4_unicode_ci;

-- 2. 切换到新创建的数据库
USE `BatteryPackingMES_Dev`;

-- 3. 创建专用用户（如果不存在）
-- 注意：在生产环境中应该使用更强的密码
DROP USER IF EXISTS 'batterypack_user'@'localhost';
CREATE USER 'batterypack_user'@'localhost' IDENTIFIED BY 'Battery@2024!';

-- 4. 授予权限
GRANT ALL PRIVILEGES ON `BatteryPackingMES_Dev`.* TO 'batterypack_user'@'localhost';
GRANT ALL PRIVILEGES ON `BatteryPackingMES_Dev`.* TO 'root'@'localhost';

-- 5. 刷新权限
FLUSH PRIVILEGES;

-- ========================================
-- 核心表结构创建
-- ========================================

-- 权限表
CREATE TABLE IF NOT EXISTS `permissions` (
  `Id` bigint NOT NULL,
  `Name` varchar(100) NOT NULL COMMENT '权限名称',
  `Code` varchar(100) NOT NULL COMMENT '权限代码',
  `Description` varchar(500) NULL COMMENT '权限描述',
  `Module` varchar(50) NULL COMMENT '所属模块',
  `IsSystemPermission` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否系统权限',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_permissions_code` (`Code`),
  KEY `IX_permissions_module` (`Module`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 角色表
CREATE TABLE IF NOT EXISTS `roles` (
  `Id` bigint NOT NULL,
  `Name` varchar(50) NOT NULL COMMENT '角色名称',
  `Description` varchar(200) NULL COMMENT '角色描述',
  `IsSystemRole` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否系统角色',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_roles_name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 角色权限关联表
CREATE TABLE IF NOT EXISTS `role_permissions` (
  `Id` bigint NOT NULL,
  `RoleId` bigint NOT NULL COMMENT '角色ID',
  `PermissionId` bigint NOT NULL COMMENT '权限ID',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_role_permissions` (`RoleId`, `PermissionId`),
  KEY `FK_role_permissions_role` (`RoleId`),
  KEY `FK_role_permissions_permission` (`PermissionId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 用户表
CREATE TABLE IF NOT EXISTS `users` (
  `Id` bigint NOT NULL,
  `Username` varchar(50) NOT NULL COMMENT '用户名',
  `PasswordHash` varchar(255) NOT NULL COMMENT '密码哈希',
  `Salt` varchar(50) NOT NULL COMMENT '盐值',
  `Email` varchar(100) NULL COMMENT '邮箱',
  `RealName` varchar(50) NULL COMMENT '真实姓名',
  `PhoneNumber` varchar(20) NULL COMMENT '手机号',
  `LastLoginTime` datetime NULL COMMENT '最后登录时间',
  `LastLoginIp` varchar(45) NULL COMMENT '最后登录IP',
  `FailedLoginAttempts` int NOT NULL DEFAULT 0 COMMENT '登录失败次数',
  `LockoutEnd` datetime NULL COMMENT '账户锁定到期时间',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `RefreshToken` varchar(500) NULL COMMENT '刷新令牌',
  `RefreshTokenExpiryTime` datetime NULL COMMENT '刷新令牌过期时间',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_users_username` (`Username`),
  UNIQUE KEY `UK_users_email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 用户角色关联表
CREATE TABLE IF NOT EXISTS `user_roles` (
  `Id` bigint NOT NULL,
  `UserId` bigint NOT NULL COMMENT '用户ID',
  `RoleId` bigint NOT NULL COMMENT '角色ID',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_user_roles` (`UserId`, `RoleId`),
  KEY `FK_user_roles_user` (`UserId`),
  KEY `FK_user_roles_role` (`RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 工序表（修正表名为process，与实体定义一致）
CREATE TABLE IF NOT EXISTS `process` (
  `Id` bigint NOT NULL,
  `ProcessCode` varchar(50) NOT NULL COMMENT '工序编码',
  `ProcessName` varchar(100) NOT NULL COMMENT '工序名称',
  `ProcessType` int NOT NULL COMMENT '工序类型',
  `Description` varchar(500) NULL COMMENT '工序描述',
  `StandardTime` decimal(10,2) NOT NULL DEFAULT 0 COMMENT '标准工时（分钟）',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `SortOrder` int NOT NULL DEFAULT 0 COMMENT '排序号',
  `ParameterConfig` text NULL COMMENT '工艺参数配置（JSON格式）',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_process_code` (`ProcessCode`),
  KEY `IX_process_type` (`ProcessType`),
  KEY `IX_process_sort` (`SortOrder`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 设备表
CREATE TABLE IF NOT EXISTS `equipments` (
  `Id` bigint NOT NULL,
  `EquipmentCode` varchar(50) NOT NULL COMMENT '设备编码',
  `EquipmentName` varchar(100) NOT NULL COMMENT '设备名称',
  `EquipmentType` int NOT NULL COMMENT '设备类型',
  `Category` varchar(50) NULL COMMENT '设备分类',
  `Model` varchar(100) NULL COMMENT '设备型号',
  `Manufacturer` varchar(100) NULL COMMENT '制造商',
  `SerialNumber` varchar(100) NULL COMMENT '序列号',
  `Specifications` text NULL COMMENT '规格参数（JSON格式）',
  `WorkstationId` bigint NULL COMMENT '所属工作站ID',
  `ProductionLineId` bigint NULL COMMENT '所属产线ID',
  `Location` varchar(200) NULL COMMENT '设备位置',
  `CurrentStatus` int NOT NULL DEFAULT 0 COMMENT '当前状态',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `IsCritical` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否关键设备',
  `InstallationDate` datetime NULL COMMENT '安装日期',
  `CommissioningDate` datetime NULL COMMENT '启用日期',
  `WarrantyEndDate` datetime NULL COMMENT '保修期结束日期',
  `ResponsiblePersonId` bigint NULL COMMENT '责任人ID',
  `ContactPhone` varchar(20) NULL COMMENT '联系电话',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_equipments_code` (`EquipmentCode`),
  KEY `IX_equipments_type` (`EquipmentType`),
  KEY `IX_equipments_status` (`CurrentStatus`),
  KEY `FK_equipments_responsible` (`ResponsiblePersonId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 设备状态记录表
CREATE TABLE IF NOT EXISTS `equipment_status_records` (
  `Id` bigint NOT NULL,
  `EquipmentId` bigint NOT NULL COMMENT '设备ID',
  `EquipmentCode` varchar(50) NOT NULL COMMENT '设备编码',
  `PreviousStatus` int NULL COMMENT '前一状态',
  `CurrentStatus` int NOT NULL COMMENT '当前状态',
  `StatusChangeTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '状态变更时间',
  `OperatorId` bigint NULL COMMENT '操作员ID',
  `StatusChangeReason` varchar(200) NULL COMMENT '状态变更原因',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_status_record_equipment` (`EquipmentId`),
  KEY `IX_status_record_time` (`StatusChangeTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 设备维护记录表
CREATE TABLE IF NOT EXISTS `equipment_maintenance_records` (
  `Id` bigint NOT NULL,
  `EquipmentId` bigint NOT NULL COMMENT '设备ID',
  `EquipmentCode` varchar(50) NOT NULL COMMENT '设备编码',
  `MaintenanceType` int NOT NULL COMMENT '维护类型',
  `ScheduledDate` datetime NOT NULL COMMENT '计划日期',
  `ActualStartDate` datetime NOT NULL COMMENT '实际开始日期',
  `ActualEndDate` datetime NULL COMMENT '实际结束日期',
  `Description` varchar(1000) NOT NULL COMMENT '维护描述',
  `WorkContent` varchar(1000) NULL COMMENT '工作内容',
  `Materials` text NULL COMMENT '使用材料（JSON格式）',
  `MaintenancePersonId` bigint NULL COMMENT '维护人员ID',
  `MaintenancePersonName` varchar(50) NULL COMMENT '维护人员姓名',
  `Status` int NOT NULL COMMENT '维护状态',
  `Cost` decimal(10,2) NULL COMMENT '维护成本',
  `NextMaintenanceDate` datetime NULL COMMENT '下次维护日期',
  `ValidationResult` varchar(500) NULL COMMENT '验证结果',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_maintenance_equipment` (`EquipmentId`),
  KEY `IX_maintenance_date` (`ScheduledDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 设备报警表
CREATE TABLE IF NOT EXISTS `equipment_alarms` (
  `Id` bigint NOT NULL,
  `EquipmentId` bigint NOT NULL COMMENT '设备ID',
  `EquipmentCode` varchar(50) NOT NULL COMMENT '设备编码',
  `AlarmType` int NOT NULL COMMENT '报警类型',
  `AlarmLevel` int NOT NULL COMMENT '报警级别',
  `AlarmCode` varchar(50) NULL COMMENT '报警代码',
  `Message` varchar(500) NOT NULL COMMENT '报警消息',
  `Details` text NULL COMMENT '详细信息（JSON格式）',
  `OccurredAt` datetime NOT NULL COMMENT '发生时间',
  `AcknowledgedAt` datetime NULL COMMENT '确认时间',
  `ResolvedAt` datetime NULL COMMENT '解决时间',
  `IsActive` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否活跃',
  `AcknowledgedById` bigint NULL COMMENT '确认人ID',
  `AcknowledgedByName` varchar(50) NULL COMMENT '确认人姓名',
  `ResolvedById` bigint NULL COMMENT '解决人ID',
  `Resolution` varchar(1000) NULL COMMENT '解决方案',
  `Resolution_Notes` varchar(500) NULL COMMENT '解决备注',
  `PreventiveMeasures` varchar(500) NULL COMMENT '预防措施',
  `RecurrenceCount` int NOT NULL DEFAULT 1 COMMENT '重复次数',
  `RelatedAlarmId` bigint NULL COMMENT '关联报警ID',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_alarm_equipment` (`EquipmentId`),
  KEY `IX_alarm_time` (`OccurredAt`),
  KEY `IX_alarm_active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 设备操作日志表
CREATE TABLE IF NOT EXISTS `equipment_operation_logs` (
  `Id` bigint NOT NULL,
  `EquipmentId` bigint NOT NULL COMMENT '设备ID',
  `EquipmentCode` varchar(50) NOT NULL COMMENT '设备编码',
  `OperationType` int NOT NULL COMMENT '操作类型',
  `Description` varchar(500) NOT NULL COMMENT '操作描述',
  `Parameters` text NULL COMMENT '操作参数（JSON格式）',
  `Result` varchar(500) NULL COMMENT '操作结果',
  `IsSuccessful` tinyint(1) NOT NULL COMMENT '是否成功',
  `OperatedAt` datetime NOT NULL COMMENT '操作时间',
  `OperatorId` bigint NOT NULL COMMENT '操作员ID',
  `OperatorName` varchar(50) NULL COMMENT '操作员姓名',
  `WorkstationCode` varchar(50) NULL COMMENT '工作站编码',
  `ShiftCode` varchar(50) NULL COMMENT '班次编码',
  `BatchNumber` varchar(50) NULL COMMENT '批次号',
  `ErrorCode` varchar(500) NULL COMMENT '错误代码',
  `ErrorMessage` varchar(500) NULL COMMENT '错误信息',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_operation_log_equipment` (`EquipmentId`),
  KEY `IX_operation_log_time` (`OperatedAt`),
  KEY `IX_operation_log_operator` (`OperatorId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 生产批次表
CREATE TABLE IF NOT EXISTS `production_batch` (
  `Id` bigint NOT NULL,
  `BatchNumber` varchar(50) NOT NULL COMMENT '批次号',
  `ProductType` varchar(50) NOT NULL COMMENT '产品类型',
  `ProductCode` varchar(50) NULL COMMENT '产品编码',
  `ProductName` varchar(100) NULL COMMENT '产品名称',
  `ProcessRouteId` bigint NULL COMMENT '工艺路线ID',
  `PlannedQuantity` int NOT NULL COMMENT '计划数量',
  `ActualQuantity` int NOT NULL DEFAULT 0 COMMENT '实际数量',
  `CompletedQuantity` int NOT NULL DEFAULT 0 COMMENT '已完成数量',
  `QualifiedQuantity` int NOT NULL DEFAULT 0 COMMENT '合格数量',
  `BatchStatus` int NOT NULL DEFAULT 0 COMMENT '批次状态',
  `Priority` int NOT NULL DEFAULT 0 COMMENT '优先级',
  `PlannedStartTime` datetime NULL COMMENT '计划开始时间',
  `PlannedEndTime` datetime NULL COMMENT '计划结束时间',
  `ActualStartTime` datetime NULL COMMENT '实际开始时间',
  `ActualEndTime` datetime NULL COMMENT '实际结束时间',
  `WorkOrder` varchar(50) NULL COMMENT '工作订单',
  `CustomerOrder` varchar(50) NULL COMMENT '客户订单',
  `ProductSpecification` varchar(500) NULL COMMENT '产品规格',
  `QualityRequirements` varchar(500) NULL COMMENT '质量要求',
  `QualityResult` varchar(500) NULL COMMENT '质量结果',
  `CurrentProcessId` bigint NULL COMMENT '当前工序ID',
  `CurrentOperator` varchar(50) NULL COMMENT '当前操作员',
  `CurrentWorkstation` varchar(50) NULL COMMENT '当前工作站',
  `ProcessingNotes` varchar(500) NULL COMMENT '加工备注',
  `CompletionNotes` varchar(500) NULL COMMENT '完成备注',
  `StatusReason` varchar(200) NULL COMMENT '状态原因',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_batch_number` (`BatchNumber`),
  KEY `IX_batch_status` (`BatchStatus`),
  KEY `IX_batch_product_type` (`ProductType`),
  KEY `FK_batch_process_route` (`ProcessRouteId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 产品项表
CREATE TABLE IF NOT EXISTS `product_items` (
  `Id` bigint NOT NULL,
  `SerialNumber` varchar(100) NOT NULL COMMENT '产品序列号',
  `Barcode` varchar(100) NOT NULL COMMENT '产品条码',
  `BatchId` bigint NOT NULL COMMENT '批次ID',
  `BatchNumber` varchar(50) NOT NULL COMMENT '批次号',
  `ProductModel` varchar(100) NULL COMMENT '产品型号',
  `ProductType` varchar(50) NOT NULL COMMENT '产品类型',
  `ProductionDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '生产日期',
  `ItemStatus` int NOT NULL DEFAULT 0 COMMENT '产品项状态',
  `QualityGrade` int NOT NULL DEFAULT 0 COMMENT '质量等级',
  `QualityNotes` varchar(500) NULL COMMENT '质量备注',
  `ParentSerialNumber` varchar(100) NULL COMMENT '父级产品序列号',
  `AssemblyPosition` varchar(50) NULL COMMENT '组装位置',
  `ProductionStartTime` datetime NULL COMMENT '生产开始时间',
  `ProductionEndTime` datetime NULL COMMENT '生产完成时间',
  `CurrentProcessId` bigint NULL COMMENT '当前工序ID',
  `CurrentWorkstation` varchar(50) NULL COMMENT '当前工作站',
  `OperatorId` bigint NULL COMMENT '操作员ID',
  `LastInspectionTime` datetime NULL COMMENT '最后检测时间',
  `InspectionResults` text NULL COMMENT '检测结果（JSON格式）',
  `CustomerCode` varchar(50) NULL COMMENT '客户编码',
  `CustomerOrderNumber` varchar(100) NULL COMMENT '客户订单号',
  `ShippedTime` datetime NULL COMMENT '出货时间',
  `ShippingBatchNumber` varchar(100) NULL COMMENT '出货批次号',
  `Remarks` varchar(1000) NULL COMMENT '备注',
  `IsReworked` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否返工品',
  `ReworkCount` int NOT NULL DEFAULT 0 COMMENT '返工次数',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_product_serial_number` (`SerialNumber`),
  UNIQUE KEY `UK_product_barcode` (`Barcode`),
  KEY `IX_product_batch` (`BatchId`),
  KEY `IX_product_status` (`ItemStatus`),
  KEY `IX_product_type` (`ProductType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 编码生成规则表
CREATE TABLE IF NOT EXISTS `code_generation_rules` (
  `Id` bigint NOT NULL,
  `RuleName` varchar(100) NOT NULL COMMENT '规则名称',
  `CodeType` varchar(50) NOT NULL COMMENT '编码类型',
  `ProductType` varchar(50) NOT NULL COMMENT '产品类型',
  `Prefix` varchar(20) NULL COMMENT '前缀',
  `Suffix` varchar(20) NULL COMMENT '后缀',
  `DateFormat` varchar(20) NULL COMMENT '日期格式',
  `SequenceLength` int NOT NULL DEFAULT 3 COMMENT '序号长度',
  `StartNumber` int NOT NULL DEFAULT 1 COMMENT '序号起始值',
  `ResetDaily` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否按日重置',
  `ResetMonthly` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否按月重置',
  `ResetYearly` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否按年重置',
  `CurrentSequence` int NOT NULL DEFAULT 0 COMMENT '当前序号',
  `LastGeneratedDate` datetime NULL COMMENT '最后生成日期',
  `Template` varchar(200) NOT NULL COMMENT '编码模板',
  `ValidationPattern` varchar(500) NULL COMMENT '验证规则',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `Description` varchar(500) NULL COMMENT '规则描述',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_code_rules` (`CodeType`, `ProductType`),
  KEY `IX_code_rules_type` (`CodeType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 编码生成历史表
CREATE TABLE IF NOT EXISTS `code_generation_histories` (
  `Id` bigint NOT NULL,
  `RuleId` bigint NOT NULL COMMENT '规则ID',
  `GeneratedCode` varchar(100) NOT NULL COMMENT '生成的编码',
  `CodeType` varchar(50) NOT NULL COMMENT '编码类型',
  `EntityType` varchar(50) NULL COMMENT '关联实体类型',
  `EntityId` bigint NULL COMMENT '关联实体ID',
  `IsUsed` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否已使用',
  `UsedAt` datetime NULL COMMENT '使用时间',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_generated_code` (`GeneratedCode`),
  KEY `FK_code_history_rule` (`RuleId`),
  KEY `IX_code_history_type` (`CodeType`),
  KEY `IX_code_history_entity` (`EntityType`, `EntityId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 系统指标表
CREATE TABLE IF NOT EXISTS `system_metrics` (
  `Id` bigint NOT NULL,
  `MetricName` varchar(100) NOT NULL COMMENT '指标名称',
  `MetricValue` double NOT NULL COMMENT '指标值',
  `MetricType` varchar(50) NOT NULL COMMENT '指标类型',
  `Tags` json NULL COMMENT '标签（JSON格式）',
  `Timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '时间戳',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_metrics_name` (`MetricName`),
  KEY `IX_metrics_timestamp` (`Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 审计日志表
CREATE TABLE IF NOT EXISTS `audit_logs` (
  `Id` bigint NOT NULL,
  `EntityType` varchar(100) NOT NULL COMMENT '实体类型',
  `EntityId` bigint NOT NULL COMMENT '实体ID',
  `ActionType` varchar(50) NOT NULL COMMENT '操作类型',
  `UserId` bigint NULL COMMENT '用户ID',
  `UserName` varchar(100) NULL COMMENT '用户名',
  `IpAddress` varchar(45) NULL COMMENT 'IP地址',
  `UserAgent` varchar(500) NULL COMMENT '用户代理',
  `OldValues` json NULL COMMENT '原值（JSON格式）',
  `NewValues` json NULL COMMENT '新值（JSON格式）',
  `AffectedColumns` varchar(1000) NULL COMMENT '影响的列',
  `PrimaryKey` varchar(100) NULL COMMENT '主键值',
  `Timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '时间戳',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_audit_entity` (`EntityType`, `EntityId`),
  KEY `IX_audit_user` (`UserId`),
  KEY `IX_audit_timestamp` (`Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================
-- 仓库管理相关表
-- ========================================

-- 仓库表
CREATE TABLE IF NOT EXISTS `warehouses` (
  `Id` bigint NOT NULL,
  `WarehouseCode` varchar(50) NOT NULL COMMENT '仓库编码',
  `WarehouseName` varchar(100) NOT NULL COMMENT '仓库名称',
  `WarehouseType` int NOT NULL DEFAULT 0 COMMENT '仓库类型',
  `Address` varchar(200) NULL COMMENT '仓库地址',
  `ManagerId` bigint NULL COMMENT '仓库管理员ID',
  `Phone` varchar(20) NULL COMMENT '联系电话',
  `Area` decimal(10,2) NULL COMMENT '仓库面积（平方米）',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_warehouses_code` (`WarehouseCode`),
  KEY `FK_warehouses_manager` (`ManagerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 库位表
CREATE TABLE IF NOT EXISTS `warehouse_locations` (
  `Id` bigint NOT NULL,
  `WarehouseId` bigint NOT NULL COMMENT '仓库ID',
  `LocationCode` varchar(50) NOT NULL COMMENT '库位编码',
  `LocationName` varchar(100) NOT NULL COMMENT '库位名称',
  `LocationType` int NOT NULL DEFAULT 0 COMMENT '库位类型',
  `ZoneCode` varchar(20) NULL COMMENT '区域编码',
  `AisleCode` varchar(20) NULL COMMENT '巷道编码',
  `RackCode` varchar(20) NULL COMMENT '货架编码',
  `Level` int NULL COMMENT '层数',
  `Position` int NULL COMMENT '位置',
  `MaxCapacity` decimal(10,2) NOT NULL DEFAULT 0 COMMENT '最大容量',
  `UsedCapacity` decimal(10,2) NOT NULL DEFAULT 0 COMMENT '当前占用容量',
  `CapacityUnit` varchar(20) NULL COMMENT '容量单位',
  `LocationStatus` int NOT NULL DEFAULT 0 COMMENT '库位状态',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `EnvironmentRequirements` text NULL COMMENT '环境要求（JSON格式）',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_locations_code` (`LocationCode`),
  KEY `FK_locations_warehouse` (`WarehouseId`),
  KEY `IX_locations_zone` (`ZoneCode`),
  KEY `IX_locations_status` (`LocationStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 库存记录表
CREATE TABLE IF NOT EXISTS `inventory_records` (
  `Id` bigint NOT NULL,
  `WarehouseId` bigint NOT NULL COMMENT '仓库ID',
  `LocationId` bigint NOT NULL COMMENT '库位ID',
  `ProductCode` varchar(50) NOT NULL COMMENT '产品编码',
  `ProductName` varchar(100) NOT NULL COMMENT '产品名称',
  `ProductType` int NOT NULL COMMENT '产品类型',
  `BatchNumber` varchar(50) NULL COMMENT '批次号',
  `SerialNumber` varchar(100) NULL COMMENT '序列号',
  `LotNumber` varchar(50) NULL COMMENT '批号',
  `ExpiryDate` datetime NULL COMMENT '过期日期',
  `ProductionDate` datetime NULL COMMENT '生产日期',
  `Quantity` decimal(10,2) NOT NULL COMMENT '数量',
  `AvailableQuantity` decimal(10,2) NOT NULL COMMENT '可用数量',
  `ReservedQuantity` decimal(10,2) NOT NULL DEFAULT 0 COMMENT '预留数量',
  `AllocatedQuantity` decimal(10,2) NOT NULL DEFAULT 0 COMMENT '分配数量',
  `Unit` varchar(20) NOT NULL COMMENT '单位',
  `QualityStatus` int NULL COMMENT '质量状态',
  `InventoryStatus` int NULL COMMENT '库存状态',
  `ReceiptStatus` int NOT NULL COMMENT '入库状态',
  `LastMovementType` int NOT NULL COMMENT '最后库存移动类型',
  `LastMovementDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '最后移动日期',
  `LastCountDate` datetime NULL COMMENT '最后盘点日期',
  `UnitCost` decimal(10,2) NOT NULL DEFAULT 0 COMMENT '单位成本',
  `TotalCost` decimal(12,2) NOT NULL DEFAULT 0 COMMENT '总成本',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_inventory_warehouse` (`WarehouseId`),
  KEY `FK_inventory_location` (`LocationId`),
  KEY `IX_inventory_product` (`ProductCode`),
  KEY `IX_inventory_batch` (`BatchNumber`),
  KEY `IX_inventory_status` (`InventoryStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 库存事务表
CREATE TABLE IF NOT EXISTS `inventory_transactions` (
  `Id` bigint NOT NULL,
  `TransactionNumber` varchar(50) NOT NULL COMMENT '事务单号',
  `TransactionType` int NOT NULL COMMENT '事务类型',
  `WarehouseId` bigint NOT NULL COMMENT '仓库ID',
  `LocationId` bigint NOT NULL COMMENT '库位ID',
  `ProductCode` varchar(50) NOT NULL COMMENT '产品编码',
  `ProductName` varchar(100) NOT NULL COMMENT '产品名称',
  `BatchNumber` varchar(50) NULL COMMENT '批次号',
  `SerialNumber` varchar(100) NULL COMMENT '序列号',
  `Quantity` decimal(10,2) NOT NULL COMMENT '数量',
  `QuantityBefore` decimal(10,2) NOT NULL COMMENT '变更前数量',
  `QuantityAfter` decimal(10,2) NOT NULL COMMENT '变更后数量',
  `Unit` varchar(20) NOT NULL COMMENT '单位',
  `UnitCost` decimal(10,2) NULL COMMENT '单位成本',
  `TotalCost` decimal(12,2) NULL COMMENT '总成本',
  `SourceLocationCode` varchar(50) NULL COMMENT '来源库位编码',
  `TargetLocationCode` varchar(50) NULL COMMENT '目标库位编码',
  `TransactionDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '事务日期',
  `OperatorId` bigint NOT NULL COMMENT '操作员ID',
  `RelatedOrderNumber` varchar(100) NULL COMMENT '关联单据号',
  `TransactionReason` varchar(200) NULL COMMENT '事务原因',
  `Remarks` varchar(500) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_transaction_number` (`TransactionNumber`),
  KEY `FK_transaction_warehouse` (`WarehouseId`),
  KEY `FK_transaction_location` (`LocationId`),
  KEY `FK_transaction_operator` (`OperatorId`),
  KEY `IX_transaction_date` (`TransactionDate`),
  KEY `IX_transaction_type` (`TransactionType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 库存报警表
CREATE TABLE IF NOT EXISTS `inventory_alerts` (
  `Id` bigint NOT NULL,
  `ProductCode` varchar(50) NOT NULL COMMENT '产品编码',
  `WarehouseId` bigint NOT NULL COMMENT '仓库ID',
  `AlertType` int NOT NULL COMMENT '报警类型',
  `AlertLevel` int NULL COMMENT '报警级别',
  `Threshold` decimal(10,2) NULL COMMENT '阈值',
  `CurrentQuantity` decimal(10,2) NULL COMMENT '当前数量',
  `MinQuantity` decimal(10,2) NULL COMMENT '最小数量',
  `MaxQuantity` decimal(10,2) NULL COMMENT '最大数量',
  `IsActive` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否活跃',
  `IsProcessed` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否已处理',
  `AlertData` text NULL COMMENT '报警数据（JSON格式）',
  `ProcessingNotes` varchar(500) NULL COMMENT '处理备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_alert_warehouse` (`WarehouseId`),
  KEY `IX_alert_product` (`ProductCode`),
  KEY `IX_alert_type` (`AlertType`),
  KEY `IX_alert_active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================
-- 产品追溯表
-- ========================================

-- 产品追溯记录表
CREATE TABLE IF NOT EXISTS `product_traceability` (
  `Id` bigint NOT NULL,
  `ProductItemId` bigint NOT NULL COMMENT '产品项ID',
  `SerialNumber` varchar(100) NOT NULL COMMENT '产品序列号',
  `BatchNumber` varchar(50) NOT NULL COMMENT '批次号',
  `ProcessStep` varchar(100) NOT NULL COMMENT '工序步骤',
  `OperationType` varchar(100) NOT NULL COMMENT '操作类型',
  `OperationResult` varchar(100) NULL COMMENT '操作结果',
  `Operator` varchar(100) NOT NULL COMMENT '操作员',
  `WorkstationCode` varchar(50) NULL COMMENT '工作站代码',
  `OperationTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '操作时间',
  `TestParameters` text NULL COMMENT '测试参数（JSON格式）',
  `QualityParameters` varchar(500) NULL COMMENT '质量参数',
  `EnvironmentalData` text NULL COMMENT '环境数据（JSON格式）',
  `IsQualityCheckpoint` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否质量检查点',
  `EquipmentCode` varchar(50) NULL COMMENT '设备编号',
  `Remarks` varchar(1000) NULL COMMENT '备注',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_traceability_product` (`ProductItemId`),
  KEY `IX_traceability_serial` (`SerialNumber`),
  KEY `IX_traceability_batch` (`BatchNumber`),
  KEY `IX_traceability_time` (`OperationTime`),
  KEY `IX_traceability_quality` (`IsQualityCheckpoint`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================
-- 本地化资源相关表
-- ========================================

-- 本地化资源表
CREATE TABLE IF NOT EXISTS `localization_resources` (
  `Id` bigint NOT NULL,
  `Key` varchar(500) NOT NULL COMMENT '资源键',
  `LanguageCode` varchar(10) NOT NULL COMMENT '语言代码',
  `Value` text NOT NULL COMMENT '资源值',
  `Category` varchar(100) NOT NULL DEFAULT 'Common' COMMENT '资源分类',
  `Description` varchar(1000) NULL COMMENT '描述/备注',
  `IsApproved` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否已审核',
  `ApprovedBy` bigint NULL COMMENT '审核人ID',
  `ApprovedAt` datetime NULL COMMENT '审核时间',
  `TranslatedBy` bigint NULL COMMENT '翻译人ID',
  `TranslatedAt` datetime NULL COMMENT '翻译时间',
  `IsAutoTranslated` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否是自动翻译',
  `QualityScore` int NOT NULL DEFAULT 0 COMMENT '翻译质量评分 (1-5)',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_localization_key_lang` (`Key`, `LanguageCode`),
  KEY `IX_localization_category` (`Category`),
  KEY `IX_localization_language` (`LanguageCode`),
  KEY `FK_localization_approver` (`ApprovedBy`),
  KEY `FK_localization_translator` (`TranslatedBy`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 支持的语言配置表
CREATE TABLE IF NOT EXISTS `supported_language_configs` (
  `Id` bigint NOT NULL,
  `LanguageCode` varchar(10) NOT NULL COMMENT '语言代码',
  `Name` varchar(100) NOT NULL COMMENT '语言名称（英文）',
  `NativeName` varchar(100) NOT NULL COMMENT '语言名称（本地语言）',
  `Flag` varchar(50) NOT NULL DEFAULT '' COMMENT '国旗图标',
  `IsRightToLeft` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否从右到左书写',
  `IsDefault` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否默认语言',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `DisplayOrder` int NOT NULL DEFAULT 0 COMMENT '排序顺序',
  `CompletionPercentage` decimal(5,2) NOT NULL DEFAULT 0 COMMENT '翻译完成度百分比',
  `LastTranslationUpdate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '最后更新时间',
  `CurrencyCode` varchar(3) NOT NULL DEFAULT 'USD' COMMENT '货币代码',
  `DateFormat` varchar(50) NOT NULL DEFAULT 'yyyy-MM-dd' COMMENT '日期格式',
  `TimeFormat` varchar(50) NOT NULL DEFAULT 'HH:mm:ss' COMMENT '时间格式',
  `NumberFormat` varchar(50) NOT NULL DEFAULT '#,##0.##' COMMENT '数字格式',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_language_code` (`LanguageCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================
-- 数据验证规则相关表
-- ========================================

-- 数据验证规则表
CREATE TABLE IF NOT EXISTS `data_validation_rules` (
  `Id` bigint NOT NULL,
  `RuleName` varchar(100) NOT NULL COMMENT '规则名称',
  `EntityType` varchar(100) NOT NULL COMMENT '实体类型',
  `FieldName` varchar(100) NOT NULL COMMENT '字段名称',
  `ValidationType` varchar(50) NOT NULL COMMENT '验证类型',
  `ValidationExpression` varchar(1000) NOT NULL COMMENT '验证表达式',
  `ErrorMessage` varchar(500) NOT NULL COMMENT '错误消息',
  `Severity` varchar(20) NOT NULL DEFAULT 'Error' COMMENT '严重程度',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `Description` varchar(1000) NULL COMMENT '规则描述',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_validation_entity_field` (`EntityType`, `FieldName`),
  KEY `IX_validation_type` (`ValidationType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 数据验证结果表
CREATE TABLE IF NOT EXISTS `data_validation_results` (
  `Id` bigint NOT NULL,
  `RuleId` bigint NOT NULL COMMENT '验证规则ID',
  `EntityId` bigint NOT NULL COMMENT '实体ID',
  `EntityType` varchar(100) NOT NULL COMMENT '实体类型',
  `Status` varchar(20) NOT NULL COMMENT '验证状态',
  `Message` varchar(1000) NULL COMMENT '验证消息',
  `ValidatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '验证时间',
  `ValidatedValue` varchar(500) NULL COMMENT '验证值',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_validation_result_rule` (`RuleId`),
  KEY `IX_validation_result_entity` (`EntityType`, `EntityId`),
  KEY `IX_validation_result_status` (`Status`),
  KEY `IX_validation_result_time` (`ValidatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================
-- 生产参数相关表
-- ========================================

-- 生产参数记录表
CREATE TABLE IF NOT EXISTS `production_parameter` (
  `Id` bigint NOT NULL,
  `BatchNumber` varchar(50) NOT NULL COMMENT '批次号',
  `ProcessId` bigint NOT NULL COMMENT '工序ID',
  `ParameterName` varchar(100) NOT NULL COMMENT '参数名称',
  `ParameterValue` varchar(200) NOT NULL COMMENT '参数值',
  `Unit` varchar(20) NULL COMMENT '参数单位',
  `UpperLimit` decimal(10,2) NULL COMMENT '上限值',
  `LowerLimit` decimal(10,2) NULL COMMENT '下限值',
  `IsQualified` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否合格',
  `CollectTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '采集时间',
  `EquipmentCode` varchar(50) NULL COMMENT '设备编号',
  `Operator` varchar(50) NULL COMMENT '操作员',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_parameter_process` (`ProcessId`),
  KEY `IX_parameter_batch` (`BatchNumber`),
  KEY `IX_parameter_collect_time` (`CollectTime`),
  KEY `IX_parameter_equipment` (`EquipmentCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 高频参数数据表（按月分表的基础表）
CREATE TABLE IF NOT EXISTS `high_frequency_parameter` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BatchNumber` varchar(50) NOT NULL COMMENT '批次号',
  `ProcessId` bigint NOT NULL COMMENT '工序ID',
  `ParameterName` varchar(100) NOT NULL COMMENT '参数名称',
  `ParameterValue` varchar(200) NOT NULL COMMENT '参数值',
  `CollectTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '采集时间',
  `EquipmentCode` varchar(50) NULL COMMENT '设备编号',
  PRIMARY KEY (`Id`),
  KEY `IX_hf_parameter_batch` (`BatchNumber`),
  KEY `IX_hf_parameter_time` (`CollectTime`),
  KEY `IX_hf_parameter_equipment` (`EquipmentCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================
-- 工艺路线相关表
-- ========================================

-- 工艺路线表
CREATE TABLE IF NOT EXISTS `process_route` (
  `Id` bigint NOT NULL,
  `RouteCode` varchar(50) NOT NULL COMMENT '路线编码',
  `RouteName` varchar(100) NOT NULL COMMENT '路线名称',
  `ProductType` varchar(50) NOT NULL COMMENT '产品类型',
  `Description` varchar(500) NULL COMMENT '路线描述',
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
  `VersionNumber` varchar(20) NOT NULL DEFAULT '1.0' COMMENT '版本号',
  `RouteConfig` text NULL COMMENT '路线配置（JSON格式）',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_route_code_version` (`RouteCode`, `VersionNumber`),
  KEY `IX_route_product_type` (`ProductType`),
  KEY `IX_route_enabled` (`IsEnabled`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 工艺路线步骤表
CREATE TABLE IF NOT EXISTS `process_route_step` (
  `Id` bigint NOT NULL,
  `ProcessRouteId` bigint NOT NULL COMMENT '工艺路线ID',
  `ProcessId` bigint NOT NULL COMMENT '工序ID',
  `StepOrder` int NOT NULL COMMENT '步骤序号',
  `IsRequired` tinyint(1) NOT NULL DEFAULT 1 COMMENT '是否必需',
  `StepConfig` text NULL COMMENT '步骤参数配置（JSON格式）',
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreatedBy` bigint NULL,
  `UpdatedTime` datetime NULL,
  `UpdatedBy` bigint NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `FK_route_step_route` (`ProcessRouteId`),
  KEY `FK_route_step_process` (`ProcessId`),
  KEY `IX_route_step_order` (`ProcessRouteId`, `StepOrder`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================
-- 创建索引和约束
-- ========================================

-- 外键约束（如果需要）
-- ALTER TABLE `role_permissions` ADD CONSTRAINT `FK_role_permissions_role` 
--   FOREIGN KEY (`RoleId`) REFERENCES `roles` (`Id`) ON DELETE CASCADE;
-- ALTER TABLE `role_permissions` ADD CONSTRAINT `FK_role_permissions_permission` 
--   FOREIGN KEY (`PermissionId`) REFERENCES `permissions` (`Id`) ON DELETE CASCADE;
-- ALTER TABLE `user_roles` ADD CONSTRAINT `FK_user_roles_user` 
--   FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE;
-- ALTER TABLE `user_roles` ADD CONSTRAINT `FK_user_roles_role` 
--   FOREIGN KEY (`RoleId`) REFERENCES `roles` (`Id`) ON DELETE CASCADE;

-- ========================================
-- 初始化种子数据
-- ========================================

-- 插入系统权限
INSERT IGNORE INTO `permissions` (`Id`, `Name`, `Code`, `Description`, `Module`, `IsSystemPermission`, `CreatedTime`) VALUES
(1, '用户查看', 'users.view', '查看用户信息', '用户管理', 1, NOW()),
(2, '用户管理', 'users.manage', '创建、编辑、删除用户', '用户管理', 1, NOW()),
(3, '工序查看', 'processes.view', '查看工序信息', '工序管理', 1, NOW()),
(4, '工序管理', 'processes.manage', '创建、编辑、删除工序', '工序管理', 1, NOW()),
(5, '生产查看', 'production.view', '查看生产批次', '生产管理', 1, NOW()),
(6, '生产管理', 'production.manage', '管理生产批次', '生产管理', 1, NOW()),
(7, '编码管理', 'codes.manage', '管理批次号和序列号', '编码管理', 1, NOW()),
(8, '产品追溯', 'traceability.view', '查看产品追溯信息', '质量管理', 1, NOW()),
(9, '报表查看', 'reports.view', '查看各类报表', '报表管理', 1, NOW()),
(10, '系统管理', 'system.manage', '系统配置和管理', '系统管理', 1, NOW()),
(11, '仓库查看', 'warehouses.view', '查看仓库信息', '仓库管理', 1, NOW()),
(12, '仓库管理', 'warehouses.manage', '创建、编辑、删除仓库', '仓库管理', 1, NOW()),
(13, '库存管理', 'inventory.manage', '管理库存信息', '仓库管理', 1, NOW()),
(14, '工艺路线查看', 'routes.view', '查看工艺路线', '工艺管理', 1, NOW()),
(15, '工艺路线管理', 'routes.manage', '管理工艺路线', '工艺管理', 1, NOW()),
(16, '本地化管理', 'localization.manage', '管理多语言资源', '系统管理', 1, NOW()),
(17, '数据验证管理', 'validation.manage', '管理数据验证规则', '系统管理', 1, NOW()),
(18, '生产参数查看', 'parameters.view', '查看生产参数', '生产管理', 1, NOW()),
(19, '生产参数管理', 'parameters.manage', '管理生产参数', '生产管理', 1, NOW()),
(20, '设备查看', 'equipments.view', '查看设备信息', '设备管理', 1, NOW()),
(21, '设备管理', 'equipments.manage', '管理设备信息', '设备管理', 1, NOW());

-- 插入系统角色
INSERT IGNORE INTO `roles` (`Id`, `Name`, `Description`, `IsSystemRole`, `CreatedTime`) VALUES
(1, '系统管理员', '拥有系统所有权限', 1, NOW()),
(2, '操作员', '生产操作权限', 1, NOW()),
(3, '质量员', '质量检测和追溯权限', 1, NOW()),
(4, '查看者', '只读权限', 1, NOW());

-- 管理员角色权限分配（拥有所有权限）
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(1, 1, 1, NOW()), (2, 1, 2, NOW()), (3, 1, 3, NOW()), (4, 1, 4, NOW()), (5, 1, 5, NOW()),
(6, 1, 6, NOW()), (7, 1, 7, NOW()), (8, 1, 8, NOW()), (9, 1, 9, NOW()), (10, 1, 10, NOW()),
(11, 1, 11, NOW()), (12, 1, 12, NOW()), (13, 1, 13, NOW()), (14, 1, 14, NOW()), (15, 1, 15, NOW()),
(16, 1, 16, NOW()), (17, 1, 17, NOW()), (18, 1, 18, NOW()), (19, 1, 19, NOW()), (20, 1, 20, NOW()),
(21, 1, 21, NOW());

-- 操作员角色权限分配
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(22, 2, 3, NOW()), (23, 2, 5, NOW()), (24, 2, 6, NOW()), (25, 2, 7, NOW()),
(35, 2, 11, NOW()), (36, 2, 13, NOW()), (37, 2, 14, NOW()), (38, 2, 18, NOW()), (39, 2, 19, NOW()),
(40, 2, 20, NOW());

-- 质量员角色权限分配
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(26, 3, 3, NOW()), (27, 3, 5, NOW()), (28, 3, 8, NOW()), (29, 3, 9, NOW()),
(41, 3, 11, NOW()), (42, 3, 14, NOW()), (43, 3, 18, NOW()), (44, 3, 20, NOW());

-- 查看者角色权限分配
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(30, 4, 1, NOW()), (31, 4, 3, NOW()), (32, 4, 5, NOW()), (33, 4, 8, NOW()), (34, 4, 9, NOW()),
(45, 4, 11, NOW()), (46, 4, 14, NOW()), (47, 4, 18, NOW()), (48, 4, 20, NOW());

-- 创建默认管理员用户 (用户名: admin, 密码: Admin@123)
-- 注意：这是开发环境的默认密码，生产环境请修改
INSERT IGNORE INTO `users` (`Id`, `Username`, `PasswordHash`, `Salt`, `Email`, `RealName`, `IsEnabled`, `CreatedTime`) VALUES
(1, 'admin', 'A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3', 'admin_salt_2024', 'admin@batterypack.com', '系统管理员', 1, NOW());

-- 分配管理员角色给默认用户
INSERT IGNORE INTO `user_roles` (`Id`, `UserId`, `RoleId`, `CreatedTime`) VALUES
(1, 1, 1, NOW());

-- 插入默认工序
INSERT IGNORE INTO `process` (`Id`, `ProcessCode`, `ProcessName`, `ProcessType`, `Description`, `StandardTime`, `IsEnabled`, `SortOrder`, `CreatedTime`) VALUES
(1, 'P001', '电芯包装', 0, '电芯包装工序', 30, 1, 1, NOW()),
(2, 'P002', '模组包装', 1, '模组包装工序', 60, 1, 2, NOW()),
(3, 'P003', 'Pack包装', 2, 'Pack包装工序', 90, 1, 3, NOW()),
(4, 'P004', '栈板包装', 3, '栈板包装工序', 45, 1, 4, NOW());

-- 插入编码生成规则
INSERT IGNORE INTO `code_generation_rules` (`Id`, `RuleName`, `CodeType`, `ProductType`, `Prefix`, `DateFormat`, `SequenceLength`, `StartNumber`, `ResetDaily`, `Template`, `ValidationPattern`, `Description`, `CreatedTime`) VALUES
(1, '电芯批次号规则', 'BatchCode', 'Cell', 'CELL', 'yyyyMMdd', 3, 1, 1, '{Prefix}{Date}{Sequence}', '^CELL\\d{8}\\d{3}$', '电芯产品批次号自动生成规则', NOW()),
(2, '模组批次号规则', 'BatchCode', 'Module', 'MOD', 'yyyyMMdd', 3, 1, 1, '{Prefix}{Date}{Sequence}', '^MOD\\d{8}\\d{3}$', '模组产品批次号自动生成规则', NOW()),
(3, 'Pack批次号规则', 'BatchCode', 'Pack', 'PACK', 'yyyyMMdd', 3, 1, 1, '{Prefix}{Date}{Sequence}', '^PACK\\d{8}\\d{3}$', 'Pack产品批次号自动生成规则', NOW());

-- 插入默认仓库
INSERT IGNORE INTO `warehouses` (`Id`, `WarehouseCode`, `WarehouseName`, `WarehouseType`, `Address`, `ManagerId`, `Phone`, `Area`, `IsEnabled`, `Remarks`, `CreatedTime`) VALUES
(1, 'WH001', '原材料仓库', 0, '生产基地A区1号仓库', 1, '400-001-0001', 1000.00, 1, '存储电芯等原材料', NOW()),
(2, 'WH002', '半成品仓库', 1, '生产基地A区2号仓库', 1, '400-001-0002', 800.00, 1, '存储模组等半成品', NOW()),
(3, 'WH003', '成品仓库', 2, '生产基地A区3号仓库', 1, '400-001-0003', 1200.00, 1, '存储Pack等成品', NOW()),
(4, 'WH004', '不良品仓库', 3, '生产基地B区1号仓库', 1, '400-001-0004', 300.00, 1, '存储不良品和返工品', NOW());

-- 插入默认库位
INSERT IGNORE INTO `warehouse_locations` (`Id`, `WarehouseId`, `LocationCode`, `LocationName`, `LocationType`, `ZoneCode`, `AisleCode`, `RackCode`, `Level`, `Position`, `MaxCapacity`, `UsedCapacity`, `CapacityUnit`, `LocationStatus`, `IsEnabled`, `CreatedTime`) VALUES
(1, 1, 'WH001-A01-R01-L01-P01', 'A区1巷1架1层1位', 0, 'A01', 'A01', 'R01', 1, 1, 100.00, 0.00, '个', 0, 1, NOW()),
(2, 1, 'WH001-A01-R01-L01-P02', 'A区1巷1架1层2位', 0, 'A01', 'A01', 'R01', 1, 2, 100.00, 0.00, '个', 0, 1, NOW()),
(3, 2, 'WH002-B01-R01-L01-P01', 'B区1巷1架1层1位', 0, 'B01', 'B01', 'R01', 1, 1, 50.00, 0.00, '个', 0, 1, NOW()),
(4, 3, 'WH003-C01-R01-L01-P01', 'C区1巷1架1层1位', 0, 'C01', 'C01', 'R01', 1, 1, 30.00, 0.00, '个', 0, 1, NOW());

-- 插入支持的语言配置
INSERT IGNORE INTO `supported_language_configs` (`Id`, `LanguageCode`, `Name`, `NativeName`, `Flag`, `IsRightToLeft`, `IsDefault`, `IsEnabled`, `DisplayOrder`, `CompletionPercentage`, `LastTranslationUpdate`, `CurrencyCode`, `DateFormat`, `TimeFormat`, `NumberFormat`, `CreatedTime`) VALUES
(1, 'zh-CN', 'Chinese (Simplified)', '简体中文', '🇨🇳', 0, 1, 1, 1, 100.00, NOW(), 'CNY', 'yyyy-MM-dd', 'HH:mm:ss', '#,##0.##', NOW()),
(2, 'en-US', 'English (United States)', 'English', '🇺🇸', 0, 0, 1, 2, 95.00, NOW(), 'USD', 'MM/dd/yyyy', 'hh:mm:ss tt', '#,##0.##', NOW()),
(3, 'ja-JP', 'Japanese', '日本語', '🇯🇵', 0, 0, 1, 3, 85.00, NOW(), 'JPY', 'yyyy/MM/dd', 'HH:mm:ss', '#,##0', NOW()),
(4, 'ko-KR', 'Korean', '한국어', '🇰🇷', 0, 0, 1, 4, 80.00, NOW(), 'KRW', 'yyyy-MM-dd', 'HH:mm:ss', '#,##0', NOW());

-- 插入基础本地化资源（中文）
INSERT IGNORE INTO `localization_resources` (`Id`, `Key`, `LanguageCode`, `Value`, `Category`, `Description`, `IsApproved`, `CreatedTime`) VALUES
(1, 'Common.Save', 'zh-CN', '保存', 'Common', '通用保存按钮', 1, NOW()),
(2, 'Common.Cancel', 'zh-CN', '取消', 'Common', '通用取消按钮', 1, NOW()),
(3, 'Common.Delete', 'zh-CN', '删除', 'Common', '通用删除按钮', 1, NOW()),
(4, 'Common.Edit', 'zh-CN', '编辑', 'Common', '通用编辑按钮', 1, NOW()),
(5, 'Common.Search', 'zh-CN', '搜索', 'Common', '通用搜索按钮', 1, NOW()),
(6, 'Common.Add', 'zh-CN', '新增', 'Common', '通用新增按钮', 1, NOW()),
(7, 'Common.Confirm', 'zh-CN', '确认', 'Common', '通用确认按钮', 1, NOW()),
(8, 'Common.Close', 'zh-CN', '关闭', 'Common', '通用关闭按钮', 1, NOW());

-- 插入基础本地化资源（英文）
INSERT IGNORE INTO `localization_resources` (`Id`, `Key`, `LanguageCode`, `Value`, `Category`, `Description`, `IsApproved`, `CreatedTime`) VALUES
(9, 'Common.Save', 'en-US', 'Save', 'Common', 'Common save button', 1, NOW()),
(10, 'Common.Cancel', 'en-US', 'Cancel', 'Common', 'Common cancel button', 1, NOW()),
(11, 'Common.Delete', 'en-US', 'Delete', 'Common', 'Common delete button', 1, NOW()),
(12, 'Common.Edit', 'en-US', 'Edit', 'Common', 'Common edit button', 1, NOW()),
(13, 'Common.Search', 'en-US', 'Search', 'Common', 'Common search button', 1, NOW()),
(14, 'Common.Add', 'en-US', 'Add', 'Common', 'Common add button', 1, NOW()),
(15, 'Common.Confirm', 'en-US', 'Confirm', 'Common', 'Common confirm button', 1, NOW()),
(16, 'Common.Close', 'en-US', 'Close', 'Common', 'Common close button', 1, NOW());

-- 插入默认工艺路线
INSERT IGNORE INTO `process_route` (`Id`, `RouteCode`, `RouteName`, `ProductType`, `Description`, `IsEnabled`, `VersionNumber`, `RouteConfig`, `CreatedTime`) VALUES
(1, 'RT-CELL-001', '电芯标准工艺路线', 'Cell', '电芯产品的标准包装工艺路线', 1, '1.0', '{"nodes":[{"id":"P001","name":"电芯包装","type":"process"}],"edges":[]}', NOW()),
(2, 'RT-MOD-001', '模组标准工艺路线', 'Module', '模组产品的标准包装工艺路线', 1, '1.0', '{"nodes":[{"id":"P001","name":"电芯包装","type":"process"},{"id":"P002","name":"模组包装","type":"process"}],"edges":[{"source":"P001","target":"P002"}]}', NOW()),
(3, 'RT-PACK-001', 'Pack标准工艺路线', 'Pack', 'Pack产品的标准包装工艺路线', 1, '1.0', '{"nodes":[{"id":"P001","name":"电芯包装","type":"process"},{"id":"P002","name":"模组包装","type":"process"},{"id":"P003","name":"Pack包装","type":"process"}],"edges":[{"source":"P001","target":"P002"},{"source":"P002","target":"P003"}]}', NOW());

-- 插入工艺路线步骤
INSERT IGNORE INTO `process_route_step` (`Id`, `ProcessRouteId`, `ProcessId`, `StepOrder`, `IsRequired`, `StepConfig`, `CreatedTime`) VALUES
(1, 1, 1, 1, 1, '{"parameters":{"temperature":"25±2℃","humidity":"45-75%RH"}}', NOW()),
(2, 2, 1, 1, 1, '{"parameters":{"temperature":"25±2℃","humidity":"45-75%RH"}}', NOW()),
(3, 2, 2, 2, 1, '{"parameters":{"temperature":"25±2℃","humidity":"45-75%RH"}}', NOW()),
(4, 3, 1, 1, 1, '{"parameters":{"temperature":"25±2℃","humidity":"45-75%RH"}}', NOW()),
(5, 3, 2, 2, 1, '{"parameters":{"temperature":"25±2℃","humidity":"45-75%RH"}}', NOW()),
(6, 3, 3, 3, 1, '{"parameters":{"temperature":"25±2℃","humidity":"45-75%RH"}}', NOW());

-- 插入默认数据验证规则
INSERT IGNORE INTO `data_validation_rules` (`Id`, `RuleName`, `EntityType`, `FieldName`, `ValidationType`, `ValidationExpression`, `ErrorMessage`, `Severity`, `IsEnabled`, `Description`, `CreatedTime`) VALUES
(1, '批次号格式验证', 'ProductionBatch', 'BatchNumber', 'Regex', '^(CELL|MOD|PACK)\\d{8}\\d{3}$', '批次号格式不正确，应为：前缀+日期+序号', 'Error', 1, '验证批次号是否符合规定格式', NOW()),
(2, '序列号唯一性验证', 'ProductItem', 'SerialNumber', 'Unique', 'SerialNumber', '序列号已存在，请使用唯一的序列号', 'Error', 1, '确保产品序列号的唯一性', NOW()),
(3, '用户名长度验证', 'User', 'Username', 'Length', 'min:3,max:50', '用户名长度必须在3-50个字符之间', 'Error', 1, '验证用户名长度是否合规', NOW()),
(4, '邮箱格式验证', 'User', 'Email', 'Email', '', '邮箱格式不正确', 'Error', 1, '验证邮箱地址格式', NOW());

-- ========================================
-- 完成初始化
-- ========================================

-- 显示初始化完成信息
SELECT '数据库初始化完成!' as Status, 
       DATABASE() as CurrentDatabase,
       NOW() as InitTime;

-- 显示创建的表
SHOW TABLES;

-- 显示默认用户信息
SELECT 'admin' as Username, 'Admin@123' as DefaultPassword, '请在生产环境中修改默认密码' as Notice; 