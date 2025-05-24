-- 锂电池包装工序MES系统数据库初始化脚本

-- 设置字符集
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 创建数据库（如果不存在）
CREATE DATABASE IF NOT EXISTS `BatteryPackingMES` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `BatteryPackingMES`;

-- 创建用户（用于主从复制）
CREATE USER IF NOT EXISTS 'replication'@'%' IDENTIFIED BY 'replication_password';
GRANT REPLICATION SLAVE ON *.* TO 'replication'@'%';

-- 创建应用用户
CREATE USER IF NOT EXISTS 'battery_mes'@'%' IDENTIFIED BY 'battery_mes_password';
GRANT ALL PRIVILEGES ON `BatteryPackingMES`.* TO 'battery_mes'@'%';

-- 刷新权限
FLUSH PRIVILEGES;

-- 启用二进制日志（用于主从复制）
-- 注意：这需要在my.cnf中配置，这里只是注释说明

-- 设置时区
SET time_zone = '+08:00';

-- 创建索引优化配置
SET GLOBAL innodb_buffer_pool_size = 268435456; -- 256MB
SET GLOBAL query_cache_size = 67108864; -- 64MB
SET GLOBAL query_cache_type = 1;

-- 输出初始化完成信息
SELECT 'MySQL数据库初始化完成' AS message; 