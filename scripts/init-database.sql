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
(10, '系统管理', 'system.manage', '系统配置和管理', '系统管理', 1, NOW());

-- 插入系统角色
INSERT IGNORE INTO `roles` (`Id`, `Name`, `Description`, `IsSystemRole`, `CreatedTime`) VALUES
(1, '系统管理员', '拥有系统所有权限', 1, NOW()),
(2, '操作员', '生产操作权限', 1, NOW()),
(3, '质量员', '质量检测和追溯权限', 1, NOW()),
(4, '查看者', '只读权限', 1, NOW());

-- 管理员角色权限分配（拥有所有权限）
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(1, 1, 1, NOW()), (2, 1, 2, NOW()), (3, 1, 3, NOW()), (4, 1, 4, NOW()), (5, 1, 5, NOW()),
(6, 1, 6, NOW()), (7, 1, 7, NOW()), (8, 1, 8, NOW()), (9, 1, 9, NOW()), (10, 1, 10, NOW());

-- 操作员角色权限分配
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(11, 2, 3, NOW()), (12, 2, 5, NOW()), (13, 2, 6, NOW()), (14, 2, 7, NOW());

-- 质量员角色权限分配
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(15, 3, 3, NOW()), (16, 3, 5, NOW()), (17, 3, 8, NOW()), (18, 3, 9, NOW());

-- 查看者角色权限分配
INSERT IGNORE INTO `role_permissions` (`Id`, `RoleId`, `PermissionId`, `CreatedTime`) VALUES
(19, 4, 1, NOW()), (20, 4, 3, NOW()), (21, 4, 5, NOW()), (22, 4, 8, NOW()), (23, 4, 9, NOW());

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