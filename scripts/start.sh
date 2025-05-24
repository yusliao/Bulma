#!/bin/bash

# 锂电池包装工序MES系统启动脚本

echo "=== 锂电池包装工序MES系统启动 ==="

# 检查Docker是否安装
if ! command -v docker &> /dev/null; then
    echo "错误：Docker未安装，请先安装Docker"
    exit 1
fi

# 检查Docker Compose是否安装
if ! command -v docker-compose &> /dev/null; then
    echo "错误：Docker Compose未安装，请先安装Docker Compose"
    exit 1
fi

# 创建日志目录
mkdir -p logs

echo "正在启动服务..."

# 启动Docker Compose服务
docker-compose up -d

echo "等待服务启动完成..."
sleep 30

# 检查服务状态
echo "=== 服务状态检查 ==="
docker-compose ps

echo ""
echo "=== 服务访问地址 ==="
echo "🚀 API文档: http://localhost:5000/swagger"
echo "📊 Grafana监控: http://localhost:3000 (admin/admin123)"
echo "💾 MySQL数据库: localhost:3306 (root/123456)"
echo "🔥 Redis缓存: localhost:6379"
echo "📈 InfluxDB: http://localhost:8086 (admin/password123)"

echo ""
echo "✅ 锂电池包装工序MES系统启动完成！"
echo "📖 详细文档请查看README.md" 