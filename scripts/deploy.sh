#!/bin/bash

# 锂电池包装工序MES系统多环境部署脚本

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 显示使用说明
show_usage() {
    echo -e "${BLUE}锂电池包装工序MES系统部署脚本${NC}"
    echo ""
    echo "用法: $0 [环境] [操作]"
    echo ""
    echo "环境选项:"
    echo "  dev, development    开发环境"
    echo "  staging            预发布环境"
    echo "  prod, production   生产环境"
    echo ""
    echo "操作选项:"
    echo "  start              启动服务"
    echo "  stop               停止服务"
    echo "  restart            重启服务"
    echo "  logs               查看日志"
    echo "  status             查看状态"
    echo "  clean              清理环境"
    echo ""
    echo "示例:"
    echo "  $0 dev start       启动开发环境"
    echo "  $0 staging stop    停止预发布环境"
    echo "  $0 prod logs       查看生产环境日志"
}

# 检查参数
if [ $# -lt 2 ]; then
    show_usage
    exit 1
fi

ENVIRONMENT=$1
OPERATION=$2

# 标准化环境名称
case $ENVIRONMENT in
    dev|development)
        ENVIRONMENT="development"
        ENV_NAME="开发环境"
        COMPOSE_FILE="docker-compose.yml"
        ;;
    staging)
        ENVIRONMENT="staging"
        ENV_NAME="预发布环境"
        COMPOSE_FILE="docker-compose.staging.yml"
        ;;
    prod|production)
        ENVIRONMENT="production"
        ENV_NAME="生产环境"
        COMPOSE_FILE="docker-compose.production.yml"
        ;;
    *)
        echo -e "${RED}错误: 不支持的环境 '$ENVIRONMENT'${NC}"
        show_usage
        exit 1
        ;;
esac

# 检查Docker和Docker Compose
check_dependencies() {
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}错误: Docker未安装${NC}"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        echo -e "${RED}错误: Docker Compose未安装${NC}"
        exit 1
    fi
}

# 加载环境变量
load_environment() {
    local env_file=".env.${ENVIRONMENT}"
    
    if [ -f "$env_file" ]; then
        echo -e "${BLUE}加载环境变量: $env_file${NC}"
        export $(cat $env_file | grep -v '^#' | xargs)
    else
        echo -e "${YELLOW}警告: 环境变量文件 $env_file 不存在${NC}"
        if [ "$ENVIRONMENT" != "development" ]; then
            echo -e "${RED}错误: $ENV_NAME 需要环境变量文件${NC}"
            exit 1
        fi
    fi
    
    # 设置ASPNETCORE_ENVIRONMENT
    case $ENVIRONMENT in
        development)
            export ASPNETCORE_ENVIRONMENT=Development
            ;;
        staging)
            export ASPNETCORE_ENVIRONMENT=Staging
            ;;
        production)
            export ASPNETCORE_ENVIRONMENT=Production
            ;;
    esac
}

# 验证环境变量
validate_environment() {
    if [ "$ENVIRONMENT" = "production" ] || [ "$ENVIRONMENT" = "staging" ]; then
        local required_vars=("MYSQL_PASSWORD" "REDIS_PASSWORD" "JWT_SECRET_KEY")
        
        for var in "${required_vars[@]}"; do
            if [ -z "${!var}" ]; then
                echo -e "${RED}错误: 缺少必需的环境变量 $var${NC}"
                exit 1
            fi
        done
        
        # 检查JWT密钥长度
        if [ ${#JWT_SECRET_KEY} -lt 32 ]; then
            echo -e "${RED}错误: JWT_SECRET_KEY 长度至少需要32个字符${NC}"
            exit 1
        fi
    fi
}

# 启动服务
start_services() {
    echo -e "${GREEN}启动 $ENV_NAME...${NC}"
    
    # 创建必要的目录
    mkdir -p logs
    mkdir -p ssl
    
    # 构建并启动服务
    docker-compose -f $COMPOSE_FILE up -d --build
    
    echo -e "${GREEN}等待服务启动...${NC}"
    sleep 30
    
    # 检查服务状态
    docker-compose -f $COMPOSE_FILE ps
    
    echo ""
    echo -e "${GREEN}✅ $ENV_NAME 启动完成！${NC}"
    
    if [ "$ENVIRONMENT" = "development" ]; then
        echo -e "${BLUE}🚀 API文档: http://localhost:5000/swagger${NC}"
        echo -e "${BLUE}📊 Grafana: http://localhost:3000 (admin/admin123)${NC}"
    fi
}

# 停止服务
stop_services() {
    echo -e "${YELLOW}停止 $ENV_NAME...${NC}"
    docker-compose -f $COMPOSE_FILE down
    echo -e "${GREEN}✅ $ENV_NAME 已停止${NC}"
}

# 重启服务
restart_services() {
    echo -e "${YELLOW}重启 $ENV_NAME...${NC}"
    stop_services
    sleep 5
    start_services
}

# 查看日志
show_logs() {
    local service=${3:-"battery-mes-api"}
    echo -e "${BLUE}查看 $ENV_NAME 日志 (服务: $service)...${NC}"
    docker-compose -f $COMPOSE_FILE logs -f $service
}

# 查看状态
show_status() {
    echo -e "${BLUE}$ENV_NAME 服务状态:${NC}"
    docker-compose -f $COMPOSE_FILE ps
    
    echo ""
    echo -e "${BLUE}系统资源使用:${NC}"
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
}

# 清理环境
clean_environment() {
    echo -e "${RED}警告: 这将删除 $ENV_NAME 的所有数据！${NC}"
    read -p "确认继续? (y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}清理 $ENV_NAME...${NC}"
        docker-compose -f $COMPOSE_FILE down -v --remove-orphans
        docker system prune -f
        echo -e "${GREEN}✅ $ENV_NAME 清理完成${NC}"
    else
        echo -e "${BLUE}操作已取消${NC}"
    fi
}

# 主逻辑
echo -e "${BLUE}=== 锂电池包装工序MES系统部署脚本 ===${NC}"
echo -e "${BLUE}环境: $ENV_NAME${NC}"
echo -e "${BLUE}操作: $OPERATION${NC}"
echo ""

check_dependencies
load_environment
validate_environment

case $OPERATION in
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        restart_services
        ;;
    logs)
        show_logs $@
        ;;
    status)
        show_status
        ;;
    clean)
        clean_environment
        ;;
    *)
        echo -e "${RED}错误: 不支持的操作 '$OPERATION'${NC}"
        show_usage
        exit 1
        ;;
esac 